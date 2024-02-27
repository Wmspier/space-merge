using System;
using Hex.Data;
using Hex.Enemy;
using Hex.Model;
using Hex.UI;
using UnityEngine;

namespace Hex.Managers
{
	public class BattleContext : MonoBehaviour, IDisposable
	{
		[SerializeField] private GridInteractionManager _gridInteractionManager;
		[SerializeField] private EnemyAttackManager _enemyAttackManager;
		[SerializeField] private PlayerUnitManager _playerUnitManager;

		[Header("UI")]
		[SerializeField] private BattleUI _battleUI;

		[Header("Debug")] 
		[SerializeField] private BattleData _testBattle;
		[SerializeField] private BattleConfig _testConfig;
		
		public void StartBattle()
		{
			var battleModel = new BattleModel
			{
				RemainingUnitMoves = _testConfig.UnitMovesPerBattle
			};
			ApplicationManager.RegisterResource(battleModel);
			
			_gridInteractionManager.GridStateChanged += _enemyAttackManager.UpdateDamagePreview;
			
			_gridInteractionManager.Initialize();
			_gridInteractionManager.SpawnUnit = _playerUnitManager.DrawNextUnit;
			
			_playerUnitManager.Initialize();
            
			_enemyAttackManager.Initialize(_gridInteractionManager.Grid, _testBattle);
			_enemyAttackManager.AttackResolved = OnAttackResolved;
			
			_battleUI.gameObject.SetActive(true);
			_battleUI.MoveUI.Initialize(battleModel.RemainingUnitMoves);
		}

		public void Dispose()
		{
			_gridInteractionManager.GridStateChanged -= _enemyAttackManager.UpdateDamagePreview;
			
			_battleUI.gameObject.SetActive(false);

			_gridInteractionManager.Dispose();
			_enemyAttackManager.Dispose();
			_playerUnitManager.Dispose();
			
			ApplicationManager.UnRegisterResource<BattleModel>();
		}
		
		private void OnAttackResolved()
		{
			_playerUnitManager.DrawNewHand();
		}
	}
}