using System.Collections;
using Hex.UI;
using UnityEngine;

namespace Hex.Managers
{
    public class NavigationManager : MonoBehaviour
    {
        [SerializeField] private TopBarUI topBarUI;
        [SerializeField] private MainMenuUI mainMenuUI;
        [SerializeField] private GameObject splashUI;
        [SerializeField] private GameObject gridRoot;

        [SerializeField] private BattleContext _battleContext;
        
        private void Awake()
        {
            ApplicationManager.RegisterResource(this);
            splashUI.SetActive(true);
            
            mainMenuUI.PlayPressed = GoToBattle;
            topBarUI.HomePressed = GoToMainMenu;
        }

        private IEnumerator Start()
        {
            yield return new WaitForEndOfFrame();
            splashUI.SetActive(false);
        }

        private void GoToBattle()
        {
            mainMenuUI.gameObject.SetActive(false);
            gridRoot.SetActive(true);
            _battleContext.StartBattle();
            
            topBarUI.ToggleHomeButton(true);
        }

        public void GoToMainMenu()
        {
            mainMenuUI.gameObject.SetActive(true);
            gridRoot.SetActive(false);
            _battleContext.Dispose();
            
            topBarUI.ToggleHomeButton(false);
        }
    }
}