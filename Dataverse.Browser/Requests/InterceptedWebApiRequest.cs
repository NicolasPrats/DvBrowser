using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dataverse.Plugin.Emulator.ExecutionTree;
using Microsoft.Xrm.Sdk;

namespace Dataverse.Browser.Requests
{
    internal class InterceptedWebApiRequest
    {
        public ExecutionTreeNode ExecutionTreeRoot { get; internal set; }
        public Exception ExecuteException { get; internal set; }
        internal string Method { get; set; }
        internal string Url { get; set; }
        internal string Body {get; set; }


        internal string ConvertFailureMessage {get; set; }
        internal OrganizationRequest ConvertedRequest { get; set; }
    }
}
