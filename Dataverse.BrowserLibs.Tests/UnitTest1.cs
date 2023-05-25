using System;
using System.Net.Http;
using System.Xml;
using Dataverse.Plugin.Emulator.Steps;
using Dataverse.Utils;
using Dataverse.WebApi2IOrganizationService.Converters;
using Dataverse.WebApi2IOrganizationService.Converterss;
using Dataverse.WebApi2IOrganizationService.Model;
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
            var converters = Helper.GetConverters(TestContext);

            WebApiRequest webApiRequest = WebApiRequest.CreateFromLocalPathWithQuery("POST", "/api/data/v9.2/contacts", new System.Collections.Specialized.NameValueCollection(), "{\"lastname\":\"test\"}");
            var result = converters.RequestConverter.Convert(webApiRequest);
            Assert.IsNotNull(result);
            Assert.IsNull(result.ConvertFailureMessage, result.ConvertFailureMessage);
            Assert.IsNotNull(result.ConvertedRequest);
            

            var orgResponse = converters.ProxyWithEmulator.Execute(result.ConvertedRequest);
            Assert.IsNotNull(orgResponse);
            var webApiResponse = converters.ResponseConverter.Convert(result, orgResponse);

        }
    }
}
