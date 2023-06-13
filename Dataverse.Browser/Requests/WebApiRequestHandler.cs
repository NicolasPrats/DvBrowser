using System;
using System.Linq;
using System.Text;
using CefSharp;
using Dataverse.Browser.Context;
using Dataverse.WebApi2IOrganizationService.Converters;
using Dataverse.WebApi2IOrganizationService.Model;

namespace Dataverse.Browser.Requests
{
    internal class WebApiRequestHandler
        : IRequestHandler
    {
        private BrowserContext Context { get; }
        private RequestConverter RequestConverter { get; }

        public WebApiRequestHandler(BrowserContext context)
        {
            this.Context = context ?? throw new ArgumentNullException(nameof(context));
            this.RequestConverter = new RequestConverter(this.Context);
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
            if (postDataElement == null || postDataElement.Type == PostDataElementType.Empty)
            {
                return null;
            }
            if (postDataElement.Type != PostDataElementType.Bytes)
            {
                throw new ApplicationException("Unknown body type");
            }
            //TODO encoding
            var body = Encoding.UTF8.GetString(postDataElement.Bytes);
            //webApiRequest.Body = body;
            return body;
        }

        public IResourceRequestHandler GetResourceRequestHandler(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IRequest request, bool isNavigation, bool isDownload, string requestInitiator, ref bool disableDefaultHandling)
        {

            var webApiRequest = WebApiRequest.Create(request.Method, request.Url, request.Headers, ExtractRequestBody(request));

            if (webApiRequest == null)
            {
                return null;
            }
            var conversionResult = this.RequestConverter.Convert(webApiRequest);
            var interceptedRequest = new InterceptedWebApiRequest(conversionResult);
            this.Context.LastRequests.AddRequest(interceptedRequest);
            if (conversionResult.ConvertedRequest == null || !this.Context.IsEnabled)
            {
                return null;
            }
            return new WebApiResourceRequestHandler(this.Context, interceptedRequest);

        }
    }
}
