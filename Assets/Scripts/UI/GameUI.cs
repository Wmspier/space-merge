using System;
using UnityEngine;

namespace Hex.UI
{
    public class GameUI : MonoBehaviour
    {
        [SerializeField] private TopBarUI topBarUI;
        [SerializeField] private DeckQueueUI _queueUI;

        public Action ResetPressed;
        
        public TopBarUI TopBar => topBarUI;
        public DeckQueueUI PreviewQueueUI => _queueUI;
    }
}