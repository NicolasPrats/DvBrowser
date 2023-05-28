using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json.Nodes;
using Dataverse.Utils;
using Dataverse.WebApi2IOrganizationService.Model;
using Microsoft.OData.Edm;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;

namespace Dataverse.WebApi2IOrganizationService.Converters
{
    public class ResponseConverter
    {
        internal DataverseContext Context { get; }

        public ResponseConverter(DataverseContext context)
        {
            this.Context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public WebApiResponse Convert(Exception ex)
        {

            var errorText = JavaScriptEncoder.UnsafeRelaxedJsonEscaping.Encode(ex.Message);
            var errorDetails = JavaScriptEncoder.UnsafeRelaxedJsonEscaping.Encode(ex.ToString());

            byte[] body = Encoding.UTF8.GetBytes(
$@"{{
                ""error"":
                    {{
                    ""code"":""0x80040265"",
                    ""message"":""{errorText}""
                    //""@Microsoft.PowerApps.CDS.ErrorDetails.HttpStatusCode"":""400"",
                    //""@Microsoft.PowerApps.CDS.InnerError"":""{errorText}"",
                    //""@Microsoft.PowerApps.CDS.TraceText"":""{errorDetails}""
                    }}
                }}");

            return new WebApiResponse()
            {
                StatusCode = 400,
                Body = body,
                Headers = new NameValueCollection()
                {
                    { "OData-Version", "4.0" },
                    { "Content-Type", "application/json; odata.metadata = minimal" },
                    { "Content-Length", body.Length.ToString() }
                }
            };
        }

        public WebApiResponse Convert(RequestConversionResult conversionResult, OrganizationResponse response)
        {
            switch (response)
            {
                case CreateResponse createResponse:
                    return ConvertCreateResponse((CreateRequest)conversionResult.ConvertedRequest, createResponse);
                case UpdateResponse _:
                    return ConvertUpdateResponse((UpdateRequest)conversionResult.ConvertedRequest);
                case RetrieveResponse _:
                    return ConvertRetrieveResponse(conversionResult);
                case DeleteResponse _:
                    return ConvertDeleteResponse();
                default:
                    if (response.GetType() != typeof(OrganizationResponse))
                    {
                        throw new NotImplementedException("Message has been executed but response is not implemented:" + response.GetType().Name);
                    }
                    //OrganizationResponse without specialized type are assumed to be CustomApi
                    return ConvertCustomApiResponse(response);

            }
        }

        private WebApiResponse ConvertCustomApiResponse(OrganizationResponse organizationResponse)
        {

            if (organizationResponse.Results.Count == 0)
            {
                return new WebApiResponse()
                {
                    Body = null,
                    Headers = new NameValueCollection
                {
                    { "OData-Version", "4.0" }
                },
                    StatusCode = 204
                };
            }

            var body = new JsonObject();
            body["@odata.context"] = $"https://{this.Context.Host}/api/data/v9.2/$metadata#Microsoft.Dynamics.CRM.{organizationResponse.ResponseName}Response";
            foreach (var property in organizationResponse.Results)
            {
                AddValueToJsonObject(body, property, null);
            }
            string jsonBody = body.ToJsonString();
            return new WebApiResponse()
            {
                Body = Encoding.UTF8.GetBytes(jsonBody),
                Headers = new NameValueCollection
                {
                    { "OData-Version", "4.0" }
                },
                StatusCode = 200
            };
        }

        private void AddValueToJsonObject(JsonObject body, KeyValuePair<string, object> property, string currentEntityLogicalName)
        {
            switch (property.Value)
            {
                case null:
                    body[property.Key] = null;
                    break;
                case string strValue:
                    body[property.Key] = strValue;
                    break;
                case int intValue:
                    body[property.Key] = intValue;
                    break;
                case byte byteValue:
                    body[property.Key] = byteValue;
                    break;
                case Guid guidValue:
                    body[property.Key] = guidValue;
                    break;
                case float floatValue:
                    body[property.Key] = floatValue;
                    break;
                case double doubleValue:
                    body[property.Key] = doubleValue;
                    break;
                case decimal decimalValue:
                    body[property.Key] = decimalValue;
                    break;
                case DateTime dateTimeValue:
                    body[property.Key] = dateTimeValue;
                    break;
                case bool boolValue:
                    body[property.Key] = boolValue;
                    break;
                case OptionSetValue optionSetValue:
                    body[property.Key] = optionSetValue.Value;
                    break;
                case EntityReference entityReferenceValue:
                    if (currentEntityLogicalName == null)
                        throw new Exception("TODO response property");
                    var entityTypeDefinition = (IEdmStructuredType)this.Context.Model.FindDeclaredType("Microsoft.Dynamics.CRM." + currentEntityLogicalName);
                    var declaredProperty = entityTypeDefinition.DeclaredProperties.Where(p => p.Name == property.Key).Single();
                    if (declaredProperty.PropertyKind == EdmPropertyKind.Navigation)
                    {
                        body["_" + property.Key + "_value"] = entityReferenceValue.Id;
                    }
                    else
                    {
                        body[property.Key] = entityReferenceValue.Id;
                    }
                    break;
                case Entity record:
                    var recordJson = new JsonObject();
                    body[property.Key] = recordJson;
                    //recordJson["@odata.type"] = "Microsoft.Dynamics.CRM." + record.LogicalName;
                    recordJson["@odata.etag"] = "DvbError: NotImplemented";
                    //dataverse seems to always add the column ownerid
                    if (!record.Contains("ownerid"))
                    {
                        recordJson["ownerid"] = this.Context.CrmServiceClient.GetMyCrmUserId();
                    }
                    foreach (var kvp in record.Attributes)
                    {
                        AddValueToJsonObject(recordJson, kvp, record.LogicalName);
                    }
                    break;
                default:
                    throw new NotImplementedException($"Message has been executed but response cannot be generated. Parameter:{property.Key}={property.Value}");
            }
        }

        private WebApiResponse ConvertDeleteResponse()
        {
            return new WebApiResponse()
            {
                Body = new byte[0],
                Headers = new NameValueCollection
                {
                    { "OData-Version", "4.0" }
                },
                StatusCode = 204
            };
        }

        private WebApiResponse ConvertUpdateResponse(UpdateRequest updateRequest)
        {
            string setName = this.Context.MetadataCache.GetEntityFromLogicalName(updateRequest.Target.LogicalName).EntitySetName;
            var id = $"{this.Context.WebApiBaseUrl}{setName}({updateRequest.Target.Id})";
            return new WebApiResponse()
            {
                Body = new byte[0],
                Headers = new NameValueCollection
                {
                    { "OData-Version", "4.0" },
                    { "OData-EntityId", id }
                },
                StatusCode = 204
            };
        }

        private WebApiResponse ConvertCreateResponse(CreateRequest createRequest, CreateResponse createResponse)
        {
            string setName = this.Context.MetadataCache.GetEntityFromLogicalName(createRequest.Target.LogicalName).EntitySetName;
            var id = $"{this.Context.WebApiBaseUrl}{setName}({createResponse.id})";

            //TODO: convert the response instead of requesting
            //TODO : returns result only if return representation was requested
            var retrieveResult = HttpGet(id, true);
            return new WebApiResponse()
            {
                Body = retrieveResult.Content.ReadAsByteArrayAsync().Result,
                Headers = new NameValueCollection
                {
                    { "OData-Version", "4.0" },
                    { "OData-EntityId", id }
                },
                StatusCode = 201
            };
        }

        private WebApiResponse ConvertRetrieveResponse(RequestConversionResult conversionResult)
        {
            //TODO: convert the response instead of requesting
            string url = $"https://{this.Context.Host}{conversionResult.SrcRequest.LocalPathWithQuery}";
            HttpResponseMessage retrieveResult = HttpGet(url, false);
            NameValueCollection headers = new NameValueCollection();
            foreach (var header in retrieveResult.Headers)
            {
                headers.Add(header.Key, String.Concat(header.Value, ","));
            }
            return new WebApiResponse()
            {
                Body = retrieveResult.Content.ReadAsByteArrayAsync().Result,
                Headers = headers,
                StatusCode = 204
            };
        }

        private HttpResponseMessage HttpGet(string url, bool bypassPLugins)
        {
            HttpRequestMessage retrieveMessage = new HttpRequestMessage(HttpMethod.Get, url);
            retrieveMessage.Headers.Add("Authorization", "Bearer " + this.Context.CrmServiceClient.CurrentAccessToken);
            if (bypassPLugins)
            {
                retrieveMessage.Headers.Add("MSCRM.BypassCustomPluginExecution", "true");
            }
            var retrieveResult = this.Context.HttpClient.SendAsync(retrieveMessage).Result;
            return retrieveResult;
        }
    }
}
