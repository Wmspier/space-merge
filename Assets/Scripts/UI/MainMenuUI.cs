using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Hex.UI
{
    public class MainMenuUI : MonoBehaviour
    {
        [SerializeField] private List<BottomBarTab> tabs;
        [SerializeField] private BasicButton playButton;
        [SerializeField] private GameObject playButtonRoot;
        [SerializeField] private TopBarUI topBarUI;

        public Action PlayPressed;
        
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

            playButton.Button.onClick.AddListener(() => PlayPressed?.Invoke());
        }

        private void OnEnable() 
        {
            // topBarUI.ToggleResourceBar(ResourceType.CoinGold, true);
            // topBarUI.ToggleResourceBar(ResourceType.CoinSilver, false); 
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