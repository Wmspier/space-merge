using System;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;

namespace Hex.Util
{
    public static class MathUtil
    {
        public static Vector3 SmoothLerp(Vector3 from, Vector3 to, float progress)
        {
            var x = Mathf.SmoothStep(from.x, to.x, progress);
            var y = Mathf.SmoothStep(from.y, to.y, progress);
            var z = Mathf.SmoothStep(from.z, to.z, progress);
            return new Vector3(x, y, z);
        }
        public static Vector2 SmoothLerp(Vector2 from, Vector2 to, float progress)
        {
            var x = Mathf.SmoothStep(from.x, to.x, progress);
            var y = Mathf.SmoothStep(from.y, to.y, progress);
            return new Vector2(x, y);
        }

        public static async Task DoInterpolation(float timeSeconds, Action<float> progressAction, (float percentage, Action action) softCompleteAction = default)
        {
            var startTime = Time.time;
            var elapsedTime = 0f;
            var softCompleted = false;
            while (elapsedTime <= timeSeconds)
            {
                elapsedTime = Time.time - startTime;
                progressAction.Invoke(elapsedTime / timeSeconds);
                if (softCompleteAction.action != null 
                    && !softCompleted
                    && elapsedTime / timeSeconds >= softCompleteAction.percentage)
                {
                    softCompleted = true;
                    softCompleteAction.action.Invoke();
                }
                
                await Task.Delay(10);
            }
        }
        public static IEnumerator DoInterpolationEnumerator(float timeSeconds, Action<float> progressAction)
        {
            var startTime = Time.time;
            var elapsedTime = 0f;
            while (elapsedTime <= timeSeconds)
            {
                elapsedTime = Time.time - startTime;
                progressAction.Invoke(elapsedTime / timeSeconds);
                yield return null;
            }
        }
    }
}