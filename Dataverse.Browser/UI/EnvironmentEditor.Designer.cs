namespace Dataverse.Browser.UI
{
    partial class EnvironmentEditor
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(EnvironmentEditor));
            this.label1 = new System.Windows.Forms.Label();
            this.txtHostName = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.txtAssemblyPath = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.txtName = new System.Windows.Forms.TextBox();
            this.btnOk = new System.Windows.Forms.Button();
            this.cbDisableAsyncSteps = new System.Windows.Forms.CheckBox();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            this.openFileDialog = new System.Windows.Forms.OpenFileDialog();
            this.btnSelectAssembly = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(9, 59);
            this.label1.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(294, 13);
            this.label1.TabIndex = 1;
            this.label1.Text = "Environment hostname (eg: myinstance.crm4.dynamics.com):";
            // 
            // txtHostName
            // 
            this.txtHostName.Location = new System.Drawing.Point(11, 75);
            this.txtHostName.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.txtHostName.Name = "txtHostName";
            this.txtHostName.Size = new System.Drawing.Size(320, 20);
            this.txtHostName.TabIndex = 2;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(9, 106);
            this.label2.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(255, 13);
            this.label2.TabIndex = 3;
            this.label2.Text = "Assembly Path (preferably compiled in debug mode): ";
            // 
            // txtAssemblyPath
            // 
            this.txtAssemblyPath.Location = new System.Drawing.Point(9, 122);
            this.txtAssemblyPath.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.txtAssemblyPath.Name = "txtAssemblyPath";
            this.txtAssemblyPath.Size = new System.Drawing.Size(296, 20);
            this.txtAssemblyPath.TabIndex = 3;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(9, 19);
            this.label3.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(179, 13);
            this.label3.TabIndex = 5;
            this.label3.Text = "Choose a name for this environment:";
            // 
            // txtName
            // 
            this.txtName.Location = new System.Drawing.Point(11, 34);
            this.txtName.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.txtName.Name = "txtName";
            this.txtName.Size = new System.Drawing.Size(320, 20);
            this.txtName.TabIndex = 1;
            // 
            // btnOk
            // 
            this.btnOk.Location = new System.Drawing.Point(274, 171);
            this.btnOk.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.btnOk.Name = "btnOk";
            this.btnOk.Size = new System.Drawing.Size(56, 19);
            this.btnOk.TabIndex = 4;
            this.btnOk.Text = "Go";
            this.btnOk.UseVisualStyleBackColor = true;
            this.btnOk.Click += new System.EventHandler(this.BtnOk_Click);
            // 

            // cbDisableAsyncSteps
            // 
            this.cbDisableAsyncSteps.AutoSize = true;
            this.cbDisableAsyncSteps.Location = new System.Drawing.Point(15, 179);
            this.cbDisableAsyncSteps.Name = "cbDisableAsyncSteps";
            this.cbDisableAsyncSteps.Size = new System.Drawing.Size(199, 20);
            this.cbDisableAsyncSteps.TabIndex = 6;
            this.cbDisableAsyncSteps.Text = "Disable asynchronous steps";
            this.toolTip1.SetToolTip(this.cbDisableAsyncSteps, resources.GetString("cbDisableAsyncSteps.ToolTip"));
            this.cbDisableAsyncSteps.UseVisualStyleBackColor = true;

            // openFileDialog
            // 
            this.openFileDialog.FileName = "openFileDialog1";
            // 
            // btnSelectAssembly
            // 
            this.btnSelectAssembly.Location = new System.Drawing.Point(307, 122);
            this.btnSelectAssembly.Name = "btnSelectAssembly";
            this.btnSelectAssembly.Size = new System.Drawing.Size(24, 20);
            this.btnSelectAssembly.TabIndex = 6;
            this.btnSelectAssembly.Text = "...";
            this.btnSelectAssembly.UseVisualStyleBackColor = true;
            this.btnSelectAssembly.Click += new System.EventHandler(this.btnSelectAssembly_Click);

            // 
            // EnvironmentEditor
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;

            this.ClientSize = new System.Drawing.Size(449, 246);
            this.Controls.Add(this.cbDisableAsyncSteps);
            this.Controls.Add(this.btnSelectAssembly);
            this.Controls.Add(this.btnOk);
            this.Controls.Add(this.txtName);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.txtAssemblyPath);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.txtHostName);
            this.Controls.Add(this.label1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));

            this.MaximizeBox = false;
            this.Name = "EnvironmentEditor";
            this.Text = "Environment Settings";
            this.Load += new System.EventHandler(this.EnvironmentEditor_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox txtHostName;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox txtAssemblyPath;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox txtName;
        private System.Windows.Forms.Button btnOk;
        private System.Windows.Forms.CheckBox cbDisableAsyncSteps;
        private System.Windows.Forms.ToolTip toolTip1;
        private System.Windows.Forms.OpenFileDialog openFileDialog;
        private System.Windows.Forms.Button btnSelectAssembly;
    }
}

