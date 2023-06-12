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
            this.ComboBoxBehavior = new System.Windows.Forms.ComboBox();
            this.btnClear = new System.Windows.Forms.Button();
            this.treeView1 = new System.Windows.Forms.TreeView();
            this.imageList1 = new System.Windows.Forms.ImageList(this.components);
            this.groupBoxRight = new System.Windows.Forms.GroupBox();
            this.btnDebugger = new System.Windows.Forms.Button();
            this.toolTipButtons = new System.Windows.Forms.ToolTip(this.components);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.groupBoxRight.SuspendLayout();
            this.SuspendLayout();
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(0, 0);
            this.splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.groupBoxLeft);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.ComboBoxBehavior);
            this.splitContainer1.Panel2.Controls.Add(this.btnClear);
            this.splitContainer1.Panel2.Controls.Add(this.treeView1);
            this.splitContainer1.Panel2.Controls.Add(this.groupBoxRight);
            this.splitContainer1.Size = new System.Drawing.Size(800, 450);
            this.splitContainer1.SplitterDistance = 531;
            this.splitContainer1.TabIndex = 1;
            // 
            // groupBoxLeft
            // 
            this.groupBoxLeft.Dock = System.Windows.Forms.DockStyle.Fill;
            this.groupBoxLeft.Location = new System.Drawing.Point(0, 0);
            this.groupBoxLeft.Name = "groupBoxLeft";
            this.groupBoxLeft.Size = new System.Drawing.Size(531, 450);
            this.groupBoxLeft.TabIndex = 0;
            this.groupBoxLeft.TabStop = false;
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
            this.ComboBoxBehavior.Location = new System.Drawing.Point(43, 12);
            this.ComboBoxBehavior.Name = "ComboBoxBehavior";
            this.ComboBoxBehavior.Size = new System.Drawing.Size(213, 24);
            this.ComboBoxBehavior.TabIndex = 5;
            this.ComboBoxBehavior.SelectedIndexChanged += new System.EventHandler(this.ComboBoxBehavior_SelectedIndexChanged);
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
            this.treeView1.ImageIndex = 0;
            this.treeView1.ImageList = this.imageList1;
            this.treeView1.Location = new System.Drawing.Point(6, 42);
            this.treeView1.Margin = new System.Windows.Forms.Padding(0);
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
            // groupBoxRight
            // 
            this.groupBoxRight.Controls.Add(this.btnDebugger);
            this.groupBoxRight.Dock = System.Windows.Forms.DockStyle.Fill;
            this.groupBoxRight.Location = new System.Drawing.Point(0, 0);
            this.groupBoxRight.Name = "groupBoxRight";
            this.groupBoxRight.Size = new System.Drawing.Size(265, 450);
            this.groupBoxRight.TabIndex = 6;
            this.groupBoxRight.TabStop = false;
            // 
            // btnDebugger
            // 
            this.btnDebugger.Image = ((System.Drawing.Image)(resources.GetObject("btnDebugger.Image")));
            this.btnDebugger.Location = new System.Drawing.Point(6, 8);
            this.btnDebugger.Name = "btnDebugger";
            this.btnDebugger.Size = new System.Drawing.Size(31, 31);
            this.btnDebugger.TabIndex = 4;
            this.toolTipButtons.SetToolTip(this.btnDebugger, "Attach a debugger");
            this.btnDebugger.UseVisualStyleBackColor = true;
            this.btnDebugger.Click += new System.EventHandler(this.BtnDebugger_Click);
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
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.groupBoxRight.ResumeLayout(false);
            this.ResumeLayout(false);

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
    }
}

