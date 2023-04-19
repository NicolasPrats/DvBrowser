﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
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
            string hostName = GetValidHostName();
            if (hostName == null)
                return;
            string assemblyPath = GetValidAssemblyPath();
            this.SelectedEnvironment = CurrentEnvironment ?? new EnvironnementConfiguration();
            this.SelectedEnvironment.Name = txtName.Text;


            this.SelectedEnvironment.PluginAssemblies = new string[] { assemblyPath };

            this.SelectedEnvironment.DataverseHost = hostName;
            this.Close();
        }

        private string GetValidAssemblyPath()
        {
            string assemblyPath = txtAssemblyPath.Text;
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
