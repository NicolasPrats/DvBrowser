using System;
using System.Linq;
using Dataverse.Utils;
using Dataverse.WebApi2IOrganizationService.Model;
using Microsoft.OData.Edm;
using Microsoft.OData.UriParser;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Metadata;

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
            RequestConversionResult result = new RequestConversionResult()
            {
                SrcRequest = request
            };
            ODataUriParser parser;
            ODataPath path;
            try
            {
                parser = new ODataUriParser(this.Context.Model, new Uri(request.LocalPathWithQuery.Substring(15), UriKind.Relative))
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


    }
}
