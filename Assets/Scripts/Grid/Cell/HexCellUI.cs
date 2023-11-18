using TMPro;
using UnityEngine;

namespace Hex.Grid.Cell
{
	public class HexCellUI : MonoBehaviour
	{
		[Header("Merge UI")]
		[SerializeField] private GameObject mergeCanvasRoot;
		[SerializeField] private GameObject canCombineRoot;
		[SerializeField] private GameObject cannotCombineRoot;
		[SerializeField] private TMP_Text matchCountText;
		[SerializeField] private TMP_Text matchRequirementText;
		
		[Header("Unit Info UI")] 
		[SerializeField] private GameObject unitInfoCanvasRoot;
		[SerializeField] private TMP_Text powerText;
		
		public void ToggleMergeCanvas(bool visible) => mergeCanvasRoot.SetActive(visible);
		public void ToggleUnitInfoCanvas(bool visible) => unitInfoCanvasRoot.SetActive(visible);
		
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
		
		public void SetMatchCount(int count, int req)
		{
			matchCountText.text = count.ToString();
			matchRequirementText.text = req.ToString();
		}

		public void ToggleCanCombine(bool canCombine)
		{
			canCombineRoot.SetActive(canCombine);
			cannotCombineRoot.SetActive(!canCombine);
		}
	}
}