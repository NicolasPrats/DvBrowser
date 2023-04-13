using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
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
                default:
                    throw new NotImplementedException("Message has been executed but response is not implemented:" + response.GetType().Name);
            }
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
            HttpRequestMessage retrieveMessage = new HttpRequestMessage(HttpMethod.Get, id);
            retrieveMessage.Headers.Add("Authorization", "Bearer " + context.CrmServiceClient.CurrentAccessToken);
            retrieveMessage.Headers.Add("MSCRM.BypassCustomPluginExecution", "true");
            var retrieveResult = context.HttpClient.SendAsync(retrieveMessage).Result;
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
    }
}
