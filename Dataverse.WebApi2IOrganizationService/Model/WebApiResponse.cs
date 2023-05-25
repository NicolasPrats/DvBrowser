using System.Collections.Specialized;

namespace Dataverse.WebApi2IOrganizationService.Model
{
    public class WebApiResponse
    {

        public byte[] Body { get;  set; }
        public NameValueCollection Headers { get; internal set; }
        public int StatusCode { get; internal set; }

        internal WebApiResponse()
        {
        }

    }
}
