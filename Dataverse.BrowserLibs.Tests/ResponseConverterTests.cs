using System;
using System.Collections.Specialized;
using System.Text;
using Dataverse.WebApi2IOrganizationService.Converters;
using Dataverse.WebApi2IOrganizationService.Model;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;

namespace Dataverse.BrowserLibs.Tests
{
    /// <summary>
    /// Tests for ResponseConverter - validates conversion of IOrganizationService responses to WebAPI responses
    /// </summary>
    [TestClass]
    public class ResponseConverterTests
    {
        private static Helper.Converters Converters;

        [ClassInitialize]
        public static void Initialize(TestContext testContext)
        {
            Converters = Helper.GetConverters(testContext);
        }

        [TestMethod]
        public void Convert_CreateResponse_ReturnsCorrectStatusCode()
        {
            // Arrange
            var entityId = Guid.NewGuid();
            var createRequest = new CreateRequest
            {
                Target = new Entity("account")
                {
                    ["name"] = "Test Account"
                }
            };
            var createResponse = new CreateResponse
            {
                ["id"] = entityId
            };
            var conversionResult = new RequestConversionResult
            {
                ConvertedRequest = createRequest,
                SrcRequest = WebApiRequest.CreateFromLocalPathWithQuery(
                    "POST",
                    "/api/data/v9.2/accounts",
                    new NameValueCollection(),
                    @"{""name"":""Test Account""}")
            };

            // Act
            var result = Converters.ResponseConverter.Convert(conversionResult, createResponse);

            // Assert
            result.Should().NotBeNull();
            result.StatusCode.Should().Be(204); // Create returns 204 No Content
            result.Headers.Should().ContainKey("OData-EntityId");
        }

        [TestMethod]
        public void Convert_UpdateResponse_ReturnsNoContent()
        {
            // Arrange
            var accountId = Guid.NewGuid();
            var updateRequest = new UpdateRequest
            {
                Target = new Entity("account", accountId)
                {
                    ["name"] = "Updated Account"
                }
            };
            var updateResponse = new UpdateResponse();
            var conversionResult = new RequestConversionResult
            {
                ConvertedRequest = updateRequest,
                SrcRequest = WebApiRequest.CreateFromLocalPathWithQuery(
                    "PATCH",
                    $"/api/data/v9.2/accounts({accountId})",
                    new NameValueCollection(),
                    @"{""name"":""Updated Account""}")
            };

            // Act
            var result = Converters.ResponseConverter.Convert(conversionResult, updateResponse);

            // Assert
            result.Should().NotBeNull();
            result.StatusCode.Should().Be(204); // Update returns 204 No Content
        }

        [TestMethod]
        public void Convert_DeleteResponse_ReturnsNoContent()
        {
            // Arrange
            var deleteResponse = new DeleteResponse();
            var accountId = Guid.NewGuid();
            var deleteRequest = new DeleteRequest
            {
                Target = new EntityReference("account", accountId)
            };
            var conversionResult = new RequestConversionResult
            {
                ConvertedRequest = deleteRequest,
                SrcRequest = WebApiRequest.CreateFromLocalPathWithQuery(
                    "DELETE",
                    $"/api/data/v9.2/accounts({accountId})",
                    new NameValueCollection())
            };

            // Act
            var result = Converters.ResponseConverter.Convert(conversionResult, deleteResponse);

            // Assert
            result.Should().NotBeNull();
            result.StatusCode.Should().Be(204); // Delete returns 204 No Content
        }

        [TestMethod]
        public void Convert_Exception_ReturnsErrorResponse()
        {
            // Arrange
            var exception = new InvalidOperationException("Test error message");

            // Act
            var result = Converters.ResponseConverter.Convert(exception);

            // Assert
            result.Should().NotBeNull();
            result.StatusCode.Should().Be(400); // Error responses return 400
            result.Body.Should().NotBeNull();
            var bodyText = Encoding.UTF8.GetString(result.Body);
            bodyText.Should().Contain("error");
            bodyText.Should().Contain("Test error message");
        }

        [TestMethod]
        public void Convert_RetrieveResponse_ReturnsJsonBody()
        {
            // Arrange
            var accountId = Guid.NewGuid();
            var entity = new Entity("account", accountId)
            {
                ["name"] = "Test Account",
                ["accountnumber"] = "ACC-001"
            };
            var retrieveResponse = new RetrieveResponse
            {
                ["Entity"] = entity
            };
            var retrieveRequest = new RetrieveRequest
            {
                Target = new EntityReference("account", accountId),
                ColumnSet = new Microsoft.Xrm.Sdk.Query.ColumnSet("name", "accountnumber")
            };
            var conversionResult = new RequestConversionResult
            {
                ConvertedRequest = retrieveRequest,
                SrcRequest = WebApiRequest.CreateFromLocalPathWithQuery(
                    "GET",
                    $"/api/data/v9.2/accounts({accountId})?$select=name,accountnumber",
                    new NameValueCollection())
            };

            // Act
            var result = Converters.ResponseConverter.Convert(conversionResult, retrieveResponse);

            // Assert
            result.Should().NotBeNull();
            result.StatusCode.Should().Be(200);
            result.Body.Should().NotBeNull();
            var bodyText = Encoding.UTF8.GetString(result.Body);
            bodyText.Should().Contain("Test Account");
            bodyText.Should().Contain("ACC-001");
        }

        [TestMethod]
        public void Convert_ExecuteMultipleResponse_ReturnsBatchResponse()
        {
            // Arrange
            var executeMultipleResponse = new ExecuteMultipleResponse();
            executeMultipleResponse.Results["Responses"] = new ExecuteMultipleResponseItemCollection
            {
                new ExecuteMultipleResponseItem
                {
                    Response = new CreateResponse { ["id"] = Guid.NewGuid() }
                },
                new ExecuteMultipleResponseItem
                {
                    Response = new CreateResponse { ["id"] = Guid.NewGuid() }
                }
            };

            var conversionResult = new RequestConversionResult
            {
                ConvertedRequest = new ExecuteMultipleRequest(),
                SrcRequest = WebApiRequest.CreateFromLocalPathWithQuery(
                    "POST",
                    "/api/data/v9.2/$batch",
                    new NameValueCollection { { "Content-Type", "multipart/mixed; boundary=batch" } },
                    "batch content")
            };
            
            // Add inner conversion results
            conversionResult.CustomData["InnerConversions"] = new System.Collections.Generic.List<RequestConversionResult>
            {
                new RequestConversionResult
                {
                    ConvertedRequest = new CreateRequest { Target = new Entity("account") },
                    SrcRequest = WebApiRequest.CreateFromLocalPathWithQuery("POST", "/api/data/v9.2/accounts", new NameValueCollection(), "{}")
                },
                new RequestConversionResult
                {
                    ConvertedRequest = new CreateRequest { Target = new Entity("account") },
                    SrcRequest = WebApiRequest.CreateFromLocalPathWithQuery("POST", "/api/data/v9.2/accounts", new NameValueCollection(), "{}")
                }
            };

            // Act
            var result = Converters.ResponseConverter.Convert(conversionResult, executeMultipleResponse);

            // Assert
            result.Should().NotBeNull();
            result.StatusCode.Should().Be(200);
            result.Headers["Content-Type"].Should().Contain("multipart/mixed");
            result.Body.Should().NotBeNull();
            var bodyText = Encoding.UTF8.GetString(result.Body);
            bodyText.Should().Contain("batchresponse");
        }

        [TestMethod]
        public void Convert_CustomApiResponse_ConvertsSuccessfully()
        {
            // Arrange
            var customResponse = new OrganizationResponse();
            customResponse.Results["OutputParam1"] = "Test Value";
            customResponse.Results["OutputParam2"] = 42;

            var conversionResult = new RequestConversionResult
            {
                ConvertedRequest = new OrganizationRequest(),
                SrcRequest = WebApiRequest.CreateFromLocalPathWithQuery(
                    "POST",
                    "/api/data/v9.2/CustomApiName",
                    new NameValueCollection(),
                    @"{""InputParam"":""value""}")
            };

            // Act
            var result = Converters.ResponseConverter.Convert(conversionResult, customResponse);

            // Assert
            result.Should().NotBeNull();
            result.StatusCode.Should().Be(200);
            result.Body.Should().NotBeNull();
        }
    }
}
