using System;
using CefSharp;
using CefSharp.Handler;
using CefSharp.WinForms;

namespace Dataverse.UI.BrowserHandlers
{
    /// <summary>
    /// Redirects popups (target=_blank, window.open) to the current tab to avoid silent failures.
    /// </summary>
    internal class PopupHandler : LifeSpanHandler
    {
        private readonly ChromiumWebBrowser browser;

        public PopupHandler(ChromiumWebBrowser browser)
        {
            this.browser = browser ?? throw new ArgumentNullException(nameof(browser));
        }

        protected override bool OnBeforePopup(IWebBrowser chromiumWebBrowser, IBrowser browserControl, IFrame frame, string targetUrl, string targetFrameName, WindowOpenDisposition targetDisposition, bool userGesture, IPopupFeatures popupFeatures, IWindowInfo windowInfo, IBrowserSettings browserSettings, ref bool noJavascriptAccess, out IWebBrowser newBrowser)
        {
            newBrowser = null;
            if (!string.IsNullOrWhiteSpace(targetUrl))
            {
                // Load the popup destination into the existing tab.
                browser.Load(targetUrl);
            }
            return true; // cancel default popup creation
        }
    }
}
