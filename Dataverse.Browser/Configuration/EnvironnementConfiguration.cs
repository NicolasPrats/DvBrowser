using System;
using System.IO;
using System.Linq;
using System.Text;

namespace Dataverse.Browser.Configuration
{
    internal class EnvironnementConfiguration
    {
        public string Name { get; set; }
        public string DataverseHost { get; set; }
        public string[] PluginAssemblies { get; set; }
        public StepBehavior StepBehavior { get; set; }
        public Guid Id { get; set; }

        public string GetWorkingDirectory()
        {
            string hostname = this.DataverseHost;
            StringBuilder directoryNameBuilder = new StringBuilder();
            directoryNameBuilder.Append(this.Id).Append("-");
            var invalidChars = Path.GetInvalidFileNameChars(); ;
            foreach (var c in hostname)
            {
                if (!invalidChars.Contains(c))
                {
                    directoryNameBuilder.Append(c);
                }
                else
                {
                    directoryNameBuilder.Append(Convert.ToByte(c).ToString("x2"));
                }
            }
            string workingDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Dataverse.Browser", directoryNameBuilder.ToString());
            if (!Directory.Exists(workingDirectory))
                Directory.CreateDirectory(workingDirectory);
            return workingDirectory;
        }
    }
}
