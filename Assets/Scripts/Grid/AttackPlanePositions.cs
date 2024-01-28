using System.Collections.Generic;
using Hex.Managers;
using Unity.Mathematics;
using UnityEngine;

namespace Hex.Grid
{
	public class AttackPlanePositions : MonoBehaviour
	{
		[SerializeField] private GameObject _planeCollider;
		[SerializeField] private HexGrid _grid;
		[SerializeField] private Vector3 _projectionPointOffset = new (0, 100f, 0);

		private readonly Dictionary<int3, Vector3> _positions = new();
		private readonly RaycastHit[] _hitBuffer = new RaycastHit[10];

		private bool _gridInitialized;
		private Quaternion _cachedPlaneRotation;
		private Vector3 _cachedOffsetPosition;

		public IReadOnlyDictionary<int3, Vector3> Positions => _positions;

		private void Awake()
		{
			_grid.GridInitialized += GridInitialized;
			
			ApplicationManager.RegisterResource(this);
		}

		private void OnDestroy()
		{
			_grid.GridInitialized -= GridInitialized;
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

				_positions[cell.Coordinates] = positionOnPlane.Value;
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
				Gizmos.DrawSphere(pos.Value, .25f);
			}
			Gizmos.color = Color.red;
			Gizmos.DrawSphere(_projectionPointOffset, .25f);
		}
	}
}