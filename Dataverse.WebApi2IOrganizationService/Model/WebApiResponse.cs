using System.Collections.Specialized;

namespace Dataverse.WebApi2IOrganizationService.Model
{
    public class WebApiResponse
    {

        public byte[] Body { get; set; }
        public NameValueCollection Headers { get; set; }
        public int StatusCode { get; set; }

        public WebApiResponse()
        {
        }

    }
}
