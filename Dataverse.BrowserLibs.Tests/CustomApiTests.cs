using System;
using System.Collections.Generic;
using Dataverse.WebApi2IOrganizationService.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace Dataverse.BrowserLibs.Tests
{
    [TestClass]
    public class CustomApiTests
    {
        public TestContext TestContext { get; set; }

        //TODO : tester avec 1 valeur primitve

        [TestMethod]
        public void Unbound_2ResponseWhose1Entity()
        {
            WebApiRequest webApiRequest = WebApiRequest.CreateFromLocalPathWithQuery(
                "POST",
                $"/api/data/v9.2/dvb_Unbound_2ResponseWhose1Entity",
                new System.Collections.Specialized.NameValueCollection()
                {
                    {"Content-Type", "application/json" }
                },
                "{}"
                );
            Helper.TestAgainstExpected(this.TestContext, webApiRequest);
        }

        // TODO: when only 1 reponse property of type entity or entitycollection, dataverse doesn't return anything. To investigate
        //[TestMethod]
        //public void Unbound_1ResponseEntity()
        //{
        //    WebApiRequest webApiRequest = WebApiRequest.CreateFromLocalPathWithQuery(
        //       "POST",
        //       $"/api/data/v9.2/dvb_Unbound_1ResponseEntity",
        //       new System.Collections.Specialized.NameValueCollection()
        //       {
        //            {"Content-Type", "application/json" }
        //       },
        //       "{}"
        //       );
        //    Helper.TestAgainstExpected(this.TestContext, webApiRequest);
        //}

        //[TestMethod]
        //public void Unbound_1ResponseCollection()
        //{
        //    WebApiRequest webApiRequest = WebApiRequest.CreateFromLocalPathWithQuery(
        //        "POST",
        //        $"/api/data/v9.2/dvb_Unbound_1ResponseCollection",
        //        new System.Collections.Specialized.NameValueCollection()
        //        {
        //            {"Content-Type", "application/json" }
        //        },
        //        "{}"
        //        );
        //    Helper.TestAgainstExpected(this.TestContext, webApiRequest);
        //}

        [TestMethod]
        public void Unbound_1ResponseEntityReference()
        {
            WebApiRequest webApiRequest = WebApiRequest.CreateFromLocalPathWithQuery(
               "POST",
               $"/api/data/v9.2/dvb_Unbound_1ResponseEntityReference",
               new System.Collections.Specialized.NameValueCollection()
               {
                    {"Content-Type", "application/json" }
               },
               "{}"
               );
            Helper.TestAgainstExpected(this.TestContext, webApiRequest);
        }


        [TestMethod]
        public void Unbound_NoResponse()
        {
            WebApiRequest webApiRequest = WebApiRequest.CreateFromLocalPathWithQuery(
                "POST",
                $"/api/data/v9.2/dvb_Unbound_NoResponse",
                new System.Collections.Specialized.NameValueCollection()
                {
                    {"Content-Type", "application/json" }
                },
                "{}"
                );
            Helper.TestAgainstExpected(this.TestContext, webApiRequest);
        }





        [TestMethod]
        public void Unbound_AllRequestsAllResponses()
        {
            var jsonRecord = new Dictionary<string, object>()
            {
                {"ownerid",  Helper.GetId(this.TestContext, "systemuser") },
                {"@odata.type", "Microsoft.Dynamics.CRM.systemuser" }
            };
            var jsonObject = new
            {
                Unbound_AllRequestsAllResponses_ReqBool = true,
                Unbound_AllRequestsAllResponses_Req_Datetime = DateTime.Now,
                Unbound_AllRequestsAllResponses_Req_Decimal = 123.45m,
                Unbound_AllRequestsAllResponses_Req_Coll = new[] { jsonRecord },
                Unbound_AllRequestsAllResponses_Req_EntityRef = jsonRecord,
                Unbound_AllRequestsAllResponses_Req_Float = 123.45,
                Unbound_AllRequestsAllResponses_Req_Int = 123,
                Unbound_AllRequestsAllResponses_Req_Money = 123.45m,
                Unbound_AllRequestsAllResponses_Req_Picklist = 1,
                Unbound_AllRequestsAllResponses_Req_String = "ABC",
                Unbound_AllRequestsAllResponses_Req_StringArray = new[] { "", "ABC", "DEF" },
                Unbound_AllRequestsAllResponses_Req_Guid = Guid.NewGuid(),
                Unbound_AllRequestsAllResponses_Req_Entity = jsonRecord
            };


            WebApiRequest webApiRequest = WebApiRequest.CreateFromLocalPathWithQuery(
                "POST",
                $"/api/data/v9.2/dvb_Unbound_AllRequestsAllResponses",
                new System.Collections.Specialized.NameValueCollection()
                {
                    {"Content-Type", "application/json" }
                },
                JsonConvert.SerializeObject(jsonObject)

                );
            Helper.TestAgainstExpected(this.TestContext, webApiRequest);
        }


    }

}
