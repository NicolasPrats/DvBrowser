using System;
using Dataverse.WebApi2IOrganizationService.Model;
using Dataverse.Plugin.Emulator.ExecutionTree;

namespace Dataverse.Browser.Requests
{
    internal class InterceptedWebApiRequest
    {
        public ExecutionTreeNode ExecutionTreeRoot { get; internal set; }
        public RequestConversionResult ConversionResult { get; internal set; }
        public Exception ExecuteException { get; internal set; }


        public InterceptedWebApiRequest(RequestConversionResult conversionResult)
        {
            this.ConversionResult = conversionResult;
        }
    }
}
