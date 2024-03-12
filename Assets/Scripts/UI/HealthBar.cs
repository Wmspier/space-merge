using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;

namespace Hex.UI
{
	public class HealthBar : MonoBehaviour
	{
		private const string DamagePointerSprite = "<sprite=\"Battle\" name=\"damage_pointer\">";
		
		[SerializeField] private RectTransform _fillBar;
		[SerializeField] private RectTransform _secondaryFillBar;
		[SerializeField] private RectTransform _previewFillBar;
		[SerializeField] private TMP_Text _previewText;
		[SerializeField] private GameObject _previewTextAnchor;
		[SerializeField] private Color _previewTextColor;
		[SerializeField] private TMP_Text _text;
		[SerializeField] private float _secondaryFillTimeSeconds = 1f;
		[SerializeField][Range(1,100)] private int _currentHealthTextSize;
		[SerializeField] private bool _showPreviewFill = true;
		[SerializeField] private bool _showPreviewInline = true;

		private int _totalHealth = 100;
		private int _currentHealth = 100;
		private int _displayedHealth = 100;
		private readonly StringBuilder _textBuilder = new();

		private Coroutine _modificationCoroutine;

		public void SetHealthToMax(int? totalOverride = null)
		{
			if (totalOverride.HasValue)
			{
				_totalHealth = totalOverride.Value;
			}

			_displayedHealth = _currentHealth = _totalHealth;
			_fillBar.anchorMax = new Vector2(1, 1);
			SetText();
		}

		public void HidePreview()
		{
			_previewFillBar.gameObject.SetActive(false);
			_previewText.gameObject.SetActive(false);
			if(_previewTextAnchor != null) _previewTextAnchor.SetActive(false);
		}
		
		public void ShowPreview(int previewAmount)
		{
			 if (_showPreviewFill) _previewFillBar.gameObject.SetActive(true);
			 if(_previewTextAnchor != null && !_showPreviewInline) _previewTextAnchor.SetActive(true);
			
			// Rect transforms don't like anchor Min/Max diffs of smaller than .07f
			var previewPercentage = Mathf.Max(0.07f,previewAmount / (float)_totalHealth);

			var previewMaxX = _fillBar.anchorMax.x;
			var previewMinX = Mathf.Max(0, previewMaxX - previewPercentage);
			
			_previewFillBar.anchorMin = new Vector2(previewMinX, 0);
			_previewFillBar.anchorMax = new Vector2(previewMaxX, 1);

			if (_showPreviewInline)
			{
				_textBuilder.Clear();
				_textBuilder.Append($"<size={_currentHealthTextSize}%>{_currentHealth}</size>");
				_textBuilder.Append($" {DamagePointerSprite} ");
				_textBuilder.Append($"<color=#{ColorUtility.ToHtmlStringRGB(_previewTextColor)}>{Mathf.Max(0, _currentHealth - previewAmount)}</color>");
				_text.text = _textBuilder.ToString();
			}
			else
			{
				_previewText.gameObject.SetActive(true);
				_previewText.text = $"-{previewAmount}";
			}
		}
		
		private void SetText(string currentHealthOverride = null)
		{
			var currentHealth = currentHealthOverride ?? _currentHealth.ToString();
			
			_textBuilder.Clear();
			_textBuilder.Append($"<size={_currentHealthTextSize}%>{currentHealth}</size>");
			_text.text = _textBuilder.ToString();
		}
		
		public void ModifyValue(int change)
		{
			var previous = _currentHealth;
			_currentHealth = Mathf.Clamp(_currentHealth += change, 0, _totalHealth);
			if (previous == _currentHealth) return;
			
			if(_modificationCoroutine != null) StopCoroutine(_modificationCoroutine);

			var newValuePercentage = (float)_currentHealth / _totalHealth;
			_fillBar.anchorMax = new Vector2(newValuePercentage, 1);
			var changeTime = _secondaryFillTimeSeconds * Mathf.Abs(change) / _totalHealth;
			_modificationCoroutine = StartCoroutine(ModifyInternal(_displayedHealth, _currentHealth, changeTime));
			SetText();
		}

		private IEnumerator ModifyInternal(int fromValue, int toValue, float time)
		{
			var elapsedTime = 0f;
			var diff = toValue - fromValue;
			var initialFill = (float)fromValue / _totalHealth;

			while (elapsedTime < time)
			{
				elapsedTime += Time.deltaTime;
				var ratio = elapsedTime / time;
				var newValuePercentage = initialFill + (float)diff/_totalHealth*ratio;
				_secondaryFillBar.anchorMax = new Vector2(newValuePercentage, 1);

				_displayedHealth = (int)(newValuePercentage * _totalHealth);
				yield return null;
			}
		}
	}
}