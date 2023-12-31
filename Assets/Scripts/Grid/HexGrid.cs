using System;
using System.Collections.Generic;
using System.Linq;
using Hex.Extensions;
using Hex.Grid.Cell;
using Hex.Managers;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Hex.Grid
{
    public class HexGrid : MonoBehaviour
    {
        public enum GridShape
        {
            Hex,
            Rectangle
        }

        [Header("Debug")]
        [SerializeField] public bool showGizmos;
        [SerializeField] public GridShape gridShape;

        [Header("Hex")]
        [SerializeField][Range(2, 10)] private int numEdgeCells;
        [SerializeField] private HexCell hexCellPrefab;

        [Space] 
        [SerializeField] private Transform cellsAnchor;

        public Action GridInitialized;
        
        public Dictionary<int, HexCell> Registry { get; } = new();

        #region Grid Accessing
         
        public HexCell GetCenterCell() => Registry[(numEdgeCells, numEdgeCells, numEdgeCells).GetHashCode()];
 
        public HexCell GetRandomCell()
        {
            var allCells = Registry.Values.ToList();
            return allCells[Random.Range(0, allCells.Count)];
        }
         
        public Dictionary<double, List<HexCell>> GetCellsByXPosRounded()
        {
            var cellsByXPosRounded = new Dictionary<double, List<HexCell>>();
            foreach (var (_, cell) in Registry)
            {
                var roundedXPos = Math.Round(cell.transform.position.x, 3);
                if (!cellsByXPosRounded.TryGetValue(roundedXPos, out var list))
                {
                    cellsByXPosRounded[roundedXPos] = new List<HexCell>();
                }
                
                cellsByXPosRounded[roundedXPos].Add(cell);
            }
 
            return cellsByXPosRounded;
        }
         
        #endregion

        public bool Load(GameMode mode = GameMode.Merge, bool immediateDestroy = false)
        {
            DestroyGrid(immediateDestroy);
            
            var savedGrid = LocalSaveManager.LoadGridFromDisk(mode);
            if (savedGrid == null)
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
            else
            {
                foreach (var cell in savedGrid)
                {
                    CreateCellForHexGrid(
                        cell.Coordinates.x, 
                        cell.Coordinates.y, 
                        cell.Coordinates.z,
                        cell.Detail);
                }
            }
            
            RegisterCellNeighbors();
            GridInitialized?.Invoke();
            return savedGrid != null;
        }
        
        private void CreateCellForHexGrid(int x, int y, int z, int? initialDetail = null)
        {
            var coordOffset = numEdgeCells;
            if (initialDetail.HasValue)
            {
                x -= coordOffset;
                y -= coordOffset;
                z -= coordOffset;
            }
            
            var cell = Instantiate(hexCellPrefab);
            var offsetX = x + coordOffset;
            var offsetY = y + coordOffset;
            var offsetZ = z + coordOffset;
            var key = (offsetX, offsetY, offsetZ).GetHashCode();
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
                            var neighborHash = (cell.Coordinates.x + x, cell.Coordinates.y + y, cell.Coordinates.z + z).GetHashCode();
                            if (Registry.TryGetValue(neighborHash, out var neighbor) && neighbor != cell)
                            {
                                cell.RegisterNeighbor(neighbor);
                            }
                        }
            }
        }

        public void Save(GameMode mode)=> LocalSaveManager.SerializeAndSaveGridToDisk(mode, GetCellDefinitions());

        private List<HexCellDefinition> GetCellDefinitions()
        {
            var definitions = new List<HexCellDefinition>();
            foreach (var (_, cell) in Registry)
            {
                definitions.Add(new HexCellDefinition
                {
                    Coordinates = cell.Coordinates
                });
            }

            return definitions;
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
        public void SpawnGrid() => Load(GameMode.Merge, true);
        
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
                switch (gridShape)
                {
                    case GridShape.Hex:
                    {
                        Handles.Label(labelPosition, $"({cell.Coordinates.x}, {cell.Coordinates.y}, {cell.Coordinates.z})", style);
                        break;
                    }
                    case GridShape.Rectangle:
                    {
                        Handles.Label(labelPosition, $"({cell.Coordinates.x}, {cell.Coordinates.z})", style);
                        break;
                    }
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }
        #endif
    }
}
