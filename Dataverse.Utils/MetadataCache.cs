using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;

namespace Dataverse.Utils
{
    [Serializable]
    internal class EntityMetadataCache
    {
        public string LogicalName { get; set; }
        public string EntitySetName { get; set; }
        public int ObjectTypeCode { get; set; }
    }

    public class MetadataCache
    {
        private IOrganizationService Service { get; }
        private EntityMetadata[] EntityMetadata { get; }
        private Dictionary<string, EntityMetadata> EntityByLogicalName { get; }
        private Dictionary<string, EntityMetadata> EntityBySetName { get; }
        private Dictionary<string, EntityMetadata> EntityMetadataWithAttributes { get; }
        private Dictionary<string, Entity> CustomApiRequestParameters { get; }


        public MetadataCache(IOrganizationService Service, string cachePath = null, bool forceRefresh = false)
        {
            this.Service = Service;
            
            string cacheFile = null;
            if (!string.IsNullOrEmpty(cachePath))
            {
                cacheFile = Path.Combine(cachePath, "metadata.cache");
            }

            // Try to load from cache first
            if (!forceRefresh && cacheFile != null && TryLoadFromCache(cacheFile, out var cachedMetadata))
            {
                System.Diagnostics.Trace.WriteLine($"[MetadataCache] Loaded {cachedMetadata.Length} entities from disk cache");
                this.EntityMetadata = cachedMetadata;
            }
            else
            {
                System.Diagnostics.Trace.WriteLine("[MetadataCache] Fetching metadata from Dataverse...");
                // Only retrieve basic entity info (not attributes, relationships, privileges)
                // This significantly speeds up initialization
                RetrieveAllEntitiesRequest request = new RetrieveAllEntitiesRequest
                {
                    EntityFilters = EntityFilters.Entity
                };
                var result = (RetrieveAllEntitiesResponse)this.Service.Execute(request);
                this.EntityMetadata = result.EntityMetadata;
                System.Diagnostics.Trace.WriteLine($"[MetadataCache] Fetched {this.EntityMetadata.Length} entities from Dataverse");

                // Save to cache
                if (cacheFile != null)
                {
                    TrySaveToCache(cacheFile, this.EntityMetadata);
                    System.Diagnostics.Trace.WriteLine($"[MetadataCache] Saved metadata to disk cache: {cacheFile}");
                }
            }

            this.EntityByLogicalName = this.EntityMetadata.ToDictionary(e => e.LogicalName, StringComparer.OrdinalIgnoreCase);
            this.EntityBySetName = this.EntityMetadata.ToDictionary(e => e.EntitySetName, StringComparer.OrdinalIgnoreCase);
            this.EntityMetadataWithAttributes = new Dictionary<string, EntityMetadata>();
            this.CustomApiRequestParameters = new Dictionary<string, Entity>();
        }

        private bool TryLoadFromCache(string cacheFile, out EntityMetadata[] metadata)
        {
            metadata = null;
            try
            {
                if (!File.Exists(cacheFile))
                {
                    return false;
                }

                // Check if cache is older than 7 days
                var fileInfo = new FileInfo(cacheFile);
                if ((DateTime.Now - fileInfo.LastWriteTime).TotalDays > 7)
                {
                    return false;
                }

                // Read simplified cache format (just the essential fields)
                var lines = File.ReadAllLines(cacheFile);
                var metadataList = new List<EntityMetadata>();
                
                foreach (var line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    
                    var parts = line.Split('|');
                    if (parts.Length < 2) continue;
                    
                    // Create minimal EntityMetadata with just what we need
                    // Note: We can't set ObjectTypeCode as it's readonly, but LogicalName and EntitySetName are sufficient
                    var entity = new EntityMetadata
                    {
                        LogicalName = parts[0],
                        EntitySetName = parts[1]
                    };
                    metadataList.Add(entity);
                }

                metadata = metadataList.ToArray();
                return metadata.Length > 0;
            }
            catch
            {
                // If cache read fails, return false to fetch fresh
                return false;
            }
        }

        private void TrySaveToCache(string cacheFile, EntityMetadata[] metadata)
        {
            try
            {
                // Save simplified format: LogicalName|EntitySetName
                var lines = metadata.Select(e => 
                    $"{e.LogicalName}|{e.EntitySetName}");
                File.WriteAllLines(cacheFile, lines);
            }
            catch
            {
                // If cache save fails, just continue - not critical
            }
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
