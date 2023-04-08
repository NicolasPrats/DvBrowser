using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net.Http;
using Microsoft.Xrm.Sdk.Messages;
using Dataverse.Plugin.Emulator;
using Microsoft.Xrm.Sdk;

namespace Dataverse.Browser.Requests
{

    internal class WebApiCreateResourceHandler
         : BaseResourceHandler<CreateRequest>
    {
        
        protected override void Execute()
        {

            var createResponse = (CreateResponse)(ExecuteWithTree());

            string setName = this.Context.MetadataCache.GetEntityFromLogicalName(this.Request.Target.LogicalName).EntitySetName;
            var id = $"{this.Context.WebApiBaseUrl}{setName}({createResponse.id})";


            HttpRequestMessage retrieveMessage = new HttpRequestMessage(HttpMethod.Get, id);
            retrieveMessage.Headers.Add("Authorization", "Bearer " + this.Context.CrmServiceClient.CurrentAccessToken);

            var retrieveResult = this.Context.HttpClient.SendAsync(retrieveMessage).Result;
            this.ResultBody = retrieveResult.Content.ReadAsByteArrayAsync().Result;
            this.ResultHeaders = new NameValueCollection
                {
                    { "OData-Version", "4.0" },
                    { "OData-EntityId", id }
                };
            this.ResultStatusCode = 204;

        }

      
    }


}
