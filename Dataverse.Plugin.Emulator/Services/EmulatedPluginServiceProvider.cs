using System;
using System.Collections.Generic;

namespace Dataverse.Plugin.Emulator.Services
{
    internal class EmulatedPluginServiceProvider
        : IServiceProvider
    {
        private readonly Dictionary<Type, object> Services = new Dictionary<Type, object>();
        public EmulatedPluginServiceProvider()
        {

        }

        /// <typeparam name="T"></typeparam>
        /// <param name="service"></param>
        internal void AddService<T>(T service)
        {
            Services[typeof(T)] = service;
        }


        public object GetService(Type serviceType)
        {
            if (Services.TryGetValue(serviceType, out var service))
            {
                return service;
            }
            foreach (var kvp in this.Services)
            {
                if (serviceType.IsAssignableFrom(kvp.Key))
                {
                    return kvp.Value;
                }
            }
            throw new NotImplementedException("No service of type: " + serviceType);
        }
    }
}
