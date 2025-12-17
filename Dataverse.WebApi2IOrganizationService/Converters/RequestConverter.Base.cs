using System;
using System.Linq;
using Dataverse.Utils;
using Dataverse.WebApi2IOrganizationService.Model;
using Microsoft.OData.Edm;
using Microsoft.OData.UriParser;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Metadata;
using System.Web;
using Microsoft.Crm.Sdk.Messages;

namespace Dataverse.WebApi2IOrganizationService.Converters
{
    public partial class RequestConverter

    {
        private DataverseContext Context { get; }

        public RequestConverter(DataverseContext context)
        {
            this.Context = context ?? throw new ArgumentNullException(nameof(context));
        }





        public RequestConversionResult Convert(WebApiRequest request)
        {
            if (request == null)
                return null;
                
            // Skip conversion for system metadata functions that don't need plugin emulation
            var skipFunctions = new[] 
            { 
                "GetClientMetadata", 
                "RetrieveMetadataChanges", 
                "RetrieveAllEntities",
                "RetrieveEntity",
                "RetrieveAttribute"
            };
            
            if (skipFunctions.Any(func => request.LocalPathWithQuery.IndexOf($"/{func}(", StringComparison.OrdinalIgnoreCase) >= 0))
            {
                // Return null to indicate no conversion needed - let the request pass through
                return null;
            }
            
            RequestConversionResult result = new RequestConversionResult()
            {
                SrcRequest = request
            };
            ODataUriParser parser;
            ODataPath path;
            try
            {
                var apiRelativeUri = request.LocalPathWithQuery;
                if (apiRelativeUri.StartsWith("/api"))
                    apiRelativeUri = apiRelativeUri.Substring(15);
                parser = new ODataUriParser(this.Context.Model, new Uri(apiRelativeUri, UriKind.Relative))
                {
                    Resolver = new AlternateKeysODataUriResolver(this.Context.Model)
                };
                path = parser.ParsePath();
            }
            catch (Exception ex)
            {
                result.ConvertFailureMessage = "Unable to parse: " + ex.Message;
                return result;
            }
            try
            {
                switch (request.Method)
                {
                    case "POST":
                        switch (path.Count)
                        {
                            case 1:
                                ManagePost1Segment();
                                break;
                            case 2:
                                ManagePost2Segment();
                                break;
                            case 3:
                                ManagePost3Segment();
                                break;
                            default:
                                throw new NotImplementedException("POST is not implemented for: " + path.Count + " segments");
                        }

                        break;
                    case "PATCH":
                        if (path.Count != 2)
                        {
                            throw new NotImplementedException("PATCH is not implemented for: " + path.Count + " segments");
                        }
                        if (path.FirstSegment.EdmType?.TypeKind != EdmTypeKind.Collection)
                        {
                            throw new NotImplementedException("PATCH is not implemented for: " + path.FirstSegment.EdmType?.TypeKind);
                        }
                        ConvertToCreateUpdateRequest(result, path);
                        break;
                    case "GET":
                        switch (path.Count)
                        {
                            case 1:
                                throw new NotImplementedException("Retrievemultiple are not implemented");
                            case 2:
                                ConvertToRetrieveRequest(result, parser, path);
                                break;
                            case 3:
                                if (path.LastSegment is OperationSegment operationSegment)
                                {
                                    ConvertToBoundFunction(result, path, operationSegment);
                                    break;
                                }
                                throw new NotSupportedException("Unexpected number of segments:" + path.Count);
                            default:
                                throw new NotSupportedException("Unexpected number of segments:" + path.Count);
                        }
                        break;
                    case "DELETE":
                        if (path.Count != 2)
                        {
                            throw new NotSupportedException("Unexpected number of segments:" + path.Count);
                        }
                        ConvertToDeleteRequest(result, path);
                        break;
                    default:
                        result.ConvertFailureMessage = "method not implemented";
                        break;
                }
            }
            catch (Exception ex)
            {
                result.ConvertFailureMessage = ex.Message;
            }
            return result;

            void ManagePost3Segment()
            {
                if (!(path.LastSegment is OperationSegment))
                {
                    throw new NotImplementedException("Post with 3 segments are implemented only for custom api!");
                }
                var entity = this.Context.MetadataCache.GetEntityFromSetName(path.FirstSegment.Identifier) ?? throw new NotSupportedException($"Entity: {path.FirstSegment.Identifier} not found!");
                var keySegment = path.Skip(1).First() as KeySegment ?? throw new NotSupportedException("2nd segment should be of type identifier");
                EntityReference target = GetEntityReferenceFromKeySegment(entity, keySegment);
                string identifier = path.LastSegment.Identifier;
                var declaredOperation = this.Context.Model.FindDeclaredOperations(identifier).Single();

                ConvertToAction(declaredOperation, result, target);
            }

            void ManagePost2Segment()
            {
                if (!(path.LastSegment is OperationSegment))
                {
                    throw new NotImplementedException("Post with 3 segments are implemented only for custom api!");
                }
                string identifier = path.LastSegment.Identifier;
                var declaredOperation = this.Context.Model.FindDeclaredOperations(identifier).Single();

                ConvertToAction(declaredOperation, result, null);
            }

            void ManagePost1Segment()
            {
                if (path.FirstSegment.EdmType?.TypeKind == EdmTypeKind.Collection)
                {
                    string identifier = path.FirstSegment.Identifier;
                    var entity = this.Context.MetadataCache.GetEntityFromSetName(path.FirstSegment.Identifier);
                    if (entity != null)
                    {
                        ConvertToCreateUpdateRequest(result, path);
                    }
                    else
                    {
                        //Custom api returning only one collection have a first segment of type collection
                        var declaredOperation = this.Context.Model.FindDeclaredOperationImports(identifier).FirstOrDefault() ?? throw new NotImplementedException("Identifier unknown:" + identifier);
                        ConvertToAction(declaredOperation.Operation, result, null);
                    }
                }
                else if (path.FirstSegment.Identifier == "$batch")
                {
                    ConvertToExecuteMultipleRequest(result);
                }
                else if (path.FirstSegment is OperationImportSegment)
                {
                    string identifier = path.FirstSegment.Identifier;
                    var declaredOperation = this.Context.Model.FindDeclaredOperationImports(identifier).Single();
                    ConvertToAction(declaredOperation.Operation, result, null);
                }
                else
                {
                    throw new NotImplementedException("POST is not implemented for: " + path.FirstSegment.EdmType?.TypeKind);
                }
            }
        }

        private static EntityReference GetEntityReferenceFromKeySegment(EntityMetadata entity, KeySegment keySegment)
        {
            GetIdFromKeySegment(keySegment, out var id, out var keys);

            EntityReference target;
            if (id == Guid.Empty)
            {
                target = new EntityReference(entity.LogicalName, keys);
            }
            else
            {
                target = new EntityReference(entity.LogicalName, id);
            }

            return target;
        }

        private static void GetIdFromKeySegment(KeySegment keySegment, out Guid id, out KeyAttributeCollection keys)
        {
            if (keySegment.Keys.Count() == 1)
            {
                var key = keySegment.Keys.First();
                if (key.Value is Guid keyId)
                {
                    id = keyId;
                    keys = null;
                    return;
                }
            }
            id = Guid.Empty;
            keys = new KeyAttributeCollection();
            foreach (var key in keySegment.Keys)
            {
                keys[key.Key] = key.Value;
            }
        }

        private void ConvertToBoundFunction(RequestConversionResult conversionResult, ODataPath path, OperationSegment operationSegment)
        {
            // Support for RetrievePrincipalAccess bound function
            if (string.Equals(operationSegment.Identifier, "Microsoft.Dynamics.CRM.RetrievePrincipalAccess", StringComparison.OrdinalIgnoreCase))
            {
                var entitySegment = path.FirstSegment as EntitySetSegment ?? throw new NotSupportedException("First segment should be entity set");
                var keySegment = path.Skip(1).First() as KeySegment ?? throw new NotSupportedException("Second segment should be key");

                // The entity in the URL path is the PRINCIPAL (systemuser or team)
                var principalEntity = this.Context.MetadataCache.GetEntityFromSetName(entitySegment.Identifier) ?? throw new NotSupportedException("Entity not found: " + entitySegment.Identifier);
                var principalRef = GetEntityReferenceFromKeySegment(principalEntity, keySegment);

                var uri = new Uri("http://localhost" + conversionResult.SrcRequest.LocalPathWithQuery);
                var query = HttpUtility.ParseQueryString(uri.Query);
                
                // The Target parameter contains the target entity reference
                var targetRaw = query["@Target"] ?? query["Target"];
                
                if (string.IsNullOrEmpty(targetRaw))
                {
                    throw new NotSupportedException("RetrievePrincipalAccess missing Target parameter");
                }

                // Parse the Target JSON: {"@odata.id":"entityset(guid)"}
                EntityReference targetRef;
                try
                {
                    var targetJson = System.Text.Json.JsonDocument.Parse(targetRaw);
                    var odataId = targetJson.RootElement.GetProperty("@odata.id").GetString();
                    
                    // Parse "entityset(guid)" format
                    var match = System.Text.RegularExpressions.Regex.Match(odataId, @"^([^(]+)\(([^)]+)\)$");
                    if (!match.Success)
                    {
                        throw new NotSupportedException($"Invalid @odata.id format: {odataId}");
                    }
                    
                    var entitySetName = match.Groups[1].Value;
                    var guidStr = match.Groups[2].Value;
                    
                    var targetEntity = this.Context.MetadataCache.GetEntityFromSetName(entitySetName);
                    if (targetEntity == null)
                    {
                        throw new NotSupportedException($"Entity not found for set name: {entitySetName}");
                    }
                    
                    if (!Guid.TryParse(guidStr, out var targetId))
                    {
                        throw new NotSupportedException($"Invalid GUID in Target: {guidStr}");
                    }
                    
                    targetRef = new EntityReference(targetEntity.LogicalName, targetId);
                }
                catch (Exception ex)
                {
                    throw new NotSupportedException($"Failed to parse Target parameter: {ex.Message}");
                }

                conversionResult.ConvertedRequest = new RetrievePrincipalAccessRequest
                {
                    Principal = principalRef,
                    Target = targetRef
                };
                return;
            }

            // Support for RetrievePrincipalAccessInfo bound function (legacy)
            if (string.Equals(operationSegment.Identifier, "Microsoft.Dynamics.CRM.RetrievePrincipalAccessInfo", StringComparison.OrdinalIgnoreCase))
            {

            var entitySegment = path.FirstSegment as EntitySetSegment ?? throw new NotSupportedException("First segment should be entity set");
            var keySegment = path.Skip(1).First() as KeySegment ?? throw new NotSupportedException("Second segment should be key");

            var principalEntity = this.Context.MetadataCache.GetEntityFromSetName(entitySegment.Identifier) ?? throw new NotSupportedException("Entity not found: " + entitySegment.Identifier);
            var principalRef = GetEntityReferenceFromKeySegment(principalEntity, keySegment);

            var uri = new Uri("http://localhost" + conversionResult.SrcRequest.LocalPathWithQuery);
            var query = HttpUtility.ParseQueryString(uri.Query);
            var objectIdRaw = query["@ObjectId"] ?? query["ObjectId"];
            var entityName = TrimQuotes(query["@EntityName"] ?? query["EntityName"]);
            if (string.IsNullOrEmpty(objectIdRaw) || string.IsNullOrEmpty(entityName))
            {
                throw new NotSupportedException("RetrievePrincipalAccessInfo missing parameters");
            }

            var targetEntity = this.Context.MetadataCache.GetEntityFromSetName(entityName) ?? this.Context.MetadataCache.GetEntityFromLogicalName(entityName);
            if (targetEntity == null)
            {
                throw new NotSupportedException("Entity not found: " + entityName);
            }

            if (!Guid.TryParse(objectIdRaw, out var targetId))
            {
                throw new NotSupportedException("RetrievePrincipalAccessInfo ObjectId is not a Guid: " + objectIdRaw);
            }
            var targetRef = new EntityReference(targetEntity.LogicalName, targetId);

            conversionResult.ConvertedRequest = new RetrievePrincipalAccessRequest
            {
                Principal = principalRef,
                Target = targetRef
            };
                return;
            }

            throw new NotSupportedException("Bound function not supported: " + operationSegment.Identifier);
        }

        private static string TrimQuotes(string value)
        {
            if (string.IsNullOrEmpty(value))
                return value;
            if (value.Length >= 2 && value.StartsWith("'") && value.EndsWith("'"))
            {
                return value.Substring(1, value.Length - 2);
            }
            return value;
        }


    }
}
