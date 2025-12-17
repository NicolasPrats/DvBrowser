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
            this.groupBoxRight = new System.Windows.Forms.GroupBox();
            this.btnClear = new System.Windows.Forms.Button();
            this.treeView1 = new System.Windows.Forms.TreeView();
            this.imageList1 = new System.Windows.Forms.ImageList(this.components);
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
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.groupBoxLeft.SuspendLayout();
            this.tabControl1.SuspendLayout();
            this.groupBoxRight.SuspendLayout();
            this.SuspendLayout();
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.FixedPanel = System.Windows.Forms.FixedPanel.Panel2;
            this.splitContainer1.Location = new System.Drawing.Point(0, 70);
            this.splitContainer1.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.BackColor = System.Drawing.Color.White;
            this.splitContainer1.Panel1.Controls.Add(this.groupBoxLeft);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(250)))), ((int)(((byte)(250)))), ((int)(((byte)(250)))));
            this.splitContainer1.Panel2.Controls.Add(this.groupBoxRight);
            this.splitContainer1.Size = new System.Drawing.Size(800, 355);
            this.splitContainer1.SplitterDistance = 520;
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
            this.groupBoxLeft.Size = new System.Drawing.Size(520, 355);
            this.groupBoxLeft.TabIndex = 0;
            this.groupBoxLeft.TabStop = false;
            // 
            // tabControl1
            // 
            this.tabControl1.Controls.Add(this.tabPage1);
            this.tabControl1.Controls.Add(this.tabPage2);
            this.tabControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabControl1.Location = new System.Drawing.Point(0, 0);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(520, 355);
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
            // groupBoxRight
            // 
            this.groupBoxRight.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(250)))), ((int)(((byte)(250)))), ((int)(((byte)(250)))));
            this.groupBoxRight.Controls.Add(this.btnClear);
            this.groupBoxRight.Controls.Add(this.treeView1);
            this.groupBoxRight.Dock = System.Windows.Forms.DockStyle.Fill;
            this.groupBoxRight.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.groupBoxRight.Location = new System.Drawing.Point(0, 0);
            this.groupBoxRight.Margin = new System.Windows.Forms.Padding(0);
            this.groupBoxRight.Name = "groupBoxRight";
            this.groupBoxRight.Padding = new System.Windows.Forms.Padding(8, 8, 8, 50);
            this.groupBoxRight.Size = new System.Drawing.Size(274, 355);
            this.groupBoxRight.TabIndex = 6;
            this.groupBoxRight.TabStop = false;
            this.groupBoxRight.Text = "Request History";
            // 
            // btnClear
            // 
            this.btnClear.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnClear.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(63)))), ((int)(((byte)(81)))), ((int)(((byte)(181)))));
            this.btnClear.FlatAppearance.BorderSize = 0;
            this.btnClear.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnClear.ForeColor = System.Drawing.Color.White;
            this.btnClear.Image = ((System.Drawing.Image)(resources.GetObject("btnClear.Image")));
            this.btnClear.Location = new System.Drawing.Point(220, 315);
            this.btnClear.Name = "btnClear";
            this.btnClear.Size = new System.Drawing.Size(44, 40);
            this.btnClear.TabIndex = 3;
            this.toolTipButtons.SetToolTip(this.btnClear, "Clear the history of requests");
            this.btnClear.UseVisualStyleBackColor = false;
            this.btnClear.Click += new System.EventHandler(this.BtnClear_Click);
            // 
            // treeView1
            // 
            this.treeView1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.treeView1.BackColor = System.Drawing.Color.White;
            this.treeView1.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.treeView1.ContextMenuStrip = this.contextMenuTreeView;
            this.treeView1.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.treeView1.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(33)))), ((int)(((byte)(33)))), ((int)(((byte)(33)))));
            this.treeView1.ImageIndex = 0;
            this.treeView1.ImageList = this.imageList1;
            this.treeView1.Location = new System.Drawing.Point(8, 24);
            this.treeView1.Margin = new System.Windows.Forms.Padding(0);
            this.treeView1.Name = "treeView1";
            this.treeView1.SelectedImageIndex = 0;
            this.treeView1.ShowNodeToolTips = true;
            this.treeView1.Size = new System.Drawing.Size(258, 291);
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
            // btnDebugger
            // 
            this.btnDebugger.BackColor = System.Drawing.Color.Transparent;
            this.btnDebugger.FlatAppearance.BorderColor = System.Drawing.Color.White;
            this.btnDebugger.FlatAppearance.BorderSize = 1;
            this.btnDebugger.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(83)))), ((int)(((byte)(101)))), ((int)(((byte)(201)))));
            this.btnDebugger.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnDebugger.ForeColor = System.Drawing.Color.White;
            this.btnDebugger.Image = ((System.Drawing.Image)(resources.GetObject("btnDebugger.Image")));
            this.btnDebugger.Location = new System.Drawing.Point(12, 12);
            this.btnDebugger.Name = "btnDebugger";
            this.btnDebugger.Size = new System.Drawing.Size(40, 46);
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
            // statusStrip1
            // 
            this.statusStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripStatusLabel1});
            this.statusStrip1.Location = new System.Drawing.Point(0, 425);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Size = new System.Drawing.Size(800, 25);
            this.statusStrip1.TabIndex = 2;
            this.statusStrip1.Text = "statusStrip1";
            // 
            // toolStripStatusLabel1
            // 
            this.toolStripStatusLabel1.Name = "toolStripStatusLabel1";
            this.toolStripStatusLabel1.Size = new System.Drawing.Size(49, 20);
            this.toolStripStatusLabel1.Text = "Ready";
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
            // panelTop
            // 
            this.panelTop.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(63)))), ((int)(((byte)(81)))), ((int)(((byte)(181)))));
            this.panelTop.Controls.Add(this.ComboBoxBehavior);
            this.panelTop.Controls.Add(this.labelMode);
            this.panelTop.Controls.Add(this.btnDebugger);
            this.panelTop.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelTop.Location = new System.Drawing.Point(0, 0);
            this.panelTop.Name = "panelTop";
            this.panelTop.Padding = new System.Windows.Forms.Padding(12);
            this.panelTop.Size = new System.Drawing.Size(800, 70);
            this.panelTop.TabIndex = 3;
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
            this.Controls.Add(this.splitContainer1);
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
            this.groupBoxLeft.ResumeLayout(false);
            this.tabControl1.ResumeLayout(false);
            this.groupBoxRight.ResumeLayout(false);
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
        private System.Windows.Forms.GroupBox groupBoxRight;
        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tabPage1;
        private System.Windows.Forms.TabPage tabPage2;
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
    }
}

