using System;
using System.IO;
using System.Diagnostics;
using System.Windows.Forms;
using Dataverse.Browser.Context;
using Dataverse.Browser.Requests;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace Dataverse.Browser.UI
{
    internal partial class BrowserTab : UserControl
    {
        // Bump this when changing WebView2 environment/profile behaviors to avoid corrupted state
        // causing runtime issues (IndexedDB/service worker/cache incompatibilities).
        private const int WebView2ProfileVersion = 2;

        private delegate void StringDelegate(string value);
        private WebView2 webView;
        private WebViewRequestRouter requestRouter;
        private readonly bool bypassServiceWorker;
        private readonly bool isRestBuilderTab;
        private readonly bool interceptNonGetOnly;

        public BrowserTab(BrowserContext context, string url = null, bool bypassServiceWorker = false, bool isRestBuilderTab = false, bool interceptNonGetOnly = false)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            this.bypassServiceWorker = bypassServiceWorker;
            this.isRestBuilderTab = isRestBuilderTab;
            this.interceptNonGetOnly = interceptNonGetOnly;

            InitializeComponent();

            this.Dock = DockStyle.Fill;
            this.webView = new WebView2
            {
                Dock = DockStyle.Fill
            };
            this.panel1.Controls.Add(this.webView);
            this.txtAddress.KeyPress += TxtAddress_KeyPress;
            this.webView.KeyDown += WebView_KeyDown;

            var startUrl = url ?? $"https://{context.Host}";
            InitializeWebViewAsync(context, startUrl);
        }

        private async void InitializeWebViewAsync(BrowserContext context, string startUrl)
        {
            var userDataFolder = Path.Combine(context.CachePath, $"WebView2_v{WebView2ProfileVersion}");
            Directory.CreateDirectory(userDataFolder);

            var env = await CoreWebView2Environment.CreateAsync(null, userDataFolder, null);

            await this.webView.EnsureCoreWebView2Async(env);
            this.requestRouter = new WebViewRequestRouter(context, env, alwaysProxyWebApi: false, interceptNonGetOnly: this.interceptNonGetOnly);

            var core = this.webView.CoreWebView2;
            if (this.isRestBuilderTab)
            {
                try
                {
                    // Enable DRB XTB mode inside WebView2: DRB will read chrome.webview.hostObjects.xtbSettings.
                    core.AddHostObjectToScript("xtbSettings", new DrbXtbSettings(context));
                    Trace.WriteLine("[BrowserTab] Added host object: xtbSettings");
                }
                catch (Exception ex)
                {
                    Trace.WriteLine("[BrowserTab] Failed to add host object xtbSettings: " + ex);
                }
            }
            // WebView2 filter patterns can be picky; register a couple of broad patterns to ensure we
            // receive events for both document navigations and subresource loads.
            core.AddWebResourceRequestedFilter("*", CoreWebView2WebResourceContext.All);
            core.AddWebResourceRequestedFilter("https://*/*", CoreWebView2WebResourceContext.All);
            core.AddWebResourceRequestedFilter("http://*/*", CoreWebView2WebResourceContext.All);
            // Some WebView2 versions are more reliable when Document is explicitly filtered.
            core.AddWebResourceRequestedFilter("*", CoreWebView2WebResourceContext.Document);
            core.AddWebResourceRequestedFilter("https://*/*", CoreWebView2WebResourceContext.Document);
            core.AddWebResourceRequestedFilter("http://*/*", CoreWebView2WebResourceContext.Document);
            core.WebResourceRequested += requestRouter.HandleWebResourceRequested;
            core.NewWindowRequested += (s, e) =>
            {
                e.Handled = true;
                if (!string.IsNullOrWhiteSpace(e.Uri))
                {
                    core.Navigate(e.Uri);
                }
            };
            core.HistoryChanged += (s, e) => SetTxtAddress(this.webView.Source?.ToString() ?? string.Empty);

            // Mimic DevTools "Bypass for network" without disabling IndexedDB/other features.
            // This is needed for some orgs where the service worker interferes with fake webresource loads.
            if (this.bypassServiceWorker)
            {
                try
                {
                    await core.CallDevToolsProtocolMethodAsync("Network.enable", "{}");
                    await core.CallDevToolsProtocolMethodAsync("Network.setBypassServiceWorker", "{\"bypass\":true}");
                    Trace.WriteLine("[BrowserTab] Enabled bypassServiceWorker for this tab via DevTools protocol.");
                }
                catch (Exception ex)
                {
                    // Best effort: do not block navigation if protocol isn't available.
                    Trace.WriteLine("[BrowserTab] Failed to enable bypassServiceWorker: " + ex);
                }
            }

            if (this.isRestBuilderTab)
            {
                // WebView2 version used here doesn't expose ConsoleMessageReceived.
                // Instead, inject a small script that forwards errors to the host via postMessage.
                core.WebMessageReceived += (s, e) =>
                {
                    try
                    {
                        string msg = null;
                        try
                        {
                            msg = e.TryGetWebMessageAsString();
                        }
                        catch
                        {
                            // fallback (older API surface)
                            msg = e.WebMessageAsJson;
                        }

                        if (string.IsNullOrEmpty(msg))
                            return;

                        if (msg.Length > 800)
                        {
                            msg = msg.Substring(0, 800) + "...";
                        }

                        Trace.WriteLine("[WebView2][REST][MSG] " + msg);
                    }
                    catch
                    {
                        // ignore logging issues
                    }
                };

                try
                {
                    var forwardErrorsScript = @"
(function () {
  function send(level, message) {
    try {
      if (window && window.chrome && window.chrome.webview && window.chrome.webview.postMessage) {
        window.chrome.webview.postMessage(JSON.stringify({ t: 'console', level: level, message: String(message || '') }));
      }
    } catch (e) { }
  }

  try {
    var parentXrm = 'unknown';
    var topXrm = 'unknown';
    var selfXrm = 'unknown';
    var hasXtbSettings = 'unknown';
    try { parentXrm = (typeof parent !== 'undefined') ? (typeof parent.Xrm) : 'no-parent'; } catch (e) { parentXrm = 'error'; }
    try { topXrm = (typeof top !== 'undefined') ? (typeof top.Xrm) : 'no-top'; } catch (e) { topXrm = 'error'; }
    try { selfXrm = (typeof window !== 'undefined') ? (typeof window.Xrm) : 'no-window'; } catch (e) { selfXrm = 'error'; }
    try {
      hasXtbSettings = (window && window.chrome && window.chrome.webview && window.chrome.webview.hostObjects && window.chrome.webview.hostObjects.xtbSettings) ? 'true' : 'false';
    } catch (e) { hasXtbSettings = 'error'; }
    send('info', 'init location=' + location.href + ' parentXrm=' + parentXrm + ' topXrm=' + topXrm + ' selfXrm=' + selfXrm + ' hasXtbSettings=' + hasXtbSettings);
  } catch (e) { }

  window.addEventListener('error', function (e) {
    try { send('error', (e && e.message) ? e.message : 'window.error'); } catch (ex) { }
  });

  window.addEventListener('unhandledrejection', function (e) {
    try {
      var r = (e && e.reason) ? e.reason : '';
      var msg = (r && r.message) ? r.message : r;
      send('unhandledrejection', msg);
    } catch (ex) { }
  });
})();";
                    await core.AddScriptToExecuteOnDocumentCreatedAsync(forwardErrorsScript);
                }
                catch (Exception ex)
                {
                    Trace.WriteLine("[BrowserTab] Failed to install error forwarder: " + ex);
                }
            }

            this.webView.Source = new Uri(startUrl);
            SetTxtAddress(startUrl);
        }

        private void WebView_KeyDown(object sender, KeyEventArgs e)
        {
            if (this.webView?.CoreWebView2 == null)
            {
                return;
            }
            switch (e.KeyCode)
            {
                case Keys.F5:
                    this.webView.CoreWebView2.Reload();
                    e.Handled = true;
                    break;
                case Keys.F12:
                    this.webView.CoreWebView2.OpenDevToolsWindow();
                    e.Handled = true;
                    break;
            }
        }

        private void TxtAddress_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == 13 && this.webView?.CoreWebView2 != null)
            {
                this.webView.CoreWebView2.Navigate(this.txtAddress.Text);
                e.Handled = true;
            }
        }

        private void SetTxtAddress(string address)
        {
            if (this.txtAddress.InvokeRequired)
            {
                StringDelegate del = SetTxtAddress;
                this.txtAddress.Invoke(del, address);
                return;
            }
            this.txtAddress.Text = address;
        }

        private void BtnDevTools_Click(object sender, EventArgs e)
        {
            this.webView?.CoreWebView2?.OpenDevToolsWindow();
        }
    }
}
