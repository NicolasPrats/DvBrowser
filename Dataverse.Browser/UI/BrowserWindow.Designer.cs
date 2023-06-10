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
            this.ComboBoxBehavior = new System.Windows.Forms.ComboBox();
            this.btnDebugger = new System.Windows.Forms.Button();
            this.btnClear = new System.Windows.Forms.Button();
            this.treeView1 = new System.Windows.Forms.TreeView();
            this.imageList1 = new System.Windows.Forms.ImageList(this.components);
            this.btnDevTools = new System.Windows.Forms.Button();
            this.toolTipButtons = new System.Windows.Forms.ToolTip(this.components);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.SuspendLayout();
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(0, 0);
            this.splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.ComboBoxBehavior);
            this.splitContainer1.Panel2.Controls.Add(this.btnDebugger);
            this.splitContainer1.Panel2.Controls.Add(this.btnClear);
            this.splitContainer1.Panel2.Controls.Add(this.treeView1);
            this.splitContainer1.Panel2.Controls.Add(this.btnDevTools);
            this.splitContainer1.Size = new System.Drawing.Size(800, 450);
            this.splitContainer1.SplitterDistance = 531;
            this.splitContainer1.TabIndex = 1;
            // 
            // ComboBoxBehavior
            // 
            this.ComboBoxBehavior.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.ComboBoxBehavior.CausesValidation = false;
            this.ComboBoxBehavior.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.ComboBoxBehavior.FormattingEnabled = true;
            this.ComboBoxBehavior.Items.AddRange(new object[] {
            "Do not execute plugins",
            "Execute plugins ",
            "Execute plugins with auto break"});
            this.ComboBoxBehavior.Location = new System.Drawing.Point(86, 12);
            this.ComboBoxBehavior.Name = "ComboBoxBehavior";
            this.ComboBoxBehavior.Size = new System.Drawing.Size(176, 24);
            this.ComboBoxBehavior.TabIndex = 5;
            this.ComboBoxBehavior.SelectedIndexChanged += new System.EventHandler(this.ComboBoxBehavior_SelectedIndexChanged);
            // 
            // btnDebugger
            // 
            this.btnDebugger.Image = ((System.Drawing.Image)(resources.GetObject("btnDebugger.Image")));
            this.btnDebugger.Location = new System.Drawing.Point(49, 5);
            this.btnDebugger.Name = "btnDebugger";
            this.btnDebugger.Size = new System.Drawing.Size(31, 31);
            this.btnDebugger.TabIndex = 4;
            this.toolTipButtons.SetToolTip(this.btnDebugger, "Attach a debugger");
            this.btnDebugger.UseVisualStyleBackColor = true;
            this.btnDebugger.Click += new System.EventHandler(this.BtnDebugger_Click);
            // 
            // btnClear
            // 
            this.btnClear.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnClear.Image = ((System.Drawing.Image)(resources.GetObject("btnClear.Image")));
            this.btnClear.Location = new System.Drawing.Point(231, 416);
            this.btnClear.Name = "btnClear";
            this.btnClear.Size = new System.Drawing.Size(31, 31);
            this.btnClear.TabIndex = 3;
            this.toolTipButtons.SetToolTip(this.btnClear, "Clear the history of requests");
            this.btnClear.UseVisualStyleBackColor = true;
            this.btnClear.Click += new System.EventHandler(this.BtnClear_Click);
            // 
            // treeView1
            // 
            this.treeView1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.treeView1.ImageIndex = 0;
            this.treeView1.ImageList = this.imageList1;
            this.treeView1.Location = new System.Drawing.Point(12, 42);
            this.treeView1.Name = "treeView1";
            this.treeView1.SelectedImageIndex = 0;
            this.treeView1.ShowNodeToolTips = true;
            this.treeView1.Size = new System.Drawing.Size(250, 405);
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
            // btnDevTools
            // 
            this.btnDevTools.AccessibleDescription = "";
            this.btnDevTools.Image = ((System.Drawing.Image)(resources.GetObject("btnDevTools.Image")));
            this.btnDevTools.Location = new System.Drawing.Point(12, 5);
            this.btnDevTools.Name = "btnDevTools";
            this.btnDevTools.Size = new System.Drawing.Size(31, 31);
            this.btnDevTools.TabIndex = 1;
            this.toolTipButtons.SetToolTip(this.btnDevTools, "Open the browser Dev Tool");
            this.btnDevTools.UseVisualStyleBackColor = true;
            this.btnDevTools.Click += new System.EventHandler(this.BtnDevTools_Click);
            // 
            // BrowserWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.splitContainer1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "BrowserWindow";
            this.Text = "Browser";
            this.splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.Button btnDevTools;
        private System.Windows.Forms.TreeView treeView1;
        private System.Windows.Forms.ImageList imageList1;
        private System.Windows.Forms.Button btnClear;
        private System.Windows.Forms.Button btnDebugger;
        private System.Windows.Forms.ToolTip toolTipButtons;
        private System.Windows.Forms.ComboBox ComboBoxBehavior;
    }
}

