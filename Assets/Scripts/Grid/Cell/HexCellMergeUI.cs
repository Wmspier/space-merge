using TMPro;
using UnityEngine;

namespace Hex.Grid.Cell
{
	public class HexCellMergeUI : MonoBehaviour
	{
		[SerializeField] private GameObject powerOnlyRoot;
		[SerializeField] private TMP_Text powerOnlyNewPowerText;
		
		[SerializeField] private GameObject shieldOnlyRoot;
		[SerializeField] private TMP_Text shieldOnlyNewShieldText;

		[SerializeField] private GameObject powerAndShieldRoot;
		[SerializeField] private TMP_Text newPowerText;
		[SerializeField] private TMP_Text newShieldText;
		
		[SerializeField] private TMP_Text upgradeText;

		public void ShowWithInfo(int newPower, int newShield, bool isUpgrade)
		{
			powerOnlyRoot.SetActive(newPower > 0 && newShield <= 0);
			shieldOnlyRoot.SetActive(newShield > 0 && newPower <= 0);
			powerAndShieldRoot.SetActive(newPower > 0 && newShield > 0);
			upgradeText.gameObject.SetActive(isUpgrade);

			powerOnlyNewPowerText.text = newPower.ToString();
			newPowerText.text = newPower.ToString();
			
			shieldOnlyNewShieldText.text = newShield.ToString();
			newShieldText.text = newShield.ToString();
		}
	}
}