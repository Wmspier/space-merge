using System;
using System.Threading.Tasks;
using Hex.Model;
using UnityEngine;
using UnityEngine.UI;

namespace Hex.UI.Popup
{
    public class TownCompletePopup : MonoBehaviour
    {
        [SerializeField] private ResourceBar totalScoreResource;
        [SerializeField] private Button claimButton;
        [SerializeField] private Sprite goldCoinSprite;

        public Action ClaimPressed;
        public ResourceBar TotalScore => totalScoreResource;

        private void Awake()
        {
            claimButton.onClick.AddListener(() => ClaimPressed?.Invoke());
        }

        public async Task DoClaim(TopBarUI topBarUI, ResourcesModel model)
        {
            topBarUI.ToggleResourceBar(ResourceType.CoinSilver, false);
            topBarUI.ToggleResourceBar(ResourceType.CoinGold, true);

            await totalScoreResource.DoTransform(goldCoinSprite);
            totalScoreResource.SetScoreImmediate(0, true);
            
            await topBarUI.AddResource(ResourceType.CoinGold, model.ResourceAmounts[ResourceType.CoinSilver], totalScoreResource.transform.position);
            await Task.Delay(1000);
        }
    }
}