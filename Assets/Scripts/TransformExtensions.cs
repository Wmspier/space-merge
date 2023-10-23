using UnityEngine;

namespace Hex
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
    }
}