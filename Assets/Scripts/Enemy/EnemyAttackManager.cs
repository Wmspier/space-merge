using System;
using System.Collections.Generic;
using System.Linq;
using Hex.Extensions;
using Hex.Grid;
using Hex.Grid.Cell;
using Hex.UI;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.VFX;
using Task = System.Threading.Tasks.Task;

namespace Hex.Enemy
{
	public enum AttackResultType
	{
		Contested,
		PlayerHit,
		PlayerMiss,
		EnemyHit
	}

	public struct EnemyAttackInfo
	{
		public int Damage;
		public GameObject Ship;
		public HexCell TargetCell;
		public Vector3 OriginPosition;

		public EnemyAttackInfo(int damage, GameObject ship, HexCell target, Vector3 originPos)
		{
			Damage = damage;
			Ship = ship;
			TargetCell = target;
			OriginPosition = originPos;
		}
	}
	
	public class EnemyAttackManager : MonoBehaviour
	{
		private static readonly int[] AttackPowerList = { 3, 5, 8, 12, 17, 23, 30, 38, 47 };

		[SerializeField] private EnemyAttackUI _ui;
		[SerializeField] private HealthBar _enemyHealthBar;
		[SerializeField] private HealthBar _playerHealthBar;
		[SerializeField] private EnemyShipSpawner _shipSpawner;

		[Space] 
		[Header("VFX")] 
		[SerializeField] private Transform _vfxAnchor;
		[SerializeField] private VisualEffect _targetingEffect;
		[SerializeField] private VisualEffect _beamEffectContested;
		[SerializeField] private VisualEffect _beamEffectEnemyHit;
		[SerializeField] private VisualEffect _beamEffectPlayerHit;
		[SerializeField] private VisualEffect _beamEffectPlayerMiss;
		[SerializeField] private float _attackTextDisplayDelaySeconds = 2f;

		[Space] 
		[Header("Debug")] 
		[SerializeField]
		private List<int3> _debugAttacks;
		
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

		public void Dispose()
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

			if (_debugAttacks.Count > 0)
			{
				foreach (var attack in _debugAttacks)
				{
					var cell = _grid.Registry[attack];
					
					// Assign attack 
					cell.InfoHolder.HoldEnemyAttack(999, false);

					// Create and store attack info
					var newShip = _shipSpawner.SpawnSmallShip(cell.Coordinates, out var originPosition);
					var attackInfo = new EnemyAttackInfo(999, newShip, cell, originPosition);
					_attacksByCoord[cell.Coordinates] = attackInfo;
				
					// Start vfx sequence
					PlayTargetSequence(attackInfo);
				}
			}
			else
			{
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

					var attackValue = attackList[0];
				
					// Assign attack 
					randomCell.InfoHolder.HoldEnemyAttack(attackValue, false);

					// Create and store attack info
					var newShip = _shipSpawner.SpawnSmallShip(randomCell.Coordinates, out var originPosition);
					var attackInfo = new EnemyAttackInfo(attackValue, newShip, randomCell, originPosition);
					_attacksByCoord[randomCell.Coordinates] = attackInfo;
				
					// Start vfx sequence
					PlayTargetSequence(attackInfo);
				
					// Remove attack
					attackList.RemoveAt(0);
				}

			}
			_attackPhaseCount++;
			if (isEvenAttack) _staggeredPhaseCount++;
			
			UpdateDamagePreview();
		}

		private async void ResolveAttacks()
		{
			var resolutionTasks = new List<Task>();
			
			var cellsByRow = _grid.GetCellsByXPosRounded();
			foreach (var row in cellsByRow)
			{
				foreach (var cell in row.Value)
				{
					var cellHoldingUint = cell.HoldingUnit;
					if (!cell.HoldingEnemyAttack) continue; // Cell does not contain enemy attack

					var attackResult = AttackResultType.Contested;
					
					var powerDifference = cell.InfoHolder.ResolveAttack();
					if (powerDifference > 0)
					{
						// Player unit is stronger
						_enemyHealthBar.ModifyValue(-powerDifference);
						attackResult = AttackResultType.PlayerHit;
					}
					else if (powerDifference < 0)
					{
						// Enemy Attack is stronger
						_playerHealthBar.ModifyValue(powerDifference);
						attackResult = AttackResultType.EnemyHit;
					}
					else
					{
						attackResult = AttackResultType.Contested;
					}

					if (!_attacksByCoord.TryGetValue(cell.Coordinates, out var attackInfo))
					{
						Debug.LogError("Attack info not found when resolving attack");
						continue;
					}
					
					resolutionTasks.Add(PlayBeamSequence(attackInfo, attackResult, () =>
					{
						if (cellHoldingUint)
						{
							cell.InfoHolder.ClearEnemyAttack();

							// Remove stored attack info
							Destroy(attackInfo.Ship);
							_attacksByCoord.Remove(cell.Coordinates);
						}
					}));
				}
			}

			await Task.WhenAll(resolutionTasks);
			
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

		private async void PlayTargetSequence(EnemyAttackInfo attackInfo)
		{
			var effectInstance = Instantiate(_targetingEffect, _vfxAnchor);
			effectInstance.transform.position = attackInfo.OriginPosition;
			
			WaitThenDestroyVFX(effectInstance.gameObject, (int)effectInstance.GetFloat("Duration") * 1000);

			await Task.Delay((int)_attackTextDisplayDelaySeconds * 1000);
			attackInfo.TargetCell.UI.ToggleAttackCanvas(true);

			async void WaitThenDestroyVFX(GameObject vfxInstance, int delay)
			{
				await Task.Delay(delay);
			
				Destroy(vfxInstance);
			}
		}

		private async Task PlayBeamSequence(EnemyAttackInfo attackInfo, AttackResultType result, Action onComplete)
		{
			var effectPrefab = result switch
			{
				AttackResultType.Contested => _beamEffectContested,
				AttackResultType.EnemyHit => _beamEffectEnemyHit,
				AttackResultType.PlayerHit => _beamEffectPlayerHit,
				AttackResultType.PlayerMiss => _beamEffectPlayerMiss,
				_ => throw new ArgumentOutOfRangeException(nameof(result), result, null)
			};
			
			var effectInstance = Instantiate(effectPrefab, _vfxAnchor);
			effectInstance.transform.position = attackInfo.OriginPosition;
			
			await Task.Delay((int)effectInstance.GetFloat("Duration") * 1000);
			Destroy(effectInstance);
		}
	}
}