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
            var converters = Helper.GetConverters(TestContext);

        }
    }
}
