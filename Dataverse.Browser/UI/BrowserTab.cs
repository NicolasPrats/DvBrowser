using System;
using System.Windows.Forms;
using CefSharp;
using CefSharp.WinForms;
using Dataverse.Browser.Context;
using Dataverse.Browser.Requests;

namespace Dataverse.Browser.UI
{
    internal partial class BrowserTab : UserControl
    {
        private delegate void StringDelegate(string value);
        public ChromiumWebBrowser CurrentBrowser { get; }

        public BrowserTab(BrowserContext context)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            InitializeComponent();

            this.Dock = DockStyle.Fill;
            this.CurrentBrowser = new ChromiumWebBrowser("https://" + context.Host)
            {
                RequestHandler = new BrowserRequestHandler(context),
                KeyboardHandler = new Dataverse.UI.BrowserHandlers.KeyboardHandler()
            };
            this.CurrentBrowser.AddressChanged += CurrentBrowser_AddressChanged;
            this.panel1.Controls.Add(this.CurrentBrowser);
            this.txtAddress.KeyPress += TxtAddress_KeyPress;
        }

        private void TxtAddress_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == 13)
            {
                this.CurrentBrowser.LoadUrl(this.txtAddress.Text);
            }
        }

        private void CurrentBrowser_AddressChanged(object sender, AddressChangedEventArgs e)
        {
            SetTxtAddress(e.Address);
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
            this.CurrentBrowser.ShowDevTools();
        }
    }
}
