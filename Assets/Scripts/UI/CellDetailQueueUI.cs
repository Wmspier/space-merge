using System.Threading.Tasks;
using Hex.Grid;
using Hex.Util;
using TMPro;
using UnityEngine;

namespace Hex.UI
{
    public class CellDetailQueueUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text nextTileText;
        [SerializeField] private TMP_Text remainingTilesText;

        public void Initialize(MergeCellDetailType nextTileType, int remainingTiles)
        {
            nextTileText.gameObject.SetActive(true);
            remainingTilesText.gameObject.SetActive(true);
            nextTileText.text = nextTileType.ToString();
            
            remainingTilesText.text = $"{remainingTiles} tiles left";
            remainingTilesText.gameObject.GetComponent<RectTransform>().pivot = new Vector2(0, .5f);
            remainingTilesText.alignment = TextAlignmentOptions.MidlineLeft;
        }

        public async Task SetNextAndDecrement(MergeCellDetailType nextTileType, int remainingTiles)
        {
            var t = transform;
            const float lerpTimeSeconds = .15f;
            // Grow
            await MathUtil.DoInterpolation(lerpTimeSeconds, Grow);
            
            // Update text
            if (remainingTiles > 0)
            {
                nextTileText.text = nextTileType.ToString();
                remainingTilesText.text = $"{remainingTiles} tiles left";
            }
            else
            {
                nextTileText.gameObject.SetActive(false);
                remainingTilesText.gameObject.SetActive(false);
            }
            
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