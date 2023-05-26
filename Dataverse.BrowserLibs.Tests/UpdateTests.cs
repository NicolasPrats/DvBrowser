using System;
using System.Linq;
using Dataverse.WebApi2IOrganizationService.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xrm.Sdk.Query;

namespace Dataverse.BrowserLibs.Tests
{
    [TestClass]
    public class UpdateTests
    {
        public TestContext TestContext { get; set; }

        public Guid GetContactId()
        {
            var converters = Helper.GetConverters(this.TestContext);
            QueryExpression query = new QueryExpression("contact")
            {
                TopCount = 1
            };
            query.Orders.Add(new OrderExpression("modifiedon", OrderType.Ascending));
            var result = converters.DataverseContext.CrmServiceClient.RetrieveMultiple(query);
            return result.Entities.FirstOrDefault().Id;
        }


        [TestMethod]
        public void Update()
        {
            WebApiRequest webApiRequest = WebApiRequest.CreateFromLocalPathWithQuery(
                "PATCH",
                $"/api/data/v9.2/contacts({GetContactId()})",
                new System.Collections.Specialized.NameValueCollection()
                {
                    {"Content-Type", "application/json" }
                },
                "{\"firstname\":\"test\"}"
                );
            WebApiResponse webApiResponseToTest = Helper.GetResponseUsingConversionAndPlugins(this.TestContext, webApiRequest);
            WebApiResponse webApiResponseExpected = Helper.GetDirectResponse(this.TestContext, webApiRequest);

            AssertExtensions.AreEquals(webApiResponseToTest, webApiResponseExpected);

        }

    }
}
