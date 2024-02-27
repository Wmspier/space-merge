using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Hex.UI
{
	public class GridMoveUI : MonoBehaviour
	{
		[SerializeField] private TMP_Text _countText;
		[SerializeField] private Image _countBackground;
		[SerializeField] private Image _icon;
		
		public void Initialize(int moveCount)
		{
			SetCount(moveCount);
		}

		public void SetCount(int amount)
		{
			_countText.text = amount.ToString();
		}
	}
}