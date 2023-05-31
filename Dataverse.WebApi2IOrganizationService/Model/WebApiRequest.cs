using System;
using System.Collections.Specialized;

namespace Dataverse.WebApi2IOrganizationService.Model
{
    public class WebApiRequest
    {
        public string Method { get; }
        public string LocalPathWithQuery { get; }
        public string Body { get; }
        public NameValueCollection Headers { get; }

        public static WebApiRequest Create(string method, string url, NameValueCollection headers, string body = null)
        {
            var uri = new Uri(url);
            var localPathWithQuery = uri.LocalPath + uri.Query;
            return CreateFromLocalPathWithQuery(method, localPathWithQuery, headers, body);
        }

        public static WebApiRequest CreateFromLocalPathWithQuery(string method, string localPathWithQuery, NameValueCollection headers, string body = null)
        {
            if (!localPathWithQuery.StartsWith("/api/data/v9."))
                return null;
            return new WebApiRequest(method, localPathWithQuery, headers, body);
        }

        internal WebApiRequest(string method, string localPathWithQuery, NameValueCollection headers, string body)
        {
            this.Method = method ?? throw new ArgumentNullException(nameof(method));
            this.LocalPathWithQuery = localPathWithQuery ?? throw new ArgumentNullException(nameof(localPathWithQuery));
            this.Headers = headers ?? throw new ArgumentNullException(nameof(headers));
            this.Body = body;
        }


    }
}
