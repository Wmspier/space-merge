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

        private void Awake()
        {
            ApplicationManager.RegisterResource(this);
            splashUI.SetActive(true);
            
            mainMenuUI.PlayPressed = GoToHexGame;
            topBarUI.HomePressed = GoToMainMenu;
        }

        private IEnumerator Start()
        {
            yield return new WaitForEndOfFrame();
            splashUI.SetActive(false);
        }

        private void GoToHexGame(GameMode mode)
        {
            ApplicationManager.Model.ActiveMode = mode;
            
            mainMenuUI.gameObject.SetActive(false);
            gridRoot.SetActive(true);
            ApplicationManager.GetGameManager().Initialize();
            
            topBarUI.ToggleHomeButton(true);
        }

        public void GoToMainMenu()
        {
            mainMenuUI.gameObject.SetActive(true);
            gridRoot.SetActive(false);
            ApplicationManager.GetGameManager().Dispose();
            
            topBarUI.ToggleHomeButton(false);
        }
    }
}