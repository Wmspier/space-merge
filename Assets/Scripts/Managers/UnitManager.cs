using System.Collections.Generic;
using Hex.Configuration;
using Hex.Grid;
using UnityEngine;

namespace Hex.Managers
{
    public class UnitManager : MonoBehaviour
    {
        [SerializeField] private HexGrid grid;
        [SerializeField] private HexUnitConfiguration config;
        [SerializeField] private Transform unitRoot;

        private readonly Dictionary<HexCell, HexUnit> _registry = new();

        public bool SpawnUnitOnCell(HexCell cell, string unitId)
        {
            var nullableData = config.GetUnitDataNullable(unitId);
            
            // Bad data, cannot spawn
            if (nullableData == null) return false;

            // Cell already contains unit, cannot spawn
            if (cell.Unit != null) return false;
            
            // Cell already exists in registry, cannot spawn
            if (_registry.ContainsKey(cell)) return false;

            // Instantiate, position, and apply data
            var unitInstance = Instantiate(nullableData.Prefab, unitRoot);
            unitInstance.transform.position = cell.Detail.Anchor.position;
            unitInstance.ApplyData(nullableData);

            // Add to registry
            _registry[cell] = unitInstance;
            
            return true;
        }
    }
}