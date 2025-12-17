using Microsoft.VisualStudio.TestTools.UnitTesting;
using Dataverse.Browser.ViewModels;
using Dataverse.Browser.Services;
using System;
using System.Linq;

namespace Dataverse.BrowserLibs.Tests
{
    [TestClass]
    public class ViewModelTests
    {
        [TestMethod]
        public void BrowserViewModel_Constructor_InitializesSuccessfully()
        {
            // Act
            var viewModel = new BrowserViewModel();

            // Assert
            Assert.IsNotNull(viewModel);
            Assert.IsNotNull(viewModel.RequestHistory);
            Assert.AreEqual(0, viewModel.RequestHistory.Count);
            Assert.IsNotNull(viewModel.ClearHistoryCommand);
            Assert.IsNotNull(viewModel.TogglePluginsCommand);
            Assert.IsNotNull(viewModel.AttachDebuggerCommand);
        }

        [TestMethod]
        public void BrowserViewModel_AddRequest_AddsToHistory()
        {
            // Arrange
            var viewModel = new BrowserViewModel();

            // Act
            viewModel.AddRequest("GET", "/api/data/v9.2/accounts", 200, TimeSpan.FromMilliseconds(150));

            // Assert
            Assert.AreEqual(1, viewModel.RequestHistory.Count);
            var request = viewModel.RequestHistory.First();
            Assert.AreEqual("GET", request.Method);
            Assert.AreEqual("/api/data/v9.2/accounts", request.Url);
            Assert.AreEqual(200, request.StatusCode);
        }

        [TestMethod]
        public void BrowserViewModel_AddRequest_LimitsHistoryTo100Items()
        {
            // Arrange
            var viewModel = new BrowserViewModel();

            // Act - Add 150 requests
            for (int i = 0; i < 150; i++)
            {
                viewModel.AddRequest("GET", $"/api/data/v9.2/accounts({i})", 200, TimeSpan.FromMilliseconds(100));
            }

            // Assert
            Assert.AreEqual(100, viewModel.RequestHistory.Count);
        }

        [TestMethod]
        public void EnvironmentViewModel_Constructor_InitializesSuccessfully()
        {
            // Act
            var viewModel = new EnvironmentViewModel();

            // Assert
            Assert.IsNotNull(viewModel);
            Assert.IsNotNull(viewModel.Environments);
            Assert.AreEqual(0, viewModel.Environments.Count);
            Assert.IsNotNull(viewModel.ConnectCommand);
            Assert.IsNotNull(viewModel.AddEnvironmentCommand);
            Assert.IsNotNull(viewModel.EditEnvironmentCommand);
            Assert.IsNotNull(viewModel.DeleteEnvironmentCommand);
        }

        [TestMethod]
        public void RequestItemViewModel_StatusCodeColor_ReturnsGreenFor200()
        {
            // Arrange
            var viewModel = new RequestItemViewModel
            {
                StatusCode = 200
            };

            // Act
            var color = viewModel.StatusCodeColor;

            // Assert
            Assert.AreEqual(System.Drawing.Color.Green, color);
        }

        [TestMethod]
        public void RequestItemViewModel_StatusCodeColor_ReturnsRedFor404()
        {
            // Arrange
            var viewModel = new RequestItemViewModel
            {
                StatusCode = 404
            };

            // Act
            var color = viewModel.StatusCodeColor;

            // Assert
            Assert.AreEqual(System.Drawing.Color.Red, color);
        }

        [TestMethod]
        public void EnvironmentService_LoadEnvironments_ReturnsArray()
        {
            // Arrange
            var service = new EnvironmentService();

            // Act
            var environments = service.LoadEnvironments();

            // Assert
            Assert.IsNotNull(environments);
            Assert.IsInstanceOfType(environments, typeof(Configuration.EnvironnementConfiguration[]));
        }

        [TestMethod]
        public void PluginService_IsDebuggerAttached_ReturnsBoolean()
        {
            // Arrange
            var service = new PluginService();

            // Act
            var isAttached = service.IsDebuggerAttached;

            // Assert
            Assert.IsInstanceOfType(isAttached, typeof(bool));
        }
    }
}
