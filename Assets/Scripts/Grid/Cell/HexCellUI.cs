using System.Collections.Generic;
using DG.Tweening;
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
		[SerializeField] private CanvasGroup attackCanvasRoot;
		[SerializeField] private TMP_Text enemyPowerText;
		[SerializeField] private TMP_Text playerPowerText;
		[SerializeField] private TMP_Text resultText;
		[SerializeField] private UnityEngine.UI.Image resultDirection;

		private int cachedPlayerPower;
		private int cachedEnemyPower;
		
		private readonly RaycastHit[] _hitBuffer = new RaycastHit[10];
		
		public void ToggleMergeCanvas(bool visible) => mergeCanvasRoot.SetActive(visible);
		public void ToggleUnitInfoCanvas(bool visible) => unitInfoCanvasRoot.SetActive(visible);

		public void ToggleAttackCanvas(bool visible)
		{
			attackCanvasRoot.DOFade(visible ? 1 : 0, .5f);
			PositionAttackCanvas();
		}

		private void Awake()
		{
			if(mergeCanvasRoot != null) ToggleMergeCanvas(false);
			if(attackCanvasRoot != null) ToggleAttackCanvas(false);
		}

		private void PositionAttackCanvas()
		{
			var hitCount = Physics.RaycastNonAlloc(transform.position, Vector3.up, _hitBuffer);
			
			for (var i = 0; i < hitCount; i++)
			{
				var hit = _hitBuffer[i];
				if (!hit.collider.CompareTag("AttackPlane")) continue;

				attackCanvasRoot.transform.position = hit.point;
			}
		}

		public void SetEnemyAttackPower(int power)
		{
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
				// resultText.color = playerPowerText.color;
				var color = playerPowerText.color;
				color.a = .5f;
				resultDirection.color = color;
				resultDirection.transform.rotation = Quaternion.Euler(45, 0, -90);
			}
			// Enemy winning
			else
			{
				resultDirection.gameObject.SetActive(true);
				// resultText.color = enemyPowerText.color;
				var color = enemyPowerText.color;
				color.a = .5f;
				resultDirection.color = color;
				resultDirection.transform.rotation = Quaternion.Euler(45, 0, 90);
			}
		}
	}
}