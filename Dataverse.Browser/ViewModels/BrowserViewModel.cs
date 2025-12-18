using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dataverse.Browser.Configuration;
using Dataverse.Browser.Context;

namespace Dataverse.Browser.ViewModels
{
    /// <summary>
    /// ViewModel for the main browser window.
    /// This class will be reusable when migrating to WPF.
    /// </summary>
    public partial class BrowserViewModel : ObservableObject
    {
        private BrowserContext _context;
        private EnvironnementConfiguration _currentEnvironment;
        private bool _pluginsEnabled;
        private string _statusMessage;

        public BrowserViewModel()
        {
            RequestHistory = new ObservableCollection<RequestItemViewModel>();
            
            // Initialize commands
            ClearHistoryCommand = new RelayCommand(ClearHistory, () => RequestHistory.Count > 0);
            TogglePluginsCommand = new RelayCommand(TogglePlugins);
            AttachDebuggerCommand = new RelayCommand(AttachDebugger);
        }

        /// <summary>
        /// Current browser context with connections and services
        /// </summary>
        public BrowserContext Context
        {
            get => _context;
            set
            {
                if (SetProperty(ref _context, value))
                {
                    OnPropertyChanged(nameof(EnvironmentName));
                    OnPropertyChanged(nameof(Host));
                }
            }
        }

        /// <summary>
        /// Current environment configuration
        /// </summary>
        public EnvironnementConfiguration CurrentEnvironment
        {
            get => _currentEnvironment;
            set
            {
                if (SetProperty(ref _currentEnvironment, value))
                {
                    OnPropertyChanged(nameof(EnvironmentName));
                }
            }
        }

        /// <summary>
        /// Whether plugins are currently enabled
        /// </summary>
        public bool PluginsEnabled
        {
            get => _pluginsEnabled;
            set => SetProperty(ref _pluginsEnabled, value);
        }

        /// <summary>
        /// Current status message to display
        /// </summary>
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        /// <summary>
        /// Display name for current environment
        /// </summary>
        public string EnvironmentName => CurrentEnvironment?.Name ?? "No Environment";

        /// <summary>
        /// Current Dataverse host
        /// </summary>
        public string Host => Context?.Host ?? string.Empty;

        /// <summary>
        /// Collection of request history items
        /// </summary>
        public ObservableCollection<RequestItemViewModel> RequestHistory { get; }

        /// <summary>
        /// Command to clear request history
        /// </summary>
        public ICommand ClearHistoryCommand { get; }

        /// <summary>
        /// Command to toggle plugin execution
        /// </summary>
        public ICommand TogglePluginsCommand { get; }

        /// <summary>
        /// Command to attach debugger
        /// </summary>
        public ICommand AttachDebuggerCommand { get; }

        /// <summary>
        /// Adds a new request to the history
        /// </summary>
        public void AddRequest(string method, string url, int statusCode, TimeSpan duration)
        {
            var request = new RequestItemViewModel
            {
                Method = method,
                Url = url,
                StatusCode = statusCode,
                Duration = duration,
                Timestamp = DateTime.Now
            };
            
            RequestHistory.Insert(0, request);
            
            // Limit history to 100 items
            while (RequestHistory.Count > 100)
            {
                RequestHistory.RemoveAt(RequestHistory.Count - 1);
            }
            
            ((RelayCommand)ClearHistoryCommand).NotifyCanExecuteChanged();
        }

        private void ClearHistory()
        {
            RequestHistory.Clear();
            ((RelayCommand)ClearHistoryCommand).NotifyCanExecuteChanged();
        }

        public void ClearHistoryItems()
        {
            ClearHistory();
        }

        private void TogglePlugins()
        {
            PluginsEnabled = !PluginsEnabled;
            StatusMessage = PluginsEnabled ? "Plugins enabled" : "Plugins disabled";
        }

        private void AttachDebugger()
        {
            System.Diagnostics.Debugger.Launch();
        }
    }
}
