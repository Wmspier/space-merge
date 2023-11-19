using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Hex.Grid.Cell
{
	public class HexCellUI : MonoBehaviour
	{
		[Header("Merge UI")]
		[SerializeField] private GameObject mergeCanvasRoot;
		[SerializeField] private TMP_Text newPowerText;
		[SerializeField] private TMP_Text upgradeText;
		
		[Header("Unit Info UI")] 
		[SerializeField] private GameObject unitInfoCanvasRoot;
		[SerializeField] private TMP_Text powerText;
		[SerializeField] private List<GameObject> rarityObjects;
		
		public void ToggleMergeCanvas(bool visible) => mergeCanvasRoot.SetActive(visible);
		public void ToggleUnitInfoCanvas(bool visible) => unitInfoCanvasRoot.SetActive(visible);

		private void Awake()
		{
			if(mergeCanvasRoot != null) ToggleMergeCanvas(false);
		}

		public void SetPower(int power)
		{
			if (power <= 0)
			{
				powerText.gameObject.SetActive(false);
				return;
			}
			
			powerText.gameObject.SetActive(true);
			powerText.text = power.ToString();
		}

		public void SetRarityBaseZero(int rarityIndex)
		{
			for (var i = 0; i < rarityObjects.Count; i++)
			{
				rarityObjects[i].SetActive(rarityIndex >= i);
			}
		}

		public void SetMergeInfo(int finalPower, bool resultsInUpgrade)
		{
			newPowerText.text = finalPower.ToString();
			upgradeText.gameObject.SetActive(resultsInUpgrade);
		}
	}
}