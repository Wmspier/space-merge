using System;
using Hex.Data;
using Hex.Enemy;
using Hex.Model;
using Hex.UI;
using Hex.UI.Popup;
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
		[SerializeField] private PopupBattleResult _battleResultPopupPrefab;

		[Header("Debug")] 
		[SerializeField] private BattleData _testBattle;
		[SerializeField] private BattleConfig _testConfig;
		
		public void StartBattle()
		{
			var battleModel = new BattleModel
			{
				RemainingUnitMoves = _testConfig.UnitMovesPerBattle,
				MaxMergeCount =  _testConfig.MaxMergeCount
			};
			ApplicationManager.RegisterResource(battleModel);
			
			_gridInteractionManager.GridStateChanged += _enemyAttackManager.UpdateDamagePreview;
			
			_gridInteractionManager.Initialize();
			_gridInteractionManager.SpawnUnit = _playerUnitManager.DrawNextUnit;
			
			_playerUnitManager.Initialize();
            
			_enemyAttackManager.Initialize(_gridInteractionManager.Grid, _testBattle, OnAllEnemiesDestroyed);
			_enemyAttackManager.AttackResolved = OnAttackResolved;
			
			_battleUI.gameObject.SetActive(true);
			_battleUI.MoveUI.Initialize(battleModel.RemainingUnitMoves);
			_battleUI.PlayerHealthBar.SetHealthToMax(_testConfig.PlayerStartingHealth);
			_battleUI.PlayerHealthBar.SetDepletedAction(OnPlayerHealthDepleted);
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

		private void OnAllEnemiesDestroyed()
		{
			var resultPopup = Instantiate(_battleResultPopupPrefab);
			resultPopup.ShowAsVictory(OnResultContinuePressed);

			var popupsUI = ApplicationManager.GetResource<PopupsUI>();
			popupsUI.gameObject.SetActive(true);
			popupsUI.RegisterPopup(resultPopup);
			popupsUI.ToggleInputBlock(true);
		}

		private void OnPlayerHealthDepleted()
		{
			var resultPopup = Instantiate(_battleResultPopupPrefab);
			resultPopup.ShowAsDefeat(OnResultContinuePressed);

			var popupsUI = ApplicationManager.GetResource<PopupsUI>();
			popupsUI.gameObject.SetActive(true);
			popupsUI.RegisterPopup(resultPopup);
			popupsUI.ToggleInputBlock(true);
		}

		private static void OnResultContinuePressed()
		{
			var popupsUI = ApplicationManager.GetResource<PopupsUI>();
			popupsUI.ToggleInputBlock(false);
			popupsUI.gameObject.SetActive(false);
			popupsUI.UnregisterAndDestroy<PopupBattleResult>();
			
			ApplicationManager.GetResource<NavigationManager>().GoToMainMenu();
		}
	}
}