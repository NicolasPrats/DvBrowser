using System;
using CefSharp;
using Dataverse.Browser.Context;

namespace Dataverse.Browser.Requests
{
    internal class WebApiResourceRequestHandler
        : CefSharp.Handler.ResourceRequestHandler
    {
        protected BrowserContext Context { get; }
        public InterceptedWebApiRequest WebApiRequest { get; }
        public WebApiResourceRequestHandler(BrowserContext context, InterceptedWebApiRequest webApiRequest)
        {
            this.Context = context ?? throw new ArgumentNullException(nameof(context));
            this.WebApiRequest = webApiRequest ?? throw new ArgumentNullException(nameof(webApiRequest));
        }

        protected override IResourceHandler GetResourceHandler(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IRequest request)
        {
            return new WebApiResourceHandler(this.Context, this.WebApiRequest);
        }
    }

}
