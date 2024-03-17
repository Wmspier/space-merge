using System;
using UnityEngine;
using UnityEngine.UI;

namespace Hex.UI.Popup
{
	public class PopupBattleResult : MonoBehaviour
	{
		[SerializeField] private GameObject _victoryTitle;
		[SerializeField] private GameObject _defeatTitle;
		[SerializeField] private Button _continueButton;

		public void ShowAsVictory(Action continueCallback)
		{
			_defeatTitle.SetActive(false);
			_victoryTitle.SetActive(true);
			
			_continueButton.onClick.RemoveAllListeners();
			_continueButton.onClick.AddListener(() => continueCallback?.Invoke());
		}
		
		public void ShowAsDefeat(Action continueCallback)
		{
			_defeatTitle.SetActive(true);
			_victoryTitle.SetActive(false);
			
			_continueButton.onClick.RemoveAllListeners();
			_continueButton.onClick.AddListener(() => continueCallback?.Invoke());
		}
	}
}