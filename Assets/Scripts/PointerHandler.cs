using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Hex
{
    public class PointerHandler : MonoBehaviour
    {
        public event Action<float> PinchContinue;
        public event Action<Vector2> PinchStart;
        public event Action PinchEnd;
        public event Action<Vector2> PointerDown;
        public event Action<Vector2> PointerClick;
        public event Action PointerUp;
        public event Action PointerStay;
        public event Action<Vector2> BeginDrag;
        public event Action<Vector2> DragContinue;
        public event Action EndDrag;

        public bool pinching;
        public bool dragging;

        private Vector2 _pointerDownPosition;
#if UNITY_EDITOR || UNITY_STANDALONE
        private Coroutine _pinchEndDelay;
#endif
        protected bool IsPointerDown;

        private const float PointerClickThreshold = 15f;
        private const float PointerDragThreshold = 15f;

#if UNITY_EDITOR || UNITY_STANDALONE
        private const float ScrollWheelSpeedMod = 10f;
#endif
        public Vector3 PointerPosition { get; private set; }

        private void Update()
        {
#if UNITY_EDITOR || UNITY_STANDALONE
            HandleTouchPC();
#else
            HandleTouchMobile();
#endif
        }

#if UNITY_EDITOR || UNITY_STANDALONE
        private void HandleTouchPC()
        {
            HandlePinchPC();

            var onePointerDown = Input.GetMouseButton(0);
            var pointerPos = Input.mousePosition;

            // Pointer Up/Down/Click
            if (onePointerDown && !IsPointerOverUI())
            {
                PointerPosition = pointerPos;
                if (!IsPointerDown)
                {
                    _pointerDownPosition = pointerPos;
                    PointerDown?.Invoke(_pointerDownPosition);
                }
                else
                {
                    if (!dragging)
                    {
                        var pointerDif = Vector2.Distance(PointerPosition, _pointerDownPosition);
                        if (pointerDif >= PointerDragThreshold)
                        {
                            BeginDrag?.Invoke(PointerPosition);
                            dragging = true;
                        }
                    }
                    else
                    {
                        var pointerDif = Vector2.Distance(PointerPosition, _pointerDownPosition);
                        if (pointerDif >= PointerDragThreshold)
                            DragContinue?.Invoke(PointerPosition);
                        else
                            PointerStay?.Invoke();
                    }
                }

                IsPointerDown = true;
            }
            else
            {
                if (IsPointerDown)
                {
                    IsPointerDown = false;
                    var pointerDif = Vector2.Distance(PointerPosition, _pointerDownPosition);
                    if (pointerDif <= PointerClickThreshold)
                    {
                        PointerClick?.Invoke(PointerPosition);
                    }

                    PointerUp?.Invoke();
                    if (dragging) EndDrag?.Invoke();
                }

                dragging = false;
            }
        }

        private void HandlePinchPC()
        {
            if (Input.mouseScrollDelta != Vector2.zero)
            {
                if (!pinching) PinchStart?.Invoke(Input.mousePosition);
                pinching = true;
                PinchContinue?.Invoke(-Input.mouseScrollDelta.y * ScrollWheelSpeedMod);
            }
            else
            {
                if (!pinching || _pinchEndDelay != null) return;

                StartCoroutine(PinchEndDelay());
            }
        }

        private IEnumerator PinchEndDelay()
        {
            yield return new WaitForSecondsRealtime(.1f);

            PinchEnd?.Invoke();
            pinching = false;
            _pinchEndDelay = null;
        }

#else
        private void HandleTouchMobile()
        {
            IsPointerDown = Input.touchCount > 0;
            if (Input.touchCount > 1)
            {
                HandlePinchMobile();
                return;
            }

            if (pinching)
            {
                PinchEnd?.Invoke();
                pinching = false;
            }

            if (Input.touchCount == 0)
            {
                pinching = dragging = false;
                return;
            }

            var touch = Input.GetTouch(0);
            switch (touch.phase)
            {
                case TouchPhase.Began:
                {
                    if (IsPointerOverUI())
                    {
                        break;
                    }
                    
                    PointerDown?.Invoke(touch.position);
                    _pointerDownPosition = touch.position;
                    PointerPosition = touch.position;
                    break;
                }
                case TouchPhase.Moved:
                {
                    PointerPosition = touch.position;
                    var pointerDif = Vector2.Distance(PointerPosition, _pointerDownPosition);
                    if (!dragging)
                    {
                        if (pointerDif >= PointerDragThreshold)
                        {
                            BeginDrag?.Invoke(PointerPosition);
                            dragging = true;
                        }
                    }
                    else
                    {
                        if (pointerDif >= PointerDragThreshold)
                        {
                            DragContinue?.Invoke(PointerPosition);
                        }
                    }

                    break;
                }
                case TouchPhase.Ended:
                {
                    PointerPosition = touch.position;
                    var pointerDifEnded = Vector2.Distance(PointerPosition, _pointerDownPosition);
                    if (pointerDifEnded <= PointerClickThreshold)
                    {
                        PointerClick?.Invoke(PointerPosition);
                    }

                    PointerUp?.Invoke();
                    if (dragging)
                    {
                        EndDrag?.Invoke();
                    }
                    dragging = false;
                    IsPointerDown = false;
                    break;
                }
                case TouchPhase.Stationary:
                {
                    if (dragging)
                    {
                        PointerStay?.Invoke();
                    }
                    break;
                }
            }
        }

        private void HandlePinchMobile()
        {
            if (Input.touchCount != 2)
            {
                if (pinching)
                {
                    PinchEnd?.Invoke();
                    pinching = false;
                }

                return;
            }

            // Store both touches.
            var touchZero = Input.GetTouch(0);
            var touchOne = Input.GetTouch(1);

            // Find the position in the previous frame of each touch.
            var touchZeroPrevPos = touchZero.position - touchZero.deltaPosition;
            var touchOnePrevPos = touchOne.position - touchOne.deltaPosition;

            // Find the magnitude of the vector (the distance) between the touches in each frame.
            var prevTouchDeltaMag = (touchZeroPrevPos - touchOnePrevPos).magnitude;
            var touchDeltaMag = (touchZero.position - touchOne.position).magnitude;

            // Find the difference in the distances between each frame.
            var deltaMagnitudeDiff = prevTouchDeltaMag - touchDeltaMag;

            if (!pinching)
            {
                PinchStart?.Invoke((touchZero.position + touchOne.position) / 2);
            }
            pinching = true;
            
            PinchContinue?.Invoke(deltaMagnitudeDiff);
        }
#endif

        private static readonly List<RaycastResult> UIRaycastResult = new();
        private static bool IsPointerOverUI()
        {
            UIRaycastResult.Clear();
            var eventDataCurrentPosition = new PointerEventData(EventSystem.current)
            {
                position = new Vector2(Input.mousePosition.x, Input.mousePosition.y)
            };
            EventSystem.current.RaycastAll(eventDataCurrentPosition, UIRaycastResult);
            return UIRaycastResult.Count > 0;
        }
    }
}