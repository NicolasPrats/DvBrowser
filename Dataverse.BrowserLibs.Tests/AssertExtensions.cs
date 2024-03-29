﻿using System.Text;
using Dataverse.WebApi2IOrganizationService.Model;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;

namespace Dataverse.BrowserLibs.Tests
{
    internal static class AssertExtensions
    {
        public static void AreEquals(WebApiResponse webApiResponseToTest, WebApiResponse webApiResponseExpected, bool compareBodyContent = true)
        {
            Assert.AreEqual(webApiResponseExpected.StatusCode, webApiResponseToTest.StatusCode);
            foreach (string header in webApiResponseExpected.Headers)
            {
                if (!header.StartsWith("OData"))
                    continue;
                Assert.AreEqual(webApiResponseToTest.Headers["header"], webApiResponseExpected.Headers["header"]);
            }
            bool bodyToTestIsEmpty = webApiResponseToTest.Body == null || webApiResponseToTest.Body.Length == 0;
            bool bodyExpectedIsEmpty = webApiResponseExpected.Body == null || webApiResponseExpected.Body.Length == 0;
            if (bodyToTestIsEmpty && bodyExpectedIsEmpty)
                return;
            Assert.IsFalse(bodyToTestIsEmpty, "Body was expected");
            Assert.IsFalse(bodyExpectedIsEmpty, "Body was not expected");

            if (compareBodyContent)
            {
                JToken bodyExpected = JToken.Parse(Encoding.UTF8.GetString(webApiResponseExpected.Body));
                JToken bodyToTest = JToken.Parse(Encoding.UTF8.GetString(webApiResponseToTest.Body));

                bodyToTest.Should().BeEquivalentTo(bodyExpected);
            }
        }
    }
}
