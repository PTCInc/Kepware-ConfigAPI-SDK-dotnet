using Kepware.Api.Model;
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
    public class ProjectPropertiesTests : TestApiClientBase
    {
        private const string ENDPOINT_PROJECT = "/config/v1/project";

        #region GetProjectPropertiesAsync Tests

        [Fact]
        public async Task GetProjectPropertiesAsync_ShouldReturnProjectProperties_WhenApiRespondsSuccessfully()
        {
            // Arrange
            var projectPropSettings = CreateTestProjectProperties();
            _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, $"{TEST_ENDPOINT}{ENDPOINT_PROJECT}")
                .ReturnsResponse(HttpStatusCode.OK, JsonSerializer.Serialize(projectPropSettings), "application/json");

            // Act
            var result = await _kepwareApiClient.GetProjectPropertiesAsync();

            // Assert
            result.ShouldNotBeNull();
            result.ProjectProperties.Title.ShouldBe("Default Project");
            result.ProjectProperties.OpcUaEnableDiagnostics.ShouldNotBeNull();
            result.ProjectProperties.OpcUaEnableDiagnostics.Value.ShouldBeFalse();
            result.ProjectProperties.ThingWorxMaxDatastoreSize.ShouldBe(ThingWorxDataStoreMaxSize.Size2GB);
            result.ProjectProperties.EnableOpcDa1.ShouldNotBeNull();
            result.ProjectProperties.EnableOpcDa1.Value.ShouldBeTrue();
        }

        [Fact]
        public async Task GetProjectPropertiesAsync_ShouldReturnNull_WhenApiReturnsError()
        {
            // Arrange
            _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, $"{TEST_ENDPOINT}{ENDPOINT_PROJECT}")
                .ReturnsResponse(HttpStatusCode.InternalServerError, "Internal Server Error");

            // Act
            var result = await _kepwareApiClient.GetProjectPropertiesAsync();

            // Assert
            result.ShouldBeNull();
            _loggerMock.Verify(logger =>
                logger.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => true),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
                Times.Once);
        }

        [Fact]
        public async Task GetProjectPropertiesAsync_ShouldReturnNull_OnHttpRequestException()
        {
            // Arrange
            _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, $"{TEST_ENDPOINT}{ENDPOINT_PROJECT}")
                .ThrowsAsync(new HttpRequestException("Network error"));

            // Act
            var result = await _kepwareApiClient.GetProjectPropertiesAsync();

            // Assert
            result.ShouldBeNull();
            _loggerMock.Verify(logger =>
                logger.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => true),
                    It.IsAny<HttpRequestException>(),
                    It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
                Times.Once);
        }

        [Fact]
        public async Task GetProjectPropertiesAsync_ShouldReturnNull_WhenApiReturnsNotFound()
        {
            // Arrange
            _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, $"{TEST_ENDPOINT}{ENDPOINT_PROJECT}")
                .ReturnsResponse(HttpStatusCode.NotFound, "Not Found");

            // Act
            var result = await _kepwareApiClient.GetProjectPropertiesAsync();

            // Assert
            result.ShouldBeNull();
            _loggerMock.Verify(logger =>
                logger.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => true),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
                Times.Once);
        }

        [Fact]
        public async Task GetProjectPropertiesAsync_ShouldReturnNull_WhenApiReturnsUnauthorized()
        {
            // Arrange
            _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, $"{TEST_ENDPOINT}{ENDPOINT_PROJECT}")
                .ReturnsResponse(HttpStatusCode.Unauthorized, "Unauthorized");

            // Act
            var result = await _kepwareApiClient.GetProjectPropertiesAsync();

            // Assert
            result.ShouldBeNull();
            _loggerMock.Verify(logger =>
                logger.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => true),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
                Times.Once);
        }

        #endregion

        #region SetProjectPropertiesAsync Tests

        [Fact]
        public async Task SetProjectPropertiesAsync_ShouldReturnTrue_WhenUpdateSuccessful()
        {
            // Arrange
            var currentSettings = CreateTestProjectProperties();
            var newSettings = CreateTestProjectProperties();
            newSettings.ProjectProperties.OpcUaEnableDiagnostics = true;
            newSettings.ProjectProperties.OpcDaEnableDiagnostics = true;

            _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, $"{TEST_ENDPOINT}{ENDPOINT_PROJECT}")
                .ReturnsResponse(HttpStatusCode.OK, JsonSerializer.Serialize(currentSettings), "application/json");

            _httpMessageHandlerMock.SetupRequest(HttpMethod.Put, $"{TEST_ENDPOINT}{ENDPOINT_PROJECT}")
                .ReturnsResponse(HttpStatusCode.OK);

            // Act
            var result = await _kepwareApiClient.SetProjectPropertiesAsync(newSettings);

            // Assert
            result.ShouldBeTrue();
            _httpMessageHandlerMock.VerifyRequest(HttpMethod.Get, $"{TEST_ENDPOINT}{ENDPOINT_PROJECT}", Times.Once());
            _httpMessageHandlerMock.VerifyRequest(HttpMethod.Put, $"{TEST_ENDPOINT}{ENDPOINT_PROJECT}", Times.Once());
        }

        [Fact]
        public async Task SetProjectPropertiesAsync_ShouldThrowInvalidOperationException_WhenGetCurrentSettingsFails()
        {
            // Arrange
            var newSettings = CreateTestProjectProperties();
            _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, $"{TEST_ENDPOINT}{ENDPOINT_PROJECT}")
                .ReturnsResponse(HttpStatusCode.InternalServerError, "Internal Server Error");

            // Act & Assert
            await Should.ThrowAsync<InvalidOperationException>(async () =>
                await _kepwareApiClient.SetProjectPropertiesAsync(newSettings));

            _httpMessageHandlerMock.VerifyRequest(HttpMethod.Get, $"{TEST_ENDPOINT}{ENDPOINT_PROJECT}", Times.Once());
            _httpMessageHandlerMock.VerifyRequest(HttpMethod.Put, $"{TEST_ENDPOINT}{ENDPOINT_PROJECT}", Times.Never());
        }

        [Fact]
        public async Task SetProjectPropertiesAsync_ShouldReturnFalse_WhenUpdateFails()
        {
            // Arrange
            var currentSettings = CreateTestProjectProperties();
            var newSettings = CreateTestProjectProperties();
            newSettings.ProjectProperties.OpcDaEnableDiagnostics = true;

            _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, $"{TEST_ENDPOINT}{ENDPOINT_PROJECT}")
                .ReturnsResponse(HttpStatusCode.OK, JsonSerializer.Serialize(currentSettings), "application/json");

            _httpMessageHandlerMock.SetupRequest(HttpMethod.Put, $"{TEST_ENDPOINT}{ENDPOINT_PROJECT}")
                .ReturnsResponse(HttpStatusCode.BadRequest, "Invalid setting value");

            // Act
            var result = await _kepwareApiClient.SetProjectPropertiesAsync(newSettings);

            // Assert
            result.ShouldBeFalse();
            _httpMessageHandlerMock.VerifyRequest(HttpMethod.Get, $"{TEST_ENDPOINT}{ENDPOINT_PROJECT}", Times.Once());
            _httpMessageHandlerMock.VerifyRequest(HttpMethod.Put, $"{TEST_ENDPOINT}{ENDPOINT_PROJECT}", Times.Once());
            _loggerMock.Verify(logger =>
                logger.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => true),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
                Times.Once);
        }

        [Fact]
        public async Task SetProjectPropertiesAsync_ShouldReturnFalse_OnHttpRequestException()
        {
            // Arrange
            var currentSettings = CreateTestProjectProperties();
            var newSettings = CreateTestProjectProperties();
            newSettings.ProjectProperties.OpcDaEnableDiagnostics = true;

            _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, $"{TEST_ENDPOINT}{ENDPOINT_PROJECT}")
                .ReturnsResponse(HttpStatusCode.OK, JsonSerializer.Serialize(currentSettings), "application/json");

            _httpMessageHandlerMock.SetupRequest(HttpMethod.Put, $"{TEST_ENDPOINT}{ENDPOINT_PROJECT}")
                .ThrowsAsync(new HttpRequestException("Network error during update"));

            // Act
            var result = await _kepwareApiClient.SetProjectPropertiesAsync(newSettings);

            // Assert
            result.ShouldBeFalse();
            _httpMessageHandlerMock.VerifyRequest(HttpMethod.Get, $"{TEST_ENDPOINT}{ENDPOINT_PROJECT}", Times.Once());
            _httpMessageHandlerMock.VerifyRequest(HttpMethod.Put, $"{TEST_ENDPOINT}{ENDPOINT_PROJECT}", Times.Once());
            _loggerMock.Verify(logger =>
                logger.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => true),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
                Times.Once);
        }

        [Fact]
        public async Task SetProjectPropertiesAsync_ShouldReturnFalse_WhenApiReturnsUnauthorized()
        {
            // Arrange
            var currentSettings = CreateTestProjectProperties();
            var newSettings = CreateTestProjectProperties();
            newSettings.ProjectProperties.OpcDaEnableDiagnostics = true;

            _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, $"{TEST_ENDPOINT}{ENDPOINT_PROJECT}")
                .ReturnsResponse(HttpStatusCode.OK, JsonSerializer.Serialize(currentSettings), "application/json");

            _httpMessageHandlerMock.SetupRequest(HttpMethod.Put, $"{TEST_ENDPOINT}{ENDPOINT_PROJECT}")
                .ReturnsResponse(HttpStatusCode.Unauthorized, "Unauthorized");

            // Act
            var result = await _kepwareApiClient.SetProjectPropertiesAsync(newSettings);

            // Assert
            result.ShouldBeFalse();
            _httpMessageHandlerMock.VerifyRequest(HttpMethod.Get, $"{TEST_ENDPOINT}{ENDPOINT_PROJECT}", Times.Once());
            _httpMessageHandlerMock.VerifyRequest(HttpMethod.Put, $"{TEST_ENDPOINT}{ENDPOINT_PROJECT}", Times.Once());
            _loggerMock.Verify(logger =>
                logger.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => true),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
                Times.Once);
        }

        [Fact]
        public async Task SetProjectPropertiesAsync_ShouldReturnFalse_WhenApiReturnsForbidden()
        {
            // Arrange
            var currentSettings = CreateTestProjectProperties();
            var newSettings = CreateTestProjectProperties();
            newSettings.ProjectProperties.OpcDaEnableDiagnostics = true;

            _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, $"{TEST_ENDPOINT}{ENDPOINT_PROJECT}")
                .ReturnsResponse(HttpStatusCode.OK, JsonSerializer.Serialize(currentSettings), "application/json");

            _httpMessageHandlerMock.SetupRequest(HttpMethod.Put, $"{TEST_ENDPOINT}{ENDPOINT_PROJECT}")
                .ReturnsResponse(HttpStatusCode.Forbidden, "Forbidden");

            // Act
            var result = await _kepwareApiClient.SetProjectPropertiesAsync(newSettings);

            // Assert
            result.ShouldBeFalse();
            _httpMessageHandlerMock.VerifyRequest(HttpMethod.Get, $"{TEST_ENDPOINT}{ENDPOINT_PROJECT}", Times.Once());
            _httpMessageHandlerMock.VerifyRequest(HttpMethod.Put, $"{TEST_ENDPOINT}{ENDPOINT_PROJECT}", Times.Once());
            _loggerMock.Verify(logger =>
                logger.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => true),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
                Times.Once);
        }

#endregion

        #region Helper Methods
        private static Project CreateTestProjectProperties()
        {
            var project = new Project();
            var settings = new ProjectProperties(project)
            {
                ProjectId = 1291004473,
                Title = "Default Project",
                EnableOpcDa1 = true,
                EnableOpcDa2 = true,
                EnableOpcDa3 = true,
                OpcDaShowHintsOnBrowse = false,
                OpcDaShowTagPropertiesOnBrowse = false,
                OpcDaShutdownWaitSec = 15,
                OpcDaSyncRequestTimeoutSec = 15,
                OpcDaEnableDiagnostics = false,
                OpcDaMaxConnections = 512,
                OpcDaMaxTagGroups = 2000,
                OpcDaRejectUnsupportedLangId = true,
                OpcDaIgnoreDeadbandOnCache = false,
                OpcDaIgnoreBrowseFilter = false,
                OpcDa205aDataTypeSupport = true,
                OpcDaSyncReadErrorOnBadQuality = false,
                OpcDaReturnInitialUpdatesInSingleCallback = false,
                OpcDaRespectClientLangId = true,
                OpcDaCompliantDataChange = true,
                OpcDaIgnoreGroupUpdateRate = false,
                EnableFastDdeSuiteLink = false,
                FastDdeSuiteLinkApplicationName = "server_runtime",
                FastDdeSuiteLinkClientUpdateIntervalMs = 100,
                EnableDde = false,
                DdeServiceName = "ptcdde",
                EnableDdeAdvancedDde = true,
                EnableDdeXlTable = true,
                EnableDdeCfText = true,
                DdeClientUpdateIntervalMs = 100,
                DdeRequestTimeoutSec = 15,
                EnableOpcUa = true,
                OpcUaEnableDiagnostics = false,
                OpcUaAllowAnonymousLogin = false,
                OpcUaMaxConnections = 128,
                OpcUaMinSessionTimeoutSec = 15,
                OpcUaMaxSessionTimeoutSec = 60,
                OpcUaTagCacheTimeoutSec = 5,
                OpcUaShowTagPropertiesOnBrowse = false,
                OpcUaShowHintsOnBrowse = false,
                OpcUaMaxDataQueueSize = 2,
                OpcUaMaxRetransmitQueueSize = 10,
                OpcUaMaxNotificationPerPublish = 65536,
                EnableAeServer = false,
                EnableSimpleEvents = true,
                MaxSubscriptionBufferSize = 100,
                MinSubscriptionBufferTimeMs = 1000,
                MinKeepAliveTimeMs = 1000,
                EnableHda = false,
                OpcHdaEnableDiagnostics = false,
                EnableThingWorx = false,
                ThingWorxHostName = "localhost",
                ThingWorxPort = 443,
                ThingWorxResourceUrl = "/Thingworx/WS",
                ThingWorxApplicationKey = "defaultKey",
                ThingWorxTrustSelfSignedCertificate = false,
                ThingWorxTrustAllCertificates = false,
                ThingWorxDisableEncryption = false,
                ThingWorxMaxThingCount = 500,
                ThingWorxThingName = "DefaultThing",
                ThingWorxPublishFloorMs = 1000,
                ThingWorxLoggingEnabled = false,
                ThingWorxLoggingLevel = ThingwWorxLoggingLevel.Warning,
                ThingWorxLogVerbose = false,
                ThingWorxStoreAndForwardEnabled = false,
                ThingWorxStoreAndForwardStoragePath = "C:\\StoreAndForward",
                ThingWorxMaxDatastoreSize = ThingWorxDataStoreMaxSize.Size2GB,
                ThingWorxStoreAndForwardMode = ThingWorxForwardMode.Active,
                ThingWorxDelayBetweenPublishes = 0,
                ThingWorxMaxUpdatesPerPublish = 25000,
                ThingWorxProxyEnabled = false,
                ThingWorxProxyHost = "localhost",
                ThingWorxProxyPort = 3128,
                ThingWorxProxyUsername = "",
                ThingWorxProxyPassword = ""
            };

            return project;
        }

        #endregion
    }
}
