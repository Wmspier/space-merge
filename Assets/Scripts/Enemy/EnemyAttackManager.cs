using System;
using System.Collections.Generic;
using System.Linq;
using Hex.Extensions;
using Hex.Grid;
using Hex.Grid.Cell;
using Hex.UI;
using Unity.Mathematics;
using UnityEngine;

namespace Hex.Enemy
{
	public struct EnemyAttackInfo
	{
		public int Damage;
		public GameObject Ship;

		public EnemyAttackInfo(int damage, GameObject ship)
		{
			Damage = damage;
			Ship = ship;
		}
	}
	
	public class EnemyAttackManager : MonoBehaviour
	{
		private static readonly int[] AttackPowerList = { 3, 5, 8, 12, 17, 23, 30, 38, 47 };

		[SerializeField] private EnemyAttackUI _ui;
		[SerializeField] private HealthBar _enemyHealthBar;
		[SerializeField] private HealthBar _playerHealthBar;
		[SerializeField] private EnemyShipSpawner _shipSpawner;

		private readonly Dictionary<int3, EnemyAttackInfo> _attacksByCoord = new();

		private int _attackPhaseCount;
		private int _staggeredPhaseCount;
		private HexGrid _grid;

		public Action AttackResolved;

		public bool IsAttackPhase => _ui.TurnsBeforeAttack == 0;

		public IReadOnlyDictionary<int3, EnemyAttackInfo> AttacksByCoordByCoord => _attacksByCoord;

		public bool ElapseTurn() => _ui.ElapseTurn();
		public void ResetTurns() => _ui.ResetTurns();

		public void Initialize(HexGrid grid)
		{
			_grid = grid;
			_ui.ResolveAttackPressed = ResolveAttacks;
			_attackPhaseCount = _staggeredPhaseCount = 0;
			
			_playerHealthBar.SetHealthToMax(50);;
			_enemyHealthBar.SetHealthToMax(100);
		}

		public void Cleanup()
		{
			foreach (var (_, info) in _attacksByCoord)
			{
				Destroy(info.Ship);
			}
			_attacksByCoord.Clear();
		}
		
		public void AssignAttacksToGrid()
		{
			var isEvenAttack = _attackPhaseCount % 2 == 0;
			var numToSpawn = isEvenAttack ? 2 : 3;
			var attackList = new List<int>().FillWithDefault(numToSpawn);
			
			// Always assign at least two attacks using the current attack index
			for (var i = 0; i < 2; i++)
			{
				attackList[i] = AttackPowerList[Mathf.Min(AttackPowerList.Length - 1, Mathf.Max(0, isEvenAttack ? _staggeredPhaseCount : _staggeredPhaseCount-1))];
			}
			
			// If we're on a turn on which 3 attacks spawn, spawn one at a higher index
			if (!isEvenAttack)
			{
				attackList[2] = AttackPowerList[Mathf.Min(AttackPowerList.Length - 1, _staggeredPhaseCount)];
			}

			attackList.Shuffle();

			var cellsByXPos = _grid.GetCellsByXPosRounded()
				.Where(kvp => !kvp.Value.Any(c => c.HoldingEnemyAttack))
				.Where(kvp => kvp.Value.Any(c => c.Coordinates.x == 1))
				.ToList()
				.Shuffle();

			while (attackList.Count > 0)
			{
				HexCell randomCell;
				if (cellsByXPos.Count == 0)
				{
					randomCell = _grid.GetRandomCell();
				}
				else
				{
					// Pick the first column (list should be shuffled)
					var cellsInRow = cellsByXPos[0].Value;
					// Pick a random cell in this row
					randomCell = cellsInRow.FirstOrDefault(c => c.Coordinates.x == 1); //cellsInRow.Shuffle().First();

					if (randomCell == null)
					{
						Debug.LogWarning("Issue when assigning attack: Random Cell is null");
						return;
					}

					if (_attacksByCoord.ContainsKey(randomCell.Coordinates))
					{
						Debug.LogWarning("Issue when assigning attack: Attack is already on cell");
						return;
					}
					
					// Remove row
					cellsByXPos.RemoveAt(0);
				}
				
				// Assign attack 
				randomCell.InfoHolder.HoldEnemyAttack(attackList[0]);

				// Create and store attack info
				var newShip = _shipSpawner.SpawnSmallShip(randomCell.Coordinates);
				_attacksByCoord[randomCell.Coordinates] = new EnemyAttackInfo(attackList[0], newShip);
				
				// Remove attack
				attackList.RemoveAt(0);
			}

			_attackPhaseCount++;
			if (isEvenAttack) _staggeredPhaseCount++;
			
			UpdateDamagePreview();
		}

		private void ResolveAttacks()
		{
			var cellsByRow = _grid.GetCellsByXPosRounded();
			foreach (var row in cellsByRow)
			{
				foreach (var cell in row.Value)
				{
					var cellHoldingUint = cell.HoldingUnit;
					if (!cell.HoldingEnemyAttack) continue; // Cell does not contain enemy attack

					var powerDifference = cell.InfoHolder.ResolveAttack();
					if (powerDifference > 0)
					{
						// Player unit is stronger
						_enemyHealthBar.ModifyValue(-powerDifference);
					}
					else if (powerDifference < 0)
					{
						// Enemy Attack is stronger
						_playerHealthBar.ModifyValue(powerDifference);
					}

					if (cellHoldingUint)
					{
						cell.InfoHolder.ClearEnemyAttack();

						// Remove stored attack info
						if (_attacksByCoord.TryGetValue(cell.Coordinates, out var attackInfo))
						{
							Destroy(attackInfo.Ship);
							_attacksByCoord.Remove(cell.Coordinates);
						}
					}
				}
			}
			
			_ui.ResetTurns();
			AssignAttacksToGrid();

			AttackResolved?.Invoke();
		}

		public void UpdateDamagePreview()
		{
			var totalEnemyDamageTaken = GridUtility.GetTotalEnemyDamageTaken(_grid);
			var totalPlayerDamageTaken = GridUtility.GetTotalPlayerDamageTaken(_grid);

			// Update enemy total damage preview
			if (totalEnemyDamageTaken > 0)
			{
				_enemyHealthBar.ShowPreview(totalEnemyDamageTaken);
			}
			else
			{
				_enemyHealthBar.HidePreview();
			}
			
			// Update player total damage preview
			if (totalPlayerDamageTaken > 0)
			{
				_playerHealthBar.ShowPreview(totalPlayerDamageTaken);
			}
			else
			{
				_playerHealthBar.HidePreview();
			}
		}
	}
}