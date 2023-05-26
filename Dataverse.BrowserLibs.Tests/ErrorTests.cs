using Dataverse.WebApi2IOrganizationService.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Dataverse.BrowserLibs.Tests
{
    [TestClass]
    public class ErrorTests
    {
        public TestContext TestContext { get; set; }

        [TestMethod]
        public void NoWebApiRequest()
        {
            string url = $"https://{this.TestContext.Properties["hostname"]}/blah";
            var request = WebApiRequest.Create("get", url, new System.Collections.Specialized.NameValueCollection(), null);
            Assert.IsNull(request);
        }


        [TestMethod]
        public void BadWebApiRequest()
        {
            string url = $"https://{this.TestContext.Properties["hostname"]}/api/data/v9.2/blah";
            var request = WebApiRequest.Create("get", url, new System.Collections.Specialized.NameValueCollection(), null);

            var converters = Helper.GetConverters(this.TestContext);
            var result = converters.RequestConverter.Convert(request);

            Assert.IsNotNull(result);
            Assert.IsNotNull(result.ConvertFailureMessage);
            Assert.IsNotNull(result.SrcRequest);
        }
    }
}
