using System;
using System.Text.RegularExpressions;
using System.Windows.Forms;
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
            if (AreEntriesValid())
            {
                this.SelectedEnvironment = CurrentEnvironment ?? new EnvironnementConfiguration();
                this.SelectedEnvironment.Name = txtName.Text;
                this.SelectedEnvironment.PluginAssemblies = new string[] { txtAssemblyPath.Text };
                this.SelectedEnvironment.DataverseHost = txtHostName.Text;
                this.Close();
            }
            else
            {
                DisplayEntriesErrorMessage();
            }
        }
        /// <summary>
        /// Method used to display message box when entries are not correct.
        /// </summary>
        private void DisplayEntriesErrorMessage()
        {
            string message = "It seems that at least one of the entries does not fit the expected format. \nDo you want to try again ?";
            string caption = "Error Detected in Input";
            var buttons = MessageBoxButtons.YesNo;

            var resultDialog = MessageBox.Show(message, caption, buttons);
            if (resultDialog == DialogResult.No)
                this.Close();
        }
        /// <summary>
        /// Method to check if all entries are correct.
        /// </summary>
        /// <returns></returns>
        private bool AreEntriesValid()
        {
            return IsHostNameValid();
        }
        /// <summary>
        /// Method to know if the HostName filled in is a correct one.
        /// </summary>
        /// <returns></returns>
        private bool IsHostNameValid()
        {
            //Remove http,https and www.
            var result = Regex.Replace(txtHostName.Text, @"http(s)?(:)?(\/\/)?|(\/\/)?(www\.)", "");
            //Ensure hostname match with a correct Dataverse instance.
            var rx = new Regex(@"([A-Za-z]{1,})(.)((([A-Za-z]{3})([0-9]{1}))|([A-Za-z]{3}))(.)(dynamics)(.)(com)",
                RegexOptions.Compiled | RegexOptions.IgnoreCase);
            var matches = rx.Matches(result);
            if (matches.Count != 0)
            {
                txtHostName.Text = result;
                return true;
            }
            else
            {
                return false;
            }
        }

    }
}
