namespace Dataverse.Browser.UI
{
    partial class BrowserTab
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(BrowserTab));
            this.txtAddress = new System.Windows.Forms.TextBox();
            this.BtnDevTools = new System.Windows.Forms.Button();
            this.panel1 = new System.Windows.Forms.Panel();
            this.SuspendLayout();
            // 
            // txtAddress
            // 
            this.txtAddress.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtAddress.Location = new System.Drawing.Point(5, 4);
            this.txtAddress.Name = "txtAddress";
            this.txtAddress.Size = new System.Drawing.Size(919, 22);
            this.txtAddress.TabIndex = 0;
            // 
            // BtnDevTools
            // 
            this.BtnDevTools.AccessibleDescription = "";
            this.BtnDevTools.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.BtnDevTools.Image = ((System.Drawing.Image)(resources.GetObject("BtnDevTools.Image")));
            this.BtnDevTools.Location = new System.Drawing.Point(930, 0);
            this.BtnDevTools.Name = "BtnDevTools";
            this.BtnDevTools.Size = new System.Drawing.Size(31, 31);
            this.BtnDevTools.TabIndex = 2;
            this.BtnDevTools.UseVisualStyleBackColor = true;
            this.BtnDevTools.Click += new System.EventHandler(this.BtnDevTools_Click);
            // 
            // panel1
            // 
            this.panel1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.panel1.Location = new System.Drawing.Point(2, 32);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(961, 276);
            this.panel1.TabIndex = 3;
            // 
            // BrowserTab
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.BtnDevTools);
            this.Controls.Add(this.txtAddress);
            this.Name = "BrowserTab";
            this.Size = new System.Drawing.Size(964, 309);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox txtAddress;
        private System.Windows.Forms.Button BtnDevTools;
        private System.Windows.Forms.Panel panel1;
    }
}
