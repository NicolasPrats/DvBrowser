using System.Collections.Generic;
using Microsoft.Xrm.Sdk;

namespace Dataverse.WebApi2IOrganizationService.Model
{
    public class RequestConversionResult
    {
        public WebApiRequest SrcRequest { get; internal set; }
        public string ConvertFailureMessage { get; internal set; }
        public OrganizationRequest ConvertedRequest { get; internal set; }
        internal Dictionary<string, object> CustomData { get; } = new Dictionary<string, object>();

        internal RequestConversionResult()
        {
        }
    }
}
