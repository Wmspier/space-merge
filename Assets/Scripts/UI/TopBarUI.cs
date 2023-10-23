using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Hex.Extensions;
using Hex.Grid;
using Hex.Util;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Hex.UI
{
    public class TopBarUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text scoreAdditionPrefab;
        [SerializeField] private Camera gridCamera;
        [SerializeField] private Transform additionOrigin;
        [SerializeField] private Button homeButton;

        [Header("Animation")] 
        [SerializeField] private AnimationCurve scoreAdditionScale;
        [SerializeField] private AnimationCurve scoreAdditionInterpolation;
        [SerializeField] private float scoreAdditionAnimDuration;
        [SerializeField] private float scoreAdditionAnimDurationWithMultiplier;

        [Header("Resources")] 
        [SerializeField] private List<ResourceBar> resources;

        private readonly Dictionary<ResourceType, ResourceBar> _resourceByType = new();

        private Coroutine _incrementCoroutine;
        
        public Action HomePressed;

        private void Awake()
        {
            homeButton.onClick.AddListener(() => HomePressed?.Invoke());
            foreach (var resource in resources)
            {
                _resourceByType[resource.Type] = resource;
            }
        }

        public void ToggleHomeButton(bool visible) => homeButton.gameObject.SetActive(visible);
        
        public void ToggleResourceBar(ResourceType type, bool visible)
        {
            if (!_resourceByType.TryGetValue(type, out var resource))
            {
                Debug.LogWarning($"Failed to find resource for top bar addition: {type}");
                return;
            }
            resource.gameObject.SetActive(visible);
        }

        public void SetResourceImmediate(ResourceType type, int amount)
        {
            if (_resourceByType.TryGetValue(type, out var bar))
            {
                bar.SetScoreImmediate(amount);
            }
        }
        
        public async void AddResourceFromTile(HexCell cell, int combinedTiles, ResourceType resource, int scoreForType, float multiplierForCombine, int toAdd)
        {
            // No points added, don't instantiate UI
            if (toAdd == 0)
            {
                return;
            }
            
            // Get screen position from cell
            var newAddition = Instantiate(scoreAdditionPrefab, additionOrigin);
            newAddition.text = FormatScore(scoreForType, multiplierForCombine, combinedTiles, cell.Detail.Type, resource);
            var rectTransform = newAddition.GetComponent<RectTransform>();
            rectTransform.position = gridCamera.WorldToScreenPoint(cell.transform.position);

            await DoResourceAddition(resource, rectTransform.position, rectTransform, toAdd, newAddition, combinedTiles > 3);
        }

        public async Task AddResource(ResourceType resource, int toAdd, Vector3 origin)
        {
            // No points added, don't instantiate UI
            if (toAdd == 0)
            {
                return;
            }
            
            // Get screen position from cell
            var newAddition = Instantiate(scoreAdditionPrefab, additionOrigin);
            newAddition.text = FormatScore(toAdd, resource);
            var rectTransform = newAddition.GetComponent<RectTransform>();

            await DoResourceAddition(resource, origin, rectTransform, toAdd, newAddition, false);
        }

        private async Task DoResourceAddition(ResourceType type, Vector3 origin, Transform rectTransform, int toAdd, Component newAddition, bool hasMulti)
        {
            if (!_resourceByType.TryGetValue(type, out var resource))
            {
                Debug.LogError($"Failed to find resource for top bar addition: {type}");
                return;
            }
            
            // Interpolate the score addition
            var startPosition = origin;
            var endPosition = resource.transform.position;
            var duration = hasMulti ? scoreAdditionAnimDurationWithMultiplier : scoreAdditionAnimDuration;
            await MathUtil.DoInterpolation(duration, InterpolateScoreAddition, (.85f, () => resource.Pulse()));
            Destroy(newAddition.gameObject);

            //resource.Pulse();
            
            // Increment score
            resource.AdjustScore(toAdd);

            void InterpolateScoreAddition(float progress)
            {
                rectTransform.position = MathUtil.SmoothLerp(startPosition, endPosition, scoreAdditionInterpolation.Evaluate(progress));

                // Also adjust the scale
                var scale = scoreAdditionScale.Evaluate(progress);
                rectTransform.localScale = new Vector3(scale, scale, scale);
            }
        }

        public async void Clear()
        {
            while (_incrementCoroutine != null)
            {
                await Task.Delay(10);
            }
            
            foreach (var (_, resource) in _resourceByType)
            {
                resource.SetScoreImmediate(0);
            }
        }

        private static string FormatScore(int flatScore, float multiplier, int combinedTiles, MergeCellDetailType type, ResourceType resource)
        {
            if (type.IsSpecial())
            {
                return type == MergeCellDetailType.Castle 
                    ? $"Castle!\n<size=75%><sprite={(int)resource}>{flatScore}" 
                    : $"Special Tile!\n<size=75%><sprite={(int)resource}>{flatScore}";
            }
            return combinedTiles switch
            {
                4 => $"Extra Tile!\n<size=75%><sprite={(int)resource}>{flatScore} <size=100%>x{multiplier}",
                > 4 => $"Extra Tiles!\n<size=75%><sprite={(int)resource}>{flatScore} <size=100%>x{multiplier}",
                _ => $"<size=75%><sprite={(int)resource}>{flatScore}"
            };
        }

        private static string FormatScore(int score, ResourceType resource) => $"<size=75%><sprite={(int)resource}>{score}";
    }
}