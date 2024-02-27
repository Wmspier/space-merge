using System;
using UnityEngine;

namespace Hex.UI
{
    public class BattleUI : MonoBehaviour
    {
        [field: SerializeField] public TopBarUI TopBarUI { get; private set; }
        [field: SerializeField] public DeckQueueUI QueueUI { get; private set; }
        [field: SerializeField] public GridMoveUI MoveUI { get; private set; }

        public Action ResetPressed;
    }
}