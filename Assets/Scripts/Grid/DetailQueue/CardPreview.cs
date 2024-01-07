using System;
using System.Collections;
using System.Threading.Tasks;
using Hex.Data;
using Hex.Grid.Cell;
using Hex.Util;
using UnityEngine;
using UnityEngine.Rendering;

namespace Hex.Grid.DetailQueue
{
    public class CardPreview : MonoBehaviour
    {
        [SerializeField] private Transform previewAnchor;
        [SerializeField] private HexCellInfoHolder previewPrefab;
        [SerializeField] private AnimationCurve dequeueLerpCurve;
        
        private Coroutine _rotateCoroutine;
        private HexCellInfoHolder _cellInfoHolder;

        private void OnEnable()
        {
            if (_rotateCoroutine != null)
            {
                StopCoroutine(_rotateCoroutine);
            }
            _rotateCoroutine = StartCoroutine(RotateDetail());
        }

        public void ApplyPreview(UnitData unitData, bool isMain, bool applyScale = true)
        {
            if (previewAnchor.childCount > 0)
            {
                Destroy(previewAnchor.GetChild(0).gameObject);
            }
            
            _cellInfoHolder = Instantiate(previewPrefab, previewAnchor);
            _cellInfoHolder.SpawnUnit(unitData);
            
            var renderers = GetComponentsInChildren<MeshRenderer>();
            foreach (var r in renderers)
            {
                r.shadowCastingMode = ShadowCastingMode.Off;
            }

            if (!applyScale) return;
            
            var scale = isMain ? 1f : .5f;
            transform.localScale = new Vector3(scale, scale, scale);
        }

        public async Task ApplyDetailAndLerp(UnitData unitData, Transform newAnchor, bool isMain, Action completeAction = null)
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
                    ApplyPreview(unitData, isMain, false);
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
                if (_cellInfoHolder != null && _cellInfoHolder.HeldPlayerUnit)
                {
                    var child = _cellInfoHolder.UnitAnchor;
                    child.Rotate(Vector3.up, Time.deltaTime * 15f);
                }
                yield return null;
            }
        }
    }
}