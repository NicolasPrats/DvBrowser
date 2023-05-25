using System.Net.Http;
using Dataverse.Plugin.Emulator.Services;
using Dataverse.Plugin.Emulator.Steps;
using Dataverse.Utils;
using Microsoft.OData.Edm;
using Microsoft.Xrm.Tooling.Connector;


namespace Dataverse.Browser.Context
{
    internal class BrowserContext
        : DataverseContext
    {

        public string CachePath { get; set; }
        public PluginEmulator PluginsEmulator { get; set; }


        public OrganizationServiceWithEmulatedPlugins ProxyForWeb { get; set; }



        public LastRequestsList LastRequests { get; } = new LastRequestsList();



    }
}
