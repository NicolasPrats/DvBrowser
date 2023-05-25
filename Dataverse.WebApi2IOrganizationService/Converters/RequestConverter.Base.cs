using System;
using System.Linq;
using Dataverse.WebApi2IOrganizationService.Model;
using Dataverse.Utils;
using Microsoft.OData.Edm;
using Microsoft.OData.UriParser;

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
                parser = new ODataUriParser(this.Context.Model, new Uri(request.LocalPathWithQuery.Substring(15), UriKind.Relative));
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
                if (!(path.LastSegment is OperationSegment operationImport))
                {
                    throw new NotImplementedException("Post with 3 segments are implemented only for custom api!");
                }
                string identifier = path.LastSegment.Identifier;
                var declaredOperation = this.Context.Model.FindDeclaredOperations(identifier).Single();
                ConvertToAction(declaredOperation, result, true);
            }

            void ManagePost1Segment()
            {
                if (path.FirstSegment.EdmType?.TypeKind == EdmTypeKind.Collection)
                {
                    ConvertToCreateUpdateRequest(result, path);
                }
                else if (path.FirstSegment.Identifier == "$batch")
                {
                    ConvertToExecuteMultipleRequest(result);
                }
                else if (path.FirstSegment is OperationImportSegment operationImport)
                {
                    string identifier = path.FirstSegment.Identifier;
                    var declaredOperation = this.Context.Model.FindDeclaredOperationImports(identifier).Single();
                    ConvertToAction(declaredOperation.Operation, result, false);
                }
                else
                {
                    throw new NotImplementedException("POST is not implemented for: " + path.FirstSegment.EdmType?.TypeKind);
                }
            }
        }


    }
}
