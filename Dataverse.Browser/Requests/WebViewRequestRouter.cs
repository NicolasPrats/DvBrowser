using System;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;
using System.Diagnostics;
using Dataverse.Browser.Context;
using Dataverse.Plugin.Emulator.ExecutionTree;
using Dataverse.WebApi2IOrganizationService.Converters;
using Dataverse.WebApi2IOrganizationService.Model;
using Microsoft.Web.WebView2.Core;
using Microsoft.Xrm.Sdk;

namespace Dataverse.Browser.Requests
{
    /// <summary>
    /// Routes WebView2 resource requests to either the fake webresource server or the Web API emulator.
    /// </summary>
    internal class WebViewRequestRouter
    {
        private readonly BrowserContext context;
        private readonly CoreWebView2Environment environment;
        private readonly ExtensionsRequestHandler extensions;
        private readonly WebApiRequestHandler webApiRequestHandler;

        public WebViewRequestRouter(BrowserContext context, CoreWebView2Environment environment)
        {
            this.context = context ?? throw new ArgumentNullException(nameof(context));
            this.environment = environment ?? throw new ArgumentNullException(nameof(environment));
            this.extensions = new ExtensionsRequestHandler();
            this.webApiRequestHandler = new WebApiRequestHandler(context);
        }

        public void HandleWebResourceRequested(object sender, CoreWebView2WebResourceRequestedEventArgs e)
        {
            // First serve fake webresources if matched.
            if (this.extensions.TryHandle(e, this.environment))
            {
                return;
            }

            // Then handle intercepted Dataverse Web API calls.
            if (this.webApiRequestHandler.TryHandle(e, this.environment))
            {
                return;
            }
        }
    }
}
