using System;
using System.Collections.Generic;
using System.Linq;
using Hex.Extensions;
using Hex.Grid.Cell;
using Hex.Tools;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Hex.Grid
{
    public class HexGrid : MonoBehaviour
    {
        [Header("Debug")]
        [SerializeField] public bool showGizmos;

        [Header("Hex")]
        [SerializeField][Range(2, 10)] protected int numEdgeCells;
        [SerializeField] private HexCell hexCellPrefab;

        [Space] 
        [SerializeField] private Transform cellsAnchor;

        public event Action GridInitialized;
        
        public Dictionary<int3, HexCell> Registry { get; } = new();

        public List<List<HexCell>> CellsByColumn { get; } = new();
        
        private int Diameter => numEdgeCells * 2 - 1;
        
        #region Grid Accessing
         
        public HexCell GetCenterCell() => Registry[new int3(numEdgeCells, numEdgeCells, numEdgeCells)];
 
        public HexCell GetRandomCell()
        {
            var allCells = Registry.Values.ToList();
            return allCells[Random.Range(0, allCells.Count)];
        }
         
        #endregion

        public void LoadFromFile()
        {
            var loadedLevel = LevelEditorUtility.LoadLevel("Test");
            Load(true, loadedLevel);
        }

        // Used for sorting cells into columns
        private int _currentRow;
        private int _indexInRow;
        private void InitializeColumnMap()
        {
            _currentRow = -1;
            _indexInRow = 0;
            
            CellsByColumn.Clear();
            var numRows = Diameter * 2 - 1;
            for (var i = 0; i < numRows; i++)
            {
                CellsByColumn.Add(new List<HexCell>());
            }
        }
        
        public virtual void Load(bool immediateDestroy = false, List<HexCellDefinition> definitions = null, bool forceSpawnCells = false)
        {
            DestroyGrid(immediateDestroy);
            InitializeColumnMap();

            if (definitions != null)
            {
                foreach (var def in definitions)
                {
                    var state = (CellState)def.State;
                    if (state != CellState.Playable && !forceSpawnCells) continue;
                    CreateCellForHexGrid(def.Coordinates.x, def.Coordinates.y, def.Coordinates.z, (CellState)def.State);
                }
            }
            else
            {
                var gridSize = Mathf.Max(1, numEdgeCells - 1);
                for (var i = -gridSize; i < gridSize + 1; i += 1) {
                    for (var j = -gridSize; j < gridSize + 1; j += 1) {
                        for (var k = -gridSize; k < gridSize + 1; k += 1) {
                            if (i + j + k == 0) {
                                CreateCellForHexGrid(i, j, k);
                            }
                        }
                    }
                }  
            }
            
            RegisterCellNeighbors();
            GridInitialized?.Invoke();
        }

        private void CreateCellForHexGrid(int x, int y, int z, CellState? initialState = null)
        {
            var cell = Instantiate(hexCellPrefab);
            
            // This offset ensures coordinates remain in the positive numbers
            var coordOffset = numEdgeCells;
            var offsetX = x + coordOffset;
            var offsetY = y + coordOffset;
            var offsetZ = z + coordOffset;
            var key = new int3(offsetX, offsetY, offsetZ);
            Registry.Add(key, cell);
            
            cell.ApplyCoordinates(offsetX, offsetY, offsetZ);
            cell.name = $"Cell[{offsetX},{offsetY},{offsetZ}]";

            const float edgeW = GameConstants.HexMetrics.EdgeLength * 3f / 2f;
            var edgeH = GameConstants.HexMetrics.EdgeLength * Mathf.Sqrt(3) / 2f;
            
            var position = new Vector3(
                (-y + z) * edgeH, 
                0f, 
                x * edgeW);
            
            Transform cellTransform;
            (cellTransform = cell.transform).SetParent(cellsAnchor, false);
            cellTransform.localPosition = position;
            cell.SetLocalOrigin(position);

            if (initialState.HasValue)
            {
                cell.ApplyState(initialState.Value);
            }

            // Determine the column row of the cell
            var startingColumnIndex = Mathf.Abs(offsetX - numEdgeCells);
            if (_currentRow == -1 || _currentRow != offsetX)
            {
                _currentRow = offsetX;
                _indexInRow = 0;
            }

            var columnIndex = startingColumnIndex + _indexInRow;
            _indexInRow += 2;
            CellsByColumn[columnIndex].Add(cell);
        }

        private void RegisterCellNeighbors()
        {
            foreach (var kvp in Registry)
            {
                var cell = kvp.Value;
                for(var x = -1; x <= 1; x++)
                    for(var y = -1; y <= 1; y++)
                        for(var z = -1; z <= 1; z++)
                        {
                            var neighborKey = new int3(cell.Coordinates.x + x, cell.Coordinates.y + y, cell.Coordinates.z + z);
                            if (Registry.TryGetValue(neighborKey, out var neighbor) && neighbor != cell)
                            {
                                cell.RegisterNeighbor(neighbor);
                            }
                        }
            }
        }

        private void DestroyGrid(bool immediate = false)
        {
            foreach (var (_, cell) in Registry)
            {
                if (immediate)
                {
                    DestroyImmediate(cell.gameObject);
                }
                else
                {
                    Destroy(cell.gameObject);
                }
            }
            Registry.Clear();
            cellsAnchor.DestroyAllChildGameObjects(immediate);
        }
        
#if UNITY_EDITOR
        [ContextMenu("Clear Grid")]
        public void ForceClear() => DestroyGrid(true);
        [ContextMenu("Spawn Grid")]
        public void SpawnGrid() => Load(true);
        
        private void OnDrawGizmos()
        {
            if (!showGizmos)
            {
                return;
            }
            var style = new GUIStyle
            {
                normal =
                {
                    textColor = Color.white
                },
                alignment = TextAnchor.MiddleCenter
            };

            foreach (var kvp in Registry)
            {
                var cell = kvp.Value;
                var labelPosition = cell.transform.position;
                labelPosition.y = 1;
                Handles.Label(labelPosition, $"({cell.Coordinates.x}, {cell.Coordinates.y}, {cell.Coordinates.z})", style);
            }
        }
#endif
    }
}
