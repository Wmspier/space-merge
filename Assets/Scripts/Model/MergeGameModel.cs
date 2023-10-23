using System;
using System.Collections.Generic;
using Hex.Managers;
using Hex.UI;
using UnityEngine;

namespace Hex.Model
{
    public class MergeGameModel
    {
        public readonly Dictionary<ResourceType, int> ResourceAmounts = new();

        public void Load()
        {
            foreach (var resource in Enum.GetValues(typeof(ResourceType)))
            {
                var castResource = (ResourceType)resource;
                var savedAmount = LocalSaveManager.LoadResource(castResource);
                ResourceAmounts[castResource] = savedAmount;
            }
        }

        public void SetResourceAmount(ResourceType resource, int amount)
        {
            ResourceAmounts[resource] = amount;
            LocalSaveManager.SaveResource(resource, amount);
        }
        
        public void ModifyResourceAmount(ResourceType resource, int mod)
        {
            ResourceAmounts[resource] += mod;
            LocalSaveManager.SaveResource(resource, ResourceAmounts[resource]);
        }
    }
}