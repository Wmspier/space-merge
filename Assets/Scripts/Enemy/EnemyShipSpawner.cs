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
		

		public GameObject SpawnSmallShip(int3 coord)
		{
			if (!_planePositions.Positions.TryGetValue(coord, out var position))
			{
				Debug.LogError($"Failed to find position to spawn ship: {coord}");
				return null;
			}

			var shipInstance = Instantiate(_enemyShipPrefabSmall, _shipAnchor);
			shipInstance.transform.Reset();
			shipInstance.transform.position = position;

			return shipInstance;
		}
		
		public GameObject SpawnBigShip(int3 coord)
		{
			if (!_planePositions.Positions.TryGetValue(coord, out var position))
			{
				Debug.LogError($"Failed to find position to spawn ship: {coord}");
				return null;
			}

			var shipInstance = Instantiate(_enemyShipPrefabLarge, _shipAnchor);
			shipInstance.transform.Reset();
			shipInstance.transform.position = position;
			
			return shipInstance;
		}
	}
}