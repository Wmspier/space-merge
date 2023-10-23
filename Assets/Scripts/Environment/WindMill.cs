using System.Threading.Tasks;
using Hex.Util;
using UnityEngine;

namespace Hex.Environment
{
    [RequireComponent(typeof(Collider))]
    public class WindMill : MonoBehaviour
    {
        private const int FastDurationSeconds = 2;
        private const float RotationIncrement = 2f;

        [SerializeField] private Transform blades;
        [SerializeField] private AnimationCurve rotationDecayCurve;
        
        private bool _goingFast;
        private float _rotationAngle = RotationIncrement;

        private void Awake()
        {
            RotateBlades();
        }

        private void OnMouseUpAsButton()
        {
            if (_goingFast)
            {
                return;
            }
            
            SpeedUp();
        }

        private async void RotateBlades()
        {
            while (this != null && gameObject.activeSelf)
            {
                blades.Rotate(new Vector3(0,0,1), _rotationAngle);
                await Task.Delay(15);
            }
        }

        private async void SpeedUp()
        {
            _goingFast = true;
            await MathUtil.DoInterpolation(FastDurationSeconds, DoInterpolation);
            _goingFast = false;

            void DoInterpolation(float progress)
            {
                _rotationAngle = rotationDecayCurve.Evaluate(progress) * RotationIncrement;
            }
        }
    }
}