using System.Collections.Generic;
using System.Linq;
using Hex.Grid.Cell;
using UnityEngine;

namespace Hex.Grid
{
    [RequireComponent(typeof(LineRenderer))]
    public class LineDrawer : MonoBehaviour
    {
        [SerializeField] private GameObject endTriangle;
        [SerializeField] private GameObject endTriangleRoot;
        
        private LineRenderer _lineRenderer;
        private readonly List<Vector3> _points = new();

        private void Awake()
        {
            _lineRenderer = GetComponent<LineRenderer>();
        }

        public void AddCellToLine(HexCell cell)
        {
            var position = cell.transform.position;
            position.y = 1.25f;
            
            _points.Add(position);

            _lineRenderer.positionCount = _points.Count;
            _lineRenderer.SetPositions(_points.ToArray());
            
            endTriangleRoot.SetActive(_points.Count > 1);
            RotateAndPositionTriangle();
        }

        public void RemoveFromEnd()
        {
            if (_points.Count == 0)
            {
                return;
            }
            _points.Remove(_points.Last());

            _lineRenderer.positionCount = _points.Count;
            _lineRenderer.SetPositions(_points.ToArray());
            
            endTriangleRoot.SetActive(_points.Count > 1);
            RotateAndPositionTriangle();
        }

        public void Clear()
        {
            _points.Clear();
            _lineRenderer.positionCount = _points.Count;
            _lineRenderer.SetPositions(_points.ToArray());
            endTriangleRoot.SetActive(false);
        }

        private void RotateAndPositionTriangle()
        {
            if (_points.Count <= 1) return;
            
            var v1 = Vector3.forward;
            var v2 = (_points[^1] - _points[^2]).normalized;

            var angle = Vector3.SignedAngle(v1, v2, Vector3.up);
            endTriangle.transform.localRotation = Quaternion.Euler(new Vector3(90, 0, angle));
            endTriangleRoot.transform.position = _points.Last();
        }
    }
}