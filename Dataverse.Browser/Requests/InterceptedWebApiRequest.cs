using System;
using Dataverse.Browser.Requests.SimpleClasses;
using Dataverse.Plugin.Emulator.ExecutionTree;
using Microsoft.Xrm.Sdk;

namespace Dataverse.Browser.Requests
{
    internal class InterceptedWebApiRequest
    {
        public ExecutionTreeNode ExecutionTreeRoot { get; internal set; }
        public Exception ExecuteException { get; internal set; }
        internal SimpleHttpRequest SimpleHttpRequest { get; set; }


        internal string ConvertFailureMessage { get; set; }
        internal OrganizationRequest ConvertedRequest { get; set; }
    }
}
