using System;
using System.Windows.Forms;
using Dataverse.Browser.Configuration;

namespace Dataverse.Browser.UI
{
    internal partial class EnvironmentPicker : Form
    {

        public EnvironnementConfiguration SelectedEnvironment { get; private set; }

        public EnvironmentPicker(DataverseBrowserConfiguration configuration)
        {
            InitializeComponent();
            this.Configuration = configuration;

            this.listView1.Activation = ItemActivation.Standard;
            this.listView1.ItemActivate += ListView1_ItemActivate;
        }

        public DataverseBrowserConfiguration Configuration { get; }

        private void EnvironmentPicker_Load(object sender, EventArgs e)
        {
            this.listView1.Items.Add("New ...");
            foreach (var env in this.Configuration.Environnements)
            {
                ListViewItem item = new ListViewItem
                {
                    Tag = env,
                    Text = env.Name
                };
                this.listView1.Items.Add(item);
            }
            if (this.Configuration.Environnements.Length == 0)
                Pick(null);
        }

        private void ListView1_ItemActivate(object sender, EventArgs e)
        {
            Pick(this.listView1.SelectedItems[0].Tag as EnvironnementConfiguration);
        }

        private void Pick(EnvironnementConfiguration environment)
        {
            using (EnvironmentEditor editor = new EnvironmentEditor(environment))
            {
                editor.ShowDialog();
                this.SelectedEnvironment = editor.SelectedEnvironment;
            }
            if (this.SelectedEnvironment != null)
            {
                if (environment == null)
                {
                    var environments = Configuration.Environnements;
                    Array.Resize(ref environments, environments.Length + 1);
                    environments[environments.Length - 1] = this.SelectedEnvironment;
                    Configuration.Environnements = environments;
                }
                ConfigurationManager.SaveConfiguration(Configuration);
                this.Close();
            }
        }
    }
}
