using System;
using Dataverse.WebApi2IOrganizationService.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Dataverse.BrowserLibs.Tests
{
    [TestClass]
    public class CustomApiTests
    {
        public TestContext TestContext { get; set; }




        [TestMethod]
        public void Unbound()
        {
            Guid contactId = Helper.GetId(this.TestContext, "contact");
            Guid accountId = Helper.GetId(this.TestContext, "account");
            WebApiRequest webApiRequest = WebApiRequest.CreateFromLocalPathWithQuery(
                "POST",
                $"/api/data/v9.2/new_TestUnbound",
                new System.Collections.Specialized.NameValueCollection()
                {
                    {"Content-Type", "application/json" }
                },
                "{\"new_testbool\":true,\"new_testdatetime\":\"2023-05-26T18:21:00.000Z\",\"new_testdecimal\":123.45,\"new_testEntitycoll\":[{\"@odata.type\":\"Microsoft.Dynamics.CRM.account\",\"accountid\":\"" + accountId + "\"}],\"new_testEntityref\":{\"@odata.type\":\"Microsoft.Dynamics.CRM.contact\",\"contactid\":\"" + contactId + "\"},\"new_testfloat\":123.45,\"new_testint\":123,\"new_testmoney\":123,\"new_testpicklist\":123,\"new_teststring\":\"ABC\",\"new_teststringarray\":[\"abc\",\"def\"],\"new_testguid\":\"25a17064-1ae7-e611-80f4-e0071b661f01\"}"
                );
            Helper.TestAgainstExpected(this.TestContext, webApiRequest);
        }

    }
}
