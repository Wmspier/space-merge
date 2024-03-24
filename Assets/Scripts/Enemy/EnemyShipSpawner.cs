using Hex.Data;
using Hex.Extensions;
using Unity.Mathematics;
using UnityEngine;

namespace Hex.Enemy
{
	public class EnemyShipSpawner : MonoBehaviour
	{
		[SerializeField] private EnemyPlanePositions _planePositions;
		[SerializeField] private Transform _shipAnchor;

		public EnemyShipInstance SpawnShip(BattleData.BattleEnemy enemyData, out Vector3 originPosition)
		{
			var coord = enemyData.StartingPosition;
			if (!_planePositions.Positions.TryGetValue(coord, out var position))
			{
				Debug.LogError($"Failed to find position to spawn ship: {coord}");
				originPosition = Vector3.zero;
				return null;
			}

			var shipInstance = Instantiate(enemyData.ShipPrefab, _shipAnchor);
			Transform shipTransform;
			(shipTransform = shipInstance.transform).Reset();
			shipTransform.position = position;
			originPosition = position;

			return shipInstance;
		}

		public Vector3 GetPositionForCoord(int3 coord)
		{
			if (_planePositions.Positions.TryGetValue(coord, out var position))
				return position;

			Debug.LogError($"Failed to find position to spawn ship: {coord}");
			return Vector3.zero;
		}
	}
}