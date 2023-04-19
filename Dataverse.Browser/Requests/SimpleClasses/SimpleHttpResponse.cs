using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CefSharp;

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
