using System;
using Hex.UI;
using UnityEditor;
using UnityEngine;

namespace Hex.Managers
{
    public static class LocalSaveManager
    {
        public static void SaveResource(ResourceType resource, int amount)
        {
            PlayerPrefs.SetInt(GetResourceKey(resource), amount);
            PlayerPrefs.Save();
        }

        public static int LoadResource(ResourceType resource) => PlayerPrefs.GetInt(GetResourceKey(resource));
        private static string GetResourceKey(ResourceType resource) => $"{resource}_key";

        #if UNITY_EDITOR
        [MenuItem("Hex/Clear Save")]
        private static void ClearSave()
        {
            foreach (var resource in Enum.GetValues(typeof(ResourceType)))
            {
                PlayerPrefs.DeleteKey(GetResourceKey((ResourceType)resource));
            }
            
            Debug.Log("Save Cleared");
        }
        #endif
    }
}