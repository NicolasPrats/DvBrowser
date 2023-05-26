using System;
using System.Linq;
using Dataverse.WebApi2IOrganizationService.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xrm.Sdk.Query;

namespace Dataverse.BrowserLibs.Tests
{
    [TestClass]
    public class CustomApiTests
    {
        public TestContext TestContext { get; set; }

        public Guid GetId(string entityName)
        {
            var converters = Helper.GetConverters(this.TestContext);
            QueryExpression query = new QueryExpression(entityName)
            {
                TopCount = 1
            };
            query.Orders.Add(new OrderExpression("modifiedon", OrderType.Ascending));
            var result = converters.DataverseContext.CrmServiceClient.RetrieveMultiple(query);
            return result.Entities.FirstOrDefault().Id;
        }


        [TestMethod]
        public void Unbound()
        {
            WebApiRequest webApiRequest = WebApiRequest.CreateFromLocalPathWithQuery(
                "POST",
                $"/api/data/v9.2/new_TestUnbound",
                new System.Collections.Specialized.NameValueCollection()
                {
                    {"Content-Type", "application/json" }
                },
                "{\"new_testbool\":true,\"new_testdatetime\":\"2023-05-26T18:21:00.000Z\",\"new_testdecimal\":123.45,\"new_testEntitycoll\":[{\"@odata.type\":\"Microsoft.Dynamics.CRM.account\",\"accountid\":\"a56b3f4b-1be7-e611-8101-e0071b6af231\"}],\"new_testEntityref\":{\"@odata.type\":\"Microsoft.Dynamics.CRM.contact\",\"contactid\":\"25a17064-1ae7-e611-80f4-e0071b661f01\"},\"new_testfloat\":123.45,\"new_testint\":123,\"new_testmoney\":123,\"new_testpicklist\":123,\"new_teststring\":\"ABC\",\"new_teststringarray\":[\"abc\",\"def\"],\"new_testguid\":\"25a17064-1ae7-e611-80f4-e0071b661f01\"}"
                );
            WebApiResponse webApiResponseToTest = Helper.GetResponseUsingConversionAndPlugins(this.TestContext, webApiRequest);
            WebApiResponse webApiResponseExpected = Helper.GetDirectResponse(this.TestContext, webApiRequest);

            AssertExtensions.AreEquals(webApiResponseToTest, webApiResponseExpected);

        }

    }
}
