using System.Threading.Tasks;
using Hex.Data;
using Hex.Util;
using TMPro;
using UnityEngine;

namespace Hex.UI
{
    public class DeckQueueUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text nextCardText;
        [SerializeField] private TMP_Text remainingCardsText;

        public void Initialize(UnitData nextUnit, int remainingTiles)
        {
            nextCardText.gameObject.SetActive(true);
            remainingCardsText.gameObject.SetActive(true);
            nextCardText.text = nextUnit.UniqueId;
            
            remainingCardsText.text = $"{remainingTiles} cards left";
            remainingCardsText.gameObject.GetComponent<RectTransform>().pivot = new Vector2(0, .5f);
            remainingCardsText.alignment = TextAlignmentOptions.MidlineLeft;
        }

        public async Task SetNextAndDecrement(UnitData nextUnit, int remainingTiles)
        {
            var t = transform;
            const float lerpTimeSeconds = .15f;
            // Grow
            await MathUtil.DoInterpolation(lerpTimeSeconds, Grow);
            
            // Update text
            nextCardText.text = nextUnit.UniqueId;
            remainingCardsText.text = $"{remainingTiles} cards left";
            
            // Shrink
            await MathUtil.DoInterpolation(lerpTimeSeconds, Shrink);

            void Grow(float progress)
            {
                var scale = Mathf.SmoothStep(1f, 1.25f, progress);
                t.localScale = new Vector3(scale, scale, scale);
            }

            void Shrink(float progress)
            {
                var scale = Mathf.SmoothStep(1.25f, 1f, progress);
                t.localScale = new Vector3(scale, scale, scale);
            }
        }
    }
}