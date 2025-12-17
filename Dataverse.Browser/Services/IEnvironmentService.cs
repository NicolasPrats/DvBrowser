using Dataverse.Browser.Configuration;
using Dataverse.Browser.Context;

namespace Dataverse.Browser.Services
{
    /// <summary>
    /// Service interface for environment management.
    /// Abstracts environment configuration and connection logic.
    /// </summary>
    public interface IEnvironmentService
    {
        /// <summary>
        /// Loads all configured environments
        /// </summary>
        /// <returns>Array of environment configurations</returns>
        EnvironnementConfiguration[] LoadEnvironments();

        /// <summary>
        /// Saves an environment configuration
        /// </summary>
        /// <param name="environment">Environment to save</param>
        void SaveEnvironment(EnvironnementConfiguration environment);

        /// <summary>
        /// Deletes an environment configuration
        /// </summary>
        /// <param name="environment">Environment to delete</param>
        void DeleteEnvironment(EnvironnementConfiguration environment);

        /// <summary>
        /// Connects to the specified environment
        /// </summary>
        /// <param name="environment">Environment to connect to</param>
        /// <returns>Browser context for the connected environment</returns>
        BrowserContext Connect(EnvironnementConfiguration environment);

        /// <summary>
        /// Tests connection to an environment
        /// </summary>
        /// <param name="environment">Environment to test</param>
        /// <returns>True if connection successful, false otherwise</returns>
        bool TestConnection(EnvironnementConfiguration environment);
    }
}
