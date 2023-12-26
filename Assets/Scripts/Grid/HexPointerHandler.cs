using System;
using System.Linq;
using Hex.Grid.Cell;
using UnityEngine;
using UnityEngine.Pool;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

namespace Hex.Grid
{
    public class HexPointerHandler : PointerHandler
    {
        private const string HexCellTag = "HexCell";
        private const float CellCheckMinDistance = .5f; // Distance in screen-space since last check before a new raycast can be made.
        private const float PointerClickThreshold = .25f; // Time in seconds before a pointer click is considered pointer up.
        private const float PointerDownThreshold = .2f; // Time in seconds before pointer down over cell is allowed to fire.

        [SerializeField] private Camera gridCamera;

        private readonly RaycastHit[] _raycastHits = new RaycastHit[5];
        private HexCell _cellUnderPointer;
        private Vector2 _lastCellCheckPointerPosition;
        private float _pointerDownElapsed;

        public Action PointerDownOverNothing;
        public Action PointerUpOverNothing;
        public Action DragEnded;
        
        public Action<HexCell> CellClicked;
        public Action<HexCell> PointerUpOverCell;
        public Action<HexCell> PointerDownOverCell;
        public Action<HexCell, Vector3> DragOverCell;

        private void Awake()
        {
            PointerClick += OnPointerClick;
            PointerUp += OnPointerUp;
            BeginDrag += OnDrag;
            DragContinue += OnDrag;
        }

        private void FixedUpdate()
        {
            if (!IsPointerDown)
            {
                _pointerDownElapsed = 0;
                _cellUnderPointer = null;
                return;
            }
            _pointerDownElapsed += Time.fixedDeltaTime;

            // Avoid ray-casting if the pointer has not moved enough since the last check.
            var distanceFromCheck = Vector2.Distance(_lastCellCheckPointerPosition, PointerPosition);
            if (distanceFromCheck < CellCheckMinDistance)
            {
                return;
            }

            // Raycast to see if a cell is under the pointer.
            var (hasHit, cell, _) = GetHexCellUnderPointer(PointerPosition);
            if (!hasHit)
            {
                PointerDownOverNothing?.Invoke();
                _cellUnderPointer = null;
                return;
            } 
            
            // Return if the pointer is over the same cell or the pointer has not been held long enough.
            if (cell == _cellUnderPointer || _pointerDownElapsed < PointerDownThreshold)
            {
                return;
            }

            _cellUnderPointer = cell;
            _lastCellCheckPointerPosition = PointerPosition;
            PointerDownOverCell?.Invoke(cell);
        }

        private void OnPointerUp()
        {
            // Still within the click threshold. CellClicked will be fired.
            if (_pointerDownElapsed <= PointerClickThreshold)
            {
                return;
            }
            if (_cellUnderPointer != null)
            {
                PointerUpOverCell?.Invoke(_cellUnderPointer);
            }

            _cellUnderPointer = null;
            _lastCellCheckPointerPosition = new Vector2(float.MinValue, float.MinValue);
            PointerUpOverNothing?.Invoke();
        }

        private void OnPointerClick(Vector2 pointerPosition)
        {
            // Past the click threshold. PointerUpOverCell will be fired.
            if (_pointerDownElapsed > PointerClickThreshold)
            {
                return;
            }
            var (hasHit, cell, _) = GetHexCellUnderPointer(pointerPosition);
            if (hasHit)
            {
                CellClicked?.Invoke(cell);
            }
        }

        private void OnDrag(Vector2 pointerPosition)
        {
            var (hasHit, cell, point) = GetHexCellUnderPointer(pointerPosition);
            if (!hasHit)
            {
                return;
            }

            DragOverCell?.Invoke(cell, point.Value);
        }
        
        private (bool hasHit, HexCell cell, Vector3? hitPosition) GetHexCellUnderPointer(Vector2 pointerPosition)
        {
            var ray = gridCamera.ScreenPointToRay(pointerPosition);
            var hits = Physics.RaycastNonAlloc(ray, _raycastHits, Mathf.Infinity);
            var hitCells = ListPool<(HexCell cell, float distance, Vector3 hitPoint)>.Get();
            for (var i = hits-1; i >= 0; i--)
            {
                var hit = _raycastHits[i];
                if (hit.transform.parent == null || !hit.transform.gameObject.CompareTag(HexCellTag))
                {
                    continue;
                }
                
                hitCells.Add((hit.transform.GetComponentInParent<HexCell>(), hit.distance, hit.point));
            }

            if (hitCells.Count == 0)
            {
                return (false, null, null);
            }

            var (cell, _, hitPoint) = hitCells.OrderBy(h => h.distance).First();
            ListPool<(HexCell cell, float distance, Vector3 hitPoint)>.Release(hitCells);
            return (true, cell, hitPoint);
        }
    }
}