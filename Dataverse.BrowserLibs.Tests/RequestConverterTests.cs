using System.Collections.Specialized;
using Dataverse.WebApi2IOrganizationService.Converters;
using Dataverse.WebApi2IOrganizationService.Model;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xrm.Sdk.Messages;

namespace Dataverse.BrowserLibs.Tests
{
    /// <summary>
    /// Tests for RequestConverter - validates conversion of WebAPI requests to IOrganizationService requests
    /// </summary>
    [TestClass]
    public class RequestConverterTests
    {
        private static Helper.Converters Converters;

        [ClassInitialize]
        public static void Initialize(TestContext testContext)
        {
            Converters = Helper.GetConverters(testContext);
        }

        [TestMethod]
        public void Convert_CreateRequest_ConvertsSuccessfully()
        {
            // Arrange
            var body = @"{""name"":""Test Account"",""accountnumber"":""ACC-001""}";
            var request = WebApiRequest.CreateFromLocalPathWithQuery(
                "POST", 
                "/api/data/v9.2/accounts", 
                new NameValueCollection(), 
                body);

            // Act
            var result = Converters.RequestConverter.Convert(request);

            // Assert
            result.Should().NotBeNull();
            result.ConvertedRequest.Should().BeOfType<CreateRequest>();
            var createRequest = (CreateRequest)result.ConvertedRequest;
            createRequest.Target.LogicalName.Should().Be("account");
            createRequest.Target.Attributes.Should().ContainKey("name");
            createRequest.Target["name"].Should().Be("Test Account");
        }

        [TestMethod]
        public void Convert_RetrieveRequest_ConvertsSuccessfully()
        {
            // Arrange
            var accountId = "00000000-0000-0000-0000-000000000001";
            var request = WebApiRequest.CreateFromLocalPathWithQuery(
                "GET",
                $"/api/data/v9.2/accounts({accountId})?$select=name,accountnumber",
                new NameValueCollection());

            // Act
            var result = Converters.RequestConverter.Convert(request);

            // Assert
            result.Should().NotBeNull();
            result.ConvertedRequest.Should().BeOfType<RetrieveRequest>();
            var retrieveRequest = (RetrieveRequest)result.ConvertedRequest;
            retrieveRequest.Target.LogicalName.Should().Be("account");
            retrieveRequest.Target.Id.ToString().Should().Be(accountId);
            retrieveRequest.ColumnSet.Should().NotBeNull();
        }

        [TestMethod]
        public void Convert_UpdateRequest_ConvertsSuccessfully()
        {
            // Arrange
            var accountId = "00000000-0000-0000-0000-000000000001";
            var body = @"{""name"":""Updated Account"",""accountnumber"":""ACC-002""}";
            var request = WebApiRequest.CreateFromLocalPathWithQuery(
                "PATCH",
                $"/api/data/v9.2/accounts({accountId})",
                new NameValueCollection(),
                body);

            // Act
            var result = Converters.RequestConverter.Convert(request);

            // Assert
            result.Should().NotBeNull();
            result.ConvertedRequest.Should().BeOfType<UpdateRequest>();
            var updateRequest = (UpdateRequest)result.ConvertedRequest;
            updateRequest.Target.LogicalName.Should().Be("account");
            updateRequest.Target.Id.ToString().Should().Be(accountId);
            updateRequest.Target["name"].Should().Be("Updated Account");
        }

        [TestMethod]
        public void Convert_DeleteRequest_ConvertsSuccessfully()
        {
            // Arrange
            var accountId = "00000000-0000-0000-0000-000000000001";
            var request = WebApiRequest.CreateFromLocalPathWithQuery(
                "DELETE",
                $"/api/data/v9.2/accounts({accountId})",
                new NameValueCollection());

            // Act
            var result = Converters.RequestConverter.Convert(request);

            // Assert
            result.Should().NotBeNull();
            result.ConvertedRequest.Should().BeOfType<DeleteRequest>();
            var deleteRequest = (DeleteRequest)result.ConvertedRequest;
            deleteRequest.Target.LogicalName.Should().Be("account");
            deleteRequest.Target.Id.ToString().Should().Be(accountId);
        }

        [TestMethod]
        public void Convert_SystemMetadataFunction_ReturnsNull()
        {
            // Arrange - GetClientMetadata is a system function that should not be converted
            var request = WebApiRequest.CreateFromLocalPathWithQuery(
                "GET",
                "/api/data/v9.2/GetClientMetadata()",
                new NameValueCollection());

            // Act
            var result = Converters.RequestConverter.Convert(request);

            // Assert - Should return null to allow pass-through
            result.Should().BeNull();
        }

        [TestMethod]
        public void Convert_RetrieveMetadataChanges_ReturnsNull()
        {
            // Arrange
            var request = WebApiRequest.CreateFromLocalPathWithQuery(
                "POST",
                "/api/data/v9.2/RetrieveMetadataChanges()",
                new NameValueCollection());

            // Act
            var result = Converters.RequestConverter.Convert(request);

            // Assert
            result.Should().BeNull();
        }

        [TestMethod]
        public void Convert_BatchRequest_ConvertsSuccessfully()
        {
            // Arrange
            var batchBody = @"--batch_boundary
Content-Type: multipart/mixed; boundary=changeset_boundary

--changeset_boundary
Content-Type: application/http
Content-Transfer-Encoding: binary
Content-ID: 1

POST /api/data/v9.2/accounts HTTP/1.1
Content-Type: application/json

{""name"":""Batch Account 1""}

--changeset_boundary
Content-Type: application/http
Content-Transfer-Encoding: binary
Content-ID: 2

POST /api/data/v9.2/accounts HTTP/1.1
Content-Type: application/json

{""name"":""Batch Account 2""}

--changeset_boundary--
--batch_boundary--";

            var request = WebApiRequest.CreateFromLocalPathWithQuery(
                "POST",
                "/api/data/v9.2/$batch",
                new NameValueCollection { { "Content-Type", "multipart/mixed; boundary=batch_boundary" } },
                batchBody);

            // Act
            var result = Converters.RequestConverter.Convert(request);

            // Assert
            result.Should().NotBeNull();
            result.ConvertedRequest.Should().BeOfType<ExecuteMultipleRequest>();
            var batchRequest = (ExecuteMultipleRequest)result.ConvertedRequest;
            batchRequest.Requests.Count.Should().Be(2);
        }

        [TestMethod]
        public void Convert_NullRequest_ReturnsNull()
        {
            // Act
            var result = Converters.RequestConverter.Convert(null);

            // Assert
            result.Should().BeNull();
        }

        [TestMethod]
        public void Convert_InvalidODataPath_ReturnsConversionFailure()
        {
            // Arrange - invalid path that can't be parsed
            var request = WebApiRequest.CreateFromLocalPathWithQuery(
                "GET",
                "/api/data/v9.2/invalid[path{here",
                new NameValueCollection());

            // Act
            var result = Converters.RequestConverter.Convert(request);

            // Assert
            result.Should().NotBeNull();
            result.ConvertFailureMessage.Should().NotBeNullOrEmpty();
            result.ConvertedRequest.Should().BeNull();
        }
    }
}
