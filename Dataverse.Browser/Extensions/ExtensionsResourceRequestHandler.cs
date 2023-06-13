using System.IO;
using CefSharp;
using CefSharp.Handler;

namespace Dataverse.Browser.Requests
{
    internal class ExtensionsResourceRequestHandler :
        ResourceRequestHandler
    {
        public MemoryStream Webresource { get; }

        public ExtensionsResourceRequestHandler(MemoryStream webresource)
        {
            this.Webresource = webresource;
        }

        protected override IResourceHandler GetResourceHandler(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IRequest request)
        {
            return ResourceHandler.FromStream(this.Webresource);
        }


    }
}