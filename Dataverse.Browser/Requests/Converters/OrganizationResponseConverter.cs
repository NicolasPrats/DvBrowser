using System;
using System.Collections.Specialized;
using System.Net.Http;
using System.Text;
using System.Text.Json.Nodes;
using Dataverse.Browser.Context;
using Dataverse.Browser.Requests.SimpleClasses;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;

namespace Dataverse.Browser.Requests.Converters
{
    internal static class OrganizationResponseConverter
    {
        internal static SimpleHttpResponse Convert(DataverseContext context, InterceptedWebApiRequest webApiRequest, OrganizationResponse response)
        {
            switch (response)
            {
                case CreateResponse createResponse:
                    return ConvertCreateResponse(context, (CreateRequest)webApiRequest.ConvertedRequest, createResponse);
                case UpdateResponse _:
                    return ConvertUpdateResponse(context, (UpdateRequest)webApiRequest.ConvertedRequest);
                case RetrieveResponse _:
                    return ConvertRetrieveResponse(context, webApiRequest);
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

        private static SimpleHttpResponse ConvertCustomApiResponse(OrganizationResponse organizationResponse)
        {
            var body = new JsonObject();
            foreach (var property in organizationResponse.Results)
            {
                switch (property.Value)
                {
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
                    case Single singleValue:
                        body[property.Key] = singleValue;
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
                    default:
                        throw new NotImplementedException($"Message has been executed but response cannot be generated. Parameter:{property.Key}={property.Value}");
                }
            }
            string jsonBody = body.ToJsonString();
            return new SimpleHttpResponse()
            {
                Body = Encoding.UTF8.GetBytes(jsonBody),
                Headers = new NameValueCollection
                {
                    { "OData-Version", "4.0" }
                },
                StatusCode = 204
            };
        }

        private static SimpleHttpResponse ConvertDeleteResponse()
        {
            return new SimpleHttpResponse()
            {
                Body = new byte[0],
                Headers = new NameValueCollection
                {
                    { "OData-Version", "4.0" }
                },
                StatusCode = 204
            };
        }

        private static SimpleHttpResponse ConvertUpdateResponse(DataverseContext context, UpdateRequest updateRequest)
        {
            string setName = context.MetadataCache.GetEntityFromLogicalName(updateRequest.Target.LogicalName).EntitySetName;
            var id = $"{context.WebApiBaseUrl}{setName}({updateRequest.Target.Id})";
            return new SimpleHttpResponse()
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

        private static SimpleHttpResponse ConvertCreateResponse(DataverseContext context, CreateRequest createRequest, CreateResponse createResponse)
        {
            string setName = context.MetadataCache.GetEntityFromLogicalName(createRequest.Target.LogicalName).EntitySetName;
            var id = $"{context.WebApiBaseUrl}{setName}({createResponse.id})";

            //TODO: convert the response instead of requesting
            var retrieveResult = HttpGet(context, id, true);
            return new SimpleHttpResponse()
            {
                Body = retrieveResult.Content.ReadAsByteArrayAsync().Result,
                Headers = new NameValueCollection
                {
                    { "OData-Version", "4.0" },
                    { "OData-EntityId", id }
                },
                StatusCode = 204
            };
        }

        private static SimpleHttpResponse ConvertRetrieveResponse(DataverseContext context, InterceptedWebApiRequest webApiRequest)
        {
            //TODO: convert the response instead of requesting
            string url = $"https://{context.Host}{webApiRequest.SimpleHttpRequest.LocalPathWithQuery}";
            HttpResponseMessage retrieveResult = HttpGet(context, url, false);
            NameValueCollection headers = new NameValueCollection();
            foreach (var header in retrieveResult.Headers)
            {
                headers.Add(header.Key, String.Concat(header.Value, ","));
            }
            return new SimpleHttpResponse()
            {
                Body = retrieveResult.Content.ReadAsByteArrayAsync().Result,
                Headers = headers,
                StatusCode = 204
            };
        }

        private static HttpResponseMessage HttpGet(DataverseContext context, string url, bool bypassPLugins)
        {
            HttpRequestMessage retrieveMessage = new HttpRequestMessage(HttpMethod.Get, url);
            retrieveMessage.Headers.Add("Authorization", "Bearer " + context.CrmServiceClient.CurrentAccessToken);
            if (bypassPLugins)
            {
                retrieveMessage.Headers.Add("MSCRM.BypassCustomPluginExecution", "true");
            }
            var retrieveResult = context.HttpClient.SendAsync(retrieveMessage).Result;
            return retrieveResult;
        }
    }
}
