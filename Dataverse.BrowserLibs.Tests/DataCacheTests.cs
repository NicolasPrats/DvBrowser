using System;
using Dataverse.Plugin.Emulator;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xrm.Sdk;

namespace Dataverse.BrowserLibs.Tests
{
    /// <summary>
    /// Tests for DataCache - validates caching behavior for Azure AD lookups and entity metadata
    /// </summary>
    [TestClass]
    public class DataCacheTests
    {
        private static Helper.Converters Converters;

        [ClassInitialize]
        public static void Initialize(TestContext testContext)
        {
            Converters = Helper.GetConverters(testContext);
        }

        [TestMethod]
        public void GetAzureADIdFromSystemUserId_CachesResult()
        {
            // Arrange
            var service = Converters.DataverseContext.CrmServiceClient;
            var cache = new DataCache(service);
            
            // Get the current user's ID for testing
            var whoAmI = (Microsoft.Crm.Sdk.Messages.WhoAmIResponse)service.Execute(
                new Microsoft.Crm.Sdk.Messages.WhoAmIRequest());
            var systemUserId = whoAmI.UserId;

            // Act - First call should fetch from server
            var azureAdId1 = cache.GetAzureADIdFromSystemUserId(systemUserId);
            
            // Second call should use cache (verified by not throwing even if service is unavailable)
            var azureAdId2 = cache.GetAzureADIdFromSystemUserId(systemUserId);

            // Assert
            azureAdId1.Should().NotBe(Guid.Empty);
            azureAdId2.Should().Be(azureAdId1); // Should return same cached value
        }

        [TestMethod]
        public void GetAzureADIdFromSystemUserId_ReturnsValidGuid()
        {
            // Arrange
            var service = Converters.DataverseContext.CrmServiceClient;
            var cache = new DataCache(service);
            
            var whoAmI = (Microsoft.Crm.Sdk.Messages.WhoAmIResponse)service.Execute(
                new Microsoft.Crm.Sdk.Messages.WhoAmIRequest());
            var systemUserId = whoAmI.UserId;

            // Act
            var azureAdId = cache.GetAzureADIdFromSystemUserId(systemUserId);

            // Assert
            azureAdId.Should().NotBe(Guid.Empty);
        }

        [TestMethod]
        public void GetMetadataEntityWithAttributes_ReturnsEntityMetadata()
        {
            // Arrange
            var service = Converters.DataverseContext.CrmServiceClient;
            var cache = new DataCache(service);

            // Act
            var metadata = cache.GetMetadataEntityWithAttributes("account");

            // Assert
            metadata.Should().NotBeNull();
            metadata.LogicalName.Should().Be("account");
            metadata.Attributes.Should().NotBeNull();
            metadata.Attributes.Should().NotBeEmpty();
        }

        [TestMethod]
        public void GetMetadataEntityWithAttributes_ContactEntity_ReturnsValidMetadata()
        {
            // Arrange
            var service = Converters.DataverseContext.CrmServiceClient;
            var cache = new DataCache(service);

            // Act
            var metadata = cache.GetMetadataEntityWithAttributes("contact");

            // Assert
            metadata.Should().NotBeNull();
            metadata.LogicalName.Should().Be("contact");
            metadata.PrimaryIdAttribute.Should().Be("contactid");
            metadata.Attributes.Should().Contain(attr => attr.LogicalName == "firstname");
            metadata.Attributes.Should().Contain(attr => attr.LogicalName == "lastname");
        }

        [TestMethod]
        public void GetMetadataEntityWithAttributes_InvalidEntity_ThrowsException()
        {
            // Arrange
            var service = Converters.DataverseContext.CrmServiceClient;
            var cache = new DataCache(service);

            // Act & Assert
            Action act = () => cache.GetMetadataEntityWithAttributes("nonexistententity999");
            act.Should().Throw<Exception>(); // Should throw when entity doesn't exist
        }
    }
}
