using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Tooling.Connector;

namespace Dataverse.Browser.Context
{
    internal class MetadataCache
    {
        private CrmServiceClient Service { get; }
        private EntityMetadata[] EntityMetadata { get; }
        private Dictionary<string, EntityMetadata> EntityMetadataWithAttributes { get; }

        public MetadataCache(CrmServiceClient Service)
        {
            this.Service = Service;
            RetrieveAllEntitiesRequest request = new RetrieveAllEntitiesRequest();
            var result = (RetrieveAllEntitiesResponse)this.Service.Execute(request);
            this.EntityMetadata = result.EntityMetadata;
            this.EntityMetadataWithAttributes = new Dictionary<string, EntityMetadata>();
        }

        public EntityMetadata GetEntityFromLogicalName(string logicalName)
        {
            return EntityMetadata.FirstOrDefault(e => e.LogicalName == logicalName);
        }

        public EntityMetadata GetEntityFromSetName(string setName)
        {
            return EntityMetadata.FirstOrDefault(e => e.EntitySetName == setName);
        }

        public EntityMetadata GetEntityMetadataWithAttributes(string entityLogicalName)
        {
            lock (this.EntityMetadataWithAttributes)
            {
                if (this.EntityMetadataWithAttributes.TryGetValue(entityLogicalName, out var metadata))
                {
                    return metadata;
                }
                RetrieveEntityRequest request = new RetrieveEntityRequest()
                {
                    EntityFilters = EntityFilters.Attributes | EntityFilters.Relationships,
                    LogicalName = entityLogicalName
                };
                var result = (RetrieveEntityResponse)this.Service.Execute(request);
                this.EntityMetadataWithAttributes[entityLogicalName] = result.EntityMetadata;
                return result.EntityMetadata;
            }
        }
    }
}
