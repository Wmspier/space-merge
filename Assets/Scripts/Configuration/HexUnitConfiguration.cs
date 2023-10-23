using System;
using System.Collections.Generic;
using System.Linq;
using Hex.Grid;
using UnityEngine;

namespace Hex.Configuration
{
    [CreateAssetMenu(fileName = "Config_HexUnits", menuName = "Hex/Unit Config")]
    public class HexUnitConfiguration : ScriptableObject
    {
        [Serializable]
        public class HexUnitData
        {
            [SerializeField] private string id;
            [SerializeField] private HexUnit prefab;
            [SerializeField] private int baseHealth;

            public string Id => id;
            public HexUnit Prefab => prefab;
            public int BaseHealth => baseHealth;
        }

        [SerializeField] private List<HexUnitData> units;

        public HexUnitData GetUnitDataNullable(string id)
        {
            return units.FirstOrDefault(unit => unit.Id == id);
        }
    }
}