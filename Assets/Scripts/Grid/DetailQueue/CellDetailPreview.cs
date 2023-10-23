using System;
using System.Collections;
using System.Threading.Tasks;
using Hex.Util;
using UnityEngine;
using UnityEngine.Rendering;

namespace Hex.Grid.DetailQueue
{
    public class CellDetailPreview : MonoBehaviour
    {
        [SerializeField] private Transform detailAnchor;
        [SerializeField] private HexCellDetail previewPrefab;
        [SerializeField] private AnimationCurve dequeueLerpCurve;
        
        private Coroutine _rotateCoroutine;

        private void OnEnable()
        {
            if (_rotateCoroutine != null)
            {
                StopCoroutine(_rotateCoroutine);
            }
            _rotateCoroutine = StartCoroutine(RotateDetail());
        }

        public void ApplyPreview(MergeCellDetailType type, bool isMain, bool applyScale = true)
        {
            var needReplace = true;
            
            if (detailAnchor.childCount > 0)
            {
                var child = detailAnchor.GetChild(0).GetComponent<HexCellDetail>();
                needReplace = child.Type != type;
                if (needReplace)
                {
                    Destroy(detailAnchor.GetChild(0).gameObject);
                }
            }

            if (needReplace)
            {
                var newPreview = Instantiate(previewPrefab, detailAnchor);
                newPreview.SetType(type);
            
                var renderers = GetComponentsInChildren<MeshRenderer>();
                foreach (var r in renderers)
                {
                    r.shadowCastingMode = ShadowCastingMode.Off;
                }
            }

            if (!applyScale) return;
            
            var scale = isMain ? 1f : .5f;
            transform.localScale = new Vector3(scale, scale, scale);
        }

        public async Task ApplyDetailAndLerp(MergeCellDetailType type, Transform newAnchor, bool isMain, Action completeAction = null)
        {
            const float duration = .25f;
            var t = transform;
            var typeSwapped = false;
            
            var startScale = t.localScale;
            var scale = isMain ? 1f : .5f;
            var endScale = new Vector3(scale, scale, scale);

            var startPosition = t.position;
            var endPosition = newAnchor.transform.position;

            await MathUtil.DoInterpolation(duration, DoLerp);

            t.localScale = endScale;
            t.position = endPosition;
            t.SetParent(newAnchor);
            
            completeAction?.Invoke();
            
            void DoLerp(float progress)
            {
                var scaleDiff = endScale - startScale;
                t.localScale = startScale + dequeueLerpCurve.Evaluate(progress) * scaleDiff;
                t.position = Vector3.Lerp(startPosition, endPosition, progress);

                if (progress >= .5f && !typeSwapped)
                {
                    ApplyPreview(type, isMain, false);
                    typeSwapped = true;
                }
            }
        }

        public async Task ShrinkAndDestroy()
        {
            const float duration = .15f;
            var t = transform;
            
            var startScale = t.localScale;
            var endScale = new Vector3(0, 0, 0);

            await MathUtil.DoInterpolation(duration, DoLerp);

            t.localScale = endScale;
            Destroy(gameObject);
            
            void DoLerp(float progress)
            {
                t.localScale = Vector3.Lerp(startScale, endScale, progress);
            }
        }

        private IEnumerator RotateDetail()
        {
            while (true)
            {
                if (detailAnchor.childCount > 0)
                {
                    var child = detailAnchor.transform.GetChild(0);
                    child.Rotate(Vector3.up, Time.deltaTime * 15f);
                }
                yield return null;
            }
        }
    }
}