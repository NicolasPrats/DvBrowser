using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Dataverse.Browser.Properties;
using Dataverse.Plugin.Emulator.Steps;
using Microsoft.OData.Edm.Csdl;
using Microsoft.Xrm.Tooling.Connector;
using System.Xml;
using Microsoft.Xrm.Sdk;
using Dataverse.Browser.Configuration;
using System.Diagnostics;

namespace Dataverse.Browser.Context
{
    internal class ContextFactory
    {

        public ContextFactory(EnvironnementConfiguration selectedEnvironment)
        {
            this.SelectedEnvironment = selectedEnvironment ?? throw new ArgumentNullException(nameof(selectedEnvironment));
        }

        public EnvironnementConfiguration SelectedEnvironment { get; }

        public DataverseContext CreateContext()
        {

            TraceControlSettings.TraceLevel = SourceLevels.All;
            TraceControlSettings.AddTraceListener(new TextWriterTraceListener(Path.Combine(this.GetCachePath(), "log.txt")));


            string connectionString = this.GetConnectionString(false);
            var client = new CrmServiceClient(connectionString);
            if (!client.IsReady)
            {
                //Le auto ne semble par marcher tout le temps
                var tmpConnectionString = this.GetConnectionString( true);
                client = new CrmServiceClient(tmpConnectionString);
            }

            if (client.LastCrmException != null)
            {
                throw new ApplicationException("Not connected to dataverse", client.LastCrmException);
            }

            var emulator = this.InitializePluginEmulator(this.SelectedEnvironment, connectionString);
            DataverseContext context = new DataverseContext
            {
                Host = this.SelectedEnvironment.DataverseHost,
                ConnectionString = connectionString,
                CachePath = this.GetCachePath(),
                CrmServiceClient = client,
                HttpClient = new System.Net.Http.HttpClient(),
                MetadataCache = new MetadataCache(client),
                PluginsEmulator = emulator,
                ProxyForWeb = emulator.CreateNewProxy()
            };

            HttpRequestMessage downloadCsdlMessage = new HttpRequestMessage(HttpMethod.Get, $"{context.WebApiBaseUrl}$metadata");
            downloadCsdlMessage.Headers.Add("Authorization", "Bearer " + context.CrmServiceClient.CurrentAccessToken);

            var result = context.HttpClient.SendAsync(downloadCsdlMessage).Result;
            using (var stream = result.Content.ReadAsStreamAsync().Result)
            {
                context.Model = CsdlReader.Parse(XmlReader.Create(stream));
            }
            return context;
        }

        public string GetCachePath()
        {
            string hostname = this.SelectedEnvironment.DataverseHost;
            StringBuilder directoryNameBuilder = new StringBuilder();
            directoryNameBuilder.Append(this.SelectedEnvironment.Id).Append("-");
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
            string cachePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Dataverse.Browser", directoryNameBuilder.ToString());
            if (!Directory.Exists(cachePath))
                Directory.CreateDirectory(cachePath);
            return cachePath;
        }

        private string GetConnectionString(bool forceLogin)
        {
            string hostname = this.SelectedEnvironment.DataverseHost;
            string loginPrompt = "Auto";
            string tokenPath = Path.Combine( "token.dat");
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
                emulator.AddPluginAssembly(pluginPath);
            }
            if (environnementConfiguration.StepBehavior == StepBehavior.DisableAsyncSteps)
            {
                emulator.DisableAyncSteps();
            }
            return emulator;
        }
    }
}
