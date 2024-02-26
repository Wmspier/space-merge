using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Hex.Data;
using Hex.Extensions;
using Hex.Grid;
using Hex.Grid.Cell;
using Hex.Sequencers;
using Hex.UI;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.VFX;
using Task = System.Threading.Tasks.Task;

namespace Hex.Enemy
{
	public enum AttackResultType
	{
		None,
		ContestedEnemyWin,
		ContestedPlayerWin,
		ContestedTie,
		SoloPlayer,
		SoloEnemy,
		MissPlayer
	}

	public struct EnemyAttackInfo
	{
		public int Damage;
		public EnemyShipInstance Ship;
		public HexCell TargetCell;
		public Vector3 OriginPosition;

		public EnemyAttackInfo(EnemyShip ship)
		{
			Damage = ship.CurrentAttackDamage;
			Ship = ship.ShipInstance;
			TargetCell = ship.TargetingCell;
			OriginPosition = ship.CurrentWorldSpacePosition;
		}
	}
	
	public class EnemyAttackManager : MonoBehaviour
	{
		[SerializeField] private EnemyAttackUI _ui;
		[SerializeField] private HealthBar _playerHealthBar;
		[SerializeField] private EnemyShipSpawner _shipSpawner;
		[SerializeField] private EnemyHealthBarManager _enemyHealthBarManager;

		[Space] 
		[Header("VFX")] 
		[SerializeField] private Transform _vfxAnchor;
		[SerializeField] private VisualEffect _targetingEffect;
		[SerializeField] private List<float> _targetingScaleByCellRow;
		[SerializeField] private float _attackTextDisplayDelaySeconds = 2f;
		[SerializeField] private AttackSequencer _attackSequencer;

		//private readonly Dictionary<int3, EnemyAttackInfo> _attacksByCoord = new();
		private readonly Dictionary<int3, EnemyShip> _shipsByCoord = new();

		private HexGrid _grid;
		private BattleData _battleData;

		public Action AttackResolved;

		public bool IsAttackPhase => _ui.TurnsBeforeAttack == 0;
		public void ResetTurns() => _ui.ResetTurns();

		public void Initialize(HexGrid grid, BattleData battleData)
		{
			_grid = grid;
			_ui.ResolveAttackPressed = ResolveAttacks;
			
			_playerHealthBar.SetHealthToMax(50);;

			_battleData = battleData;
			foreach (var enemy in _battleData.Enemies)
			{
				StartCoroutine(SpawnShip(enemy));
			}
		}

		public void Dispose()
		{
			foreach (var (_, ship) in _shipsByCoord)
			{
				Destroy(ship.ShipInstance.gameObject);
			}
			_shipsByCoord.Clear();
			
			_enemyHealthBarManager.Dispose();
		}

		private IEnumerator SpawnShip(BattleData.BattleEnemy enemyData)
		{
			if (_shipsByCoord.ContainsKey(enemyData.StartingPosition))
			{
				Debug.LogError($"Trying to spawn ship at occupied space: {enemyData.StartingPosition}");
				yield break;
			}
			
			var targetCell = _grid.Registry[enemyData.StartingPosition];
			var newShipInstance = _shipSpawner.SpawnSmallShip(targetCell.Coordinates, out var originPosition);
			var newShip = new EnemyShip(newShipInstance, targetCell, originPosition, enemyData, _battleData.AttackPattern);
			_shipsByCoord[enemyData.StartingPosition] = newShip;
			
			_enemyHealthBarManager.SpawnEnemyHealthBar(newShip);

			yield return newShip.PlayEnter();
			yield return new WaitForSeconds(.5f);
			
			targetCell.InfoHolder.HoldEnemyAttack(newShip.CurrentAttackDamage, false);

			PlayTargetSequence(newShip, UpdateDamagePreview);
		}

		private void DestroyShip(EnemyShip ship)
		{
			Destroy(ship.ShipInstance.gameObject);
			_shipsByCoord.Remove(ship.CurrentPosition);
			_enemyHealthBarManager.DestroyEnemy(ship);
			ship.TargetingCell.InfoHolder.ClearEnemyAttack();
			ship.Dispose();
		}
		
		private async Task MoveShips()
		{
			var allShips = _shipsByCoord.Values.ToList();
			var moveTasks = new List<Task>();
			foreach (var ship in allShips)
			{
				var cellsByXPos = _grid.GetCellsByXPosRounded()
					.Where(kvp => !kvp.Value.Any(c => _shipsByCoord.Keys.Contains(c.Coordinates)))
					.Where(kvp => kvp.Value.Any(c => c.Coordinates.x == 1))
					.ToList()
					.Shuffle();
				
				// Pick the first column (list should be shuffled)
				var cellsInRow = cellsByXPos[0].Value;
				// Pick a random cell in this row
				var randomCell = cellsInRow.Shuffle().First();

				// Change ship position in map
				var oldCoord = ship.CurrentPosition;
				var newCoord = randomCell.Coordinates;
				_shipsByCoord.Remove(oldCoord);
				_shipsByCoord[newCoord] = ship;
				
				// Elapse turn
				ship.ElapseTurn();
				
				// Move ship to new coord
				moveTasks.Add(ship.MoveTo(randomCell, _shipSpawner.GetPositionForCoord(randomCell.Coordinates)));
			}

			await Task.WhenAll(moveTasks);
			
			foreach (var ship in allShips)
			{
				if (ship.CurrentAttackDamage == 0) continue;
				PlayTargetSequence(ship, () => ship.TargetingCell.InfoHolder.ToggleEnemyAttack(true));
			}
		}

		private async void ResolveAttacks()
		{
			var resolutionTasks = new List<Task>();
			var cumulativePlayerDamageTaken = 0;
			var enemyDamageTaken = new Dictionary<EnemyShip, int>();
			var cellsToResolve = new List<HexCellInfoHolder>();
			
			var cellsByRow = _grid.GetCellsByXPosRounded();
			foreach (var row in cellsByRow)
			{
				foreach (var cell in row.Value)
				{
					var cellHoldingUint = cell.HoldingUnit;
					if (!cell.HoldingEnemyAttack) continue; // Cell does not contain enemy attack
					
					if (!_shipsByCoord.TryGetValue(cell.Coordinates, out var enemyShip))
					{
						Debug.LogError("Ship not found when resolving attack");
						continue;
					}
					
					// Clear the targeting visual effect
					enemyShip.ClearTargetInstance();

					AttackResultType attackResult;

					var powerDifference = cell.InfoHolder.PowerDifference;
					if (powerDifference > 0)
					{
						// Player unit is stronger
						enemyDamageTaken[enemyShip] = powerDifference;
						attackResult = AttackResultType.ContestedPlayerWin;
					}
					else if (powerDifference < 0)
					{
						// Enemy Attack is stronger
						cumulativePlayerDamageTaken += powerDifference;
						attackResult = cellHoldingUint ? AttackResultType.ContestedEnemyWin : AttackResultType.SoloEnemy;
					}
					else
					{
						attackResult = AttackResultType.ContestedTie;
					}
					
					cellsToResolve.Add(cell.InfoHolder);

					cell.InfoHolder.ToggleEnemyAttack(false);
					
					var attackInfo = new EnemyAttackInfo(enemyShip);
					resolutionTasks.Add(_attackSequencer.PlayBeamSequence(attackInfo, attackResult, null));
				}
			}

			await Task.WhenAll(resolutionTasks);
			
			_playerHealthBar.ModifyValue(cumulativePlayerDamageTaken);
			_playerHealthBar.HidePreview();
			foreach (var (ship, damage) in enemyDamageTaken)
			{
				var healthBar = _enemyHealthBarManager.HealthBars[ship];
				healthBar.ModifyValue(-damage);
				healthBar.HidePreview();
				ship.DealDamage(damage);

				if (ship.CurrentHealth <= 0) DestroyShip(ship);
			}

			foreach (var cell in cellsToResolve)
			{
				cell.ResolveAttack();
			}
			
			_ui.ResetTurns();
			await MoveShips();
			UpdateDamagePreview();

			AttackResolved?.Invoke();
		}

		public void UpdateDamagePreview()
		{
			var totalPlayerDamageTaken = GridUtility.GetTotalPlayerDamageTaken(_grid);
			
			// Update player total damage preview
			if (totalPlayerDamageTaken > 0)
			{
				_playerHealthBar.ShowPreview(totalPlayerDamageTaken);
			}
			else
			{
				_playerHealthBar.HidePreview();
			}

			foreach (var (enemyShip, bar) in _enemyHealthBarManager.HealthBars)
			{
				var playerUnitDamage = enemyShip.TargetingCell.InfoHolder.PlayerPower;
				var enemyDamage = enemyShip.CurrentAttackDamage;

				var difference = playerUnitDamage - enemyDamage;
				if (difference <= 0)
				{
					bar.HidePreview();
				}
				else
				{
					bar.ShowPreview(difference);
				}
			}
		}

		private async void PlayTargetSequence(EnemyShip ship, Action completeAction)
		{
			var scale = _targetingScaleByCellRow[Mathf.Min(_targetingScaleByCellRow.Count - 1, ship.TargetingCell.Coordinates.x-1)];
			
			var effectInstance = Instantiate(_targetingEffect, _vfxAnchor);
			var effectTransform = effectInstance.transform;
			effectTransform.position = ship.CurrentWorldSpacePosition;
			effectTransform.localScale = new Vector3(1, scale, 1);
			
			ship.SetTargetInstance(effectInstance);

			await Task.Delay((int)_attackTextDisplayDelaySeconds * 1000);
			ship.TargetingCell.UI.ToggleAttackCanvas(true);
			
			completeAction?.Invoke();
		}
	}
}