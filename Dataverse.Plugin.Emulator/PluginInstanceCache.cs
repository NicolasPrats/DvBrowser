using System;
using System.Collections.Generic;
using Microsoft.Xrm.Sdk;

namespace Dataverse.Plugin.Emulator.Steps
{
    internal class PluginInstanceCache
    {
        private readonly Dictionary<ValueTuple<string, string, string>, IPlugin> Cache = new Dictionary<ValueTuple<string, string, string>, IPlugin>();
        public IPlugin GetPlugin(PluginStepDescription step)
        {
            ValueTuple<string, string, string> key = (step.EventHandler, (string)step.Configuration, (string)step.SecureConfiguration);

            if (!Cache.TryGetValue(key, out var plugin))
            {
                var type = step.Assembly.GetType(step.EventHandler);
                if (type.GetConstructor(new Type[] { }) != null)
                {
                    plugin = (IPlugin)Activator.CreateInstance(type);
                }
                else if (type.GetConstructor(new Type[] { typeof(string), typeof(string) }) != null)
                {
                    plugin = (IPlugin)Activator.CreateInstance(type, (string)null, (string)null);
                }
                else
                {
                    throw new NotSupportedException("No constructor found for plugin: " + step.EventHandler);
                }
                    Cache[key] = plugin;
            }
            return plugin;
        }
    }
}
