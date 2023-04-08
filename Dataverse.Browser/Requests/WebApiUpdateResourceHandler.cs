using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using Microsoft.Xrm.Sdk.Messages;
using Dataverse.Plugin.Emulator;

namespace Dataverse.Browser.Requests
{

    internal class WebApiUpdateResourceHandler
         : BaseResourceHandler<UpdateRequest>
    {


        protected override void Execute()
        {

            this.ExecuteWithTree();
            string setName = this.Context.MetadataCache.GetEntityFromLogicalName(this.Request.Target.LogicalName).EntitySetName;
            var id = $"{this.Context.WebApiBaseUrl}{setName}({this.Request.Target.Id})";
            this.ResultBody = new byte[] { };
            this.ResultHeaders = new NameValueCollection
                {
                    { "OData-Version", "4.0" },
                    { "OData-EntityId", id }
                };
            this.ResultStatusCode = 204;

        }
    }


}
