using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Dataverse.Plugin.Emulator.Services;
using Dataverse.Plugin.Emulator.Steps;
using Microsoft.OData.Edm;
using Microsoft.OData.UriParser;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Tooling.Connector;


namespace Dataverse.Browser.Context
{
    internal class DataverseContext
    {
        public string Host { get; set; }
        public string ConnectionString { get; set; }
        public string CachePath { get; set; }
        public PluginEmulator PluginsEmulator { get; set; }

        public CrmServiceClient CrmServiceClient { get; set; }
        public HttpClient HttpClient { get; set; }

        public OrganizationServiceWithEmulatedPlugins ProxyForWeb { get; set; }

        public MetadataCache MetadataCache { get; set; }


        public LastRequestsList LastRequests { get; } = new LastRequestsList();

        public string WebApiBaseUrl => $"https://{this.Host}/api/data/v9.2/";

        public IEdmModel Model { get; internal set; }
        public bool IsEnabled { get; set; } = true;

      
    }
}
