using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Forms;
using Dataverse.Browser.Configuration;
using Dataverse.Browser.Context;

namespace Dataverse.Browser.UI
{
    internal partial class ContextSpinner : Form
    {
        public ContextFactory Factory { get; }
        public EnvironnementConfiguration SelectedEnvironment { get; }

        private delegate void AddTextDelegate(string text);
        private delegate void CloseDelegate();

        public ContextSpinner(ContextFactory factory, EnvironnementConfiguration selectedEnvironment)
        {
            InitializeComponent();
            factory.OnNewProgress += Factory_OnNewProgress;
            factory.OnError += Factory_OnError;
            factory.OnFinished += Factory_OnFinished;
            this.Factory = factory;
            this.SelectedEnvironment = selectedEnvironment;
            FormClosing += ContextSpinner_FormClosing;
        }

        private void ContextSpinner_FormClosing(object sender, FormClosingEventArgs e)
        {
            this.Factory.OnFinished -= Factory_OnFinished;
            this.Factory.OnError -= Factory_OnError;
            this.Factory.OnNewProgress -= Factory_OnNewProgress;
        }

        private void Factory_OnFinished(object sender, EventArgs e)
        {
            if (this.InvokeRequired)
            {
                CloseDelegate closeDelegate = Close;
                Invoke(closeDelegate);
            }
            else
            {
                Close();
            }
        }

        private void Factory_OnError(object sender, Exception e)
        {
            AddText(e.ToString());
        }

        private void Factory_OnNewProgress(object sender, string e)
        {
            AddText(e);
        }

        private void AddText(string text)
        {
            if (this.textBox1.InvokeRequired)
            {
                var d = new AddTextDelegate(AddText);
                this.textBox1.Invoke(d, text);
                return;
            }
            if (this.textBox1.Text.Length != 0)
                this.textBox1.Text += Environment.NewLine;
            this.textBox1.Text += text.Replace("\r\n", "\n").Replace("\r", "").Replace("\n", Environment.NewLine);
        }

        private void ContextSpinner_Load(object sender, EventArgs e)
        {
            Task.Run(() =>
            {
                this.Factory.CreateContext();
            });
        }

        private void LinkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start(this.SelectedEnvironment.GetWorkingDirectory());
        }
    }
}
