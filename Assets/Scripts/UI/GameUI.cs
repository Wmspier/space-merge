using System;
using UnityEngine;

namespace Hex.UI
{
    public class GameUI : MonoBehaviour
    {
        [SerializeField] private TopBarUI topBarUI;
        [SerializeField] private DeckQueueUI _deckQueueUI;

        public Action ResetPressed;
        
        public TopBarUI TopBar => topBarUI;
        public DeckQueueUI DeckPreviewQueue => _deckQueueUI;
    }
}