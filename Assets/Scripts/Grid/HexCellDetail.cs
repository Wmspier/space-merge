using System;
using System.Linq;
using System.Threading.Tasks;
using Hex.Configuration;
using Hex.Extensions;
using UnityEngine;

namespace Hex.Grid
{
    public class HexCellDetail : MonoBehaviour
    {
        [SerializeField] private Transform anchor;
        [SerializeField] private HexDetailConfiguration config;
        
        private Vector3 _anchorOrigin;
        
        public MergeCellDetailType Type { get; private set; } = MergeCellDetailType.Empty;
        
        public Transform Anchor => anchor;

        private void Awake() => _anchorOrigin = anchor.localPosition;

        public void SetType(MergeCellDetailType type)
        {
            anchor.DestroyAllChildGameObjects();

            switch (type)
            {
                case MergeCellDetailType.Empty:
                    Type = MergeCellDetailType.Empty;
                    return;
            }

            if (type.IsBasic())
            {
                if ((int)type > config.BasicDetails.Count)
                {
                    throw new ArgumentOutOfRangeException(nameof(MergeCellDetailType), $"No registered prefab for basic type - {type}");
                }
                Instantiate(config.BasicDetails[(int)type], anchor);
            }
            else if (type.IsSpecial())
            {
                var specialDetail = config.SpecialDetails.FirstOrDefault(special => special.Type == type);
                if (specialDetail == null)
                {
                    throw new ArgumentOutOfRangeException(nameof(MergeCellDetailType), $"No registered prefab for special type - {type}");
                }
                Instantiate(specialDetail.Prefab, anchor);
            }

            Type = type;
        }

        public void Clear()
        {
            anchor.localPosition = _anchorOrigin;
            SetType(MergeCellDetailType.Empty);
        }
        
        
        public async void SpawnEffect(string id)
        {
            var effect = config.GetEffectDetailNullable(id);
            
            if (effect == null || effect.Prefab == null) return;

            var effectInstance = Instantiate(effect.Prefab, transform);
            effectInstance.transform.position = Anchor.position;

            await Task.Delay((int)effect.LifetimeSeconds * 1000);
            
            Destroy(effectInstance);
        }
    }
}