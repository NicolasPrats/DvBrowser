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
            this.FormClosing += ContextSpinner_FormClosing;
        }

        private void ContextSpinner_FormClosing(object sender, FormClosingEventArgs e)
        {
            Factory.OnFinished -= Factory_OnFinished;
            Factory.OnError -= Factory_OnError;
            Factory.OnNewProgress -= Factory_OnNewProgress;
        }

        private void Factory_OnFinished(object sender, EventArgs e)
        {
            if (this.InvokeRequired)
            {
                CloseDelegate closeDelegate = this.Close;
                this.Invoke(closeDelegate);
            }
            else
            {
                this.Close();
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
            if (textBox1.InvokeRequired)
            {
                var d = new AddTextDelegate(AddText);
                textBox1.Invoke(d, text);
                return;
            }
            if (textBox1.Text.Length != 0)
                textBox1.Text += Environment.NewLine;
            textBox1.Text += text.Replace("\r\n", "\n").Replace("\r", "").Replace("\n", Environment.NewLine);
        }

        private void ContextSpinner_Load(object sender, EventArgs e)
        {
            Task.Run(() =>
            {
                Factory.CreateContext();
            });
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start(this.SelectedEnvironment.GetWorkingDirectory());
        }
    }
}
