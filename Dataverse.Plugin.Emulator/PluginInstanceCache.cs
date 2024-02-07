using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Microsoft.Xrm.Sdk;

namespace Dataverse.Plugin.Emulator.Steps
{
    internal class PluginInstanceCache
    {
        static PluginInstanceCache()
        {
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;

        }

        private static System.Reflection.Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            if (args.Name.StartsWith("Microsoft.Xrm.Sdk"))
            {
                string path = Path.GetDirectoryName(typeof(PluginInstanceCache).Assembly.Location);
                //We ignore the version so that we can load dll compiled with an older version of the Sdk
                return Assembly.LoadFile(Path.Combine(path, "Microsoft.Xrm.Sdk.dll"));
            }
            return null;
        }

        private readonly Dictionary<ValueTuple<string, string, string>, IPlugin> Cache = new Dictionary<ValueTuple<string, string, string>, IPlugin>();
        public IPlugin GetPlugin(PluginStepDescription step)
        {
            ValueTuple<string, string, string> key = (step.EventHandler, step.Configuration, step.SecureConfiguration);

            if (!this.Cache.TryGetValue(key, out var plugin))
            {
                var type = step.Assembly.GetType(step.EventHandler, true);
                if (type.GetConstructor(new Type[] { }) != null)
                {
                    plugin = (IPlugin)Activator.CreateInstance(type);
                }
                else if (type.GetConstructor(new Type[] { typeof(string), typeof(string) }) != null)
                {
                    plugin = (IPlugin)Activator.CreateInstance(type, null, (string)null);
                }
                else
                {
                    throw new NotSupportedException("No constructor found for plugin: " + step.EventHandler);
                }
                this.Cache[key] = plugin;
            }
            return plugin;
        }
    }
}
