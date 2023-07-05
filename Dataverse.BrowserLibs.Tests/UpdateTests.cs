using Dataverse.WebApi2IOrganizationService.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;

namespace Dataverse.BrowserLibs.Tests
{
    [TestClass]
    public class UpdateTests
    {
        public TestContext TestContext { get; set; }

        [TestMethod]
        public void Update()
        {
            WebApiRequest webApiRequest = WebApiRequest.CreateFromLocalPathWithQuery(
                "PATCH",
                $"/api/data/v9.2/contacts({Helper.GetId(this.TestContext, "contact")})",
                new System.Collections.Specialized.NameValueCollection()
                {
                    {"Content-Type", "application/json" }
                },
                "{\"firstname\":\"test\"}"
                );
            Helper.TestAgainstExpected(this.TestContext, webApiRequest);

        }

        [TestMethod]
        public void UpdateAk()
        {
            #region data
            var service = Helper.GetConverters(this.TestContext).DataverseContext.CrmServiceClient;
            KeyAttributeCollection keys = new KeyAttributeCollection
            {
                ["name"] = "UpdateAk",
                ["accountnumber"] = "2"
            };
            var account = new Entity("account", keys);
            UpsertRequest request = new UpsertRequest()
            {
                Target = account

            };
            service.Execute(request);
            keys["accountnumber"] = "2";
            service.Execute(request);
            #endregion
            WebApiRequest webApiRequest = WebApiRequest.CreateFromLocalPathWithQuery(
                "PATCH",
                $"/api/data/v9.2/accounts(name='UpdateAk',accountnumber='1')",
                new System.Collections.Specialized.NameValueCollection()
                {
                    {"Content-Type", "application/json" }
                },
                "{\"parentaccountid@odata.bind\":\"/accounts(name='UpdateAk',accountnumber='2')\"}"
                );
            Helper.TestAgainstExpected(this.TestContext, webApiRequest);

        }

    }
}
