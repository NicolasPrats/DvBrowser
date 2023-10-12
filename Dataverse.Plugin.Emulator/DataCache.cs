using System;
using System.Collections.Generic;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;

namespace Dataverse.Plugin.Emulator
{
    internal class DataCache
    {

        private IOrganizationService Service { get; }


        public DataCache(IOrganizationService service)
        {
            this.Service = service;
        }

        private readonly Dictionary<Guid, Guid> SystemUserId2AzureAd = new Dictionary<Guid, Guid>();
        public Guid GetAzureADIdFromSystemUserId(Guid systemuserid)
        {
            if (this.SystemUserId2AzureAd.TryGetValue(systemuserid, out Guid id))
            {
                return id;
            }
            var systemuser = this.Service.Retrieve("systemuser", systemuserid, new Microsoft.Xrm.Sdk.Query.ColumnSet("azureactivedirectoryobjectid"));
            id = systemuser.GetAttributeValue<Guid>("azureactivedirectoryobjectid");
            this.SystemUserId2AzureAd[systemuserid] = id;
            return id;
        }

        internal EntityMetadata GetMetadataEntityWithAttributes(string logicalName)
        {
            RetrieveEntityRequest metadataRequest = new RetrieveEntityRequest { EntityFilters = EntityFilters.Attributes, LogicalName = logicalName };
            RetrieveEntityResponse metadataResponse = (RetrieveEntityResponse)this.Service.Execute(metadataRequest);
            return metadataResponse.EntityMetadata;
        }
    }
}
