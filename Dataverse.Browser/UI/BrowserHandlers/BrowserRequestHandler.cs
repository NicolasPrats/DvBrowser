using System;
using CefSharp;
using Dataverse.Browser.Context;

namespace Dataverse.Browser.Requests
{
    internal class BrowserRequestHandler : CefSharp.Handler.RequestHandler
    {

        private IRequestHandler[] Handlers { get; }


        public BrowserRequestHandler(BrowserContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }
            this.Handlers = new IRequestHandler[] {
                new ExtensionsRequestHandler(context),
                new WebApiRequestHandler(context)
            };
        }


        protected override IResourceRequestHandler GetResourceRequestHandler(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IRequest request, bool isNavigation, bool isDownload, string requestInitiator, ref bool disableDefaultHandling)
        {
            foreach (var handler in this.Handlers)
            {
                var result = handler.GetResourceRequestHandler(chromiumWebBrowser, browser, frame, request, isNavigation, isDownload, requestInitiator, ref disableDefaultHandling);
                if (result != null)
                {
                    return result;
                }
            }
            return null;
        }
    }
}
