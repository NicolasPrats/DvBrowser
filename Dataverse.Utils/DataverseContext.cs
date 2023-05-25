using System.Net.Http;
using Microsoft.OData.Edm;
using Microsoft.Xrm.Tooling.Connector;


namespace Dataverse.Utils
{
    public class DataverseContext
    {
        public string Host { get; set; }

        public CrmServiceClient CrmServiceClient { get; set; }
        public HttpClient HttpClient { get; set; }
        public MetadataCache MetadataCache { get; set; }
        public string WebApiBaseUrl => $"https://{this.Host}/api/data/v9.2/";

        public IEdmModel Model { get; set; }

    }
}
