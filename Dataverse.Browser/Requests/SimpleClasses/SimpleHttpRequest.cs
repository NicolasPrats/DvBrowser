using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CefSharp;

namespace Dataverse.Browser.Requests.SimpleClasses
{
    internal class SimpleHttpRequest
    {
        public string Method { get; set; }
        public string LocalPath { get; set; }
        public string Body { get; set; }
        public IRequest OriginRequest { get;  }

        public SimpleHttpRequest()
        {
        }

        public SimpleHttpRequest(IRequest request, string localPath)
        {
            if (request is null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            this.OriginRequest = request;
            this.Method = request.Method;
            this.LocalPath = localPath;
            this.Body = ExtractRequestBody(request);
        }

        private static string ExtractRequestBody(IRequest request)
        {
            if (request.PostData == null || request.PostData.Elements.Count == 0)
            {
                return null;
            }
            if (request.PostData.Elements.Count != 1)
            {
                throw new ApplicationException("Unable to parse body");
            }
            var postDataElement = request.PostData.Elements.FirstOrDefault();
            if (postDataElement.Type != PostDataElementType.Bytes)
            {
                throw new ApplicationException("Unknown body type");
            }
            //TODO encoding
            var body = Encoding.UTF8.GetString(postDataElement.Bytes);
            //webApiRequest.Body = body;
            return body;
        }
    }
}
