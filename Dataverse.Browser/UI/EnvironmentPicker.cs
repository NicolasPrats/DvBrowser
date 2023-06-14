using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using AutoUpdaterDotNET;
using Dataverse.Browser.Configuration;

namespace Dataverse.Browser.UI
{
    internal partial class EnvironmentPicker : Form
    {

        public EnvironnementConfiguration SelectedEnvironment { get; private set; }
        private List<Bitmap> LoadedBitmaps { get; } = new List<Bitmap>();

        public EnvironmentPicker(DataverseBrowserConfiguration configuration)
        {
            if (!Debugger.IsAttached)
            {
                string jsonPath = Path.Combine(ConfigurationManager.GetApplicationDataPath(), "autoupdater.json");
                AutoUpdater.PersistenceProvider = new JsonFilePersistenceProvider(jsonPath);
                AutoUpdater.ClearAppDirectory = true;
                AutoUpdater.Start("https://nicolasprats.github.io/pages/dvbrowser/autoupdate.xml");
            }

            InitializeComponent();
            this.Configuration = configuration;

            this.listView1.ItemActivate += ListView1_ItemActivate;
            FormClosing += EnvironmentPicker_FormClosing;
        }

        private void EnvironmentPicker_FormClosing(object sender, FormClosingEventArgs e)
        {
            foreach (var bitmap in this.LoadedBitmaps)
            {
                bitmap.Dispose();
            }
        }

        public DataverseBrowserConfiguration Configuration { get; }

        private Bitmap GetLogoBitmap(EnvironnementConfiguration environment)
        {

            if (environment.LogoPath == null)
                return null;
            try
            {
                var bitmap = new Bitmap(environment.LogoPath);
                this.LoadedBitmaps.Add(bitmap);
                return bitmap;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private void EnvironmentPicker_Load(object sender, EventArgs e)
        {

            this.listView1.Items.Add(new ListViewItem
            {
                Text = "New ...",
                ImageIndex = 0
            });
            foreach (var env in this.Configuration.Environnements)
            {
                ListViewItem item = new ListViewItem
                {
                    Tag = env,
                    Text = env.Name,
                    ImageIndex = 2
                };
                var bitmap = GetLogoBitmap(env);
                if (GetLogoBitmap(env) == null)
                {
                    item.ImageIndex = 1;
                }
                else
                {
                    item.ImageIndex = this.imageListFixed.Images.Count;
                    this.imageListFixed.Images.Add(bitmap);
                }
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
                    var environments = this.Configuration.Environnements;
                    Array.Resize(ref environments, environments.Length + 1);
                    environments[environments.Length - 1] = this.SelectedEnvironment;
                    this.Configuration.Environnements = environments;
                }
                ConfigurationManager.SaveConfiguration(this.Configuration);
                Close();
            }
        }
    }
}
