using System;
using System.Collections.Generic;
using System.Linq;
using Hex.Grid;
using Hex.UI;
using UnityEngine;

namespace Hex.Configuration
{
    [CreateAssetMenu(fileName = "Config_HexDetails", menuName = "Hex/Detail Config")]
    public class HexDetailConfiguration : ScriptableObject
    {
        [Serializable]
        public class DetailPointConfig
        {
            [SerializeField] private MergeCellDetailType type;
            [SerializeField] private ResourceType resource;
            [SerializeField] private int points;

            public MergeCellDetailType Type => type;
            public ResourceType Resource => resource;
            public int Points => points;
        }

        [Serializable]
        public class SpecialDetail
        {
            [SerializeField] private MergeCellDetailType type;
            [SerializeField] private GameObject prefab;
            [SerializeField] private List<MergeCellDetailType> components;

            public MergeCellDetailType Type => type;
            public GameObject Prefab => prefab;
            public IReadOnlyList<MergeCellDetailType> Components => components;
        }

        [Serializable]
        public class EffectDetail
        {
            [SerializeField] private string id;
            [SerializeField] private GameObject prefab;
            [SerializeField] private float lifetimeSeconds;

            public string Id => id;
            public GameObject Prefab => prefab;
            public float LifetimeSeconds => lifetimeSeconds;
        }
        
        [SerializeField] private List<DetailPointConfig> detailPoints;
        [SerializeField] private List<GameObject> basicDetailPrefabs;
        [SerializeField] private List<SpecialDetail> specialDetails;
        [SerializeField] private List<EffectDetail> effectDetails;
        [SerializeField] private List<float> extraTileMultipliers;

        private readonly Dictionary<MergeCellDetailType, (ResourceType resource, int amount)> _pointsByType = new();
        private bool _cacheInitialized;

        public IReadOnlyList<GameObject> BasicDetails => basicDetailPrefabs;
        public IEnumerable<SpecialDetail> SpecialDetails => specialDetails;

        private void InitializeCache()
        {
            foreach (var detail in detailPoints)
            {
                _pointsByType[detail.Type] = (detail.Resource, detail.Points);
            }
        }

        public (ResourceType resource, int amount) GetPointsForType(MergeCellDetailType type)
        {
            if (!_cacheInitialized)
            {
                InitializeCache();
            }
            
            _pointsByType.TryGetValue(type, out var points);
            return points;
        }

        public float GetMultiplierForCombination(int combinedTiles)
        {
            var extraTiles = combinedTiles - 3;
            return extraTiles <= 0 ? 1f : extraTileMultipliers[Mathf.Min(extraTiles - 1, extraTileMultipliers.Count - 1)];
        }

        public EffectDetail GetEffectDetailNullable(string id)
        {
            return effectDetails.FirstOrDefault(d => d.Id == id);
        }
    }
}