using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CefSharp;
using Microsoft.Xrm.Sdk.Messages;
using Dataverse.Plugin.Emulator;
using Dataverse.Browser.Context;

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
            var webApiRequest = Converter.ConvertToOrganizationRequest(request);
            if (webApiRequest == null)
            {
                return null;
            }
            this.Context.LastRequests.AddRequest(webApiRequest);
            if (webApiRequest.ConvertedRequest == null)
            {
                return null;
            }
            switch (webApiRequest.ConvertedRequest)
            {
                case CreateRequest createRequest:
                    //TODO headers
                    // "prefer"
                    //"mscrm.suppressduplicatedetection"
                    return new WebApiResourceRequestHandler<CreateRequest, WebApiCreateResourceHandler>(this.Context, webApiRequest, createRequest);
                    case UpdateRequest updateRequest:
                    return new WebApiResourceRequestHandler<UpdateRequest, WebApiUpdateResourceHandler>(this.Context, webApiRequest, updateRequest);
                default:
                    return null;
            }

        }
    }
}
