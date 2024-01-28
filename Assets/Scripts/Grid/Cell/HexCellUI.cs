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
		
		[Header("Attack UI")]
		[SerializeField] private GameObject attackCanvasRoot;
		[SerializeField] private TMP_Text enemyPowerText;
		[SerializeField] private TMP_Text playerPowerText;
		[SerializeField] private TMP_Text resultText;
		[SerializeField] private UnityEngine.UI.Image resultDirection;
		[SerializeField] private GameObject attackTarget;

		private int cachedPlayerPower;
		private int cachedEnemyPower;
		
		public void ToggleMergeCanvas(bool visible) => mergeCanvasRoot.SetActive(visible);
		public void ToggleUnitInfoCanvas(bool visible) => unitInfoCanvasRoot.SetActive(visible);

		public void ToggleAttackCanvas(bool visible)
		{
			attackCanvasRoot.SetActive(visible);
			attackTarget.SetActive(visible);
		}

		private void Awake()
		{
			if(mergeCanvasRoot != null) ToggleMergeCanvas(false);
			if(attackCanvasRoot != null) ToggleAttackCanvas(false);
		}

		public void SetEnemyAttackPower(int power)
		{
			if (power <= 0)
			{
				enemyPowerText.gameObject.SetActive(false);
				return;
			}
			
			enemyPowerText.gameObject.SetActive(true);
			enemyPowerText.text = power.ToString();
			cachedEnemyPower = power;
			
			UpdateAttackDisplay();
		}

		public void SetPlayerPower(int power)
		{
			powerText.gameObject.SetActive(true);
			powerText.text = power.ToString();
			powerText.gameObject.SetActive(power > 0);
			cachedPlayerPower = power;

			UpdateAttackDisplay();
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

		private void UpdateAttackDisplay()
		{
			// Cell does not contain enemy attack so don't show attack info
			if (attackCanvasRoot == null) return;
			
			playerPowerText.text = cachedPlayerPower.ToString();
			enemyPowerText.text = cachedEnemyPower.ToString();

			var powerDiff = cachedPlayerPower - cachedEnemyPower;

			resultText.text = Mathf.Abs(powerDiff).ToString();
			
			// Tie
			if (powerDiff == 0)
			{
				resultDirection.gameObject.SetActive(false);
				resultText.color = Color.white;
			}
			// Player winning
			else if (powerDiff > 0)
			{
				resultDirection.gameObject.SetActive(true);
				resultText.color = playerPowerText.color;
				resultDirection.color = playerPowerText.color;
				resultDirection.transform.rotation = Quaternion.Euler(0, 0, 0);
			}
			// Enemy winning
			else
			{
				resultDirection.gameObject.SetActive(true);
				resultText.color = enemyPowerText.color;
				resultDirection.color = enemyPowerText.color;
				resultDirection.transform.rotation = Quaternion.Euler(180, 0, 0);
			}
		}
	}
}