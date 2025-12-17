using Dataverse.WebApi2IOrganizationService.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Dataverse.BrowserLibs.Tests
{
    [TestClass]
    public class NullHandlingTests
    {
        public TestContext TestContext { get; set; }

        [TestMethod]
        public void SystemMetadataFunction_GetClientMetadata_ReturnsNull()
        {
            // Arrange - system metadata functions should return null (not converted)
            WebApiRequest webApiRequest = WebApiRequest.CreateFromLocalPathWithQuery(
                "GET",
                "/api/data/v9.2/GetClientMetadata()",
                new System.Collections.Specialized.NameValueCollection(),
                null
            );

            var converters = Helper.GetConverters(this.TestContext);

            // Act
            var result = converters.RequestConverter.Convert(webApiRequest);

            // Assert - should return null for system metadata functions
            Assert.IsNull(result, "System metadata function GetClientMetadata should return null (not converted)");
        }

        [TestMethod]
        public void SystemMetadataFunction_RetrieveMetadataChanges_ReturnsNull()
        {
            // Arrange
            WebApiRequest webApiRequest = WebApiRequest.CreateFromLocalPathWithQuery(
                "POST",
                "/api/data/v9.2/RetrieveMetadataChanges",
                new System.Collections.Specialized.NameValueCollection()
                {
                    {"Content-Type", "application/json" }
                },
                "{}"
            );

            var converters = Helper.GetConverters(this.TestContext);

            // Act
            var result = converters.RequestConverter.Convert(webApiRequest);

            // Assert
            Assert.IsNull(result, "System metadata function RetrieveMetadataChanges should return null (not converted)");
        }

        [TestMethod]
        public void SystemMetadataFunction_RetrieveAllEntities_ReturnsNull()
        {
            // Arrange
            WebApiRequest webApiRequest = WebApiRequest.CreateFromLocalPathWithQuery(
                "GET",
                "/api/data/v9.2/RetrieveAllEntities(EntityFilters=Microsoft.Dynamics.CRM.EntityFilters'Entity')",
                new System.Collections.Specialized.NameValueCollection(),
                null
            );

            var converters = Helper.GetConverters(this.TestContext);

            // Act
            var result = converters.RequestConverter.Convert(webApiRequest);

            // Assert
            Assert.IsNull(result, "System metadata function RetrieveAllEntities should return null (not converted)");
        }

        [TestMethod]
        public void SystemMetadataFunction_RetrieveEntity_ReturnsNull()
        {
            // Arrange
            WebApiRequest webApiRequest = WebApiRequest.CreateFromLocalPathWithQuery(
                "GET",
                "/api/data/v9.2/RetrieveEntity(EntityFilters=Microsoft.Dynamics.CRM.EntityFilters'Attributes',LogicalName='account')",
                new System.Collections.Specialized.NameValueCollection(),
                null
            );

            var converters = Helper.GetConverters(this.TestContext);

            // Act
            var result = converters.RequestConverter.Convert(webApiRequest);

            // Assert
            Assert.IsNull(result, "System metadata function RetrieveEntity should return null (not converted)");
        }

        [TestMethod]
        public void SystemMetadataFunction_RetrieveAttribute_ReturnsNull()
        {
            // Arrange
            WebApiRequest webApiRequest = WebApiRequest.CreateFromLocalPathWithQuery(
                "GET",
                "/api/data/v9.2/RetrieveAttribute(EntityLogicalName='account',LogicalName='name')",
                new System.Collections.Specialized.NameValueCollection(),
                null
            );

            var converters = Helper.GetConverters(this.TestContext);

            // Act
            var result = converters.RequestConverter.Convert(webApiRequest);

            // Assert
            Assert.IsNull(result, "System metadata function RetrieveAttribute should return null (not converted)");
        }

        [TestMethod]
        public void NormalRequest_DoesNotReturnNull()
        {
            // Arrange - normal requests should be converted
            WebApiRequest webApiRequest = WebApiRequest.CreateFromLocalPathWithQuery(
                "GET",
                "/api/data/v9.2/accounts?$top=1",
                new System.Collections.Specialized.NameValueCollection(),
                null
            );

            var converters = Helper.GetConverters(this.TestContext);

            // Act
            var result = converters.RequestConverter.Convert(webApiRequest);

            // Assert - normal requests should be converted
            Assert.IsNotNull(result, "Normal request should be converted (not null)");
            Assert.IsNotNull(result.ConvertedRequest, "Should have converted SDK request");
        }
    }
}
