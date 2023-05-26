using Dataverse.WebApi2IOrganizationService.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace Dataverse.BrowserLibs.Tests
{
    [TestClass]
    public class CreateTests
    {

        public TestContext TestContext { get; set; }

        [TestMethod]
        public void CreateSimpleAccount_NoPlugin()
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
            Helper.TestAgainstExpected(this.TestContext, webApiRequest);
        }


        [TestMethod]
        public void CreateSimpleContact_Plugin()
        {
            WebApiRequest webApiRequest = WebApiRequest.CreateFromLocalPathWithQuery(
                "POST",
                "/api/data/v9.2/contacts",
                new System.Collections.Specialized.NameValueCollection()
                {
                    {"Content-Type", "application/json" },
                    {"Prefer" ,"return=representation"},
                },
                JsonConvert.SerializeObject(new
                {
                    firstname = "test",
                    lastname = "test",
                })
                );
            Helper.TestAgainstExpected(this.TestContext, webApiRequest);

        }

        [TestMethod]
        public void CreateSimpleContact_Plugin_witherror()
        {
            WebApiRequest webApiRequest = WebApiRequest.CreateFromLocalPathWithQuery(
                "POST",
                "/api/data/v9.2/contacts",
                new System.Collections.Specialized.NameValueCollection()
                {
                    {"Content-Type", "application/json" },
                    {"Prefer" ,"return=representation"},
                },
                JsonConvert.SerializeObject(new
                {
                    firstname = "test"
                })
                );
            Helper.TestAgainstExpected(this.TestContext, webApiRequest);

        }

        [TestMethod]
        public void CreateEmailWithActivitiesParty()
        {
            WebApiRequest webApiRequest = WebApiRequest.CreateFromLocalPathWithQuery(
               "POST",
               "/api/data/v9.2/emails",
               new System.Collections.Specialized.NameValueCollection()
               {
                    {"Content-Type", "application/json" },
                    {"Prefer" ,"return=representation"},
               },
              @"{
""description"": ""something"", 
        ""subject"": ""Email"",
        ""email_activity_parties"": [
            {
                ""partyid_contact@odata.bind"": ""/contacts(" + Helper.GetId(this.TestContext, "contact") + @")"",
                ""participationtypemask"": 1
            },
            {
                ""partyid_account@odata.bind"": ""/accounts(" + Helper.GetId(this.TestContext, "account") + @")"",
                ""participationtypemask"": 2 
            }
        ]
}"
               );
            Helper.TestAgainstExpected(this.TestContext, webApiRequest);
        }
    }
}
