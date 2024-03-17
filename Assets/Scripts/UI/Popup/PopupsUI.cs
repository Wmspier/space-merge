using System;
using System.Collections.Generic;
using Hex.Extensions;
using Hex.Managers;
using UnityEngine;

namespace Hex.UI.Popup
{
    public class PopupsUI : MonoBehaviour
    {
        [SerializeField] private Transform content;
        [SerializeField] private NonDrawingGraphic inputBlocker;

        private Dictionary<Type, GameObject> _popupRegistry = new();

        private void Start()
        {
            ApplicationManager.RegisterResource(this);
        }

        private void OnDestroy()
        {
            ApplicationManager.UnRegisterResource<PopupsUI>();
        }

        public void RegisterPopup<T>(T popup) where T : MonoBehaviour
        {
            if (_popupRegistry.ContainsKey(typeof(T)))
            {
                Debug.LogWarning($"Popup registry contains popup of type: {typeof(T)}");
                return;
            }
            
            popup.transform.SetParent(content);
            popup.transform.Reset();

            _popupRegistry[typeof(T)] = popup.gameObject;
        }

        public void UnregisterAndDestroy<T>() where T : MonoBehaviour
        {
            if (_popupRegistry.TryGetValue(typeof(T), out var instance))
            {
                _popupRegistry.Remove(typeof(T));
                Destroy(instance);
            }
        }

        public void ToggleInputBlock(bool block) => inputBlocker.enabled = block;
    }
}