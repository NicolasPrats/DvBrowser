using System.Text;
using Dataverse.WebApi2IOrganizationService.Model;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;

namespace Dataverse.BrowserLibs.Tests
{
    internal static class AssertExtensions
    {
        public static void AreEquals(WebApiResponse webApiResponseToTest, WebApiResponse webApiResponseExpected)
        {
            Assert.AreEqual(webApiResponseExpected.StatusCode, webApiResponseToTest.StatusCode);
            foreach (string header in webApiResponseExpected.Headers)
            {
                if (!header.StartsWith("OData"))
                    continue;
                Assert.AreEqual(webApiResponseToTest.Headers["header"], webApiResponseExpected.Headers["header"]);
            }
            if (webApiResponseToTest.Body == null && webApiResponseExpected.Body == null)
                return;
            Assert.IsNotNull(webApiResponseToTest.Body, "Body was expected");
            Assert.IsNotNull(webApiResponseExpected.Body, "Body was not expected");

            JToken bodyExpected = JToken.Parse(Encoding.UTF8.GetString(webApiResponseExpected.Body));
            JToken bodyToTest = JToken.Parse(Encoding.UTF8.GetString(webApiResponseToTest.Body));

            bodyToTest.Should().BeEquivalentTo(bodyExpected);

        }
    }
}
