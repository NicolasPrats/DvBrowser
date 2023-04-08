using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;
using Dataverse.Browser.Configuration;

namespace Dataverse.Browser.UI
{
    internal partial class EnvironmentEditor : Form
    {
        private EnvironnementConfiguration CurrentEnvironment { get; }

        public EnvironnementConfiguration SelectedEnvironment {get; private set;}

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
            }
        }

        private void BtnOk_Click(object sender, EventArgs e)
        {
            this.SelectedEnvironment = CurrentEnvironment ?? new EnvironnementConfiguration();
            this.SelectedEnvironment.Name = txtName.Text;
            this.SelectedEnvironment.PluginAssemblies = new string[] { txtAssemblyPath.Text };
            this.SelectedEnvironment.DataverseHost = txtHostName.Text;
            this.Close();
        }
    }
}
