using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dataverse.Browser.Configuration;

namespace Dataverse.Browser.ViewModels
{
    /// <summary>
    /// ViewModel for environment management.
    /// </summary>
    public partial class EnvironmentViewModel : ObservableObject
    {
        private EnvironnementConfiguration _selectedEnvironment;
        private bool _isConnecting;

        public EnvironmentViewModel()
        {
            Environments = new ObservableCollection<EnvironnementConfiguration>();
            
            // Initialize commands
            ConnectCommand = new RelayCommand<EnvironnementConfiguration>(
                Connect, 
                env => env != null && !IsConnecting);
            AddEnvironmentCommand = new RelayCommand(AddEnvironment);
            EditEnvironmentCommand = new RelayCommand<EnvironnementConfiguration>(
                EditEnvironment, 
                env => env != null);
            DeleteEnvironmentCommand = new RelayCommand<EnvironnementConfiguration>(
                DeleteEnvironment, 
                env => env != null);
        }

        /// <summary>
        /// Available environments
        /// </summary>
        public ObservableCollection<EnvironnementConfiguration> Environments { get; }

        /// <summary>
        /// Currently selected environment
        /// </summary>
        public EnvironnementConfiguration SelectedEnvironment
        {
            get => _selectedEnvironment;
            set => SetProperty(ref _selectedEnvironment, value);
        }

        /// <summary>
        /// Whether a connection operation is in progress
        /// </summary>
        public bool IsConnecting
        {
            get => _isConnecting;
            set
            {
                if (SetProperty(ref _isConnecting, value))
                {
                    ((RelayCommand<EnvironnementConfiguration>)ConnectCommand).NotifyCanExecuteChanged();
                }
            }
        }

        /// <summary>
        /// Command to connect to an environment
        /// </summary>
        public ICommand ConnectCommand { get; }

        /// <summary>
        /// Command to add a new environment
        /// </summary>
        public ICommand AddEnvironmentCommand { get; }

        /// <summary>
        /// Command to edit an environment
        /// </summary>
        public ICommand EditEnvironmentCommand { get; }

        /// <summary>
        /// Command to delete an environment
        /// </summary>
        public ICommand DeleteEnvironmentCommand { get; }

        /// <summary>
        /// Loads environments from configuration
        /// </summary>
        public void LoadEnvironments()
        {
            Environments.Clear();
            var config = ConfigurationManager.LoadConfiguration();
            
            if (config?.Environnements != null)
            {
                foreach (var env in config.Environnements)
                {
                    Environments.Add(env);
                }
            }

            // Select first environment by default
            if (Environments.Any())
            {
                SelectedEnvironment = Environments.First();
            }
        }

        private void Connect(EnvironnementConfiguration environment)
        {
            // This will be handled by the view
            // Raising an event or using a message broker would be better for WPF migration
        }

        private void AddEnvironment()
        {
            // This will be handled by the view
        }

        private void EditEnvironment(EnvironnementConfiguration environment)
        {
            // This will be handled by the view
        }

        private void DeleteEnvironment(EnvironnementConfiguration environment)
        {
            Environments.Remove(environment);
            
            // Save to configuration
            var config = ConfigurationManager.LoadConfiguration();
            if (config?.Environnements != null)
            {
                var envList = config.Environnements.ToList();
                envList.Remove(environment);
                config.Environnements = envList.ToArray();
                ConfigurationManager.SaveConfiguration(config);
            }

            // Select another environment if available
            if (!Environments.Any())
            {
                SelectedEnvironment = null;
            }
            else if (SelectedEnvironment == environment)
            {
                SelectedEnvironment = Environments.First();
            }
        }
    }
}
