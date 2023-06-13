using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using Dataverse.Browser.Context;
using Dataverse.Browser.Requests;
using Dataverse.Plugin.Emulator.ExecutionTree;



namespace Dataverse.Browser.UI
{
    internal partial class BrowserWindow : Form
    {
        public BrowserContext DataverseContext { get; }

        private delegate void RequestEventDelegate(InterceptedWebApiRequest request);
        private delegate void ClearRequestsDelegate();
        private readonly Dictionary<InterceptedWebApiRequest, TreeNode> Nodes = new Dictionary<InterceptedWebApiRequest, TreeNode>();

        public BrowserWindow(BrowserContext context)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            InitializeComponent();

            var tab = new BrowserTab(context);
            this.tabControl1.TabPages[0].Controls.Add(tab);
            this.tabControl1.TabPages[0].Text = context.CrmServiceClient.ConnectedOrgFriendlyName;


            string url = $"https://{context.Host}/main.aspx?pagetype=webresource&webresourceName={ExtensionsRequestHandler.FakeIdentifier}%2Fgp_%2Fdrb%2Fdrb_index.htm";
            tab = new BrowserTab(context, url);
            this.tabControl1.TabPages[1].Controls.Add(tab);


            this.DataverseContext = context;
            foreach (var request in context.LastRequests)
            {
                AddNewRequest(request);
                UpdateRequest(request);
            }
            context.LastRequests.OnNewRequestIntercepted += LastRequests_OnNewRequestIntercepted;
            context.LastRequests.OnHistoryCleared += LastRequests_OnHistoryCleared;
            context.LastRequests.OnRequestUpdated += LastRequests_OnRequestUpdated;

            this.ComboBoxBehavior.SelectedIndex = 1;
        }

        private void LastRequests_OnRequestUpdated(object sender, InterceptedWebApiRequest e)
        {
            UpdateRequest(e);
        }

        private void UpdateRequest(InterceptedWebApiRequest request)
        {
            if (this.treeView1.InvokeRequired)
            {
                var d = new RequestEventDelegate(UpdateRequest);
                this.treeView1.Invoke(d, request);
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
            if (this.treeView1.InvokeRequired)
            {
                var d = new ClearRequestsDelegate(ClearRequests);
                this.treeView1.Invoke(d);
                return;
            }
            this.treeView1.Nodes.Clear();
            this.Nodes.Clear();
        }

        private void LastRequests_OnNewRequestIntercepted(object sender, InterceptedWebApiRequest e)
        {
            AddNewRequest(e);
        }

        private void AddNewRequest(InterceptedWebApiRequest request)
        {
            if (this.treeView1.InvokeRequired)
            {
                var d = new RequestEventDelegate(AddNewRequest);
                this.treeView1.Invoke(d, request);
                return;
            }
            var index = Icons.RequestNotAnalyzed;
            if (request.ConversionResult.ConvertedRequest != null)
            {
                index = request.ExecuteException == null ? Icons.RequestAnalyzed : Icons.RequestNotAnalyzed;
            }
            TreeNode node = new TreeNode(request.ConversionResult.SrcRequest.Method?.ToUpperInvariant() + " " + request.ConversionResult.SrcRequest.LocalPathWithQuery, (int)index, (int)index);
            if (request.ConversionResult.ConvertFailureMessage != null)
            {
                node.ToolTipText = request.ConversionResult.ConvertFailureMessage;
            }
            this.Nodes[request] = node;

            this.treeView1.Nodes.Add(node);
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

        private void ComboBoxBehavior_SelectedIndexChanged(object sender, EventArgs e)
        {
            string text;
            switch (this.ComboBoxBehavior.SelectedIndex)
            {
                case 0:
                    this.DataverseContext.IsEnabled = false;
                    text = "Plugins will no longer be executed locally.";
                    break;
                case 1:
                    this.DataverseContext.IsEnabled = true;
                    this.DataverseContext.PluginsEmulator.EmulatorOptions.BreakBeforeExecutingPlugins = false;
                    text = "Plugins will be executed locally. If you have attached a debugger and set breakpoints, you will be able to debug the plugins.";
                    break;
                case 2:
                    this.DataverseContext.IsEnabled = true;
                    this.DataverseContext.PluginsEmulator.EmulatorOptions.BreakBeforeExecutingPlugins = true;
                    text = "Plugins will be executed locally. If you have attached a debugger, it will automatically break before executing any plugin.\n\nYou have to \"Step Into\" to start the plugin execution.";
                    break;
                default:
                    return;
            }
            this.toolTipButtons.Hide(this.ComboBoxBehavior);
            this.toolTipButtons.Show(text, this.ComboBoxBehavior, -100, this.ComboBoxBehavior.Height + 10, 5000);
        }
    }
}
