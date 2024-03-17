using TMPro;
using UnityEngine;

namespace Hex.Enemy
{
	public class EnemyCountUI : MonoBehaviour
	{
		[SerializeField] private TMP_Text _countText;
		
		public void UpdateCount(int count) => _countText.text = count.ToString();
	}
}