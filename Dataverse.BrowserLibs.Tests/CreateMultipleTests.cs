using System.Collections.Generic;
using Dataverse.WebApi2IOrganizationService.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace Dataverse.BrowserLibs.Tests
{
    [TestClass]
    public class CreateMulitpleTests
    {

        public TestContext TestContext { get; set; }

        [TestMethod]
        public void CreateSimpleAccount_NoPlugin()
        {
            WebApiRequest webApiRequest = WebApiRequest.CreateFromLocalPathWithQuery(
                "POST",
                "/api/data/v9.2/accounts/Microsoft.Dynamics.CRM.CreateMultiple",
                new System.Collections.Specialized.NameValueCollection()
                {
                    {"Content-Type", "application/json" },
                    {"Prefer" ,"return=representation"},
                },
                 JsonConvert.SerializeObject(new
                 {
                     Targets = new[] {
                         new Dictionary<string, object>()
                            {
                                {"name",  "test1" },
                                {"@odata.type", "Microsoft.Dynamics.CRM.account" }
                            },
                     new Dictionary<string, object>()
                            {
                                {"name",  "test2" },
                                {"@odata.type", "Microsoft.Dynamics.CRM.account" }
                            }
                    }
                 })
                );
            Helper.TestAgainstExpected(this.TestContext, webApiRequest);
        }


        [TestMethod]
        public void CreateSimpleContact_Plugin()
        {
            WebApiRequest webApiRequest = WebApiRequest.CreateFromLocalPathWithQuery(
                "POST",
                "/api/data/v9.2/contacts/Microsoft.Dynamics.CRM.CreateMultiple",
                new System.Collections.Specialized.NameValueCollection()
                {
                    {"Content-Type", "application/json" },
                    {"Prefer" ,"return=representation"},
                },
                JsonConvert.SerializeObject(new
                {
                    Targets = new[] {
                         new Dictionary<string, object>()
                            {
                                {"firstname",  "test1" },
                                {"lastname",  "test1" },
                                {"@odata.type", "Microsoft.Dynamics.CRM.contact" }
                            },
                     new Dictionary<string, object>()
                            {
                                {"firstname",  "test2" },
                                {"lastname",  "test2" },
                                {"@odata.type", "Microsoft.Dynamics.CRM.contact" }
                            }
                    }
                })
                );
            Helper.TestAgainstExpected(this.TestContext, webApiRequest);

        }

        [TestMethod]
        public void CreateSimpleContact_Plugin_witherror()
        {
            WebApiRequest webApiRequest = WebApiRequest.CreateFromLocalPathWithQuery(
                "POST",
                "/api/data/v9.2/contacts/Microsoft.Dynamics.CRM.CreateMultiple",
                new System.Collections.Specialized.NameValueCollection()
                {
                    {"Content-Type", "application/json" },
                    {"Prefer" ,"return=representation"},
                },
                JsonConvert.SerializeObject(new
                {
                    Targets = new[] {
                         new Dictionary<string, object>()
                            {
                                {"firstname",  "test1" },
                                {"@odata.type", "Microsoft.Dynamics.CRM.contact" }
                            },
                     new Dictionary<string, object>()
                            {
                                {"firstname",  "test2" },
                                {"@odata.type", "Microsoft.Dynamics.CRM.contact" }
                            }
                    }
                })
                );
            Helper.TestAgainstExpected(this.TestContext, webApiRequest, false);

        }


        [TestMethod]
        public void CreateCustomTable_Plugin()
        {
            WebApiRequest webApiRequest = WebApiRequest.CreateFromLocalPathWithQuery(
                "POST",
                "/api/data/v9.2/dvb_mycustomtables/Microsoft.Dynamics.CRM.CreateMultiple",
                new System.Collections.Specialized.NameValueCollection()
                {
                    {"Content-Type", "application/json" },
                    {"Prefer" ,"return=representation"},
                },
                JsonConvert.SerializeObject(new
                {
                    Targets = new[] {
                         new Dictionary<string, object>()
                            {
                                {"dvb_name",  "test1" },
                                {"@odata.type", "Microsoft.Dynamics.CRM.dvb_mycustomtable" }
                            },
                     new Dictionary<string, object>()
                            {
                                {"dvb_name",  "test2" },
                                {"@odata.type", "Microsoft.Dynamics.CRM.dvb_mycustomtable" }
                            }
                    }
                })
                );
            Helper.TestAgainstExpected(this.TestContext, webApiRequest);
        }

        [TestMethod]
        public void CreateCustomTable_Plugin_error()
        {
            WebApiRequest webApiRequest = WebApiRequest.CreateFromLocalPathWithQuery(
                "POST",
                "/api/data/v9.2/dvb_mycustomtables/Microsoft.Dynamics.CRM.CreateMultiple",
                new System.Collections.Specialized.NameValueCollection()
                {
                    {"Content-Type", "application/json" },
                    {"Prefer" ,"return=representation"},
                },
                JsonConvert.SerializeObject(new
                {
                    Targets = new[] {
                         new Dictionary<string, object>()
                            {
                                {"dvb_name",   null },
                                {"@odata.type", "Microsoft.Dynamics.CRM.dvb_mycustomtable" }
                            },
                     new Dictionary<string, object>()
                            {
                                {"dvb_name",   null },
                                {"@odata.type", "Microsoft.Dynamics.CRM.dvb_mycustomtable" }
                            }
                    }
                })
                );
            Helper.TestAgainstExpected(this.TestContext, webApiRequest, false);
        }
    }
}
