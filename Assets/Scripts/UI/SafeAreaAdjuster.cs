using UnityEngine;

namespace Hex.UI
{
    [RequireComponent(typeof(RectTransform))]
    public class SafeAreaAdjuster : MonoBehaviour
    {
        private void Awake() => ApplySafeArea();

        private void ApplySafeArea()
        {
            var safeAreaTransform = GetComponent<RectTransform>();
            var canvas = transform.parent.GetComponent<Canvas>();
            var safeArea = Screen.safeArea;
            if (canvas == null)
            {
                return;
            }
            
            safeAreaTransform.anchorMin = new Vector2(0,0);
            safeAreaTransform.anchorMax = new Vector2(1,1);
   
            var anchorMin = safeArea.position;
            var anchorMax = safeArea.position + safeArea.size;
            var pixelRect = canvas.pixelRect;
            anchorMin.x /= pixelRect.width;
            anchorMin.y /= pixelRect.height;
            anchorMax.x /= pixelRect.width;
            anchorMax.y /= pixelRect.height;
   
            safeAreaTransform.anchorMin = anchorMin;
            safeAreaTransform.anchorMax = anchorMax;
        }
    }
}