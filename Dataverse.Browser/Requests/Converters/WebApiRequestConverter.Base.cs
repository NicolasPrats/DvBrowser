using System;
using System.Activities.DurableInstancing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows.Shapes;
using CefSharp;
using CefSharp.DevTools.DOM;
using Dataverse.Browser.Constants;
using Dataverse.Browser.Context;
using Dataverse.Browser.Requests.SimpleClasses;
using Microsoft.OData.Edm;
using Microsoft.OData.UriParser;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Tab;

namespace Dataverse.Browser.Requests.Converter
{
    internal partial class WebApiRequestConverter

    {
        private DataverseContext Context { get; }

        public WebApiRequestConverter(DataverseContext context)
        {

            this.Context = context ?? throw new ArgumentNullException(nameof(context));

        }

        internal InterceptedWebApiRequest ConvertUnknowRequestToOrganizationRequest(IRequest request)
        {
            var url = new Uri(request.Url);
            var localPathWithQuery = url.LocalPath + url.Query;
            if (!localPathWithQuery.StartsWith("/api/data/v9."))
                return null;
            SimpleHttpRequest simplifiedRequest;
            try
            {
                simplifiedRequest = new SimpleHttpRequest(request, localPathWithQuery);
            }
            catch (Exception ex)
            {
                return new InterceptedWebApiRequest()
                {
                    SimpleHttpRequest = new SimpleHttpRequest() { LocalPathWithQuery = localPathWithQuery, Method = request.Method },
                    ConvertFailureMessage = ex.Message,
                    ExecuteException = ex
                };
            }
            return ConvertDataApiSimplifiedRequestToOrganizationRequest(simplifiedRequest);
        }

        private InterceptedWebApiRequest ConvertUnknowSimplifiedRequestToOrganizationRequest(SimpleHttpRequest request)
        {
            if (!request.LocalPathWithQuery.StartsWith("/api/data/v9."))
                return null;
            return ConvertDataApiSimplifiedRequestToOrganizationRequest(request);
        }

        private InterceptedWebApiRequest ConvertDataApiSimplifiedRequestToOrganizationRequest(SimpleHttpRequest request)
        {
            InterceptedWebApiRequest webApiRequest = new InterceptedWebApiRequest()
            {
                SimpleHttpRequest = request
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
                webApiRequest.ConvertFailureMessage = "Unable to parse: " + ex.Message;
                return webApiRequest;
            }
            try
            {
                switch (request.Method)
                {
                    case "POST":
                        if (path.Count != 1)
                        {
                            throw new NotImplementedException("POST is not implemented for: " + path.Count + " segments");
                        }
                        if (path.FirstSegment.EdmType?.TypeKind == EdmTypeKind.Collection)
                        {
                            ConvertToCreateUpdateRequest(webApiRequest, path);
                        }
                        else if (path.FirstSegment.Identifier == "$batch")
                        {
                            if (request.OriginRequest == null)
                            {
                                throw new NotSupportedException("batch requests embedded in another batch request are not supported!");
                            }
                            ConvertToExecuteMultipleRequest(webApiRequest);
                        }
                        else if (path.FirstSegment.EdmType == null && path.FirstSegment is OperationImportSegment operationImport)
                        {
                            string identifier = path.FirstSegment.Identifier;
                            var declaredOperation = this.Context.Model.FindDeclaredOperationImports(identifier).Single();
                            ConvertToAction(declaredOperation.Operation, webApiRequest);
                        }
                        else
                        {
                            throw new NotImplementedException("POST is not implemented for: " + path.FirstSegment.EdmType?.TypeKind);
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
                        ConvertToCreateUpdateRequest(webApiRequest, path);
                        break;
                    case "GET":
                        switch (path.Count)
                        {
                            case 1:
                                throw new NotImplementedException("Retrievemultiple are not implemented");
                            case 2:
                                ConvertToRetrieveRequest(webApiRequest, parser, path);
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
                        ConvertToDeleteRequest(webApiRequest, path);
                        break;
                    default:
                        webApiRequest.ConvertFailureMessage = "method not implemented";
                        break;
                }
            }
            catch (Exception ex)
            {
                webApiRequest.ConvertFailureMessage = ex.Message;
            }
            return webApiRequest;
        }


    }
}
