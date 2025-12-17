using System;
using Dataverse.Plugin.Emulator.Steps;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Dataverse.BrowserLibs.Tests
{
    [TestClass]
    public class PluginCacheTests
    {
        public TestContext TestContext { get; set; }

        [TestMethod]
        public void PluginInstanceCache_Clear_RemovesAllInstances()
        {
            // Arrange
            var cache = new PluginInstanceCache();
            var testType = typeof(string); // Use a simple type for testing
            var instance1 = "test1";
            var instance2 = "test2";

            // Add instances manually to the cache (using internal knowledge of cache structure)
            // This simulates what happens during normal plugin execution
            var cacheField = typeof(PluginInstanceCache).GetField("Cache", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var cacheDict = cacheField.GetValue(cache) as System.Collections.Generic.Dictionary<Type, object>;
            
            cacheDict[testType] = instance1;

            // Verify cache has the instance
            Assert.IsTrue(cacheDict.Count > 0, "Cache should have instances before clear");

            // Act
            cache.Clear();

            // Assert
            Assert.AreEqual(0, cacheDict.Count, "Cache should be empty after clear");
        }

        [TestMethod]
        public void PluginInstanceCache_ThreadSafe_MultipleOperations()
        {
            // Arrange
            var cache = new PluginInstanceCache();
            bool exceptionThrown = false;

            // Act - simulate concurrent operations (hot reload scenario)
            System.Threading.Tasks.Parallel.For(0, 10, i =>
            {
                try
                {
                    cache.Clear();
                    // Simulate getting instances (this would normally happen during execution)
                    System.Threading.Thread.Sleep(10);
                }
                catch
                {
                    exceptionThrown = true;
                }
            });

            // Assert
            Assert.IsFalse(exceptionThrown, "Cache operations should be thread-safe");
        }
    }
}
