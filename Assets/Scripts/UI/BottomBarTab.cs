using System;
using UnityEngine;
using UnityEngine.UI;

namespace Hex.UI
{
    public enum MainGameContext
    {
        Build,
        Shop,
        Leaderboard
    }
    
    [RequireComponent(typeof(Animation), typeof(Button))]
    public class BottomBarTab : MonoBehaviour
    {
        [SerializeField] private MainGameContext context;
        
        private Animation _animation;
        private bool _selected;

        public Action<MainGameContext, BottomBarTab> Pressed;
        public MainGameContext Context => context;

        private void Awake()
        {
            GetComponent<Button>().onClick.AddListener(OnClicked);
        }

        public void Select(bool immediate = false)
        {
            (_animation ? _animation : GetComponent<Animation>()).Play(immediate ? "anm_ui_bottom_tab_select_immediate" : "anm_ui_bottom_tab_select");
            _selected = true;
        }

        public void Deselect()
        {
            if (!_selected)
            {
                return;
            }

            (_animation ? _animation : GetComponent<Animation>()).Play("anm_ui_bottom_tab_deselect");
            _selected = false;
        }

        private void OnClicked()
        {
            if (_selected)
            {
                return;
            }
            
            Pressed?.Invoke(context, this);
            if (!_selected)
            {
                Select();
            }
        }
    }
}