using System;
using System.Collections.Generic;
using System.Text;
using Hex.Grid;
using Hex.Grid.Serialization;
using Hex.UI;
using UnityEditor;
using UnityEngine;

namespace Hex.Managers
{
    public static class LocalSaveManager
    {
        private const string MergeSaveKey = "MergeGrid";
        private const string GrowSaveKey = "GrowGrid";
        
        private const string DetailListKey = "MergeDetailList";
        private const char DetailDelimiter = '|';

        private static readonly IGridSerializer BasicGridSerializer = new BasicGridSerializer();
        
        public static List<HexCellDefinition> LoadGridFromDisk(GameMode mode)
        {
            var key = mode switch
            {
                GameMode.Merge => MergeSaveKey,
                GameMode.Battle => GrowSaveKey,
                _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
            };
            
            return !PlayerPrefs.HasKey(key) 
                ? null 
                : BasicGridSerializer.Deserialize(PlayerPrefs.GetString(key));
        }
        
        public static void SerializeAndSaveGridToDisk(GameMode mode, List<HexCellDefinition> cells)
        {
            var key = mode switch
            {
                GameMode.Merge => MergeSaveKey,
                GameMode.Battle => GrowSaveKey,
                _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
            };
            
            PlayerPrefs.SetString(key, BasicGridSerializer.Serialize(cells));
            PlayerPrefs.Save();
        }
        
        public static void SaveDetailQueue(List<MergeCellDetailType> detailsList)
        {
            var details = new StringBuilder();
            for (var i = 0; i < detailsList.Count; i++)
            {
                details.Append((int)detailsList[i]);
                if (i < detailsList.Count - 1)
                {
                    details.Append(DetailDelimiter);
                }
            }
            
            PlayerPrefs.SetString(DetailListKey, details.ToString());
            PlayerPrefs.Save();
        }

        public static IEnumerable<int> LoadDetailQueue()
        {
            var loadedDetails = PlayerPrefs.GetString(DetailListKey);
            if (string.IsNullOrEmpty(loadedDetails))
            {
                return Array.Empty<int>();
            }

            var details = loadedDetails.Split(DetailDelimiter);
            var castDetails = new int[details.Length];
            for (var i = 0; i < details.Length; i++)
            {
                castDetails[i] = int.Parse(details[i]);
            }

            return castDetails;
        }

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
            PlayerPrefs.DeleteKey(GrowSaveKey);
            PlayerPrefs.DeleteKey(MergeSaveKey);
            PlayerPrefs.DeleteKey(DetailListKey);

            foreach (var resource in Enum.GetValues(typeof(ResourceType)))
            {
                PlayerPrefs.DeleteKey(GetResourceKey((ResourceType)resource));
            }
            
            Debug.Log("Save Cleared");
        }
        #endif
    }
}