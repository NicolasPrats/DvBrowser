using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CefSharp;
using Microsoft.Xrm.Sdk.Messages;
using Dataverse.Plugin.Emulator;
using Dataverse.Browser.Context;
using Dataverse.Browser.Requests.Converter;

namespace Dataverse.Browser.Requests
{
    internal class BrowserRequestHandler : CefSharp.Handler.RequestHandler
    {
        private DataverseContext Context { get; }
        private WebApiRequestConverter Converter { get; }

        public BrowserRequestHandler(DataverseContext context)
        {
            this.Context = context ?? throw new ArgumentNullException(nameof(context));
            this.Converter = new WebApiRequestConverter(this.Context);
        }


        protected override IResourceRequestHandler GetResourceRequestHandler(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IRequest request, bool isNavigation, bool isDownload, string requestInitiator, ref bool disableDefaultHandling)
        {
            var webApiRequest = Converter.ConvertUnknowRequestToOrganizationRequest(request);
            if (webApiRequest == null)
            {
                return null;
            }
            this.Context.LastRequests.AddRequest(webApiRequest);
            if (webApiRequest.ConvertedRequest == null || !this.Context.IsEnabled)
            {
                return null;
            }
            return new WebApiResourceRequestHandler(this.Context, webApiRequest);

        }
    }
}
