using System;
using System.IO;
using System.Windows.Forms;
using Dataverse.Browser.Context;
using Dataverse.Browser.Requests;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace Dataverse.Browser.UI
{
    internal partial class BrowserTab : UserControl
    {
        private delegate void StringDelegate(string value);
        private WebView2 webView;
        private WebViewRequestRouter requestRouter;

        public BrowserTab(BrowserContext context, string url = null)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

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
            var userDataFolder = Path.Combine(context.CachePath, "WebView2");
            Directory.CreateDirectory(userDataFolder);
            this.webView.CreationProperties = new CoreWebView2CreationProperties
            {
                UserDataFolder = userDataFolder
            };

            await this.webView.EnsureCoreWebView2Async();
            this.requestRouter = new WebViewRequestRouter(context, this.webView.CoreWebView2.Environment);

            var core = this.webView.CoreWebView2;
            core.AddWebResourceRequestedFilter("*", CoreWebView2WebResourceContext.All);
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
