using System;
using System.Net.Http;
using System.Xml;
using Dataverse.Plugin.Emulator.Steps;
using Dataverse.Utils;
using Dataverse.WebApi2IOrganizationService.Converters;
using Dataverse.WebApi2IOrganizationService.Converterss;
using Microsoft.OData.Edm.Csdl;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Tooling.Connector;

namespace Dataverse.BrowserLibs.Tests
{
    [TestClass]
    public class UnitTest1
    {

        public TestContext TestContext {get; set; }

        [TestMethod]
        public void TestMethod1()
        {
            string login = (string) TestContext.Properties["login"];
            string password = (string)TestContext.Properties["password"];
            string host = (string)TestContext.Properties["hostname"];
            string connectionString = $"AuthType=OAuth;Url=https://{host};AppId=51f81489-12ee-4a9e-aaae-a2591f45987d;RedirectUri=app://58145B91-0C36-4500-8554-080854F2AC97;UserName={login};Password='{password.Replace("'", "''")}';RequireNewInstance=true;";

            var client = new CrmServiceClient(connectionString);
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
            MetadataCache metadataCache = new MetadataCache(client);
            var context = new DataverseContext()
            {
                CrmServiceClient = client,
                Host = host,
                HttpClient = new System.Net.Http.HttpClient(),
                MetadataCache = metadataCache
            };
            HttpRequestMessage downloadCsdlMessage = new HttpRequestMessage(HttpMethod.Get, $"{context.WebApiBaseUrl}$metadata");
            downloadCsdlMessage.Headers.Add("Authorization", "Bearer " + context.CrmServiceClient.CurrentAccessToken);

            var result = context.HttpClient.SendAsync(downloadCsdlMessage).Result;
            using (var stream = result.Content.ReadAsStreamAsync().Result)
            {
                context.Model = CsdlReader.Parse(XmlReader.Create(stream));
            }

            var requestConverter = new RequestConverter(context);
            var responseConverter = new ResponseConverter(context);
        }
    }
}
