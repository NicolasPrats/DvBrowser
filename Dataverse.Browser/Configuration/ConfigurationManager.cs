using System;
using System.IO;
using System.Text.Json;

namespace Dataverse.Browser.Configuration
{
    internal static class ConfigurationManager
    {

        public static string GetApplicationDataPath()
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Dataverse.Browser");
        }

        public static DataverseBrowserConfiguration LoadConfiguration()
        {
            string configPath = GetConfigPath();
            if (!File.Exists(configPath))
            {
                return new DataverseBrowserConfiguration()
                {
                    Environnements = new EnvironnementConfiguration[0]
                };
            }
            return JsonSerializer.Deserialize<DataverseBrowserConfiguration>(File.ReadAllText(configPath));
        }

        public static void SaveConfiguration(DataverseBrowserConfiguration config)
        {
            File.WriteAllText(GetConfigPath(), JsonSerializer.Serialize(config));
        }

        private static string GetConfigPath()
        {
            return Path.Combine(GetApplicationDataPath(), "config.json");
        }
    }
}
