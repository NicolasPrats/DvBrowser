using System;
using Microsoft.Xrm.Sdk;

namespace Dataverse.Plugin.Emulator.Services
{
    internal class EmulatedServiceEndpointNotificationService
        : IServiceEndpointNotificationService
    {
        public string Execute(EntityReference serviceEndpoint, IExecutionContext context)
        {
            throw new NotImplementedException("Service Endpoint is not supported");
        }
    }
}
