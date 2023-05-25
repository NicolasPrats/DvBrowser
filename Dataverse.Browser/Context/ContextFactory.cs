using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Windows;
using System.Xml;
using Dataverse.Browser.Configuration;
using Dataverse.Plugin.Emulator.Steps;
using Dataverse.Utils;
using Microsoft.OData.Edm.Csdl;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Tooling.Connector;

namespace Dataverse.Browser.Context
{
    internal class ContextFactory
    {
        //TODO allow ui to stop the process
        public event EventHandler<string> OnNewProgress;
        public event EventHandler<Exception> OnError;
        public event EventHandler OnFinished;
        private BrowserContext Context { get; set; }

        public ContextFactory(EnvironnementConfiguration selectedEnvironment)
        {
            this.SelectedEnvironment = selectedEnvironment ?? throw new ArgumentNullException(nameof(selectedEnvironment));
        }

        public EnvironnementConfiguration SelectedEnvironment { get; }

        private void NotifyProgress(string progress)
        {
            OnNewProgress?.Invoke(this, progress);
        }

        public BrowserContext GetContext()
        {
            return this.Context;
        }

        public void CreateContext()
        {
            try
            {
                NotifyProgress("Initializing traces");
                TraceControlSettings.TraceLevel = SourceLevels.All;
                TraceControlSettings.AddTraceListener(new TextWriterTraceListener(Path.Combine(this.SelectedEnvironment.GetWorkingDirectory(), "log.txt")));
                OnNewProgress += ContextFactory_OnNewProgress;
                OnError += ContextFactory_OnError;


                string connectionString = GetConnectionString(false);
                NotifyProgress("Connecting to crm: " + connectionString);
                var client = new CrmServiceClient(connectionString);
                if (!client.IsReady)
                {
                    //Le auto ne semble par marcher tout le temps
                    connectionString = GetConnectionString(true);
                    NotifyProgress("Connecting to crm: " + connectionString);
                    client = new CrmServiceClient(connectionString);
                }

                if (client.LastCrmException != null)
                {
                    NotifyProgress("Unable to establish connection");
                    OnError?.Invoke(this, client.LastCrmException);
                    return;

                }

                if (!client.IsReady)
                {
                    NotifyProgress("Unable to establish connection");
                    OnError?.Invoke(this, new ApplicationException("Unknown error"));
                    return;
                }

                var emulator = InitializePluginEmulator(this.SelectedEnvironment, connectionString);

                NotifyProgress("Initializing metadata cache...");
                MetadataCache metadataCache = new MetadataCache(client);
                NotifyProgress("Creating context...");
                BrowserContext context = new BrowserContext
                {
                    Host = this.SelectedEnvironment.DataverseHost,
                    CachePath = this.SelectedEnvironment.GetWorkingDirectory(),
                    CrmServiceClient = client,
                    HttpClient = new HttpClient(),
                    MetadataCache = metadataCache,
                    PluginsEmulator = emulator,
                    ProxyWithEmulator = emulator.CreateNewProxy()
                };

                NotifyProgress("Downloading CSDL...");
                HttpRequestMessage downloadCsdlMessage = new HttpRequestMessage(HttpMethod.Get, $"{context.WebApiBaseUrl}$metadata");
                downloadCsdlMessage.Headers.Add("Authorization", "Bearer " + context.CrmServiceClient.CurrentAccessToken);

                var result = context.HttpClient.SendAsync(downloadCsdlMessage).Result;
                using (var stream = result.Content.ReadAsStreamAsync().Result)
                {
                    NotifyProgress("Parsing CSDL...");
                    context.Model = CsdlReader.Parse(XmlReader.Create(stream));
                }
                this.Context = context;
                OnFinished?.Invoke(this, EventArgs.Empty);
                return;
            }
            catch (Exception ex)
            {
                OnError?.Invoke(this, ex);
            }
        }

        private void ContextFactory_OnError(object sender, Exception e)
        {
            Trace.Write(e.ToString());
        }

        private void ContextFactory_OnNewProgress(object sender, string status)
        {
            Trace.Write(status);
        }



        private string GetConnectionString(bool forceLogin)
        {
            string hostname = this.SelectedEnvironment.DataverseHost;
            string loginPrompt = "Auto";
            string tokenPath = Path.Combine(this.SelectedEnvironment.GetWorkingDirectory(), "token.dat");
            if (forceLogin)
            {
                loginPrompt = "Always";
                if (File.Exists(tokenPath))
                {
                    File.Delete(tokenPath);
                }
            }
            return $"AuthType=OAuth;Url=https://{hostname};AppId=51f81489-12ee-4a9e-aaae-a2591f45987d;RedirectUri=app://58145B91-0C36-4500-8554-080854F2AC97;TokenCacheStorePath={tokenPath};LoginPrompt={loginPrompt};RequireNewInstance=true;";
        }

        private PluginEmulator InitializePluginEmulator(EnvironnementConfiguration environnementConfiguration, string connectionString)
        {
            NotifyProgress("Instantiating PluginEmulator");
            var emulator = new PluginEmulator((callerId) =>
            {
                var svc = new CrmServiceClient(connectionString)
                {
                    CallerId = callerId
                };
                if (svc.LastCrmException != null)
                {
                    throw new ApplicationException("Unable to connect", svc.LastCrmException);
                }
                svc.BypassPluginExecution = true;
                return (IOrganizationService)svc.OrganizationWebProxyClient ?? svc.OrganizationServiceProxy;
            }
            );

            foreach (var pluginPath in environnementConfiguration.PluginAssemblies)
            {
                NotifyProgress("Loading plugins: " + pluginPath);
                bool allStepsLoaded = emulator.AddPluginAssembly(pluginPath);
                if (!allStepsLoaded)
                {
                    //TODO: let the caller display the message
                    //TODO: give more details about steps and features
                    var result = MessageBox.Show("Some steps could not be loaded because they use some not implemented features like impersonation or configuration.\nIf you continue these steps will be ignored and not executed.\nDo you want to continue?", "Steps using not implemented features", MessageBoxButton.YesNo);
                    if (result == MessageBoxResult.No)
                    {
                        throw new NotImplementedException("Some steps could not be loaded!");
                    }
                }
            }
            if (environnementConfiguration.DisableAsyncSteps)
            {
                NotifyProgress("Disabling async steps");
                emulator.DisableAyncSteps();
            }
            return emulator;
        }
    }
}
