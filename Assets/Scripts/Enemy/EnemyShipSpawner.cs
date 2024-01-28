using Hex.Extensions;
using Unity.Mathematics;
using UnityEngine;

namespace Hex.Enemy
{
	public class EnemyShipSpawner : MonoBehaviour
	{
		[SerializeField] private EnemyPlanePositions _planePositions;
		[SerializeField] private Transform _shipAnchor;
		
		
		[SerializeField] private GameObject _enemyShipPrefabSmall;
		[SerializeField] private GameObject _enemyShipPrefabLarge;
		
		public int3 ShipSpawnCoord;

		[ContextMenu("Spawn Small Ship")]
		public void SpawnSmallShip()
		{
			if (!_planePositions.Positions.TryGetValue(ShipSpawnCoord, out var position))
			{
				Debug.LogError($"Failed to find position to spawn ship: {ShipSpawnCoord}");
				return;
			}

			var shipInstance = Instantiate(_enemyShipPrefabSmall, _shipAnchor);
			shipInstance.transform.Reset();
			shipInstance.transform.position = position;
		}
		
		[ContextMenu("Spawn Big Ship")]
		public void SpawnBigShip()
		{
			if (!_planePositions.Positions.TryGetValue(ShipSpawnCoord, out var position))
			{
				Debug.LogError($"Failed to find position to spawn ship: {ShipSpawnCoord}");
				return;
			}

			var shipInstance = Instantiate(_enemyShipPrefabLarge, _shipAnchor);
			shipInstance.transform.Reset();
			shipInstance.transform.position = position;
		}
	}
}