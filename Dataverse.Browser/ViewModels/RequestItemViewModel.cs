using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Dataverse.Browser.ViewModels
{
    /// <summary>
    /// ViewModel representing a single request in the history.
    /// </summary>
    public partial class RequestItemViewModel : ObservableObject
    {
        private string _method;
        private string _url;
        private int _statusCode;
        private TimeSpan _duration;
        private DateTime _timestamp;
        private bool _isSelected;

        /// <summary>
        /// HTTP method (GET, POST, PATCH, DELETE, etc.)
        /// </summary>
        public string Method
        {
            get => _method;
            set => SetProperty(ref _method, value);
        }

        /// <summary>
        /// Request URL
        /// </summary>
        public string Url
        {
            get => _url;
            set => SetProperty(ref _url, value);
        }

        /// <summary>
        /// HTTP status code
        /// </summary>
        public int StatusCode
        {
            get => _statusCode;
            set
            {
                if (SetProperty(ref _statusCode, value))
                {
                    OnPropertyChanged(nameof(StatusCodeColor));
                }
            }
        }

        /// <summary>
        /// Request duration
        /// </summary>
        public TimeSpan Duration
        {
            get => _duration;
            set => SetProperty(ref _duration, value);
        }

        /// <summary>
        /// When the request was made
        /// </summary>
        public DateTime Timestamp
        {
            get => _timestamp;
            set => SetProperty(ref _timestamp, value);
        }

        /// <summary>
        /// Whether this request is currently selected
        /// </summary>
        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }

        /// <summary>
        /// Color to display based on status code
        /// </summary>
        public System.Drawing.Color StatusCodeColor
        {
            get
            {
                if (StatusCode >= 200 && StatusCode < 300)
                    return System.Drawing.Color.Green;
                if (StatusCode >= 300 && StatusCode < 400)
                    return System.Drawing.Color.Orange;
                if (StatusCode >= 400)
                    return System.Drawing.Color.Red;
                return System.Drawing.Color.Gray;
            }
        }
    }
}
