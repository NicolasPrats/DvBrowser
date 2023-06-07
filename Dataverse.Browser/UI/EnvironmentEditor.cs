using System;
using System.IO;
using System.Linq;
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
            InitializeComponent();
            this.CurrentEnvironment = environment;
            this.openFileDialog.Filter = "Plugins Files (*.dll)|*.dll|All files (*.*)|*.*";
            var defaultPath = this.CurrentEnvironment?.PluginAssemblies?.FirstOrDefault();
            if (defaultPath != null)
            {
                this.openFileDialog.InitialDirectory = Path.GetDirectoryName(defaultPath);
                this.openFileDialog.FileName = Path.GetFileName(defaultPath);
            }
            else
            {
                this.openFileDialog.InitialDirectory = Environment.CurrentDirectory;
                this.openFileDialog.FileName = "*.dll";
            }
        }

        private void EnvironmentEditor_Load(object sender, EventArgs e)
        {
            if (this.CurrentEnvironment != null)
            {
                this.txtName.Text = this.CurrentEnvironment.Name;
                this.txtAssemblyPath.Text = this.CurrentEnvironment.PluginAssemblies[0];
                this.txtHostName.Text = this.CurrentEnvironment.DataverseHost;
                this.cbDisableAsyncSteps.Checked = this.CurrentEnvironment.DisableAsyncSteps;
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
            this.SelectedEnvironment = this.CurrentEnvironment ?? new EnvironnementConfiguration();
            this.SelectedEnvironment.Name = this.txtName.Text;
            this.SelectedEnvironment.DisableAsyncSteps = this.cbDisableAsyncSteps.Checked;


            this.SelectedEnvironment.PluginAssemblies = new string[] { assemblyPath };

            this.SelectedEnvironment.DataverseHost = hostName;
            if (this.SelectedEnvironment.Id == Guid.Empty)//Environments created in first versions don't have an ID.
            {
                this.SelectedEnvironment.Id = Guid.NewGuid();
            }
            Close();
        }

        private string GetValidAssemblyPath()
        {
            string assemblyPath = this.txtAssemblyPath.Text;
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
            string hostName = this.txtHostName.Text;
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

        private void BtnSelectAssembly_Click(object sender, EventArgs e)
        {
            if (this.openFileDialog.ShowDialog() == DialogResult.OK)
            {
                this.txtAssemblyPath.Text = this.openFileDialog.FileName;
            }
        }
    }
}
