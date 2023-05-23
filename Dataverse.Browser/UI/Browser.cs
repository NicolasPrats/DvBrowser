using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using CefSharp;
using CefSharp.WinForms;
using Dataverse.Browser.Context;
using Dataverse.Browser.Misc;
using Dataverse.Browser.Requests;
using Dataverse.Plugin.Emulator.ExecutionTree;



namespace Dataverse.Browser.UI
{
    internal partial class Browser : Form
    {
        private ChromiumWebBrowser CurrentBrowser { get; set; }
        public DataverseContext DataverseContext { get; }

        private delegate void RequestEventDelegate(InterceptedWebApiRequest request);
        private delegate void ClearRequestsDelegate();
        private readonly Dictionary<InterceptedWebApiRequest, TreeNode> Nodes = new Dictionary<InterceptedWebApiRequest, TreeNode>();

        public Browser(DataverseContext context)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            InitializeComponent();

            var browser = new ChromiumWebBrowser("https://" + context.Host)
            {
                RequestHandler = new BrowserRequestHandler(context),

            };
            this.splitContainer1.Panel1.Controls.Add(browser);
            this.CurrentBrowser = browser;
            this.CurrentBrowser.KeyboardHandler = new KeyboardHandler();

            this.DataverseContext = context;
            foreach (var request in context.LastRequests)
            {
                AddNewRequest(request);
                UpdateRequest(request);
            }
            context.LastRequests.OnNewRequestIntercepted += LastRequests_OnNewRequestIntercepted;
            context.LastRequests.OnHistoryCleared += LastRequests_OnHistoryCleared;
            context.LastRequests.OnRequestUpdated += LastRequests_OnRequestUpdated;

        }

        private void LastRequests_OnRequestUpdated(object sender, InterceptedWebApiRequest e)
        {
            UpdateRequest(e);
        }

        private void UpdateRequest(InterceptedWebApiRequest request)
        {
            if (treeView1.InvokeRequired)
            {
                var d = new RequestEventDelegate(UpdateRequest);
                treeView1.Invoke(d, request);
                return;
            }
            if (this.Nodes.TryGetValue(request, out var node))
            {
                if (request.ExecuteException != null)
                {
                    node.ToolTipText = request.ExecuteException.Message;
                }
                BuildTree(node, request.ExecutionTreeRoot);
                if (request.ExecuteException != null)
                {
                    node.ImageIndex = node.SelectedImageIndex = (int)Icons.RequestAnalyzedWithError;
                }
            }
        }

        private void LastRequests_OnHistoryCleared(object sender, EventArgs e)
        {
            ClearRequests();
        }

        private void ClearRequests()
        {
            if (treeView1.InvokeRequired)
            {
                var d = new ClearRequestsDelegate(ClearRequests);
                treeView1.Invoke(d);
                return;
            }
            treeView1.Nodes.Clear();
            this.Nodes.Clear();
        }

        private void LastRequests_OnNewRequestIntercepted(object sender, InterceptedWebApiRequest e)
        {
            AddNewRequest(e);
        }

        private void AddNewRequest(InterceptedWebApiRequest request)
        {
            if (treeView1.InvokeRequired)
            {
                var d = new RequestEventDelegate(AddNewRequest);
                treeView1.Invoke(d, request);
                return;
            }
            var index = Icons.RequestNotAnalyzed;
            if (request.ConvertedRequest != null)
            {
                index = request.ExecuteException == null ? Icons.RequestAnalyzed : Icons.RequestNotAnalyzed;
            }
            TreeNode node = new TreeNode(request.SimpleHttpRequest.Method?.ToUpperInvariant() + " " + request.SimpleHttpRequest.LocalPathWithQuery, (int)index, (int)index);
            if (request.ConvertFailureMessage != null)
            {
                node.ToolTipText = request.ConvertFailureMessage;
            }
            this.Nodes[request] = node;

            treeView1.Nodes.Add(node);
        }

        private void BuildTree(TreeNode parentNode, ExecutionTreeNode executionTreeNode)
        {
            if (executionTreeNode == null)
                return;
            TreeNode newNode = new TreeNode(executionTreeNode.Title);
            switch (executionTreeNode.Type)
            {
                case ExecutionTreeNodeType.Step:
                    newNode.ImageIndex = (int)Icons.Plugin;
                    break;
                case ExecutionTreeNodeType.Message:
                    newNode.ImageIndex = (int)Icons.Operation;
                    break;
                case ExecutionTreeNodeType.InnerOperation:
                    newNode.ImageIndex = (int)Icons.MessageSentToDataverse;
                    break;
            }
            newNode.ToolTipText = executionTreeNode.GetTrace();
            parentNode.Nodes.Add(newNode);
            foreach (var child in executionTreeNode.ChildNodes)
            {
                BuildTree(newNode, child);
            }
        }


        private void BtnDevTools_Click(object sender, EventArgs e)
        {
            this.CurrentBrowser.ShowDevTools();
        }

        private void BtnClear_Click(object sender, EventArgs e)
        {
            this.DataverseContext.LastRequests.Clear();
        }

        private void BtnDebugger_Click(object sender, EventArgs e)
        {
            Debugger.Launch();
            if (!Debugger.IsAttached)
            {
                using (var process = Process.GetCurrentProcess())
                {

                    MessageBox.Show("Unable to attach a debugger.\nYou may have more info in the event viewer.\nAlternatively, you can start manually your debugger and attach it to process: " + process.Id);
                }
            }
            else
            {
                MessageBox.Show("Debugger is attached. You can add relevant breakpoints in your plugins code");
            }

        }

        private void cbEnabled_CheckedChanged(object sender, EventArgs e)
        {
            this.DataverseContext.IsEnabled = cbEnabled.Checked;
        }

    }
}
