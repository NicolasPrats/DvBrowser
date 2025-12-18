using System;
using Dataverse.Browser.Context;
using Microsoft.Web.WebView2.Core;

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

        public WebViewRequestRouter(BrowserContext context, CoreWebView2Environment environment, bool alwaysProxyWebApi, bool interceptNonGetOnly)
        {
            this.context = context ?? throw new ArgumentNullException(nameof(context));
            this.environment = environment ?? throw new ArgumentNullException(nameof(environment));
            this.extensions = new ExtensionsRequestHandler();
            this.webApiRequestHandler = new WebApiRequestHandler(context, alwaysProxyWebApi, interceptNonGetOnly);
        }

        public async void HandleWebResourceRequested(object sender, CoreWebView2WebResourceRequestedEventArgs e)
        {
            if (e == null)
                return;

            // First serve fake webresources if matched.
            if (this.extensions.TryHandle(e, this.environment))
            {
                return;
            }

            var uri = e.Request?.Uri;
            if (string.IsNullOrEmpty(uri) || uri.IndexOf("/api/data/v9.", StringComparison.OrdinalIgnoreCase) < 0)
            {
                return;
            }

            // WebView2 supports async request handling via deferral.
            var deferral = e.GetDeferral();
            try
            {
                // Then handle intercepted Dataverse Web API calls.
                await this.webApiRequestHandler.TryHandleAsync(e, this.environment);
            }
            finally
            {
                deferral.Complete();
            }
        }
    }
}
