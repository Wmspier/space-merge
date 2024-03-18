using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;

namespace Hex.Grid.Cell
{
	public class HexCellUI : MonoBehaviour
	{
		[Header("Merge UI")]
		[SerializeField] private HexCellMergeUI mergeUI;
		
		[Header("Unit Info UI")] 
		[SerializeField] private GameObject unitInfoCanvasRoot;
		[SerializeField] private TMP_Text powerText;
		[SerializeField] private GameObject powerRoot;
		[SerializeField] private TMP_Text shieldText;
		[SerializeField] private GameObject shieldRoot;
		[SerializeField] private List<GameObject> rarityObjects;
		
		[Header("Attack UI")]
		[SerializeField] private CanvasGroup attackCanvasRoot;
		[SerializeField] private TMP_Text enemyPowerText;
		[SerializeField] private TMP_Text playerPowerText;
		[SerializeField] private TMP_Text resultText;
		[SerializeField] private UnityEngine.UI.Image resultDirection;

		private int _cachedPlayerPower;
		private int _cachedPlayerShield;
		private int _cachedEnemyPower;
		
		private readonly RaycastHit[] _hitBuffer = new RaycastHit[10];
		
		public void ToggleMergeCanvas(bool visible) => mergeUI.gameObject.SetActive(visible);
		public void ToggleUnitInfoCanvas(bool visible) => unitInfoCanvasRoot.SetActive(visible);

		public void ToggleAttackCanvas(bool visible)
		{
			attackCanvasRoot.DOFade(visible ? 1 : 0, .5f);
			PositionAttackCanvas();
		}

		private void Awake()
		{
			if(mergeUI != null) ToggleMergeCanvas(false);
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
			_cachedEnemyPower = power;
			
			UpdateAttackDisplay();
		}

		public void SetPlayerPower(int power)
		{
			powerRoot.gameObject.SetActive(power > 0);
			powerText.text = power.ToString();
			_cachedPlayerPower = power;

			UpdateAttackDisplay();
		}
		
		public void SetPlayerShield(int shield)
		{
			shieldRoot.gameObject.SetActive(shield > 0);
			shieldText.text = shield.ToString();
			_cachedPlayerShield = shield;
		}

		public void SetRarityBaseZero(int rarityIndex)
		{
			for (var i = 0; i < rarityObjects.Count; i++)
			{
				rarityObjects[i].SetActive(rarityIndex >= i);
			}
		}

		public void SetMergeInfo(int finalPower, int finalShield, bool resultsInUpgrade)
		{
			mergeUI.ShowWithInfo(finalPower, finalShield, resultsInUpgrade);
		}

		private void UpdateAttackDisplay()
		{
			// Cell does not contain enemy attack so don't show attack info
			if (attackCanvasRoot == null) return;
			
			playerPowerText.text = _cachedPlayerPower.ToString();
			enemyPowerText.text = _cachedEnemyPower.ToString();

			var powerDiff = _cachedPlayerPower - _cachedEnemyPower;

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
				var color = playerPowerText.color;
				color.a = .5f;
				resultDirection.color = color;
				resultDirection.transform.rotation = Quaternion.Euler(45, 0, -90);
			}
			// Enemy winning
			else
			{
				resultDirection.gameObject.SetActive(true);
				var color = enemyPowerText.color;
				color.a = .5f;
				resultDirection.color = color;
				resultDirection.transform.rotation = Quaternion.Euler(45, 0, 90);
			}
		}
	}
}