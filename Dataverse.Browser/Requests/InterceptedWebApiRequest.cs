using System;
using Dataverse.Browser.Requests.Model;
using Dataverse.Plugin.Emulator.ExecutionTree;
using Microsoft.Xrm.Sdk;

namespace Dataverse.Browser.Requests
{
    internal class InterceptedWebApiRequest
    {
        public ExecutionTreeNode ExecutionTreeRoot { get; internal set; }
        public RequestConversionResult ConversionResult { get; internal set; }
        
    }
}
