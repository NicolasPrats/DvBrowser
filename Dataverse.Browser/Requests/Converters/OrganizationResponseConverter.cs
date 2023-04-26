using System;
using System.Collections.Specialized;
using System.Net.Http;
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
                    throw new NotImplementedException("Message has been executed but response is not implemented:" + response.GetType().Name);
            }
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
