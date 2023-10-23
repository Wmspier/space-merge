using System;
using UnityEngine;
using UnityEngine.UI;

namespace Hex.UI
{
    public class GameUI : MonoBehaviour
    {
        [SerializeField] private TopBarUI topBarUI;
        [SerializeField] private CellDetailQueueUI cellDetailQueueUI;
        [SerializeField] private Button resetButton;

        public Action ResetPressed;
        
        public TopBarUI TopBar => topBarUI;
        public CellDetailQueueUI DetailQueue => cellDetailQueueUI;

        private void Awake()
        {
            resetButton.onClick.AddListener(() => ResetPressed?.Invoke());
        }

        private void OnEnable()
        {
            topBarUI.ToggleResourceBar(ResourceType.CoinGold, false);
            topBarUI.ToggleResourceBar(ResourceType.CoinSilver, true);
        }
    }
}