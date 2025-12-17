using System;
using System.IO;
using Dataverse.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Dataverse.BrowserLibs.Tests
{
    [TestClass]
    public class MetadataCacheTests
    {
        public TestContext TestContext { get; set; }

        [TestMethod]
        public void MetadataCache_WithoutDiskCache_FetchesFromDataverse()
        {
            // Arrange
            var converters = Helper.GetConverters(this.TestContext);
            var client = converters.Context.CrmServiceClient;

            // Act - no cache path provided
            var cache = new MetadataCache(client, cachePath: null, forceRefresh: false);

            // Assert - should have entities loaded
            var accountMetadata = cache.GetEntityFromLogicalName("account");
            Assert.IsNotNull(accountMetadata, "Account entity metadata should be loaded");
            Assert.AreEqual("account", accountMetadata.LogicalName);
            Assert.IsNotNull(accountMetadata.EntitySetName, "Entity set name should be populated");
        }

        [TestMethod]
        public void MetadataCache_WithDiskCache_SavesAndLoads()
        {
            // Arrange
            var converters = Helper.GetConverters(this.TestContext);
            var client = converters.Context.CrmServiceClient;
            var tempPath = Path.Combine(Path.GetTempPath(), "DvBrowserTest_" + Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempPath);

            try
            {
                // Act - First load should fetch and save
                var cache1 = new MetadataCache(client, cachePath: tempPath, forceRefresh: false);
                var account1 = cache1.GetEntityFromLogicalName("account");

                // Verify cache file was created
                var cacheFile = Path.Combine(tempPath, "metadata.cache");
                Assert.IsTrue(File.Exists(cacheFile), "Cache file should be created");

                // Second load should use cached data
                var cache2 = new MetadataCache(client, cachePath: tempPath, forceRefresh: false);
                var account2 = cache2.GetEntityFromLogicalName("account");

                // Assert - both should have same data
                Assert.AreEqual(account1.LogicalName, account2.LogicalName);
                Assert.AreEqual(account1.EntitySetName, account2.EntitySetName);
            }
            finally
            {
                // Cleanup
                if (Directory.Exists(tempPath))
                {
                    Directory.Delete(tempPath, recursive: true);
                }
            }
        }

        [TestMethod]
        public void MetadataCache_ForceRefresh_IgnoresCache()
        {
            // Arrange
            var converters = Helper.GetConverters(this.TestContext);
            var client = converters.Context.CrmServiceClient;
            var tempPath = Path.Combine(Path.GetTempPath(), "DvBrowserTest_" + Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempPath);

            try
            {
                // Act - First load to create cache
                var cache1 = new MetadataCache(client, cachePath: tempPath, forceRefresh: false);
                var cacheFile = Path.Combine(tempPath, "metadata.cache");
                var firstWriteTime = File.GetLastWriteTime(cacheFile);

                // Wait a bit to ensure timestamp difference
                System.Threading.Thread.Sleep(100);

                // Force refresh should reload even with cache present
                var cache2 = new MetadataCache(client, cachePath: tempPath, forceRefresh: true);
                var secondWriteTime = File.GetLastWriteTime(cacheFile);

                // Assert - cache should be rewritten
                Assert.IsTrue(secondWriteTime > firstWriteTime, "Cache file should be updated when force refresh is used");
            }
            finally
            {
                // Cleanup
                if (Directory.Exists(tempPath))
                {
                    Directory.Delete(tempPath, recursive: true);
                }
            }
        }

        [TestMethod]
        public void MetadataCache_GetEntityBySetName_ReturnsCorrectEntity()
        {
            // Arrange
            var converters = Helper.GetConverters(this.TestContext);
            var cache = converters.Context.MetadataCache;

            // Act
            var accountMetadata = cache.GetEntityFromSetName("accounts");

            // Assert
            Assert.IsNotNull(accountMetadata, "Should find account entity by set name");
            Assert.AreEqual("account", accountMetadata.LogicalName);
            Assert.AreEqual("accounts", accountMetadata.EntitySetName);
        }

        [TestMethod]
        public void MetadataCache_GetEntityMetadataWithAttributes_LoadsOnDemand()
        {
            // Arrange
            var converters = Helper.GetConverters(this.TestContext);
            var cache = converters.Context.MetadataCache;

            // Act
            var accountWithAttributes = cache.GetEntityMetadataWithAttributes("account");

            // Assert
            Assert.IsNotNull(accountWithAttributes, "Should load entity with attributes");
            Assert.IsTrue(accountWithAttributes.Attributes.Length > 0, "Should have attributes loaded");
        }
    }
}
