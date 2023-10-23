using UnityEngine;

namespace Hex.Extensions
{
    public static class TransformExtensions
    {
        public static void DestroyAllChildGameObjects(this Transform t)
        {
            for (var i = t.childCount-1; i >= 0; i--)
            {
                Object.Destroy(t.GetChild(i).gameObject);
            }
        }

        public static void Reset(this Transform t)
        {
            t.localPosition = Vector3.zero;
            t.localScale = Vector3.one;
            t.localRotation = Quaternion.Euler(Vector3.zero);
        }
    }
}