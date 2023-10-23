using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Hex.UI
{
    public class MainMenuUI : MonoBehaviour
    {
        [SerializeField] private List<BottomBarTab> tabs;
        [SerializeField] private BasicButton mergeButton;
        [SerializeField] private BasicButton battleButton;
        [SerializeField] private GameObject playButtonRoot;
        [SerializeField] private TopBarUI topBarUI;

        public Action<GameMode> PlayPressed;
        
        private void Awake()
        {
            foreach (var tab in tabs)
            {
                tab.Pressed = OnTabPressed;
                if (tab.Context == MainGameContext.Build)
                {
                    tab.Select(true);
                }
            }

            mergeButton.Button.onClick.AddListener(() => PlayPressed?.Invoke(GameMode.Merge));
            battleButton.Button.onClick.AddListener(() => PlayPressed?.Invoke(GameMode.Battle));
        }

        private void OnEnable() 
        {
            topBarUI.ToggleResourceBar(ResourceType.CoinGold, true);
            topBarUI.ToggleResourceBar(ResourceType.CoinSilver, false); 
        }

        private void OnTabPressed(MainGameContext context, BottomBarTab pressedTab)
        {
            foreach (var tab in tabs.Where(tab => tab != pressedTab))
            {
                tab.Deselect();
            }

            playButtonRoot.SetActive(context == MainGameContext.Build);
        }
    }
}