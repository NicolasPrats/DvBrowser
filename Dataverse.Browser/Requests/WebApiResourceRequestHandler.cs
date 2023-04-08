using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.Remoting.Contexts;
using System.Text;
using System.Threading.Tasks;
using CefSharp;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Dataverse.Plugin.Emulator;
using Dataverse.Browser.Context;

namespace Dataverse.Browser.Requests
{
    internal class WebApiResourceRequestHandler<T,U>
        : CefSharp.Handler.ResourceRequestHandler
        where T : OrganizationRequest
        where U : BaseResourceHandler<T>, new()
    {
        protected DataverseContext Context { get; }
        public InterceptedWebApiRequest WebApiRequest { get; }
        private T Request { get; }
        public WebApiResourceRequestHandler(DataverseContext context, InterceptedWebApiRequest webApiRequest, T request)
        {
            this.Context = context ?? throw new ArgumentNullException(nameof(context));
            this.WebApiRequest = webApiRequest ?? throw new ArgumentNullException(nameof(webApiRequest));
            this.Request = request ?? throw new ArgumentNullException(nameof(request));
        }

        protected override IResourceHandler GetResourceHandler(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IRequest request)
        {
            return new U()
            {
                Context = this.Context,
                Request = this.Request,
                WebApiRequest = this.WebApiRequest
            };
        }
    }

}
