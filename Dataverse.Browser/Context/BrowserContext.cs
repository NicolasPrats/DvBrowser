using Dataverse.Plugin.Emulator.Services;
using Dataverse.Plugin.Emulator.Steps;
using Dataverse.Utils;


namespace Dataverse.Browser.Context
{
    internal class BrowserContext
        : DataverseContext
    {

        public string CachePath { get; set; }
        public PluginEmulator PluginsEmulator { get; set; }


        public OrganizationServiceWithEmulatedPlugins ProxyForWeb { get; set; }
        public bool IsEnabled { get; set; } = true;


        public LastRequestsList LastRequests { get; } = new LastRequestsList();



    }
}
