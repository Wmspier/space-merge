using System.Collections.Generic;
using Hex.Grid;
using UnityEngine;

namespace Hex.Enemy
{
	public class EnemyPlanePositions : MonoBehaviour
	{
		[SerializeField] private GameObject _planeCollider;
		[SerializeField] private HexGrid _grid;
		[SerializeField] private Vector3 _projectionPointOffset = new (0, 100f, 0);

		private readonly List<Vector3> _positions = new();
		private readonly RaycastHit[] _hitBuffer = new RaycastHit[10];

		private bool _gridInitialized;
		private Quaternion _cachedPlaneRotation;
		private Vector3 _cachedOffsetPosition;
		
		private void Awake()
		{
			_grid.GridInitialized = GridInitialized;
		}

		private void Update()
		{
			if (_gridInitialized && (_cachedPlaneRotation != transform.rotation || _cachedOffsetPosition != _projectionPointOffset))
			{
				_positions.Clear();
				GridInitialized();
				_cachedOffsetPosition = _projectionPointOffset;
			}
		}

		private void GridInitialized()
		{
			_gridInitialized = true;
			_cachedPlaneRotation = transform.rotation;
			
			foreach (var (_, cell) in _grid.Registry)
			{
				var positionOnPlane = ProjectOntoPlane(cell.transform.position);
				if (!positionOnPlane.HasValue) continue;
				_positions.Add(positionOnPlane.Value);
			}
		}

		private Vector3? ProjectOntoPlane(Vector3 cellPosition)
		{
			var raycastDir = _projectionPointOffset - cellPosition;

			var hitCount = Physics.RaycastNonAlloc(cellPosition, raycastDir, _hitBuffer);
			
			for (var i = 0; i < hitCount; i++)
			{
				var hit = _hitBuffer[i];
				if (hit.collider.gameObject != _planeCollider) continue;

				return hit.point;
			}
			return null;
		}

		private void OnDrawGizmos()
		{
			foreach (var pos in _positions)
			{
				Gizmos.DrawSphere(pos, .25f);
			}
			Gizmos.color = Color.red;
			Gizmos.DrawSphere(_projectionPointOffset, .25f);
		}
	}
}