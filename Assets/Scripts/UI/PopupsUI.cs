using Hex.Extensions;
using UnityEngine;

namespace Hex.UI
{
    public class PopupsUI : MonoBehaviour
    {
        [SerializeField] private Transform content;
        [SerializeField] private NonDrawingGraphic inputBlocker;

        public void AddChild(MonoBehaviour popup)
        {
            popup.transform.SetParent(content);
            popup.transform.Reset();
        }

        public void ToggleInputBlock(bool block) => inputBlocker.enabled = block;
    }
}