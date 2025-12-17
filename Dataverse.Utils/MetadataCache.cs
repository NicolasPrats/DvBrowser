using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;

namespace Dataverse.Utils
{
    public class MetadataCache
    {
        private IOrganizationService Service { get; }
        private EntityMetadata[] EntityMetadata { get; }
        private Dictionary<string, EntityMetadata> EntityByLogicalName { get; }
        private Dictionary<string, EntityMetadata> EntityBySetName { get; }
        private Dictionary<string, EntityMetadata> EntityMetadataWithAttributes { get; }
        private Dictionary<string, Entity> CustomApiRequestParameters { get; }


        public MetadataCache(IOrganizationService Service)
        {
            this.Service = Service;
            RetrieveAllEntitiesRequest request = new RetrieveAllEntitiesRequest();
            var result = (RetrieveAllEntitiesResponse)this.Service.Execute(request);
            this.EntityMetadata = result.EntityMetadata;
            this.EntityByLogicalName = this.EntityMetadata.ToDictionary(e => e.LogicalName, StringComparer.OrdinalIgnoreCase);
            this.EntityBySetName = this.EntityMetadata.ToDictionary(e => e.EntitySetName, StringComparer.OrdinalIgnoreCase);
            this.EntityMetadataWithAttributes = new Dictionary<string, EntityMetadata>();
            this.CustomApiRequestParameters = new Dictionary<string, Entity>();
        }

        public Entity GetCustomApiRequestParameter(string name)
        {
            lock (this.CustomApiRequestParameters)
            {
                if (this.CustomApiRequestParameters.TryGetValue(name, out Entity entity))
                {
                    return entity;
                }
                QueryExpression query = new QueryExpression("customapirequestparameter");
                query.Criteria.AddCondition("name", ConditionOperator.Equal, name);
                query.ColumnSet = new ColumnSet("type", "logicalentityname");
                entity = this.Service.RetrieveMultiple(query).Entities.FirstOrDefault();
                this.CustomApiRequestParameters[name] = entity;
                return entity;
            }
        }

        public EntityMetadata GetEntityFromLogicalName(string logicalName)
        {
            this.EntityByLogicalName.TryGetValue(logicalName, out var entity);
            return entity;
        }

        public EntityMetadata GetEntityFromSetName(string setName)
        {
            this.EntityBySetName.TryGetValue(setName, out var entity);
            return entity;
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
