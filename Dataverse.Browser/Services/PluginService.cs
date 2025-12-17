using System.Diagnostics;
using Dataverse.Browser.Context;
using Dataverse.Plugin.Emulator.Steps;

namespace Dataverse.Browser.Services
{
    /// <summary>
    /// Implementation of plugin service.
    /// Handles plugin emulation and debugging operations.
    /// </summary>
    public class PluginService : IPluginService
    {
        /// <inheritdoc/>
        public PluginEmulator LoadPlugins(BrowserContext context)
        {
            if (context?.PluginsEmulator != null)
            {
                return context.PluginsEmulator;
            }

            // Plugin emulator is typically created with the context
            // This method would be used to reload or initialize if needed
            return null;
        }

        /// <inheritdoc/>
        public void ReloadPlugins(PluginEmulator emulator)
        {
            // Hot reload is handled automatically by the plugin emulator through file watchers
            // No manual reload method is exposed
        }

        /// <inheritdoc/>
        public void SetPluginsEnabled(PluginEmulator emulator, bool enabled)
        {
            // Plugin enabling/disabling would be handled through emulator options
            // Currently not implemented in the emulator
        }

        /// <inheritdoc/>
        public bool AttachDebugger()
        {
            if (IsDebuggerAttached)
            {
                return true;
            }

            return Debugger.Launch();
        }

        /// <inheritdoc/>
        public bool IsDebuggerAttached => Debugger.IsAttached;
    }
}
