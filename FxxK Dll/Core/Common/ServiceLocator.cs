using FxxkDDL.Core.Interfaces;
using System;
using System.Collections.Generic;

namespace FxxkDDL.Core.Common
{
    /// <summary>
    /// 简单的服务定位器，提供依赖注入功能
    /// </summary>
    public static class ServiceLocator
    {
        private static readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();
        private static readonly object _lock = new object();

        /// <summary>
        /// 注册服务实例
        /// </summary>
        /// <typeparam name="TService">服务接口类型</typeparam>
        /// <typeparam name="TImplementation">服务实现类型</typeparam>
        public static void Register<TService, TImplementation>() where TImplementation : TService, new()
        {
            lock (_lock)
            {
                _services[typeof(TService)] = new TImplementation();
            }
        }

        /// <summary>
        /// 注册服务实例
        /// </summary>
        /// <typeparam name="TService">服务接口类型</typeparam>
        /// <param name="instance">服务实例</param>
        public static void Register<TService>(TService instance)
        {
            lock (_lock)
            {
                _services[typeof(TService)] = instance ?? throw new ArgumentNullException(nameof(instance));
            }
        }

        /// <summary>
        /// 获取服务实例
        /// </summary>
        /// <typeparam name="TService">服务接口类型</typeparam>
        /// <returns>服务实例</returns>
        /// <exception cref="InvalidOperationException">服务未注册时抛出</exception>
        public static TService GetService<TService>()
        {
            lock (_lock)
            {
                if (_services.TryGetValue(typeof(TService), out var service))
                {
                    return (TService)service;
                }

                // 尝试自动创建（如果有无参构造函数）
                var implementationType = FindImplementation<TService>();
                if (implementationType != null)
                {
                    var instance = Activator.CreateInstance(implementationType);
                    _services[typeof(TService)] = instance;
                    return (TService)instance;
                }

                throw new InvalidOperationException($"Service {typeof(TService).Name} is not registered.");
            }
        }

        /// <summary>
        /// 尝试获取服务实例
        /// </summary>
        /// <typeparam name="TService">服务接口类型</typeparam>
        /// <param name="service">输出服务实例</param>
        /// <returns>是否成功获取</returns>
        public static bool TryGetService<TService>(out TService service)
        {
            try
            {
                service = GetService<TService>();
                return true;
            }
            catch
            {
                service = default;
                return false;
            }
        }

        /// <summary>
        /// 清除所有已注册的服务
        /// </summary>
        public static void Clear()
        {
            lock (_lock)
            {
                _services.Clear();
            }
        }

        /// <summary>
        /// 查找实现类型
        /// </summary>
        private static Type FindImplementation<TService>()
        {
            // 在当前程序集中查找实现
            var serviceType = typeof(TService);
            var assembly = serviceType.Assembly;

            foreach (var type in assembly.GetTypes())
            {
                if (!type.IsAbstract && !type.IsInterface && serviceType.IsAssignableFrom(type))
                {
                    return type;
                }
            }

            return null;
        }
    }
}