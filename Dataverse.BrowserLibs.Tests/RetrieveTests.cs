using Dataverse.WebApi2IOrganizationService.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Dataverse.BrowserLibs.Tests
{
    [TestClass]
    public class RetrieveTests
    {
        public TestContext TestContext { get; set; }

        [TestMethod]
        public void Retrieve()
        {
            WebApiRequest webApiRequest = WebApiRequest.CreateFromLocalPathWithQuery(
                "GET",
                $"/api/data/v9.2/contacts({Helper.GetId(this.TestContext, "contact")})?$select=fullname",
                new System.Collections.Specialized.NameValueCollection()
                {
                    {"Content-Type", "application/json" }
                }
                );
            Helper.TestAgainstExpected(this.TestContext, webApiRequest);

        }



    }
}
