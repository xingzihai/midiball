// ServiceLocator.cs — 轻量级服务定位器，运行时获取接口实例
using System;
using System.Collections.Generic;
using UnityEngine;

namespace StarPipe.Core
{
    public static class ServiceLocator
    {
        private static readonly Dictionary<Type, object> _services = new();

        public static void Register<T>(T instance) where T : class
        {
            var type = typeof(T);
            if (_services.ContainsKey(type))
            {
                Debug.LogWarning($"[ServiceLocator] 覆盖已注册服务: {type.Name}");
            }
            _services[type] = instance;
        }

        public static T Get<T>() where T : class
        {
            var type = typeof(T);
            if (_services.TryGetValue(type, out var service))
                return (T)service;

            Debug.LogError($"[ServiceLocator] 未找到服务: {type.Name}");
            return null;
        }

        public static bool Has<T>() where T : class
        {
            return _services.ContainsKey(typeof(T));
        }

        /// <summary>场景切换时重置所有服务</summary>
        public static void Reset()
        {
            _services.Clear();
        }
    }
}
