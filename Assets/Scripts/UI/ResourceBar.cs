using System.Collections;
using System.Threading.Tasks;
using Hex.Util;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Hex.UI
{
    public enum ResourceType
    {
        CoinGold,
        CoinSilver,
        Gem
    }
    
    public class ResourceBar : MonoBehaviour
    {
        [SerializeField] private TMP_Text text;
        [SerializeField] private Image icon;
        [SerializeField] private Transform shine;
        [SerializeField] private ResourceType type;

        private int _displayedScore;
        private int _actualScore;

        private Coroutine _adjustmentCoroutine;

        public ResourceType Type => type;

        public void SetScoreImmediate(int amount, bool animated = false)
        {
            _actualScore = amount;
            if (!animated)
            {
                _displayedScore = _actualScore;
                text.text = _actualScore.ToString();
                return;
            }

            if (_adjustmentCoroutine != null)
            {
                return;
            }

            _adjustmentCoroutine = StartCoroutine(AdjustScoreInternal());
        }
        
        public void AdjustScore(int toAdd, bool animated = true)
        {
            _actualScore += toAdd;
            if (!animated)
            {
                _displayedScore = _actualScore;
                text.text = _actualScore.ToString();
                return;
            }

            if (_adjustmentCoroutine != null)
            {
                return;
            }

            if (!gameObject.activeSelf)
            {
                _displayedScore = _actualScore;
                text.text = _actualScore.ToString();
                return;
            }
            
            _adjustmentCoroutine = StartCoroutine(AdjustScoreInternal());
        }

        public void Pulse()
        {
            if (!gameObject.activeSelf)
            {
                return;
            }
            StartCoroutine(PulseInternal());
        } 

        public async Task DoTransform(Sprite newIcon)
        {
            const float animateTime = .25f;
            var grownScale = new Vector3(2, 2, 2);
            
            shine.gameObject.SetActive(true);
            await MathUtil.DoInterpolation(animateTime, DoGrow);
            icon.sprite = newIcon;
            await MathUtil.DoInterpolation(animateTime, DoShrink);
            shine.gameObject.SetActive(false);

            void DoGrow(float progress)
            {
                shine.localScale = Vector3.Lerp(Vector3.zero, grownScale, progress);
            }
            
            void DoShrink(float progress)
            {
                shine.localScale = Vector3.Lerp(grownScale, Vector3.zero, progress);
            }
        }
        
        private IEnumerator AdjustScoreInternal()
        {
            while (_displayedScore != _actualScore)
            {
                if (_displayedScore < _actualScore)
                {
                    _displayedScore += GetModifyAmount();
                }
                else
                {
                    _displayedScore -= GetModifyAmount();
                }
                text.text = _displayedScore.ToString();
                yield return new WaitForSeconds(.025f);
            }
            
            _adjustmentCoroutine = null;

            int GetModifyAmount()
            {
                return Mathf.Abs(_displayedScore - _actualScore) switch
                {
                    > 100 => 20,
                    > 50 => 10,
                    > 20 => 5,
                    _ => 1
                };
            }
        }
        
        private IEnumerator PulseInternal()
        {
            // Pulse score
            const float pulseTimeSeconds = .25f;
            const float pulseScale = 1.25f;
            yield return MathUtil.DoInterpolationEnumerator(pulseTimeSeconds/2, Grow);
            yield return MathUtil.DoInterpolationEnumerator(pulseTimeSeconds, Shrink);

            void Grow(float progress)
            {
                transform.localScale = MathUtil.SmoothLerp(Vector3.one, new Vector3(pulseScale, pulseScale, pulseScale), progress);
            }

            void Shrink(float progress)
            {
                transform.localScale = MathUtil.SmoothLerp(new Vector3(pulseScale, pulseScale, pulseScale), Vector3.one, progress);
            }
        }
    }
}