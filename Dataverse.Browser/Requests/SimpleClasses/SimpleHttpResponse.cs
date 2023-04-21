using System.Collections.Specialized;

namespace Dataverse.Browser.Requests.SimpleClasses
{
    internal class SimpleHttpResponse
    {

        public byte[] Body { get; set; }
        public NameValueCollection Headers { get; set; }
        public int StatusCode { get; set; }

        public SimpleHttpResponse()
        {
        }

    }
}
