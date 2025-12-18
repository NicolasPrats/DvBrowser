namespace Dataverse.Browser.UI
{
    partial class BrowserWindow
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(BrowserWindow));
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.groupBoxLeft = new System.Windows.Forms.GroupBox();
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.tabPage2 = new System.Windows.Forms.TabPage();
            this.tabPageHistory = new System.Windows.Forms.TabPage();
            this.splitContainerHistory = new System.Windows.Forms.SplitContainer();
            this.listViewRequests = new System.Windows.Forms.ListView();
            this.treeView1 = new System.Windows.Forms.TreeView();
            this.imageList1 = new System.Windows.Forms.ImageList(this.components);
            this.btnClear = new System.Windows.Forms.Button();
            this.btnDebugger = new System.Windows.Forms.Button();
            this.ComboBoxBehavior = new System.Windows.Forms.ComboBox();
            this.toolTipButtons = new System.Windows.Forms.ToolTip(this.components);
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.toolStripStatusLabel1 = new System.Windows.Forms.ToolStripStatusLabel();
            this.contextMenuTreeView = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.copyUrlMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.copyMethodMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.expandAllMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.collapseAllMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.panelTop = new System.Windows.Forms.Panel();
            this.labelMode = new System.Windows.Forms.Label();
            this.splitContainerMain = new System.Windows.Forms.SplitContainer();
            this.panelLeftSidebar = new System.Windows.Forms.Panel();
            this.labelEnvironments = new System.Windows.Forms.Label();
            this.listBoxEnvironments = new System.Windows.Forms.ListBox();
            this.panelConnectionLog = new System.Windows.Forms.Panel();
            this.txtConnectionLog = new System.Windows.Forms.TextBox();
            this.labelConnectionLog = new System.Windows.Forms.Label();
            this.panelConnectionLog.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainerMain)).BeginInit();
            this.splitContainerMain.Panel1.SuspendLayout();
            this.splitContainerMain.Panel2.SuspendLayout();
            this.splitContainerMain.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.groupBoxLeft.SuspendLayout();
            this.tabControl1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainerHistory)).BeginInit();
            this.splitContainerHistory.Panel1.SuspendLayout();
            this.splitContainerHistory.Panel2.SuspendLayout();
            this.splitContainerHistory.SuspendLayout();
            this.SuspendLayout();
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.FixedPanel = System.Windows.Forms.FixedPanel.Panel2;
            this.splitContainer1.Location = new System.Drawing.Point(0, 0);
            this.splitContainer1.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.splitContainer1.Name = "splitContainer1";
            this.splitContainer1.Panel2Collapsed = true;
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.BackColor = System.Drawing.Color.White;
            this.splitContainer1.Panel1.Controls.Add(this.groupBoxLeft);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(250)))), ((int)(((byte)(250)))), ((int)(((byte)(250)))));
            this.splitContainer1.Size = new System.Drawing.Size(599, 355);
            this.splitContainer1.SplitterDistance = 307;
            this.splitContainer1.SplitterWidth = 6;
            this.splitContainer1.TabIndex = 1;
            // 
            // groupBoxLeft
            // 
            this.groupBoxLeft.BackColor = System.Drawing.Color.White;
            this.groupBoxLeft.Controls.Add(this.tabControl1);
            this.groupBoxLeft.Dock = System.Windows.Forms.DockStyle.Fill;
            this.groupBoxLeft.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.groupBoxLeft.Location = new System.Drawing.Point(0, 0);
            this.groupBoxLeft.Margin = new System.Windows.Forms.Padding(0);
            this.groupBoxLeft.Name = "groupBoxLeft";
            this.groupBoxLeft.Padding = new System.Windows.Forms.Padding(0);
            this.groupBoxLeft.Size = new System.Drawing.Size(307, 355);
            this.groupBoxLeft.TabIndex = 0;
            this.groupBoxLeft.TabStop = false;
            // 
            // tabControl1
            // 
            this.tabControl1.Controls.Add(this.tabPage1);
            this.tabControl1.Controls.Add(this.tabPage2);
            this.tabControl1.Controls.Add(this.tabPageHistory);
            this.tabControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabControl1.Location = new System.Drawing.Point(0, 0);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(307, 355);
            this.tabControl1.TabIndex = 0;
            // 
            // tabPage1
            // 
            this.tabPage1.Location = new System.Drawing.Point(4, 25);
            this.tabPage1.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.tabPage1.Size = new System.Drawing.Size(462, 402);
            this.tabPage1.TabIndex = 0;
            this.tabPage1.Text = "tabPage1";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // tabPage2
            // 
            this.tabPage2.Location = new System.Drawing.Point(4, 25);
            this.tabPage2.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.Padding = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.tabPage2.Size = new System.Drawing.Size(463, 398);
            this.tabPage2.TabIndex = 1;
            this.tabPage2.Text = "REST Builder";
            this.tabPage2.UseVisualStyleBackColor = true;
            // 
            // tabPageHistory
            // 
            this.tabPageHistory.Controls.Add(this.splitContainerHistory);
            this.tabPageHistory.Location = new System.Drawing.Point(4, 25);
            this.tabPageHistory.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.tabPageHistory.Name = "tabPageHistory";
            this.tabPageHistory.Padding = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.tabPageHistory.Size = new System.Drawing.Size(463, 398);
            this.tabPageHistory.TabIndex = 2;
            this.tabPageHistory.Text = "Request History";
            this.tabPageHistory.UseVisualStyleBackColor = true;
            // 
            // splitContainerHistory
            // 
            this.splitContainerHistory.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainerHistory.FixedPanel = System.Windows.Forms.FixedPanel.Panel2;
            this.splitContainerHistory.Location = new System.Drawing.Point(3, 2);
            this.splitContainerHistory.Margin = new System.Windows.Forms.Padding(0);
            this.splitContainerHistory.Name = "splitContainerHistory";
            this.splitContainerHistory.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainerHistory.Panel1
            // 
            this.splitContainerHistory.Panel1.Controls.Add(this.listViewRequests);
            // 
            // splitContainerHistory.Panel2
            // 
            this.splitContainerHistory.Panel2.Controls.Add(this.treeView1);
            this.splitContainerHistory.Size = new System.Drawing.Size(457, 394);
            this.splitContainerHistory.SplitterDistance = 200;
            this.splitContainerHistory.SplitterWidth = 6;
            this.splitContainerHistory.TabIndex = 0;
            // 
            // listViewRequests
            // 
            this.listViewRequests.Dock = System.Windows.Forms.DockStyle.Fill;
            this.listViewRequests.FullRowSelect = true;
            this.listViewRequests.HideSelection = false;
            this.listViewRequests.Location = new System.Drawing.Point(0, 0);
            this.listViewRequests.MultiSelect = false;
            this.listViewRequests.Name = "listViewRequests";
            this.listViewRequests.Size = new System.Drawing.Size(457, 200);
            this.listViewRequests.TabIndex = 0;
            this.listViewRequests.UseCompatibleStateImageBehavior = false;
            this.listViewRequests.View = System.Windows.Forms.View.Details;
            // 
            // treeView1
            // 
            this.treeView1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.treeView1.BackColor = System.Drawing.Color.White;
            this.treeView1.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.treeView1.ContextMenuStrip = this.contextMenuTreeView;
            this.treeView1.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.treeView1.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(33)))), ((int)(((byte)(33)))), ((int)(((byte)(33)))));
            this.treeView1.ImageIndex = 0;
            this.treeView1.ImageList = this.imageList1;
            this.treeView1.Margin = new System.Windows.Forms.Padding(0);
            this.treeView1.Name = "treeView1";
            this.treeView1.SelectedImageIndex = 0;
            this.treeView1.ShowNodeToolTips = true;
            this.treeView1.TabIndex = 2;
            // 
            // imageList1
            // 
            this.imageList1.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("imageList1.ImageStream")));
            this.imageList1.TransparentColor = System.Drawing.Color.Transparent;
            this.imageList1.Images.SetKeyName(0, "file-unknow-line.png");
            this.imageList1.Images.SetKeyName(1, "file-search-fill.png");
            this.imageList1.Images.SetKeyName(2, "upload-cloud-fill.png");
            this.imageList1.Images.SetKeyName(3, "plug-fill.png");
            this.imageList1.Images.SetKeyName(4, "terminal-line.png");
            this.imageList1.Images.SetKeyName(5, "file-search-fill-red.png");
            // 
            // btnClear
            // 
            this.btnClear.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnClear.BackColor = System.Drawing.Color.Transparent;
            this.btnClear.FlatAppearance.BorderColor = System.Drawing.Color.White;
            this.btnClear.FlatAppearance.BorderSize = 1;
            this.btnClear.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(83)))), ((int)(((byte)(101)))), ((int)(((byte)(201)))));
            this.btnClear.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnClear.ForeColor = System.Drawing.Color.White;
            this.btnClear.Image = ((System.Drawing.Image)(resources.GetObject("btnClear.Image")));
            this.btnClear.Location = new System.Drawing.Point(756, 8);
            this.btnClear.Name = "btnClear";
            this.btnClear.Size = new System.Drawing.Size(32, 32);
            this.btnClear.TabIndex = 6;
            this.toolTipButtons.SetToolTip(this.btnClear, "Clear History (Ctrl+L)");
            this.btnClear.UseVisualStyleBackColor = false;
            this.btnClear.Click += new System.EventHandler(this.BtnClear_Click);
            // 
            // btnDebugger
            // 
            this.btnDebugger.BackColor = System.Drawing.Color.Transparent;
            this.btnDebugger.FlatAppearance.BorderColor = System.Drawing.Color.White;
            this.btnDebugger.FlatAppearance.BorderSize = 1;
            this.btnDebugger.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(83)))), ((int)(((byte)(101)))), ((int)(((byte)(201)))));
            this.btnDebugger.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnDebugger.ForeColor = System.Drawing.Color.White;
            this.btnDebugger.Image = ((System.Drawing.Image)(resources.GetObject("btnDebugger.Image")));
            this.btnDebugger.Location = new System.Drawing.Point(8, 8);
            this.btnDebugger.Name = "btnDebugger";
            this.btnDebugger.Size = new System.Drawing.Size(32, 32);
            this.btnDebugger.TabIndex = 4;
            this.toolTipButtons.SetToolTip(this.btnDebugger, "Attach Debugger (Ctrl+D)");
            this.btnDebugger.UseVisualStyleBackColor = false;
            this.btnDebugger.Click += new System.EventHandler(this.BtnDebugger_Click);
            // 
            // ComboBoxBehavior
            // 
            this.ComboBoxBehavior.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(83)))), ((int)(((byte)(101)))), ((int)(((byte)(201)))));
            this.ComboBoxBehavior.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.ComboBoxBehavior.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.ComboBoxBehavior.Font = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ComboBoxBehavior.ForeColor = System.Drawing.Color.White;
            this.ComboBoxBehavior.FormattingEnabled = true;
            this.ComboBoxBehavior.Items.AddRange(new object[] {
            "🚫Do not intercept requests",
            "▶️  Intercept requests and execute plugins locally",
            "⏯️Intercept requests and execute plugins locally with auto break"});
            this.ComboBoxBehavior.Location = new System.Drawing.Point(58, 32);
            this.ComboBoxBehavior.Name = "ComboBoxBehavior";
            this.ComboBoxBehavior.Size = new System.Drawing.Size(520, 29);
            this.ComboBoxBehavior.TabIndex = 5;
            this.ComboBoxBehavior.SelectedIndexChanged += new System.EventHandler(this.ComboBoxBehavior_SelectedIndexChanged);
            // 
            // panelConnectionLog
            // 
            this.panelConnectionLog.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(37)))), ((int)(((byte)(37)))), ((int)(((byte)(38)))));
            this.panelConnectionLog.Controls.Add(this.txtConnectionLog);
            this.panelConnectionLog.Controls.Add(this.labelConnectionLog);
            this.panelConnectionLog.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panelConnectionLog.Location = new System.Drawing.Point(8, 11);
            this.panelConnectionLog.Name = "panelConnectionLog";
            this.panelConnectionLog.Size = new System.Drawing.Size(184, 336);
            this.panelConnectionLog.TabIndex = 3;
            this.panelConnectionLog.Visible = false;
            // 
            // labelConnectionLog
            // 
            this.labelConnectionLog.AutoSize = true;
            this.labelConnectionLog.Dock = System.Windows.Forms.DockStyle.Top;
            this.labelConnectionLog.Font = new System.Drawing.Font("Segoe UI Semibold", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelConnectionLog.ForeColor = System.Drawing.Color.White;
            this.labelConnectionLog.Location = new System.Drawing.Point(0, 0);
            this.labelConnectionLog.Name = "labelConnectionLog";
            this.labelConnectionLog.Padding = new System.Windows.Forms.Padding(0, 4, 0, 4);
            this.labelConnectionLog.Size = new System.Drawing.Size(112, 28);
            this.labelConnectionLog.TabIndex = 0;
            this.labelConnectionLog.Text = "Connection Log";
            // 
            // txtConnectionLog
            // 
            this.txtConnectionLog.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(37)))), ((int)(((byte)(37)))), ((int)(((byte)(38)))));
            this.txtConnectionLog.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.txtConnectionLog.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtConnectionLog.Font = new System.Drawing.Font("Consolas", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtConnectionLog.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.txtConnectionLog.Location = new System.Drawing.Point(0, 28);
            this.txtConnectionLog.Multiline = true;
            this.txtConnectionLog.Name = "txtConnectionLog";
            this.txtConnectionLog.Padding = new System.Windows.Forms.Padding(0, 0, 0, 0);
            this.txtConnectionLog.ReadOnly = true;
            this.txtConnectionLog.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtConnectionLog.Size = new System.Drawing.Size(184, 308);
            this.txtConnectionLog.TabIndex = 1;
            this.txtConnectionLog.WordWrap = true;
            // 
            // statusStrip1
            // 
            this.statusStrip1.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.statusStrip1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(63)))), ((int)(((byte)(81)))), ((int)(((byte)(181)))));
            this.statusStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripStatusLabel1});
            this.statusStrip1.Location = new System.Drawing.Point(0, 425);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.statusStrip1.Size = new System.Drawing.Size(800, 25);
            this.statusStrip1.SizingGrip = false;
            this.statusStrip1.TabIndex = 2;
            this.statusStrip1.Text = "statusStrip1";
            // 
            // toolStripStatusLabel1
            // 
            this.toolStripStatusLabel1.ForeColor = System.Drawing.Color.White;
            this.toolStripStatusLabel1.Name = "toolStripStatusLabel1";
            this.toolStripStatusLabel1.Spring = true;
            this.toolStripStatusLabel1.Size = new System.Drawing.Size(49, 20);
            this.toolStripStatusLabel1.Text = "Ready";
            this.toolStripStatusLabel1.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // contextMenuTreeView
            // 
            this.contextMenuTreeView.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.contextMenuTreeView.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.copyUrlMenuItem,
            this.copyMethodMenuItem,
            this.toolStripSeparator1,
            this.expandAllMenuItem,
            this.collapseAllMenuItem});
            this.contextMenuTreeView.Name = "contextMenuTreeView";
            this.contextMenuTreeView.Size = new System.Drawing.Size(181, 130);
            // 
            // copyUrlMenuItem
            // 
            this.copyUrlMenuItem.Name = "copyUrlMenuItem";
            this.copyUrlMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.C)));
            this.copyUrlMenuItem.Size = new System.Drawing.Size(180, 24);
            this.copyUrlMenuItem.Text = "Copy URL";
            this.copyUrlMenuItem.Click += new System.EventHandler(this.CopyUrlMenuItem_Click);
            // 
            // copyMethodMenuItem
            // 
            this.copyMethodMenuItem.Name = "copyMethodMenuItem";
            this.copyMethodMenuItem.Size = new System.Drawing.Size(180, 24);
            this.copyMethodMenuItem.Text = "Copy Method";
            this.copyMethodMenuItem.Click += new System.EventHandler(this.CopyMethodMenuItem_Click);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(177, 6);
            // 
            // expandAllMenuItem
            // 
            this.expandAllMenuItem.Name = "expandAllMenuItem";
            this.expandAllMenuItem.Size = new System.Drawing.Size(180, 24);
            this.expandAllMenuItem.Text = "Expand All";
            this.expandAllMenuItem.Click += new System.EventHandler(this.ExpandAllMenuItem_Click);
            // 
            // collapseAllMenuItem
            // 
            this.collapseAllMenuItem.Name = "collapseAllMenuItem";
            this.collapseAllMenuItem.Size = new System.Drawing.Size(180, 24);
            this.collapseAllMenuItem.Text = "Collapse All";
            this.collapseAllMenuItem.Click += new System.EventHandler(this.CollapseAllMenuItem_Click);
            // 
            // splitContainerMain
            // 
            this.splitContainerMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainerMain.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
            this.splitContainerMain.Location = new System.Drawing.Point(0, 48);
            this.splitContainerMain.Name = "splitContainerMain";
            // 
            // splitContainerMain.Panel1
            // 
            this.splitContainerMain.Panel1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(48)))));
            this.splitContainerMain.Panel1.Controls.Add(this.panelLeftSidebar);
            this.splitContainerMain.Panel1MinSize = 180;
            // 
            // splitContainerMain.Panel2
            // 
            this.splitContainerMain.Panel2.Controls.Add(this.splitContainer1);
            this.splitContainerMain.Size = new System.Drawing.Size(800, 355);
            this.splitContainerMain.SplitterDistance = 200;
            this.splitContainerMain.SplitterWidth = 1;
            this.splitContainerMain.TabIndex = 4;
            // 
            // panelLeftSidebar
            // 
            this.panelLeftSidebar.Controls.Add(this.listBoxEnvironments);
            this.panelLeftSidebar.Controls.Add(this.panelConnectionLog);
            this.panelLeftSidebar.Controls.Add(this.labelEnvironments);
            this.panelLeftSidebar.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelLeftSidebar.Location = new System.Drawing.Point(0, 0);
            this.panelLeftSidebar.Name = "panelLeftSidebar";
            this.panelLeftSidebar.Padding = new System.Windows.Forms.Padding(8);
            this.panelLeftSidebar.Size = new System.Drawing.Size(200, 355);
            this.panelLeftSidebar.TabIndex = 0;
            // 
            // labelEnvironments
            // 
            this.labelEnvironments.AutoSize = true;
            this.labelEnvironments.Dock = System.Windows.Forms.DockStyle.Top;
            this.labelEnvironments.Font = new System.Drawing.Font("Segoe UI Semibold", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelEnvironments.ForeColor = System.Drawing.Color.White;
            this.labelEnvironments.Location = new System.Drawing.Point(8, 8);
            this.labelEnvironments.Name = "labelEnvironments";
            this.labelEnvironments.Padding = new System.Windows.Forms.Padding(0, 0, 0, 4);
            this.labelEnvironments.Size = new System.Drawing.Size(96, 24);
            this.labelEnvironments.TabIndex = 0;
            this.labelEnvironments.Text = "Environments";
            // 
            // listBoxEnvironments
            // 
            this.listBoxEnvironments.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(37)))), ((int)(((byte)(37)))), ((int)(((byte)(38)))));
            this.listBoxEnvironments.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.listBoxEnvironments.Dock = System.Windows.Forms.DockStyle.Fill;
            this.listBoxEnvironments.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.listBoxEnvironments.ForeColor = System.Drawing.Color.White;
            this.listBoxEnvironments.FormattingEnabled = true;
            this.listBoxEnvironments.ItemHeight = 20;
            this.listBoxEnvironments.Location = new System.Drawing.Point(8, 32);
            this.listBoxEnvironments.Name = "listBoxEnvironments";
            this.listBoxEnvironments.Size = new System.Drawing.Size(184, 315);
            this.listBoxEnvironments.TabIndex = 1;
            this.listBoxEnvironments.DoubleClick += new System.EventHandler(this.ListBoxEnvironments_DoubleClick);
            // 
            // panelTop
            // 
            this.panelTop.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(63)))), ((int)(((byte)(81)))), ((int)(((byte)(181)))));
            this.panelTop.Controls.Add(this.btnClear);
            this.panelTop.Controls.Add(this.ComboBoxBehavior);
            this.panelTop.Controls.Add(this.labelMode);
            this.panelTop.Controls.Add(this.btnDebugger);
            this.panelTop.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelTop.Location = new System.Drawing.Point(0, 0);
            this.panelTop.Name = "panelTop";
            this.panelTop.Padding = new System.Windows.Forms.Padding(8);
            this.panelTop.Size = new System.Drawing.Size(800, 48);
            this.panelTop.TabIndex = 3;
            this.panelTop.Resize += new System.EventHandler(this.PanelTop_Resize);
            // 
            // labelMode
            // 
            this.labelMode.AutoSize = true;
            this.labelMode.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelMode.ForeColor = System.Drawing.Color.White;
            this.labelMode.Location = new System.Drawing.Point(58, 10);
            this.labelMode.Name = "labelMode";
            this.labelMode.Size = new System.Drawing.Size(107, 20);
            this.labelMode.TabIndex = 0;
            this.labelMode.Text = "Execution Mode";
            // 
            // BrowserWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(250)))), ((int)(((byte)(250)))), ((int)(((byte)(250)))));
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.splitContainerMain);
            this.Controls.Add(this.panelTop);
            this.Controls.Add(this.statusStrip1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.KeyPreview = true;
            this.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.Name = "BrowserWindow";
            this.Text = "Dataverse Browser";
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.BrowserWindow_KeyDown);
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.splitContainerMain.Panel1.ResumeLayout(false);
            this.splitContainerMain.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainerMain)).EndInit();
            this.splitContainerMain.ResumeLayout(false);
            this.panelConnectionLog.ResumeLayout(false);
            this.panelConnectionLog.PerformLayout();
            this.panelLeftSidebar.ResumeLayout(false);
            this.panelLeftSidebar.PerformLayout();
            this.groupBoxLeft.ResumeLayout(false);
            this.tabControl1.ResumeLayout(false);
            this.splitContainerHistory.Panel1.ResumeLayout(false);
            this.splitContainerHistory.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainerHistory)).EndInit();
            this.splitContainerHistory.ResumeLayout(false);
            this.panelTop.ResumeLayout(false);
            this.panelTop.PerformLayout();
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.TreeView treeView1;
        private System.Windows.Forms.ImageList imageList1;
        private System.Windows.Forms.Button btnClear;
        private System.Windows.Forms.Button btnDebugger;
        private System.Windows.Forms.ToolTip toolTipButtons;
        private System.Windows.Forms.ComboBox ComboBoxBehavior;
        private System.Windows.Forms.GroupBox groupBoxLeft;
        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tabPage1;
        private System.Windows.Forms.TabPage tabPage2;
        private System.Windows.Forms.TabPage tabPageHistory;
        private System.Windows.Forms.SplitContainer splitContainerHistory;
        private System.Windows.Forms.ListView listViewRequests;
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabel1;
        private System.Windows.Forms.ContextMenuStrip contextMenuTreeView;
        private System.Windows.Forms.ToolStripMenuItem copyUrlMenuItem;
        private System.Windows.Forms.ToolStripMenuItem copyMethodMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripMenuItem expandAllMenuItem;
        private System.Windows.Forms.ToolStripMenuItem collapseAllMenuItem;
        private System.Windows.Forms.Panel panelTop;
        private System.Windows.Forms.Label labelMode;
        private System.Windows.Forms.SplitContainer splitContainerMain;
        private System.Windows.Forms.Panel panelLeftSidebar;
        private System.Windows.Forms.Label labelEnvironments;
        private System.Windows.Forms.ListBox listBoxEnvironments;
        private System.Windows.Forms.Panel panelConnectionLog;
        private System.Windows.Forms.TextBox txtConnectionLog;
        private System.Windows.Forms.Label labelConnectionLog;
    }
}
