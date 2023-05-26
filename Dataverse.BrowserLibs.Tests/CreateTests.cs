using Dataverse.WebApi2IOrganizationService.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Dataverse.BrowserLibs.Tests
{
    [TestClass]
    public class CreateTests
    {

        public TestContext TestContext { get; set; }

        [TestMethod]
        public void CreateSimpleAccount()
        {
            WebApiRequest webApiRequest = WebApiRequest.CreateFromLocalPathWithQuery(
                "POST",
                "/api/data/v9.2/accounts",
                new System.Collections.Specialized.NameValueCollection()
                {
                    {"Content-Type", "application/json" },
                    {"Prefer" ,"return=representation"},
                },
                "{\"name\":\"test\"}"
                );
            WebApiResponse webApiResponseToTest = Helper.GetResponseUsingConversionAndPlugins(this.TestContext, webApiRequest);
            WebApiResponse webApiResponseExpected = Helper.GetDirectResponse(this.TestContext, webApiRequest);

            AssertExtensions.AreEquals(webApiResponseToTest, webApiResponseExpected);

        }



    }
}
