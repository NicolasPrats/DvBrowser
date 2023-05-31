using System;
using System.Collections.Specialized;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Xml;
using Dataverse.Plugin.Emulator.Services;
using Dataverse.Plugin.Emulator.Steps;
using Dataverse.Utils;
using Dataverse.WebApi2IOrganizationService.Converters;
using Dataverse.WebApi2IOrganizationService.Model;
using Microsoft.OData.Edm.Csdl;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Tooling.Connector;

namespace Dataverse.BrowserLibs.Tests
{
    internal static class Helper
    {
        internal class Converters
        {
            public DataverseContext DataverseContext { get; set; }
            public RequestConverter RequestConverter { get; set; }
            public ResponseConverter ResponseConverter { get; set; }
            public OrganizationServiceWithEmulatedPlugins ProxyWithEmulator { get; internal set; }
        }

        private static Converters Instance;
        public static Converters GetConverters(TestContext testContext)
        {
            if (Instance != null)
                return Instance;
            string login = (string)testContext.Properties["login"];
            string password = (string)testContext.Properties["password"];
            string host = (string)testContext.Properties["hostname"];
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
            emulator.AddPluginAssembly(@"../../../PowerPlatform.Demo/Plugins/bin/debug/PowerPlatform.Demo.Plugins.dll");
            MetadataCache metadataCache = new MetadataCache(client);
            var context = new DataverseContext()
            {
                CrmServiceClient = client,
                Host = host,
                HttpClient = new HttpClient(),
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
            Instance = new Converters()
            {
                DataverseContext = context,
                RequestConverter = requestConverter,
                ResponseConverter = responseConverter,
                ProxyWithEmulator = emulator.CreateNewProxy()
            };
            return Instance;
        }

        public static WebApiResponse GetResponseUsingConversionAndPlugins(TestContext testContext, WebApiRequest webApiRequest)
        {
            var converters = Helper.GetConverters(testContext);
            var result = converters.RequestConverter.Convert(webApiRequest);
            Assert.IsNotNull(result);
            Assert.IsNull(result.ConvertFailureMessage, result.ConvertFailureMessage);
            Assert.IsNotNull(result.ConvertedRequest);
            OrganizationResponse orgResponse;
            try
            {
                orgResponse = converters.ProxyWithEmulator.Execute(result.ConvertedRequest);
                return converters.ResponseConverter.Convert(result, orgResponse);
            }
            catch (Exception ex)
            {
                return converters.ResponseConverter.Convert(ex);
            }
        }

        internal static void TestAgainstExpected(TestContext testContext, WebApiRequest webApiRequest, bool noErrorExpected = true)
        {
            WebApiResponse webApiResponseExpected = Helper.GetDirectResponse(testContext, webApiRequest);
            WebApiResponse webApiResponseToTest = Helper.GetResponseUsingConversionAndPlugins(testContext, webApiRequest);


            AssertExtensions.AreEquals(webApiResponseToTest, webApiResponseExpected);
            if (noErrorExpected)
            {
                Assert.IsTrue(webApiResponseExpected.StatusCode < 300, "Both methods have returned an unexpected error:" + webApiResponseExpected.StatusCode);
            }
        }

        internal static WebApiResponse GetDirectResponse(TestContext testContext, WebApiRequest webApiRequest)
        {
            var method = new HttpMethod(webApiRequest.Method);
            var url = $"https://{testContext.Properties["hostname"]}/{webApiRequest.LocalPathWithQuery}";
            HttpRequestMessage httpRequest = new HttpRequestMessage(method, url);
            string contentType = null;
            foreach (string header in webApiRequest.Headers)
            {
                if (header.ToLowerInvariant() == "content-type")
                {
                    contentType = webApiRequest.Headers[header];
                }
                else
                {
                    httpRequest.Headers.Add(header, webApiRequest.Headers[header]);
                }


            }
            if (webApiRequest.Body != null)
            {
                httpRequest.Content = new StringContent(webApiRequest.Body);
                if (contentType != null)
                {
                    httpRequest.Content.Headers.ContentType = new MediaTypeHeaderValue(contentType);
                }
            }
            var converters = Helper.GetConverters(testContext);
            httpRequest.Headers.Add("Authorization", "Bearer " + converters.DataverseContext.CrmServiceClient.CurrentAccessToken);

            var result = converters.DataverseContext.HttpClient.SendAsync(httpRequest).Result;

            var response = new WebApiResponse
            {
                StatusCode = (int)result.StatusCode,
                Headers = new NameValueCollection()
            };
            foreach (var header in result.Headers)
            {
                foreach (var value in header.Value)
                {
                    response.Headers.Add(header.Key, value);
                }
            }
            response.Body = result.Content.ReadAsByteArrayAsync().Result;
            return response;
        }

        public static Guid GetId(TestContext context, string entityName)
        {
            var converters = Helper.GetConverters(context);
            QueryExpression query = new QueryExpression(entityName)
            {
                TopCount = 1
            };
            query.Orders.Add(new OrderExpression("modifiedon", OrderType.Ascending));
            var result = converters.DataverseContext.CrmServiceClient.RetrieveMultiple(query);
            return result.Entities.FirstOrDefault().Id;
        }
    }
}
