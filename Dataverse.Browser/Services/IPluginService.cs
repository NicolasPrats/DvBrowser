using Dataverse.Browser.Context;
using Dataverse.Plugin.Emulator.Steps;

namespace Dataverse.Browser.Services
{
    /// <summary>
    /// Service interface for plugin management.
    /// Abstracts plugin emulation and debugging logic.
    /// </summary>
    public interface IPluginService
    {
        /// <summary>
        /// Loads plugin assemblies from the configured path
        /// </summary>
        /// <param name="context">Browser context</param>
        /// <returns>Plugin emulator instance</returns>
        PluginEmulator LoadPlugins(BrowserContext context);

        /// <summary>
        /// Reloads all plugin assemblies
        /// </summary>
        /// <param name="emulator">Plugin emulator to reload</param>
        void ReloadPlugins(PluginEmulator emulator);

        /// <summary>
        /// Enables or disables plugin execution
        /// </summary>
        /// <param name="emulator">Plugin emulator</param>
        /// <param name="enabled">True to enable, false to disable</param>
        void SetPluginsEnabled(PluginEmulator emulator, bool enabled);

        /// <summary>
        /// Attaches the debugger for plugin debugging
        /// </summary>
        /// <returns>True if debugger attached successfully</returns>
        bool AttachDebugger();

        /// <summary>
        /// Gets whether a debugger is currently attached
        /// </summary>
        bool IsDebuggerAttached { get; }
    }
}
