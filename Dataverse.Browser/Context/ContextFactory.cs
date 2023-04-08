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

namespace Dataverse.Browser.Context
{
    internal static class ContextFactory
    {
        public static DataverseContext CreateContext(Configuration.EnvironnementConfiguration selectedEnvironment)
        {
            string connectionString = ContextFactory.GetConnectionString(selectedEnvironment.DataverseHost);
            var client = new CrmServiceClient(connectionString);
            if (!client.IsReady)
            {
                //Le auto ne semble par marcher tout le temps
                var tmpConnectionString = ContextFactory.GetConnectionString(selectedEnvironment.DataverseHost, true);
                client = new CrmServiceClient(tmpConnectionString);
            }

            if (client.LastCrmException != null)
            {
                throw new ApplicationException("Not connected to dataverse", client.LastCrmException);
            }

            var emulator = ContextFactory.InitializePluginEmulator(selectedEnvironment, connectionString);
            DataverseContext context = new DataverseContext
            {
                Host = selectedEnvironment.DataverseHost,
                ConnectionString = connectionString,
                CachePath = ContextFactory.GetCachePath(selectedEnvironment.DataverseHost),
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

        public static string GetCachePath(string hostname)
        {
            string cachePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Dataverse.Browser", hostname);
            if (!Directory.Exists(cachePath))
                Directory.CreateDirectory(cachePath);
            return cachePath;
        }
        private static string GetConnectionString(string host, bool forceLogin = false)
        {
            string loginPrompt = "Auto";
            string tokenPath = Path.Combine(ContextFactory.GetCachePath(host), "token.dat");
            if (forceLogin)
            {
                loginPrompt = "Always";
                if (File.Exists(tokenPath))
                {
                    File.Delete(tokenPath);
                }
            }
            return $"AuthType=OAuth;Url=https://{host};AppId=51f81489-12ee-4a9e-aaae-a2591f45987d;RedirectUri=app://58145B91-0C36-4500-8554-080854F2AC97;TokenCacheStorePath={tokenPath};LoginPrompt={loginPrompt};RequireNewInstance=true;";
        }
        private static PluginEmulator InitializePluginEmulator(EnvironnementConfiguration environnementConfiguration, string connectionString)
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
