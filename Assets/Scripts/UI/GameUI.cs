using System;
using UnityEngine;
using UnityEngine.UI;

namespace Hex.UI
{
    public class GameUI : MonoBehaviour
    {
        [SerializeField] private TopBarUI topBarUI;
        [SerializeField] private DeckQueueUI _deckQueueUI;
        [SerializeField] private Button resetButton;

        public Action ResetPressed;
        
        public TopBarUI TopBar => topBarUI;
        public DeckQueueUI DetailQueue => _deckQueueUI;

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