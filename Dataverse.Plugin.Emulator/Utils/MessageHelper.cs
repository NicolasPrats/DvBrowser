using System;
using System.Reflection;
using Microsoft.Xrm.Sdk;

namespace Dataverse.Plugin.Emulator.Utils
{
    internal static class MessageHelper
    {
        internal static OrganizationRequest GetSpecializedMessage(OrganizationRequest request)
        {
            if (request.GetType() != typeof(OrganizationRequest))
                return request;
            string targetTypeName = "Microsoft.Xrm.Sdk.Messages." + request.RequestName + "Request";
            var assembly = Assembly.GetAssembly(typeof(OrganizationRequest));
            var targetType = assembly.GetType(targetTypeName);
            if (targetType == null)
                return request;
            var newRequest = (OrganizationRequest)Activator.CreateInstance(targetType);
            newRequest.ExtensionData = request.ExtensionData;
            newRequest.RequestId = request.RequestId;
            newRequest.Parameters = request.Parameters;
            return newRequest;
        }

        internal static OrganizationResponse GetSpecializedMessage(OrganizationResponse response)
        {
            if (response.GetType() != typeof(OrganizationResponse))
                return response;
            string targetTypeName = "Microsoft.Xrm.Sdk.Messages." + response.ResponseName + "Response";
            var assembly = Assembly.GetAssembly(typeof(OrganizationResponse));
            var targetType = assembly.GetType(targetTypeName);
            if (targetType == null)
                return response;
            var newResponse = (OrganizationResponse)Activator.CreateInstance(targetType);
            newResponse.ExtensionData = response.ExtensionData;
            newResponse.Results = response.Results;
            return newResponse;
        }

    }
}
