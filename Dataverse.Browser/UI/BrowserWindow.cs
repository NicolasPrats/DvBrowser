using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Dataverse.Browser.Context;
using Dataverse.Browser.Requests;
using Dataverse.Browser.ViewModels;
using Dataverse.Browser.Services;
using Dataverse.Browser.Configuration;
using Dataverse.Plugin.Emulator.ExecutionTree;
using System.Net.Http;
using Newtonsoft.Json.Linq;

namespace Dataverse.Browser.UI
{
    internal partial class BrowserWindow : Form
    {
        public BrowserContext DataverseContext { get; private set; }
        public BrowserViewModel ViewModel { get; }
        public EnvironmentViewModel EnvironmentViewModel { get; }
        public PluginService PluginService { get; }

        private volatile bool _isConnecting;
        private string _connectingEnvironmentName;
        private string _lastStatusLine;

        private delegate void ClearRequestsDelegate();
        private Configuration.DataverseBrowserConfiguration _configuration;

        private readonly object _requestsLock = new object();
        private readonly ConcurrentQueue<IncomingRequest> _incomingRequests = new ConcurrentQueue<IncomingRequest>();
        private readonly List<RequestRow> _requests = new List<RequestRow>();
        private readonly Dictionary<InterceptedWebApiRequest, int> _requestIndex = new Dictionary<InterceptedWebApiRequest, int>();
        private Timer _requestsUiTimer;
        private int _requestsVersion;
        private int _requestsVersionUi;
        private bool _requestHistoryTabActive;
        private bool _suppressRequestSelection;
        private InterceptedWebApiRequest _selectedRequest;
        private readonly Dictionary<int, ListViewItem> _listViewCache = new Dictionary<int, ListViewItem>();
        private const int ListViewCacheLimit = 500;
        private int _lastRequestCountUi;
        private int _lastHistoryTabTitleCount = -1;

        private BindingList<Configuration.EnvironnementConfiguration> _environmentsBindingList;
        private BindingSource _environmentsBindingSource;

        private static class NativeMethods
        {
            private const int LVM_FIRST = 0x1000;
            private const int LVM_GETTOPINDEX = LVM_FIRST + 39;
            private const int LVM_GETCOUNTPERPAGE = LVM_FIRST + 40;

            [DllImport("user32.dll")]
            public static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

            public static int GetTopIndex(ListView listView)
            {
                if (listView == null || !listView.IsHandleCreated)
                    return 0;
                return (int)SendMessage(listView.Handle, LVM_GETTOPINDEX, IntPtr.Zero, IntPtr.Zero);
            }

            public static int GetCountPerPage(ListView listView)
            {
                if (listView == null || !listView.IsHandleCreated)
                    return 0;
                return (int)SendMessage(listView.Handle, LVM_GETCOUNTPERPAGE, IntPtr.Zero, IntPtr.Zero);
            }
        }

        private sealed class IncomingRequest
        {
            public InterceptedWebApiRequest Request { get; set; }
            public DateTime Timestamp { get; set; }
        }

        private sealed class RequestRow
        {
            public InterceptedWebApiRequest Request { get; set; }
            public DateTime Timestamp { get; set; }
            public string Method { get; set; }
            public string Url { get; set; }
        }

        private sealed class DetailsTreeTag
        {
            public InterceptedWebApiRequest Request { get; set; }
            public bool Built { get; set; }
            public ExecutionTreeNode BuiltRoot { get; set; }
        }

        // Constructor for integrated environment selection
        public BrowserWindow(Configuration.DataverseBrowserConfiguration configuration)
        {
            if (configuration is null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            _configuration = configuration;
            InitializeComponent();
            this.DoubleBuffered = true;
            EnableDoubleBuffering(this.panelTop);
            this.statusStrip1?.BringToFront();
            WireLayoutEvents();
            InitializeRequestHistoryUi();
            AlignTopControls();

            // Initialize services and ViewModels
            this.PluginService = new PluginService();
            this.ViewModel = new BrowserViewModel();
            this.EnvironmentViewModel = new EnvironmentViewModel();

            // Wire up ViewModel event handlers
            this.ViewModel.PropertyChanged += ViewModel_PropertyChanged;

            // Load environments from configuration
            if (configuration.Environnements != null)
            {
                foreach (var env in configuration.Environnements)
                {
                    this.EnvironmentViewModel.Environments.Add(env);
                }
            }

            BindEnvironmentsToList(this.EnvironmentViewModel.Environments);

            // Show instruction in status bar
            this.toolStripStatusLabel1.Text = "Select an environment from the list to connect";

            // Disable toolbar until connected
            this.ComboBoxBehavior.Enabled = false;
            this.btnDebugger.Enabled = false;
            this.btnClear.Enabled = false;
        }

        // Original constructor for backward compatibility
        public BrowserWindow(BrowserContext context)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            InitializeComponent();
            this.DoubleBuffered = true;
            EnableDoubleBuffering(this.panelTop);
            this.statusStrip1?.BringToFront();
            WireLayoutEvents();
            InitializeRequestHistoryUi();
            AlignTopControls();

            // Initialize services and ViewModels
            this.PluginService = new PluginService();
            this.ViewModel = new BrowserViewModel
            {
                Context = context
            };
            this.EnvironmentViewModel = new EnvironmentViewModel();

            // Wire up ViewModel event handlers
            this.ViewModel.PropertyChanged += ViewModel_PropertyChanged;

            // Add current environment
            var currentEnv = new Configuration.EnvironnementConfiguration
            {
                Name = context.CrmServiceClient.ConnectedOrgFriendlyName + " (Current)",
                DataverseHost = context.Host,
                Id = Guid.NewGuid(),
                PluginAssemblies = new string[0]
            };
            this.EnvironmentViewModel.Environments.Add(currentEnv);

            BindEnvironmentsToList(this.EnvironmentViewModel.Environments);

            InitializeWithContext(context);
        }

        private void BindEnvironmentsToList(IEnumerable<Configuration.EnvironnementConfiguration> environments)
        {
            var list = environments != null ? new List<Configuration.EnvironnementConfiguration>(environments) : new List<Configuration.EnvironnementConfiguration>();
            _environmentsBindingList = new BindingList<Configuration.EnvironnementConfiguration>(list);
            _environmentsBindingSource = new BindingSource { DataSource = _environmentsBindingList };

            this.listBoxEnvironments.DataSource = _environmentsBindingSource;
            this.listBoxEnvironments.DisplayMember = "Name";

            UpdateEnvironmentButtonsEnabledState();
        }

        private void UpdateEnvironmentButtonsEnabledState()
        {
            var canManage = _configuration != null;
            var hasSelection = GetSelectedEnvironment() != null;

            if (this.btnEnvAdd != null) this.btnEnvAdd.Enabled = canManage;
            if (this.btnEnvEdit != null) this.btnEnvEdit.Enabled = canManage && hasSelection;
            if (this.btnEnvDelete != null) this.btnEnvDelete.Enabled = canManage && hasSelection;
        }

        private Configuration.EnvironnementConfiguration GetSelectedEnvironment()
        {
            return this.listBoxEnvironments != null ? this.listBoxEnvironments.SelectedItem as Configuration.EnvironnementConfiguration : null;
        }

        private void SaveCurrentEnvironmentsToConfiguration()
        {
            if (_configuration == null || _environmentsBindingList == null)
                return;

            _configuration.Environnements = new List<Configuration.EnvironnementConfiguration>(_environmentsBindingList).ToArray();
            ConfigurationManager.SaveConfiguration(_configuration);
        }

        private void InitializeWithContext(BrowserContext context)
        {
            this.DataverseContext = context;

            var tab = new BrowserTab(context);
            this.tabControl1.TabPages[0].Controls.Add(tab);
            this.tabControl1.TabPages[0].Text = context.CrmServiceClient.ConnectedOrgFriendlyName;

            // Important: keep real '/' path separators so relative resources (js/, css/) resolve correctly.
            //
            // In some UCI shells, webresources are hosted in iframes where `parent.Xrm` is not available anymore,
            // which breaks DRB "Instance mode" detection. To keep DRB reliable, load it directly as a document
            // under /WebResources/ (served by our fake webresource handler) and run it in XTB mode via hostObjects.
            string url = $"https://{context.Host}/WebResources/{ExtensionsRequestHandler.FakeIdentifier}/gp_/drb/drb_index.htm";
            // Intercept only non-GET calls so DRB "Execute" can trigger local plugins and debugger.
            // Allow GETs to be handled by the browser for best compatibility/performance.
            tab = new BrowserTab(context, url, bypassServiceWorker: true, isRestBuilderTab: true, interceptNonGetOnly: true);
            this.tabControl1.TabPages[1].Controls.Add(tab);

            lock (_requestsLock)
            {
                DrainIncomingRequestsNoLock();
                _requests.Clear();
                _requestIndex.Clear();
                _listViewCache.Clear();
            }
            this.listViewRequests.VirtualListSize = 0;
            _lastRequestCountUi = 0;
            _lastHistoryTabTitleCount = -1;
            _selectedRequest = null;
            this.treeView1.Nodes.Clear();

            foreach (var request in context.LastRequests)
            {
                CaptureRequest(request);
            }
            MarkRequestsDirty();
            context.LastRequests.OnNewRequestIntercepted += LastRequests_OnNewRequestIntercepted;
            context.LastRequests.OnHistoryCleared += LastRequests_OnHistoryCleared;
            context.LastRequests.OnRequestUpdated += LastRequests_OnRequestUpdated;

            // Subscribe to plugin reload events
            if (context.PluginsEmulator != null)
            {
                context.PluginsEmulator.PluginAssemblyReloaded += OnPluginReloaded;
            }

            this.ComboBoxBehavior.SelectedIndex = 1;
            this.ComboBoxBehavior.Enabled = true;
            this.btnDebugger.Enabled = true;
            this.btnClear.Enabled = true;
             
            this.toolStripStatusLabel1.Text = $"Connected to {context.CrmServiceClient.ConnectedOrgFriendlyName}";
            UpdateEnvironmentButtonsEnabledState();
        }

        private static string TryGetPreferredAppId(BrowserContext context)
        {
            if (context == null || context.HttpClient == null || string.IsNullOrEmpty(context.Host))
                return null;

            // Default to Advanced Field Service when present (this matches typical FS deployments).
            const string preferredUniqueName = "hds_AdvancedFieldService";
            try
            {
                var url = $"https://{context.Host}/api/data/v9.2/appmodules?$select=appmoduleid,uniquename&$filter=uniquename%20eq%20%27{preferredUniqueName}%27";
                var req = new HttpRequestMessage(HttpMethod.Get, url);
                context.AddAuthorizationHeaders(req);
                req.Headers.Add("Accept", "application/json");

                var resp = context.HttpClient.SendAsync(req).Result;
                if (!resp.IsSuccessStatusCode)
                    return null;

                var json = resp.Content.ReadAsStringAsync().Result;
                if (string.IsNullOrWhiteSpace(json))
                    return null;

                var obj = JObject.Parse(json);
                var first = obj["value"]?.First as JObject;
                var id = first?["appmoduleid"]?.ToString();
                if (!string.IsNullOrWhiteSpace(id))
                {
                    Trace.WriteLine("[BrowserWindow] DRB using appid from appmodules uniquename='" + preferredUniqueName + "': " + id);
                    return id;
                }

                Trace.WriteLine("[BrowserWindow] DRB appid not found for appmodules uniquename='" + preferredUniqueName + "'.");
                return null;
            }
            catch
            {
                return null;
            }
        }

        private void PanelTop_Resize(object sender, EventArgs e)
        {
            AlignTopControls();
        }

        private void WireLayoutEvents()
        {
            if (this.splitContainerMain == null)
                return;

            this.splitContainerMain.SplitterMoved -= SplitContainerMain_SplitterMoved;
            this.splitContainerMain.SplitterMoved += SplitContainerMain_SplitterMoved;
            this.splitContainerMain.SizeChanged -= SplitContainerMain_SplitterMoved;
            this.splitContainerMain.SizeChanged += SplitContainerMain_SplitterMoved;
        }

        private void SplitContainerMain_SplitterMoved(object sender, EventArgs e)
        {
            AlignTopControls();
        }

        private void AlignTopControls()
        {
            if (this.panelTop == null)
                return;

            this.panelTop.SuspendLayout();
            try
            {
                var padding = this.panelTop.Padding;
                var yButtons = padding.Top;

                var tabAreaLeft = GetTabAreaLeftInPanelTop();
                var xStart = Math.Max(padding.Left, tabAreaLeft + padding.Left);

                // Position debugger on the left with padding
                if (this.btnDebugger != null)
                {
                    this.btnDebugger.Location = new System.Drawing.Point(xStart, yButtons);
                }

                // Position clear button on the right with padding
                if (this.btnClear != null)
                {
                    var x = Math.Max(padding.Left, this.panelTop.Width - padding.Right - this.btnClear.Width);
                    this.btnClear.Location = new System.Drawing.Point(x, yButtons);
                }

                // Align label + dropdown in the remaining space between left and right buttons
                var gap = 8;
                var leftEdge = xStart;
                if (this.btnDebugger != null)
                {
                    leftEdge = this.btnDebugger.Right + gap;
                }

                var rightEdge = this.panelTop.Width - padding.Right;
                if (this.btnClear != null)
                {
                    rightEdge = this.btnClear.Left - gap;
                }

                if (this.labelMode != null)
                {
                    var y = yButtons + (this.btnDebugger?.Height ?? this.labelMode.Height) / 2 - (this.labelMode.Height / 2);
                    this.labelMode.Location = new System.Drawing.Point(leftEdge, y);
                    leftEdge = this.labelMode.Right + gap;
                }

                if (this.ComboBoxBehavior != null)
                {
                    var buttonHeight = this.btnDebugger?.Height ?? this.ComboBoxBehavior.Height;
                    var y = yButtons + (buttonHeight / 2) - (this.ComboBoxBehavior.Height / 2);
                    var x = Math.Max(padding.Left, leftEdge);
                    var availableWidth = Math.Max(120, rightEdge - x);
                    var width = Math.Min(availableWidth, 1000);
                    this.ComboBoxBehavior.Location = new System.Drawing.Point(x, y);
                    this.ComboBoxBehavior.Width = width;
                }
            }
            finally
            {
                this.panelTop.ResumeLayout(true);
                this.panelTop.Invalidate(true);
            }
        }

        private int GetTabAreaLeftInPanelTop()
        {
            if (this.panelTop == null || this.splitContainerMain?.Panel2 == null)
                return 0;

            try
            {
                var panel2Screen = this.splitContainerMain.Panel2.PointToScreen(System.Drawing.Point.Empty);
                var panel2InTop = this.panelTop.PointToClient(panel2Screen);
                return Math.Max(0, panel2InTop.X);
            }
            catch
            {
                return 0;
            }
        }

        private static void EnableDoubleBuffering(Control control)
        {
            if (control == null)
                return;

            try
            {
                typeof(Control)
                    .GetProperty("DoubleBuffered", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?.SetValue(control, true, null);
            }
            catch
            {
                // Best effort only
            }
        }

        private void LastRequests_OnRequestUpdated(object sender, InterceptedWebApiRequest e)
        {
            MarkRequestsDirty();
        }

        private void UpdateRequest(InterceptedWebApiRequest request)
        {
            MarkRequestsDirty();
        }

        private void LastRequests_OnHistoryCleared(object sender, EventArgs e)
        {
            ClearRequests();
        }

        private void ClearRequests()
        {
            if (this.InvokeRequired)
            {
                try
                {
                    this.BeginInvoke(new Action(ClearRequests));
                }
                catch
                {
                    // ignore shutdown races
                }
                return;
            }

            lock (_requestsLock)
            {
                DrainIncomingRequestsNoLock();
                _requests.Clear();
                _requestIndex.Clear();
                _listViewCache.Clear();
            }

            this.listViewRequests.VirtualListSize = 0;
            _lastRequestCountUi = 0;
            _lastHistoryTabTitleCount = -1;
            _selectedRequest = null;
            this.treeView1.Nodes.Clear();
            MarkRequestsDirty();
             
            // Note: ViewModel already cleared via ClearHistoryCommand
        }

        private void LastRequests_OnNewRequestIntercepted(object sender, InterceptedWebApiRequest e)
        {
            EnqueueIncomingRequest(e);
        }

        private void AddNewRequest(InterceptedWebApiRequest request)
        {
            EnqueueIncomingRequest(request);
        }

        private void EnqueueIncomingRequest(InterceptedWebApiRequest request)
        {
            if (request?.ConversionResult?.SrcRequest == null)
                return;

            _incomingRequests.Enqueue(new IncomingRequest
            {
                Request = request,
                Timestamp = DateTime.Now
            });
            MarkRequestsDirty();
        }

        private void DrainIncomingRequestsNoLock()
        {
            IncomingRequest ignored;
            while (_incomingRequests.TryDequeue(out ignored))
            {
                // drain
            }
        }

        private int DrainIncomingRequests(int maxMilliseconds, int maxItems)
        {
            if (maxMilliseconds <= 0)
                maxMilliseconds = 1;
            if (maxItems <= 0)
                maxItems = 1;

            var stopwatch = Stopwatch.StartNew();
            var drained = 0;

            // Process in small chunks so insertion time is also bounded.
            const int chunkSize = 64;
            while (drained < maxItems && stopwatch.ElapsedMilliseconds < maxMilliseconds)
            {
                var batch = new List<IncomingRequest>(chunkSize);

                while (batch.Count < chunkSize && drained < maxItems && stopwatch.ElapsedMilliseconds < maxMilliseconds)
                {
                    IncomingRequest incoming;
                    if (!_incomingRequests.TryDequeue(out incoming))
                        break;

                    drained++;
                    batch.Add(incoming);
                }

                if (batch.Count == 0)
                    break;

                lock (_requestsLock)
                {
                    foreach (var incoming in batch)
                    {
                        CaptureRequestNoLock(incoming.Request, incoming.Timestamp);
                    }
                }
            }

            return drained;
        }

        private void CaptureRequest(InterceptedWebApiRequest request)
        {
            CaptureRequest(request, DateTime.Now);
        }

        private void CaptureRequest(InterceptedWebApiRequest request, DateTime timestamp)
        {
            if (request?.ConversionResult?.SrcRequest == null)
                return;

            lock (_requestsLock)
            {
                CaptureRequestNoLock(request, timestamp);
            }
        }

        private void CaptureRequestNoLock(InterceptedWebApiRequest request, DateTime timestamp)
        {
            if (request?.ConversionResult?.SrcRequest == null)
                return;

            if (_requestIndex.ContainsKey(request))
                return;

            var row = new RequestRow
            {
                Request = request,
                Timestamp = timestamp,
                Method = request.ConversionResult?.SrcRequest?.Method?.ToUpperInvariant() ?? "UNKNOWN",
                Url = request.ConversionResult?.SrcRequest?.LocalPathWithQuery ?? string.Empty,
            };

            _requestIndex[request] = _requests.Count;
            _requests.Add(row);
        }

        private void MarkRequestsDirty()
        {
            System.Threading.Interlocked.Increment(ref _requestsVersion);
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

        private void InitializeRequestHistoryUi()
        {
            EnableDoubleBuffering(this.listViewRequests);
            this.listViewRequests.VirtualMode = true;
            this.listViewRequests.Columns.Clear();
            this.listViewRequests.Columns.Add("Time", 80);
            this.listViewRequests.Columns.Add("Method", 70);
            this.listViewRequests.Columns.Add("Status", 60);
            this.listViewRequests.Columns.Add("URL", 500);

            this.listViewRequests.RetrieveVirtualItem -= ListViewRequests_RetrieveVirtualItem;
            this.listViewRequests.RetrieveVirtualItem += ListViewRequests_RetrieveVirtualItem;
            this.listViewRequests.ItemSelectionChanged -= ListViewRequests_ItemSelectionChanged;
            this.listViewRequests.ItemSelectionChanged += ListViewRequests_ItemSelectionChanged;
            this.listViewRequests.Resize -= ListViewRequests_Resize;
            this.listViewRequests.Resize += ListViewRequests_Resize;

            this.treeView1.BeforeExpand -= TreeView1_BeforeExpand;
            this.treeView1.BeforeExpand += TreeView1_BeforeExpand;

            this.tabControl1.SelectedIndexChanged -= TabControl1_SelectedIndexChanged;
            this.tabControl1.SelectedIndexChanged += TabControl1_SelectedIndexChanged;
            _requestHistoryTabActive = this.tabControl1.SelectedTab == this.tabPageHistory;

            if (_requestsUiTimer == null)
            {
                _requestsUiTimer = new Timer { Interval = 250 };
                _requestsUiTimer.Tick += RequestsUiTimer_Tick;
                _requestsUiTimer.Start();
            }

            var menu = new ContextMenuStrip();
            menu.Items.Add("Copy URL", null, (s, e) => CopySelectedRequestUrl());
            menu.Items.Add("Copy Method", null, (s, e) => CopySelectedRequestMethod());
            this.listViewRequests.ContextMenuStrip = menu;

            ListViewRequests_Resize(this, EventArgs.Empty);
        }

        private void RequestsUiTimer_Tick(object sender, EventArgs e)
        {
            var previousTotal = _lastRequestCountUi;
            var wasAtNewest = _requestHistoryTabActive && IsAtNewestInListView();

            var version = System.Threading.Volatile.Read(ref _requestsVersion);
            if (version == _requestsVersionUi && _incomingRequests.IsEmpty)
                return;

            _requestsVersionUi = version;

            try
            {
                // Drain in small batches to keep the UI responsive under heavy load.
                DrainIncomingRequests(
                    maxMilliseconds: _requestHistoryTabActive ? 12 : 6,
                    maxItems: _requestHistoryTabActive ? 600 : 300);
            }
            catch
            {
                // best-effort; don't crash UI timer
            }

            if (!_incomingRequests.IsEmpty)
            {
                // No new events may fire, but we still need more ticks to empty the queue.
                MarkRequestsDirty();
            }

            int total;
            lock (_requestsLock)
            {
                total = _requests.Count;
            }

            if (total != _lastRequestCountUi)
            {
                this.listViewRequests.VirtualListSize = total;
                _lastRequestCountUi = total;
            }

            if (_requestHistoryTabActive)
            {
                // DevTools-style: auto-scroll to newest only if user is already at newest.
                if (wasAtNewest && total > 0 && total != previousTotal)
                {
                    EnsureNewestVisible(total);
                }

                this.listViewRequests.Invalidate();
            }

            UpdateHistoryTabTitle(total);
        }

        private void TabControl1_SelectedIndexChanged(object sender, EventArgs e)
        {
            _requestHistoryTabActive = this.tabControl1.SelectedTab == this.tabPageHistory;
            MarkRequestsDirty();
        }

        private void UpdateHistoryTabTitle(int total)
        {
            if (this.tabPageHistory == null)
                return;

            if (total == _lastHistoryTabTitleCount)
                return;

            _lastHistoryTabTitleCount = total;
            this.tabPageHistory.Text = total > 0 ? $"Request History ({total})" : "Request History";
        }

        private void ListViewRequests_RetrieveVirtualItem(object sender, RetrieveVirtualItemEventArgs e)
        {
            if (e.ItemIndex < 0)
                return;

            RequestRow row;
            int internalIndex;
            lock (_requestsLock)
            {
                internalIndex = GetRequestIndexFromDisplayIndex(e.ItemIndex, _requests.Count);
                if (internalIndex < 0 || internalIndex >= _requests.Count)
                {
                    e.Item = new ListViewItem(new[] { "", "", "", "" });
                    return;
                }
                row = _requests[internalIndex];
            }

            var cached = GetCachedListViewItem(internalIndex);
            if (cached != null)
            {
                UpdateListViewItemFromRow(cached, row);
                e.Item = cached;
                return;
            }

            var status = GetRequestStatusCode(row.Request).ToString();
            var item = new ListViewItem(new[]
            {
                row.Timestamp.ToString("HH:mm:ss"),
                row.Method ?? string.Empty,
                status,
                row.Url ?? string.Empty
            });

            if (row?.Request?.ExecuteException != null)
            {
                item.ForeColor = System.Drawing.Color.DarkRed;
            }

            item.ToolTipText = row.Url ?? string.Empty;

            CacheListViewItem(internalIndex, item);
            e.Item = item;
        }

        private static void UpdateListViewItemFromRow(ListViewItem item, RequestRow row)
        {
            if (item == null || row == null)
                return;

            var status = GetRequestStatusCode(row.Request).ToString();

            if (item.SubItems.Count >= 4)
            {
                item.SubItems[0].Text = row.Timestamp.ToString("HH:mm:ss");
                item.SubItems[1].Text = row.Method ?? string.Empty;
                item.SubItems[2].Text = status;
                item.SubItems[3].Text = row.Url ?? string.Empty;
            }

            item.ForeColor = row?.Request?.ExecuteException != null
                ? System.Drawing.Color.DarkRed
                : System.Drawing.SystemColors.WindowText;
            item.ToolTipText = row.Url ?? string.Empty;
        }

        private void ListViewRequests_ItemSelectionChanged(object sender, ListViewItemSelectionChangedEventArgs e)
        {
            if (_suppressRequestSelection)
                return;

            if (!e.IsSelected)
                return;

            var displayIndex = e.ItemIndex;
            RequestRow row;
            lock (_requestsLock)
            {
                var index = GetRequestIndexFromDisplayIndex(displayIndex, _requests.Count);
                if (index < 0 || index >= _requests.Count)
                    return;
                row = _requests[index];
            }

            _selectedRequest = row.Request;
            ShowRequestDetails(row);
        }

        private void ListViewRequests_Resize(object sender, EventArgs e)
        {
            if (this.listViewRequests.Columns.Count < 4)
                return;

            var fixedWidth = this.listViewRequests.Columns[0].Width +
                             this.listViewRequests.Columns[1].Width +
                             this.listViewRequests.Columns[2].Width;
            var available = Math.Max(100, this.listViewRequests.ClientSize.Width - fixedWidth - 6);
            this.listViewRequests.Columns[3].Width = available;
        }

        private bool IsAtNewestInListView()
        {
            try
            {
                if (this.listViewRequests == null)
                    return true;

                var total = this.listViewRequests.VirtualListSize;
                if (total <= 0)
                    return true;

                var top = NativeMethods.GetTopIndex(this.listViewRequests);
                var perPage = NativeMethods.GetCountPerPage(this.listViewRequests);
                if (perPage <= 0)
                    return true;

                var lastVisible = top + perPage - 1;
                return lastVisible >= total - 1;
            }
            catch
            {
                return true;
            }
        }

        private void EnsureNewestVisible(int total)
        {
            if (this.listViewRequests == null || total <= 0)
                return;

            // Newest request is the last row (DevTools-style).
            var newestDisplayIndex = total - 1;
            try
            {
                this.listViewRequests.EnsureVisible(newestDisplayIndex);
            }
            catch
            {
                // ignore redraw races
            }
        }

        private void SelectRequestDisplayIndex(int displayIndex, bool ensureVisible)
        {
            if (displayIndex < 0 || displayIndex >= this.listViewRequests.VirtualListSize)
                return;

            if (this.listViewRequests.SelectedIndices.Count == 1 && this.listViewRequests.SelectedIndices[0] == displayIndex)
                return;

            _suppressRequestSelection = true;
            try
            {
                this.listViewRequests.SelectedIndices.Clear();
                this.listViewRequests.SelectedIndices.Add(displayIndex);

                if (ensureVisible)
                {
                    this.listViewRequests.EnsureVisible(displayIndex);
                }
            }
            finally
            {
                _suppressRequestSelection = false;
            }
        }

        private void ShowRequestDetails(RequestRow row)
        {
            this.treeView1.BeginUpdate();
            try
            {
                this.treeView1.Nodes.Clear();

                if (row == null)
                    return;

                var status = GetRequestStatusCode(row.Request);
                var rootNode = new TreeNode($"{row.Method} {row.Url}  (Status: {status})")
                {
                    ImageIndex = (int)Icons.RequestAnalyzed,
                    SelectedImageIndex = (int)Icons.RequestAnalyzed
                };

                var tag = new DetailsTreeTag
                {
                    Request = row.Request,
                    Built = false,
                    BuiltRoot = null
                };
                rootNode.Tag = tag;

                if (row.Request?.ExecutionTreeRoot != null)
                {
                    rootNode.Nodes.Add(new TreeNode("Expand to view details..."));
                }

                this.treeView1.Nodes.Add(rootNode);
            }
            finally
            {
                this.treeView1.EndUpdate();
            }
        }

        private void TreeView1_BeforeExpand(object sender, TreeViewCancelEventArgs e)
        {
            var node = e != null ? e.Node : null;
            var tag = node != null ? node.Tag as DetailsTreeTag : null;
            if (tag == null)
                return;

            var root = tag.Request?.ExecutionTreeRoot;
            if (tag.Built && ReferenceEquals(tag.BuiltRoot, root))
                return;

            node.Nodes.Clear();
            BuildTree(node, root);
            tag.Built = true;
            tag.BuiltRoot = root;
        }

        private static int GetRequestStatusCode(InterceptedWebApiRequest request)
        {
            return request?.ExecuteException == null ? 200 : 500;
        }

        private static int GetRequestIndexFromDisplayIndex(int displayIndex, int count)
        {
            // DevTools-style ordering: oldest -> newest (newest is last row)
            return displayIndex;
        }

        private void CopySelectedRequestUrl()
        {
            var row = GetSelectedRequestRow();
            if (row == null)
                return;
            Clipboard.SetText(row.Url ?? string.Empty);
            SetStatusLine("URL copied to clipboard");
        }

        private void CopySelectedRequestMethod()
        {
            var row = GetSelectedRequestRow();
            if (row == null)
                return;
            Clipboard.SetText(row.Method ?? string.Empty);
            SetStatusLine("HTTP method copied to clipboard");
        }

        private RequestRow GetSelectedRequestRow()
        {
            if (this.listViewRequests.SelectedIndices.Count == 0)
                return null;

            var displayIndex = this.listViewRequests.SelectedIndices[0];
            lock (_requestsLock)
            {
                var index = GetRequestIndexFromDisplayIndex(displayIndex, _requests.Count);
                if (index < 0 || index >= _requests.Count)
                    return null;
                return _requests[index];
            }
        }

        private void CacheListViewItem(int internalIndex, ListViewItem item)
        {
            if (item == null)
                return;

            if (_listViewCache.Count >= ListViewCacheLimit)
            {
                // Simple eviction: clear the cache when it grows too large
                _listViewCache.Clear();
            }

            _listViewCache[internalIndex] = item;
        }

        private ListViewItem GetCachedListViewItem(int internalIndex)
        {
            ListViewItem cached;
            return _listViewCache.TryGetValue(internalIndex, out cached) ? cached : null;
        }

        // Cache is evicted by size and cleared on ClearRequests.


        private void BtnClear_Click(object sender, EventArgs e)
        {
            ClearHistory();
        }

        private void ClearHistory()
        {
            try
            {
                if (this.DataverseContext?.LastRequests != null)
                {
                    this.DataverseContext.LastRequests.Clear();
                }
                else
                {
                    ClearRequests();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine($"[BrowserWindow] Clear history failed: {ex}");
                ClearRequests();
            }

            this.ViewModel?.ClearHistoryItems();
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
                ClearHistory();
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
            CopySelectedRequestUrl();
        }

        private void CopyMethodMenuItem_Click(object sender, EventArgs e)
        {
            CopySelectedRequestMethod();
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

        private void BtnEnvAdd_Click(object sender, EventArgs e)
        {
            if (_configuration == null)
            {
                MessageBox.Show(this, "Environment management is not available in legacy mode.", "Not Available", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (var editor = new EnvironmentEditor(null))
            {
                editor.ShowDialog(this);
                if (editor.SelectedEnvironment == null)
                    return;

                _environmentsBindingList.Add(editor.SelectedEnvironment);
                this.EnvironmentViewModel?.Environments?.Add(editor.SelectedEnvironment);
                SaveCurrentEnvironmentsToConfiguration();
                _environmentsBindingSource.ResetBindings(false);
                this.listBoxEnvironments.SelectedItem = editor.SelectedEnvironment;
            }
        }

        private void BtnEnvEdit_Click(object sender, EventArgs e)
        {
            if (_configuration == null)
            {
                MessageBox.Show(this, "Environment management is not available in legacy mode.", "Not Available", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var env = GetSelectedEnvironment();
            if (env == null)
                return;

            using (var editor = new EnvironmentEditor(env))
            {
                editor.ShowDialog(this);
                if (editor.SelectedEnvironment == null)
                    return;

                SaveCurrentEnvironmentsToConfiguration();
                _environmentsBindingSource.ResetBindings(false);
                this.listBoxEnvironments.Refresh();
            }
        }

        private void BtnEnvDelete_Click(object sender, EventArgs e)
        {
            if (_configuration == null)
            {
                MessageBox.Show(this, "Environment management is not available in legacy mode.", "Not Available", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var env = GetSelectedEnvironment();
            if (env == null)
                return;

            var confirm = MessageBox.Show(this, $"Delete environment '{env.Name}'?", "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (confirm != DialogResult.Yes)
                return;

            _environmentsBindingList.Remove(env);
            this.EnvironmentViewModel?.Environments?.Remove(env);
            SaveCurrentEnvironmentsToConfiguration();
            _environmentsBindingSource.ResetBindings(false);
            UpdateEnvironmentButtonsEnabledState();
        }

        private void ListBoxEnvironments_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateEnvironmentButtonsEnabledState();
        }

        private void ListBoxEnvironments_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0)
                return;

            var listBox = sender as ListBox;
            if (listBox == null)
                return;

            var isSelected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
            var background = isSelected ? Color.FromArgb(0, 120, 215) : listBox.BackColor;
            var foreground = listBox.ForeColor;

            using (var backBrush = new SolidBrush(background))
            {
                e.Graphics.FillRectangle(backBrush, e.Bounds);
            }

            var text = listBox.GetItemText(listBox.Items[e.Index]);
            var textRect = Rectangle.Inflate(e.Bounds, -8, 0);

            TextRenderer.DrawText(
                e.Graphics,
                text,
                listBox.Font,
                textRect,
                foreground,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);

            using (var separatorPen = new Pen(Color.FromArgb(55, 55, 58)))
            {
                var y = e.Bounds.Bottom - 1;
                e.Graphics.DrawLine(separatorPen, e.Bounds.Left, y, e.Bounds.Right, y);
            }

            if ((e.State & DrawItemState.Focus) == DrawItemState.Focus)
            {
                e.DrawFocusRectangle();
            }
        }

        private void ListBoxEnvironments_DoubleClick(object sender, EventArgs e)
        {
            if (this.listBoxEnvironments.SelectedItem is Configuration.EnvironnementConfiguration selectedEnv)
            {
                _isConnecting = true;
                _connectingEnvironmentName = selectedEnv.Name;
                SetStatusLine($"Connecting to {_connectingEnvironmentName} ....");
                this.Cursor = Cursors.WaitCursor;

                try
                {
                    // Create context factory
                    var factory = new ContextFactory(selectedEnv);
                    
                    // Subscribe to factory events
                    factory.OnNewProgress += (s, progress) => AddConnectionStatus(progress);
                    factory.OnError += (s, ex) => AddConnectionStatus("ERROR: " + ex.Message);
                    factory.OnFinished += (s, ev) => OnConnectionFinished(factory);
                    
                    // Start connection in background
                    System.Threading.Tasks.Task.Run(() =>
                    {
                        try
                        {
                            factory.CreateContext();
                        }
                        catch (Exception ex)
                        {
                            this.Invoke(new Action(() =>
                            {
                                AddConnectionStatus("ERROR: " + ex.Message);
                                OnConnectionFinished(null);
                            }));
                        }
                    });
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to connect to {selectedEnv.Name}:\n{ex.Message}", 
                        "Connection Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    this.toolStripStatusLabel1.Text = "Connection failed";
                    _isConnecting = false;
                    this.Cursor = Cursors.Default;
                }
            }
        }

        private void AddConnectionStatus(string text)
        {
            if (this.InvokeRequired)
            {
                try
                {
                    this.BeginInvoke(new Action<string>(AddConnectionStatus), text);
                }
                catch
                {
                    // ignore shutdown races
                }
                return;
            }

            if (!_isConnecting)
                return;

            var oneLine = (text ?? string.Empty)
                .Replace("\r\n", " ")
                .Replace("\r", " ")
                .Replace("\n", " ")
                .Trim();

            if (oneLine.Length > 220)
            {
                oneLine = oneLine.Substring(0, 220) + "...";
            }

            SetStatusLine($"Connecting to {_connectingEnvironmentName} .... {oneLine}");
        }

        private void SetStatusLine(string text)
        {
            if (string.Equals(_lastStatusLine, text, StringComparison.Ordinal))
                return;

            _lastStatusLine = text;
            this.toolStripStatusLabel1.Text = text ?? string.Empty;
        }

        private void OnConnectionFinished(ContextFactory factory)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action<ContextFactory>(OnConnectionFinished), factory);
                return;
            }

            try
            {
                if (factory != null)
                {
                    var context = factory.GetContext();
                    if (context != null)
                    {
                        _isConnecting = false; // stop progress line updates
                        SetStatusLine($"Connecting to {_connectingEnvironmentName} .... Initializing UI...");

                        // Defer heavy UI initialization so the status bar can repaint first.
                        var selectedEnvironment = factory.SelectedEnvironment;
                        this.BeginInvoke(new Action(() => FinishConnection(selectedEnvironment, context)));
                        return;
                    }
                    else
                    {
                        AddConnectionStatus("Connection failed: Context is null");
                        this.toolStripStatusLabel1.Text = "Connection failed";
                        _isConnecting = false;
                    }
                }
                else
                {
                    this.toolStripStatusLabel1.Text = "Connection failed";
                    _isConnecting = false;
                }
            }
            catch (Exception ex)
            {
                AddConnectionStatus("ERROR initializing: " + ex.Message);
                this.toolStripStatusLabel1.Text = "Connection failed";
                _isConnecting = false;
            }
            finally
            {
                this.Cursor = Cursors.Default;
            }
        }

        private void FinishConnection(Configuration.EnvironnementConfiguration selectedEnvironment, BrowserContext context)
        {
            try
            {
                // Initialize the browser with the new context (can be slow)
                InitializeWithContext(context);

                // Update ViewModel context
                this.ViewModel.Context = context;
                this.ViewModel.CurrentEnvironment = selectedEnvironment;
                Program.RegisterCleanupContext(selectedEnvironment, context);

                // Save configuration
                if (_configuration != null)
                {
                    ConfigurationManager.SaveConfiguration(_configuration);
                }

                SetStatusLine($"Connected to {selectedEnvironment?.Name ?? "environment"}");
            }
            catch (Exception ex)
            {
                SetStatusLine("Connection failed: " + ex.Message);
            }
            finally
            {
                this.Cursor = Cursors.Default;
            }
        }

        #endregion
    }
}
