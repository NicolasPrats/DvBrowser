using System.Linq;
using Dataverse.Browser.Configuration;
using Dataverse.Browser.Context;

namespace Dataverse.Browser.Services
{
    /// <summary>
    /// Implementation of environment service.
    /// Handles environment configuration and connection management.
    /// </summary>
    public class EnvironmentService : IEnvironmentService
    {
        /// <inheritdoc/>
        public EnvironnementConfiguration[] LoadEnvironments()
        {
            var config = ConfigurationManager.LoadConfiguration();
            return config?.Environnements?.ToArray() ?? new EnvironnementConfiguration[0];
        }

        /// <inheritdoc/>
        public void SaveEnvironment(EnvironnementConfiguration environment)
        {
            var config = ConfigurationManager.LoadConfiguration() ?? new DataverseBrowserConfiguration();
            
            var envList = config.Environnements?.ToList() ?? new System.Collections.Generic.List<EnvironnementConfiguration>();

            // Update existing or add new
            var existing = envList.FirstOrDefault(e => e.Name == environment.Name);
            if (existing != null)
            {
                var index = envList.IndexOf(existing);
                envList[index] = environment;
            }
            else
            {
                envList.Add(environment);
            }

            config.Environnements = envList.ToArray();
            ConfigurationManager.SaveConfiguration(config);
        }

        /// <inheritdoc/>
        public void DeleteEnvironment(EnvironnementConfiguration environment)
        {
            var config = ConfigurationManager.LoadConfiguration();
            if (config?.Environnements != null)
            {
                var envList = config.Environnements.ToList();
                envList.Remove(environment);
                config.Environnements = envList.ToArray();
                ConfigurationManager.SaveConfiguration(config);
            }
        }

        /// <inheritdoc/>
        public BrowserContext Connect(EnvironnementConfiguration environment)
        {
            var factory = new ContextFactory(environment);
            factory.CreateContext();
            return factory.GetContext();
        }

        /// <inheritdoc/>
        public bool TestConnection(EnvironnementConfiguration environment)
        {
            try
            {
                var factory = new ContextFactory(environment);
                factory.CreateContext();
                var context = factory.GetContext();
                
                if (context != null)
                {
                    // Test by retrieving organization info
                    var orgName = context.CrmServiceClient?.ConnectedOrgFriendlyName;
                    return !string.IsNullOrEmpty(orgName);
                }
                return false;
            }
            catch
            {
                return false;
            }
        }
    }
}
