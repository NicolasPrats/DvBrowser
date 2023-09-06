using System;
using System.Collections.Generic;
using Microsoft.Xrm.Sdk;

namespace Dataverse.Plugin.Emulator
{
    internal class DataCache
    {

        private IOrganizationService Service { get; }


        public DataCache(IOrganizationService service)
        {
            this.Service = service;
        }

        private readonly Dictionary<Guid, Guid> SystemUserId2AzureAd = new Dictionary<Guid, Guid>();
        public Guid GetAzureADIdFromSystemUserId(Guid systemuserid)
        {
            if (this.SystemUserId2AzureAd.TryGetValue(systemuserid, out Guid id))
            {
                return id;
            }
            var systemuser = this.Service.Retrieve("systemuser", systemuserid, new Microsoft.Xrm.Sdk.Query.ColumnSet("azureactivedirectoryobjectid"));
            id = systemuser.GetAttributeValue<Guid>("azureactivedirectoryobjectid");
            this.SystemUserId2AzureAd[systemuserid] = id;
            return id;
        }


    }
}
