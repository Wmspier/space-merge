using System.Collections.Generic;
using Hex.UI;
using UnityEngine;

namespace Hex.Enemy
{
	public class EnemyHealthBarManager : MonoBehaviour
	{
		[SerializeField] private Camera _uiCamera;
		[SerializeField] private HealthBar _healthBarPrefab;

		private readonly Dictionary<EnemyShip, HealthBar> _healthBarByEnemy = new();
		public IReadOnlyDictionary<EnemyShip, HealthBar> HealthBars => _healthBarByEnemy;

		public void Dispose()
		{
			foreach (var (_, bar) in _healthBarByEnemy)
			{
				Destroy(bar.gameObject);
			}
			_healthBarByEnemy.Clear();
		}
		
		public void SpawnEnemyHealthBar(EnemyShip enemyShip)
		{
			if (_healthBarByEnemy.ContainsKey(enemyShip)) return;

			var newHealthBar = Instantiate(_healthBarPrefab, transform);
			_healthBarByEnemy[enemyShip] = newHealthBar;
			
			newHealthBar.SetHealthToMax(enemyShip.CurrentHealth);
		}

		public void DestroyEnemy(EnemyShip enemyShip)
		{
			Destroy(_healthBarByEnemy[enemyShip].gameObject);
			_healthBarByEnemy.Remove(enemyShip);
		}

		private void Update()
		{
			foreach (var (ship, healthBar) in _healthBarByEnemy)
			{
				healthBar.transform.position = _uiCamera.WorldToScreenPoint(ship.ShipInstance.HealthBarTarget.position);
			}
		}
	}
}