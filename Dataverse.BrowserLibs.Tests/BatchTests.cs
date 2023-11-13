using System;
using Dataverse.WebApi2IOrganizationService.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Dataverse.BrowserLibs.Tests
{
    [TestClass]
    public class BatchTests
    {

        public TestContext TestContext { get; set; }

        [TestMethod]
        public void CreateOpportunity_ChangesetNoPlugin()
        {
            Guid oppId = Guid.NewGuid();
            WebApiRequest webApiRequest = WebApiRequest.CreateFromLocalPathWithQuery(
                "POST",
                "/api/data/v9.2/$batch",
                new System.Collections.Specialized.NameValueCollection()
                {
                    {"Content-Type", "multipart/mixed;boundary=batch_1699608839380" },
                    {"Prefer" ,"return=representation"},
                },
                @"--batch_1699608839380
Content-Type: multipart/mixed;boundary=changeset_1699608839381

--changeset_1699608839381
Content-Type: application/http
Content-Transfer-Encoding: binary
Content-ID: 1

POST /api/data/v9.0/opportunities HTTP/1.1
Accept: application/json
MSCRM.SuppressDuplicateDetection: false
Content-Type: application/json
Prefer: odata.include-annotations=""*""
ClientHost: Browser

{""name"":""test"",""decisionmaker"":false,""processid"":""00000000-0000-0000-0000-000000000000"",""isrevenuesystemcalculated"":false,""msdyn_ordertype"":192350000,""msdyn_forecastcategory"":100000001,""transactioncurrencyid@odata.bind"":""/transactioncurrencies(64b134ec-6c49-ee11-be6e-000d3ab1cf95)"",""transactioncurrencyid@OData.Community.Display.V1.FormattedValue"":""US Dollar"",""statuscode"":1,""statecode"":0,""ownerid@odata.bind"":""/systemusers(4c6c3e24-0f49-ee11-be6e-000d3ab1cf95)"",""ownerid@OData.Community.Display.V1.FormattedValue"":""System Administrator"",""opportunityid"":""" + oppId + @"""}
--changeset_1699608839381
Content-Type: application/http
Content-Transfer-Encoding: binary
Content-ID: 2

POST /api/data/v9.0/opportunitysalesprocesses HTTP/1.1
Accept: application/json
MSCRM.SuppressDuplicateDetection: false
Content-Type: application/json
Prefer: odata.include-annotations=""*""
ClientHost: Browser

{""processid@odata.bind"":""/workflows(3e8ebee6-a2bc-4451-9c5f-b146b085413a)"",""opportunityid@odata.bind"":""/opportunities(" + oppId + @")"",""traversedpath"":""6b9ce798-221a-4260-90b2-2a95ed51a5bc"",""activestageid@odata.bind"":""/processstages(6b9ce798-221a-4260-90b2-2a95ed51a5bc)""}
--changeset_1699608839381--

--batch_1699608839380--
"
                );
            //TODO: comparer vis à vis de l'expected
            Helper.GetResponseUsingConversionAndPlugins(this.TestContext, webApiRequest);
        }

        [TestMethod]
        public void CreateOpportunity_NoChangesetNoPlugin()
        {
            Guid oppId = Guid.NewGuid();
            WebApiRequest webApiRequest = WebApiRequest.CreateFromLocalPathWithQuery(
                "POST",
                "/api/data/v9.2/$batch",
                new System.Collections.Specialized.NameValueCollection()
                {
                    {"Content-Type", "multipart/mixed;boundary=batch_1699608839380" },
                    {"Prefer" ,"return=representation"},
                },
                @"--batch_1699608839380
Content-Type: application/http
Content-Transfer-Encoding: binary
Content-ID: 1

POST /api/data/v9.0/opportunities HTTP/1.1
Accept: application/json
MSCRM.SuppressDuplicateDetection: false
Content-Type: application/json
Prefer: odata.include-annotations=""*""
ClientHost: Browser

{""name"":""test"",""decisionmaker"":false,""processid"":""00000000-0000-0000-0000-000000000000"",""isrevenuesystemcalculated"":false,""msdyn_ordertype"":192350000,""msdyn_forecastcategory"":100000001,""transactioncurrencyid@odata.bind"":""/transactioncurrencies(64b134ec-6c49-ee11-be6e-000d3ab1cf95)"",""transactioncurrencyid@OData.Community.Display.V1.FormattedValue"":""US Dollar"",""statuscode"":1,""statecode"":0,""ownerid@odata.bind"":""/systemusers(4c6c3e24-0f49-ee11-be6e-000d3ab1cf95)"",""ownerid@OData.Community.Display.V1.FormattedValue"":""System Administrator"",""opportunityid"":""" + oppId + @"""}
--batch_1699608839380
Content-Type: application/http
Content-Transfer-Encoding: binary
Content-ID: 2

POST /api/data/v9.0/opportunitysalesprocesses HTTP/1.1
Accept: application/json
MSCRM.SuppressDuplicateDetection: false
Content-Type: application/json
Prefer: odata.include-annotations=""*""
ClientHost: Browser

{""processid@odata.bind"":""/workflows(3e8ebee6-a2bc-4451-9c5f-b146b085413a)"",""opportunityid@odata.bind"":""/opportunities(" + oppId + @")"",""traversedpath"":""6b9ce798-221a-4260-90b2-2a95ed51a5bc"",""activestageid@odata.bind"":""/processstages(6b9ce798-221a-4260-90b2-2a95ed51a5bc)""}

--batch_1699608839380--
"
                );
            //TODO: comparer vis à vis de l'expected
            Helper.GetResponseUsingConversionAndPlugins(this.TestContext, webApiRequest);
        }


    }
}
