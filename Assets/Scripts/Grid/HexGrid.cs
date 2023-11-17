using System;
using System.Collections.Generic;
using Hex.Managers;
using UnityEditor;
using UnityEngine;

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

        public Dictionary<int, HexCell> Registry { get; } = new();

        #if UNITY_EDITOR
        // private void OnValidate()
        // {
        //     void CleanupAndRegen()
        //     {
        //         if (this == null)
        //         {
        //             return;
        //         }
        //         var tempList = cellsAnchor.Cast<Transform>().ToList();
        //         foreach(var child in tempList)
        //         {
        //             DestroyImmediate(child.gameObject);
        //         }
        //         Registry.Clear();
        //         GenerateGrid();
        //         EditorApplication.delayCall -= CleanupAndRegen;
        //     }
        // 
        //     EditorApplication.delayCall += CleanupAndRegen;
        // }
        #endif

        public HexCell GetCenterCell() => Registry[(numEdgeCells, numEdgeCells, numEdgeCells).GetHashCode()];
        
        public bool Load(GameMode mode)
        {
            Destroy();

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
            if (initialDetail.HasValue)
            {
                cell.Detail.SetType((MergeCellDetailType)initialDetail);
            }

            const float edgeW = GameConstants.HexMetrics.EdgeLength * 3f / 2f;
            var edgeH = GameConstants.HexMetrics.EdgeLength * Mathf.Sqrt(3) / 2f;
            
            var position = new Vector3(
                x * edgeW, 
                0f, 
                (-y + z) * edgeH);
            
            Transform cellTransform;
            (cellTransform = cell.transform).SetParent(cellsAnchor, false);
            cellTransform.localPosition = position;
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
                    Coordinates = cell.Coordinates,
                    Detail = (int)cell.Detail.Type
                });
            }

            return definitions;
        }

        private void Destroy()
        {
            foreach (var (_, cell) in Registry)
            {
                Destroy(cell.gameObject);
            }
            Registry.Clear();
        }

        public void Empty()
        {
            foreach (var (_, cell) in Registry)
            {
                cell.Detail.SetType(MergeCellDetailType.Empty);
            }
        }

#if UNITY_EDITOR
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
