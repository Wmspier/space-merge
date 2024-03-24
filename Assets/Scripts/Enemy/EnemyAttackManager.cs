using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
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
		[SerializeField] private EnemyAttackUI _attackUi;
		[SerializeField] private EnemyCountUI _countUi;
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
		[SerializeField] private Camera _mainCamera;

		private readonly Dictionary<int3, EnemyShip> _shipsByCoord = new();

		private HexGrid _grid;
		private BattleData _battleData;
		
		private int _spawningShips;
		private int _targetingShips;
		
		private bool _shipsSpawning;
		private bool _shipsMoving;
		private bool _shipsAttacking;

		private Action _allEnemiesDestroyed;

		public Action AttackResolved;

		private bool CanResolveAttack => !_shipsSpawning && !_shipsMoving && !_shipsAttacking;

		public void Initialize(HexGrid grid, BattleData battleData, Action allEnemiesDestroyedCallback)
		{
			_grid = grid;
			_allEnemiesDestroyed = allEnemiesDestroyedCallback;
			
			_attackUi.ResolveAttackPressed = ResolveAttacks;
			_countUi.UpdateCount(battleData.Enemies.Count);
			
			_playerHealthBar.SetHealthToMax(50);;

			_battleData = battleData;
			_shipsSpawning = true;
			foreach (var enemy in _battleData.Enemies)
			{
				_spawningShips++;
				StartCoroutine(SpawnShip(enemy));
			}
		}

		public void Dispose()
		{
			foreach (var (_, ship) in _shipsByCoord)
			{;
				ship.Dispose();
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
			var newShipInstance = _shipSpawner.SpawnShip(enemyData, out var originPosition);
			var newShip = new EnemyShip(newShipInstance, targetCell, originPosition, enemyData, _battleData.AttackPattern);
			_shipsByCoord[enemyData.StartingPosition] = newShip;

			yield return newShip.PlayEnter();
			_enemyHealthBarManager.SpawnEnemyHealthBar(newShip);
			yield return new WaitForSeconds(.5f);
			
			targetCell.InfoHolder.AssignEnemyAttack(newShip.CurrentAttackDamage, false);

			PlayTargetSequence(newShip, OnShipSpawned);

			void OnShipSpawned()
			{
				UpdateDamagePreview();
				_spawningShips--;
				if (_spawningShips <= 0)
				{
					_shipsSpawning = false;
				}
			}
		}

		private void DestroyShip(EnemyShip ship)
		{
			Destroy(ship.ShipInstance.gameObject);
			_shipsByCoord.Remove(ship.CurrentPosition);
			_enemyHealthBarManager.DestroyEnemy(ship);
			ship.TargetingCell.InfoHolder.ClearEnemyAttack();
			ship.Dispose();
			
			_countUi.UpdateCount(_shipsByCoord.Count);
			
			if(_shipsByCoord.Count == 0) _allEnemiesDestroyed?.Invoke();
		}
		
		private async Task MoveShips()
		{
			_shipsMoving = true;
			var allShips = _shipsByCoord.Values.ToList();
			var moveTasks = new List<Task>();
			var newTargetCells = new List<HexCell>();
			foreach (var ship in allShips)
			{
				var unoccupiedCellsByColumn = _grid.CellsByColumn
					.Where(cells => cells.All(c => !_shipsByCoord.Values.Any(s => s.DoesOccupyCoord(c.Coordinates)))) // No cells where ships currently are
					.Where(cells => !cells.Any(c => newTargetCells.Contains(c))) // No cells where ships will move to
					.ToList()
					.Shuffle();
				
				// Pick the first column
				var cellsInRow = unoccupiedCellsByColumn[0];
				// Pick a random cell in this row
				var randomCell = cellsInRow.Shuffle().First();
				newTargetCells.Add(randomCell);

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
				_targetingShips++;
				PlayTargetSequence(ship, () => OnTargetingFinished(ship));
			}
			if (_targetingShips <= 0)
			{
				_shipsMoving = false;
			}
			
			void OnTargetingFinished(EnemyShip ship)
			{
				ship.TargetingCell.InfoHolder.ToggleEnemyAttack(true);
				_targetingShips--;
				if (_targetingShips <= 0)
				{
					_shipsMoving = false;
				}
			}
		}

		private async void ResolveAttacks()
		{
			if (!CanResolveAttack) return;

			_shipsAttacking = true;
			var resolutionTasks = new List<Task>();
			var cumulativePlayerDamageTaken = 0;
			var enemyDamageTaken = new Dictionary<EnemyShip, int>();
			var cellsToResolve = new List<HexCell>();

			foreach (var (_, enemyShip) in _shipsByCoord)
			{
				var targetingCell = enemyShip.TargetingCell;
				var isCellHoldingUnit = targetingCell.HoldingUnit;
				
				// Clear the targeting visual effect
				enemyShip.ClearTargetInstance();

				AttackResultType attackResult;

				var powerDifference = targetingCell.InfoHolder.PlayerPower - enemyShip.CurrentAttackDamage;
				switch (powerDifference)
				{
					case > 0:
						// Player unit is stronger
						enemyDamageTaken[enemyShip] = powerDifference;
						attackResult = enemyShip.IsAttacking ? AttackResultType.ContestedPlayerWin : AttackResultType.SoloPlayer;
						break;
					case < 0:
						// Enemy Attack is stronger
						cumulativePlayerDamageTaken += powerDifference;
						attackResult = isCellHoldingUnit ? AttackResultType.ContestedEnemyWin : AttackResultType.SoloEnemy;
						break;
					default:
						attackResult = AttackResultType.ContestedTie;
						break;
				}
					
				cellsToResolve.Add(targetingCell);

				targetingCell.InfoHolder.ToggleEnemyAttack(false);
					
				var attackInfo = new EnemyAttackInfo(enemyShip);
				resolutionTasks.Add(_attackSequencer.PlayBeamSequence(attackInfo, attackResult, _ => DoCameraShake()));
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
				var enemyDamage = _shipsByCoord[cell.Coordinates].CurrentAttackDamage;
				cell.InfoHolder.ResolveAttack(enemyDamage);
			}
			
			await MoveShips();
			UpdateDamagePreview();

			AttackResolved?.Invoke();
			_shipsAttacking = false;
		}

		public void UpdateDamagePreview()
		{
			var totalPlayerDamageTaken = GridUtility.GetTotalPlayerDamageTaken(_grid, this);
			
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

		public int GetEnemyDamageForCoord(int3 coord)
		{
			if (_shipsByCoord.TryGetValue(coord, out var ship))
			{
				return ship.CurrentAttackDamage;
			}

			return -1;
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

		private void DoCameraShake()
		{
			_mainCamera.transform.DOShakePosition(.75f, new Vector3(.5f, .5f, .5f));
		}
	}
}