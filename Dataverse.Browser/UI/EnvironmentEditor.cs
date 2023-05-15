using System;
using System.IO;
using System.Windows.Forms;
using Dataverse.Browser.Configuration;

namespace Dataverse.Browser.UI
{
    internal partial class EnvironmentEditor : Form
    {
        private EnvironnementConfiguration CurrentEnvironment { get; }

        public EnvironnementConfiguration SelectedEnvironment { get; private set; }

        public EnvironmentEditor(EnvironnementConfiguration environment)
        {
            this.InitializeComponent();
            this.CurrentEnvironment = environment;
        }

        private void EnvironmentEditor_Load(object sender, EventArgs e)
        {
            if (CurrentEnvironment != null)
            {
                txtName.Text = CurrentEnvironment.Name;
                txtAssemblyPath.Text = CurrentEnvironment.PluginAssemblies[0];
                txtHostName.Text = CurrentEnvironment.DataverseHost;
                cbDisableAsyncSteps.Checked = CurrentEnvironment.DisableAsyncSteps;
            }
        }

        private void BtnOk_Click(object sender, EventArgs e)
        {
            string hostName = GetValidHostName();
            if (hostName == null)
                return;
            string assemblyPath = GetValidAssemblyPath();
            if (assemblyPath == null)
                return;
            this.SelectedEnvironment = CurrentEnvironment ?? new EnvironnementConfiguration();
            this.SelectedEnvironment.Name = txtName.Text;
            this.SelectedEnvironment.DisableAsyncSteps = cbDisableAsyncSteps.Checked;


            this.SelectedEnvironment.PluginAssemblies = new string[] { assemblyPath };

            this.SelectedEnvironment.DataverseHost = hostName;
            if (this.SelectedEnvironment.Id == Guid.Empty)//Environments created in first versions don't have an ID.
            {
                this.SelectedEnvironment.Id = Guid.NewGuid();
            }
            this.Close();
        }

        private string GetValidAssemblyPath()
        {
            string assemblyPath = txtAssemblyPath.Text;
            if (assemblyPath.StartsWith("\"") && assemblyPath.EndsWith("\""))
            {
                assemblyPath = assemblyPath.Substring(1, assemblyPath.Length - 2);
            }
            if (!File.Exists(assemblyPath))
            {
                MessageBox.Show("File not found:" + assemblyPath);
                return null;
            }
            return assemblyPath;
        }

        private string GetValidHostName()
        {
            string hostName = txtHostName.Text;
            if (hostName.ToLowerInvariant().StartsWith("https://"))
            {
                hostName = hostName.Substring("https://".Length);
            }
            int indexOfSlash = hostName.IndexOf('/');
            if (indexOfSlash > 0)
            {
                hostName = hostName.Substring(0, indexOfSlash);
            }
            if (string.IsNullOrEmpty(hostName))
            {
                MessageBox.Show("Invalid hostname!");
                return null;
            }
            return hostName;
        }
    }
}
