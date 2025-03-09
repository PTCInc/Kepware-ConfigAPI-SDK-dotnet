using Kepware.Api.Model.Admin;
using Kepware.Api.Serializer;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Contrib.HttpClient;
using Shouldly;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;
using System.Net.Http.Json;

namespace Kepware.Api.Test.ApiClient
{
    public class UaEndpointTests : TestApiClientBase
    {
        private const string ENDPOINT_UA = "/config/v1/admin/ua_endpoints";

        [Fact]
        public async Task GetUaEndpointAsync_ShouldReturnUaEndpoint_WhenApiRespondsSuccessfully()
        {
            // Arrange
            var uaEndpoint = CreateTestUaEndpoint();
            _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, $"{TEST_ENDPOINT}{ENDPOINT_UA}/{uaEndpoint.Name}")
                .ReturnsResponse(HttpStatusCode.OK, JsonSerializer.Serialize(uaEndpoint), "application/json");

            // Act
            var result = await _kepwareApiClient.Admin.GetUaEndpointAsync(uaEndpoint.Name);

            // Assert
            result.ShouldNotBeNull();
            result.Name.ShouldBe(uaEndpoint.Name);
            result.Port.ShouldBe(uaEndpoint.Port);
        }

        [Fact]
        public async Task GetUaEndpointAsync_ShouldReturnNull_WhenApiReturnsNotFound()
        {
            // Arrange
            var endpointName = "NonExistentEndpoint";
            _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, $"{TEST_ENDPOINT}{ENDPOINT_UA}/{endpointName}")
                .ReturnsResponse(HttpStatusCode.NotFound, "Not Found");

            // Act
            var result = await _kepwareApiClient.Admin.GetUaEndpointAsync(endpointName);

            // Assert
            result.ShouldBeNull();
            _loggerMockGeneric.Verify(logger => 
                logger.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => true),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)), 
                Times.Once);
        }

        [Fact]
        public async Task CreateOrUpdateUaEndpointAsync_ShouldCreateUaEndpoint_WhenItDoesNotExist()
        {
            // Arrange
            var uaEndpoint = CreateTestUaEndpoint();
            _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, $"{TEST_ENDPOINT}{ENDPOINT_UA}/{uaEndpoint.Name}")
                .ReturnsResponse(HttpStatusCode.NotFound, "Not Found");

            _httpMessageHandlerMock.SetupRequest(HttpMethod.Post, $"{TEST_ENDPOINT}{ENDPOINT_UA}")
                .ReturnsResponse(HttpStatusCode.Created);

            // Act
            var result = await _kepwareApiClient.Admin.CreateOrUpdateUaEndpointAsync(uaEndpoint);

            // Assert
            result.ShouldBeTrue();
            _httpMessageHandlerMock.VerifyRequest(HttpMethod.Get, $"{TEST_ENDPOINT}{ENDPOINT_UA}/{uaEndpoint.Name}", Times.Once());
            _httpMessageHandlerMock.VerifyRequest(HttpMethod.Post, $"{TEST_ENDPOINT}{ENDPOINT_UA}", Times.Once());
        }

        [Fact]
        public async Task CreateOrUpdateUaEndpointAsync_ShouldUpdateUaEndpoint_WhenItExists()
        {
            // Arrange
            var uaEndpoint = CreateTestUaEndpoint();
            var currentEndpoint = CreateTestUaEndpoint();
            currentEndpoint.Port = 4840;

            _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, $"{TEST_ENDPOINT}{ENDPOINT_UA}/{uaEndpoint.Name}")
                .ReturnsResponse(HttpStatusCode.OK, JsonSerializer.Serialize(currentEndpoint), "application/json");

            _httpMessageHandlerMock.SetupRequest(HttpMethod.Put, $"{TEST_ENDPOINT}{ENDPOINT_UA}/{uaEndpoint.Name}")
                .ReturnsResponse(HttpStatusCode.OK);

            // Act
            var result = await _kepwareApiClient.Admin.CreateOrUpdateUaEndpointAsync(uaEndpoint);

            // Assert
            result.ShouldBeTrue();
            _httpMessageHandlerMock.VerifyRequest(HttpMethod.Get, $"{TEST_ENDPOINT}{ENDPOINT_UA}/{uaEndpoint.Name}", Times.Once());
            _httpMessageHandlerMock.VerifyRequest(HttpMethod.Put, $"{TEST_ENDPOINT}{ENDPOINT_UA}/{uaEndpoint.Name}", Times.Once());
        }

        [Fact]
        public async Task DeleteUaEndpointAsync_ShouldReturnTrue_WhenDeleteSuccessful()
        {
            // Arrange
            var endpointName = "TestEndpoint";
            _httpMessageHandlerMock.SetupRequest(HttpMethod.Delete, $"{TEST_ENDPOINT}{ENDPOINT_UA}/{endpointName}")
                .ReturnsResponse(HttpStatusCode.OK);

            // Act
            var result = await _kepwareApiClient.Admin.DeleteUaEndpointAsync(endpointName);

            // Assert
            result.ShouldBeTrue();
            _httpMessageHandlerMock.VerifyRequest(HttpMethod.Delete, $"{TEST_ENDPOINT}{ENDPOINT_UA}/{endpointName}", Times.Once());
        }

        [Fact]
        public async Task DeleteUaEndpointAsync_ShouldReturnFalse_WhenDeleteFails()
        {
            // Arrange
            var endpointName = "TestEndpoint";
            _httpMessageHandlerMock.SetupRequest(HttpMethod.Delete, $"{TEST_ENDPOINT}{ENDPOINT_UA}/{endpointName}")
                .ReturnsResponse(HttpStatusCode.InternalServerError, "Internal Server Error");

            // Act
            var result = await _kepwareApiClient.Admin.DeleteUaEndpointAsync(endpointName);

            // Assert
            result.ShouldBeFalse();
            _httpMessageHandlerMock.VerifyRequest(HttpMethod.Delete, $"{TEST_ENDPOINT}{ENDPOINT_UA}/{endpointName}", Times.Once());
            _loggerMockGeneric.Verify(logger => 
                logger.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => true),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)), 
                Times.Once);
        }

        [Fact]
        public async Task GetUaEndpointsAsync_ShouldReturnUaEndpointCollection_WhenApiRespondsSuccessfully()
        {
            // Arrange
            var uaEndpoints = new UaEndpointCollection
            {
                CreateTestUaEndpoint("TestEndpoint1"),
                CreateTestUaEndpoint("TestEndpoint2")
            };
            _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, $"{TEST_ENDPOINT}{ENDPOINT_UA}")
                .ReturnsResponse(HttpStatusCode.OK, JsonSerializer.Serialize(uaEndpoints), "application/json");

            // Act
            var result = await _kepwareApiClient.Admin.GetUaEndpointListAsync();

            // Assert
            result.ShouldNotBeNull();
            result.Count.ShouldBe(2);
            result[0].Name.ShouldBe(uaEndpoints[0].Name);
            result[1].Name.ShouldBe(uaEndpoints[1].Name);
        }

        [Fact]
        public async Task GetUaEndpointsAsync_ShouldReturnNull_WhenApiReturnsError()
        {
            // Arrange
            _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, $"{TEST_ENDPOINT}{ENDPOINT_UA}")
                .ReturnsResponse(HttpStatusCode.InternalServerError, "Internal Server Error");

            // Act
            var result = await _kepwareApiClient.Admin.GetUaEndpointListAsync();

            // Assert
            result.ShouldBeNull();
            _loggerMockGeneric.Verify(logger => 
                logger.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => true),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)), 
                Times.Once);
        }

        private static UaEndpoint CreateTestUaEndpoint(string endpointName= "TestEndpoint")
        {
            return new UaEndpoint
            {
                Name = endpointName,
                Enabled = true,
                Adapter = "Ethernet",
                Port = 49320,
                SecurityNone = false,
                SecurityBasic256Sha256 = UaEndpointSecurityMode.SignAndEncrypt
            };
        }
    }
}
