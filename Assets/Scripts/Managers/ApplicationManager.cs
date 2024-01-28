using System;
using System.Collections.Generic;
using Hex.Model;
using UnityEngine.Assertions;

namespace Hex.Managers
{
    public static class ApplicationManager
    {
        private static readonly Dictionary<Type, object> RegisteredResources = new();

        public static ApplicationModel Model { get; private set; } = new();

        public static void RegisterResource<T>(T resource)
        {
            Assert.IsFalse(RegisteredResources.ContainsKey(typeof(T)));

            RegisteredResources[typeof(T)] = resource;
        }

        public static void UnRegisterResource<T>(T resource)
        {
            RegisteredResources.Remove(typeof(T));
        }

        public static T GetResource<T>()
        {
            if (RegisteredResources.TryGetValue(typeof(T), out var resource))
            {
                return (T)resource;
            }

            throw new KeyNotFoundException($"{typeof(T)} not found in resource map.");
        }

        public static MergeGameManager GetGameManager() => GetResource<MergeGameManager>();
    }
}