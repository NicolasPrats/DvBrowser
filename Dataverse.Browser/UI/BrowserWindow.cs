using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using Dataverse.Browser.Context;
using Dataverse.Browser.Requests;
using Dataverse.Browser.ViewModels;
using Dataverse.Browser.Services;
using Dataverse.Plugin.Emulator.ExecutionTree;

namespace Dataverse.Browser.UI
{
    internal partial class BrowserWindow : Form
    {
        public BrowserContext DataverseContext { get; }
        public BrowserViewModel ViewModel { get; }
        public PluginService PluginService { get; }

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

            // Initialize services and ViewModels
            this.PluginService = new PluginService();
            this.ViewModel = new BrowserViewModel
            {
                Context = context
            };

            // Wire up ViewModel event handlers
            this.ViewModel.PropertyChanged += ViewModel_PropertyChanged;

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
                
                // Also add to ViewModel
                this.ViewModel.AddRequest(
                    request.ConversionResult?.SrcRequest?.Method?.ToUpperInvariant() ?? "UNKNOWN",
                    request.ConversionResult?.SrcRequest?.LocalPathWithQuery ?? string.Empty,
                    request.ExecuteException == null ? 200 : 500,
                    TimeSpan.Zero
                );
            }
            context.LastRequests.OnNewRequestIntercepted += LastRequests_OnNewRequestIntercepted;
            context.LastRequests.OnHistoryCleared += LastRequests_OnHistoryCleared;
            context.LastRequests.OnRequestUpdated += LastRequests_OnRequestUpdated;

            // Subscribe to plugin reload events
            if (context.PluginsEmulator != null)
            {
                context.PluginsEmulator.PluginAssemblyReloaded += OnPluginReloaded;
            }

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
                // Update icon based on latest state (proxied/converted/exceptions).
                var index = Icons.RequestNotAnalyzed;
                if (request.ConversionResult.ConvertedRequest != null || request.Proxied)
                {
                    index = request.ExecuteException == null ? Icons.RequestAnalyzed : Icons.RequestAnalyzedWithError;
                }
                node.ImageIndex = node.SelectedImageIndex = (int)index;

                if (request.ExecuteException != null)
                {
                    node.ToolTipText = request.ExecuteException.Message;
                }
                else if (string.IsNullOrEmpty(node.ToolTipText) && request.ConversionResult.ConvertFailureMessage != null)
                {
                    node.ToolTipText = request.ConversionResult.ConvertFailureMessage;
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
            
            // Note: ViewModel already cleared via ClearHistoryCommand
        }

        private void LastRequests_OnNewRequestIntercepted(object sender, InterceptedWebApiRequest e)
        {
            AddNewRequest(e);
            
            // Also add to ViewModel
            this.ViewModel.AddRequest(
                e.ConversionResult?.SrcRequest?.Method?.ToUpperInvariant() ?? "UNKNOWN",
                e.ConversionResult?.SrcRequest?.LocalPathWithQuery ?? string.Empty,
                e.ExecuteException == null ? 200 : 500,
                TimeSpan.Zero
            );
        }

        private void AddNewRequest(InterceptedWebApiRequest request)
        {
            if (this.treeView1.InvokeRequired)
            {
                var d = new RequestEventDelegate(AddNewRequest);
                this.treeView1.Invoke(d, request);
                return;
            }
            
            // Null safety after hot reload
            if (request?.ConversionResult?.SrcRequest == null)
                return;
                
            var index = Icons.RequestNotAnalyzed;
            if (request.ConversionResult.ConvertedRequest != null || request.Proxied)
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
            // Execute ViewModel command
            if (this.ViewModel.ClearHistoryCommand.CanExecute(null))
            {
                this.ViewModel.ClearHistoryCommand.Execute(null);
            }
        }

        private void BtnDebugger_Click(object sender, EventArgs e)
        {
            // Execute ViewModel command
            if (this.ViewModel.AttachDebuggerCommand.CanExecute(null))
            {
                this.ViewModel.AttachDebuggerCommand.Execute(null);
            }

            // Show feedback
            if (Debugger.IsAttached)
            {
                MessageBox.Show("Debugger is attached. You can add relevant breakpoints in your plugins code");
            }
            else
            {
                using (var process = Process.GetCurrentProcess())
                {
                    MessageBox.Show("Unable to attach a debugger.\nYou may have more info in the event viewer.\nAlternatively, you can start manually your debugger and attach it to process: " + process.Id);
                }
            }
        }

        private void ComboBoxBehavior_SelectedIndexChanged(object sender, EventArgs e)
        {
            string text;
            switch (this.ComboBoxBehavior.SelectedIndex)
            {
                case 0:
                    this.DataverseContext.IsEnabled = false;
                    this.ViewModel.PluginsEnabled = false;
                    text = "Plugins will no longer be executed locally.";
                    break;
                case 1:
                    this.DataverseContext.IsEnabled = true;
                    this.DataverseContext.PluginsEmulator.EmulatorOptions.BreakBeforeExecutingPlugins = false;
                    this.ViewModel.PluginsEnabled = true;
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

        private void OnPluginReloaded(object sender, Plugin.Emulator.PluginReloadEventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => OnPluginReloaded(sender, e)));
                return;
            }

            string message;
            MessageBoxIcon icon;

            // Consider it success if steps were loaded, even if not all of them
            // (some steps might be skipped due to unsupported features like configuration)
            if (e.Error == null)
            {
                // Recreate the proxy to use the new plugin instances
                try
                {
                    this.DataverseContext.ProxyWithEmulator = this.DataverseContext.PluginsEmulator.CreateNewProxy();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Trace.WriteLine($"[BrowserWindow] Failed to recreate proxy after reload: {ex}");
                }
                
                message = $"✓ Plugin assembly reloaded successfully!\n\nTime: {e.ReloadTime:HH:mm:ss}\nPath: {System.IO.Path.GetFileName(e.AssemblyPath)}\n\nYour latest code changes are now active.";
                
                if (!e.Success)
                {
                    message += "\n\nNote: Some plugin steps were skipped (e.g., steps using configuration/secure configuration).";
                }
                
                icon = MessageBoxIcon.Information;
            }
            else
            {
                message = $"✗ Plugin reload failed!\n\nTime: {e.ReloadTime:HH:mm:ss}\nError: {e.Error.Message}\n\nPlease check the log file for details.";
                icon = MessageBoxIcon.Error;
            }

            MessageBox.Show(this, message, "Hot Reload", MessageBoxButtons.OK, icon);
        }

        private void ViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            // Handle ViewModel property changes and update UI accordingly
            if (e.PropertyName == nameof(BrowserViewModel.PluginsEnabled))
            {
                // Sync UI state if needed
                this.Text = this.ViewModel.PluginsEnabled 
                    ? "Dataverse Browser - Plugins Enabled" 
                    : "Dataverse Browser - Plugins Disabled";
                
                // Update status bar
                this.toolStripStatusLabel1.Text = this.ViewModel.PluginsEnabled 
                    ? "Plugins Enabled" 
                    : "Plugins Disabled";
            }
            else if (e.PropertyName == nameof(BrowserViewModel.StatusMessage))
            {
                // Update status bar with ViewModel message
                if (!string.IsNullOrEmpty(this.ViewModel.StatusMessage))
                {
                    this.toolStripStatusLabel1.Text = this.ViewModel.StatusMessage;
                }
            }
        }

        private void BrowserWindow_KeyDown(object sender, KeyEventArgs e)
        {
            // Keyboard shortcuts
            if (e.Control && e.KeyCode == Keys.L)
            {
                // Ctrl+L: Clear history
                e.Handled = true;
                if (this.ViewModel.ClearHistoryCommand.CanExecute(null))
                {
                    this.ViewModel.ClearHistoryCommand.Execute(null);
                }
            }
            else if (e.Control && e.KeyCode == Keys.D)
            {
                // Ctrl+D: Attach debugger
                e.Handled = true;
                BtnDebugger_Click(sender, e);
            }
            else if (e.KeyCode == Keys.F5)
            {
                // F5: Refresh/reload
                e.Handled = true;
                this.toolStripStatusLabel1.Text = "Refreshing...";
            }
        }

        #region Context Menu Handlers

        private void CopyUrlMenuItem_Click(object sender, EventArgs e)
        {
            if (this.treeView1.SelectedNode != null)
            {
                var nodeText = this.treeView1.SelectedNode.Text;
                // Extract URL part (after method name)
                var parts = nodeText.Split(new[] { ' ' }, 2);
                if (parts.Length == 2)
                {
                    Clipboard.SetText(parts[1]);
                    this.toolStripStatusLabel1.Text = "URL copied to clipboard";
                }
            }
        }

        private void CopyMethodMenuItem_Click(object sender, EventArgs e)
        {
            if (this.treeView1.SelectedNode != null)
            {
                var nodeText = this.treeView1.SelectedNode.Text;
                // Extract method (before first space)
                var parts = nodeText.Split(new[] { ' ' }, 2);
                if (parts.Length > 0)
                {
                    Clipboard.SetText(parts[0]);
                    this.toolStripStatusLabel1.Text = "HTTP method copied to clipboard";
                }
            }
        }

        private void ExpandAllMenuItem_Click(object sender, EventArgs e)
        {
            this.treeView1.ExpandAll();
            this.toolStripStatusLabel1.Text = "All nodes expanded";
        }

        private void CollapseAllMenuItem_Click(object sender, EventArgs e)
        {
            this.treeView1.CollapseAll();
            this.toolStripStatusLabel1.Text = "All nodes collapsed";
        }

        #endregion
    }
}
