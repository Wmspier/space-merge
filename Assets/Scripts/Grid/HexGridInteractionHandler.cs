using System;
using System.Collections.Generic;
using System.Linq;
using Hex.Extensions;
using Hex.Grid.Cell;
using UnityEngine;
using UnityEngine.Pool;

namespace Hex.Grid
{
    [RequireComponent(typeof(HexPointerHandler))]
    public class HexGridInteractionHandler : MonoBehaviour
    {
        public enum SelectionMode
        {
            Outline, // Draw an outline around all selected cells
            Path // Draw a path connecting cell center points
        }
        
        [SerializeField] private LineDrawer lineDrawer;
        [SerializeField] private bool allowDragSelect;

        [SerializeField] private float minDistanceForDrag = .5f;

        private bool _interactionsBlocked;
        private SelectionMode _selectionMode;
        
        private readonly List<HexCell> _dragSelectedCells = new();

        public event Action<HexCell> CellClicked;
        public event Action<List<HexCell>> CellsDragReleased;
        public event Action<List<HexCell>> CellsDragContinue;

        public void SetSelectionMode(SelectionMode mode) => _selectionMode = mode;
        
        public bool BlockInteractions
        {
            get => _interactionsBlocked;
            set
            {
                if (_interactionsBlocked == value) return;
                _interactionsBlocked = value;
                if (_interactionsBlocked)
                {
                    UnbindActions();
                }
                else
                {
                    BindActions();
                }
            }
        }

        private void Awake()
        {
            BindActions();
        }

        private void OnDestroy() => UnbindActions();

        private void BindActions()
        {
            var pointerHandler = GetComponent<HexPointerHandler>();
            pointerHandler.CellClicked = OnCellClicked;
            pointerHandler.DragOverCell = OnDragOverCell;
            pointerHandler.PointerDownOverCell = OnPointerDownOverCell;
            pointerHandler.PointerUpOverCell = OnPointerUpOverCell;
            pointerHandler.PointerUpOverNothing = OnPointerUpOverNothing;

            pointerHandler.EndDrag += OnDragEnd;
        }

        private void UnbindActions()
        {
            var pointerHandler = GetComponent<HexPointerHandler>();
            pointerHandler.CellClicked = null;
            pointerHandler.DragOverCell = null;
            pointerHandler.PointerDownOverCell = null;
            pointerHandler.PointerUpOverCell = null;
            pointerHandler.PointerUpOverNothing = null;

            pointerHandler.EndDrag -= OnDragEnd;
        }

        private void OnCellClicked(HexCell cell)
        {
            if (_dragSelectedCells.Count > 0)
            {
                CellsDragReleased?.Invoke(_dragSelectedCells);
                ClearDragSelectedCells();
            }
            else
            {
                CellClicked?.Invoke(cell);
            }
        }

        private void OnDragOverCell(HexCell cell, Vector3 hitPoint)
        {
            if (!allowDragSelect) return;
            
            if(Vector3.Distance(hitPoint, cell.transform.position) > minDistanceForDrag)  return;
            
            // Begin drag chain
            if (_dragSelectedCells.Count == 0)
            {
                _dragSelectedCells.Add(cell);

                switch (_selectionMode)
                {
                    case SelectionMode.Path:
                        if(lineDrawer != null) lineDrawer.AddCellToLine(cell);
                        break;
                    case SelectionMode.Outline:
                        UpdateDragOutline();
                        break;
                }

                CellsDragContinue?.Invoke(_dragSelectedCells);
                return;
            }

            if (!cell.IsNeighborOf(_dragSelectedCells.Last()))
            {
                return;
            }
            
            // Dragging over the previous cell in the drag chain. Pop off the last cell.
            if (_dragSelectedCells.Count > 1 && cell == _dragSelectedCells[^2])
            {
                var last = _dragSelectedCells.Last();
                last.ToggleOutline(false);
                last.ToggleMoveArrow(false);
                last.UI.ToggleMergeCanvas(false);
                
                _dragSelectedCells.Remove(last);
                
                switch (_selectionMode)
                {
                    case SelectionMode.Path:
                        if(lineDrawer != null) lineDrawer.RemoveFromEnd();
                        break;
                    case SelectionMode.Outline:
                        UpdateDragOutline();
                        break;
                }
                
                CellsDragContinue?.Invoke(_dragSelectedCells);
                return;
            }

            if (_dragSelectedCells.Contains(cell))
            {
                return;
            }

            // Add to drag chain
            _dragSelectedCells.Add(cell);
            switch (_selectionMode)
            {
                case SelectionMode.Path:
                    if(lineDrawer != null) lineDrawer.AddCellToLine(cell);
                    break;
                case SelectionMode.Outline:
                    UpdateDragOutline();
                    break;
            }
            CellsDragContinue?.Invoke(_dragSelectedCells);
        }

        private void UpdateDragOutline()
        {
            var neighborCells = ListPool<HexCell>.Get();
            for (int i = 0, max = _dragSelectedCells.Count - 1; i < _dragSelectedCells.Count; i++)
            {
                if (i > 0)
                {
                    neighborCells.Add(_dragSelectedCells[i-1]);
                }

                if (i < max)
                {
                    neighborCells.Add(_dragSelectedCells[i+1]);
                }
                _dragSelectedCells[i].ToggleOutline(true, neighborCells);
                neighborCells.Clear();
            }
        }
        
        private void OnPointerDownOverCell(HexCell cell)
        {
            if (_dragSelectedCells.Count == 0)
            {
                OnDragOverCell(cell, cell.transform.position);
            }
        }

        private void OnPointerUpOverCell(HexCell _)
        {
            if (_dragSelectedCells.Count > 0)
            {
                CellsDragReleased?.Invoke(_dragSelectedCells);
            }

            ClearDragSelectedCells();
        }

        private void OnPointerUpOverNothing() => ClearDragSelectedCells();

        private void OnDragEnd() => OnPointerUpOverCell(null);
        
        private void ClearDragSelectedCells()
        {
            foreach (var cell in _dragSelectedCells)
            {
                cell.ToggleOutline(false);
                cell.ToggleMoveArrow(false);
                cell.UI.ToggleMergeCanvas(false);
            }
            _dragSelectedCells.Clear();
            if(lineDrawer != null) lineDrawer.Clear();
        }
    }
}