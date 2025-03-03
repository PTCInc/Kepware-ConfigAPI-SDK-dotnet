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
using Kepware.Api.Model.Admin;

namespace Kepware.Api.Test.ApiClient
{
    public class AdminModelTests : TestApiClientBase
    {
        private const string ENDPOINT_ADMIN = "/config/v1/admin";

        #region GetAdminSettingsAsync Tests

        [Fact]
        public async Task GetAdminSettingsAsync_ShouldReturnAdminSettings_WhenApiRespondsSuccessfully()
        {
            // Arrange
            var adminSettings = CreateTestAdminSettings();
            _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, $"{TEST_ENDPOINT}{ENDPOINT_ADMIN}")
                .ReturnsResponse(HttpStatusCode.OK, JsonSerializer.Serialize(adminSettings), "application/json");

            // Act
            var result = await _kepwareApiClient.GetAdminSettingsAsync();

            // Assert
            result.ShouldNotBeNull();
            result.EventLogConnectionPort.ShouldBe(39461);
            result.EventLogMaxRecords.ShouldBe(25000);
            result.LicenseServer.ShouldNotBeNull();
            result.LicenseServer.Name.ShouldBe("licenseserver.example.com");
            result.LicenseServer.Enable.ShouldNotBeNull();
            result.LicenseServer.Enable.Value.ShouldBeTrue();
        }

        [Fact]
        public async Task GetAdminSettingsAsync_ShouldReturnNull_WhenApiReturnsError()
        {
            // Arrange
            _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, $"{TEST_ENDPOINT}{ENDPOINT_ADMIN}")
                .ReturnsResponse(HttpStatusCode.InternalServerError, "Internal Server Error");

            // Act
            var result = await _kepwareApiClient.GetAdminSettingsAsync();

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
        public async Task GetAdminSettingsAsync_ShouldReturnNull_OnHttpRequestException()
        {
            // Arrange
            _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, $"{TEST_ENDPOINT}{ENDPOINT_ADMIN}")
                .ThrowsAsync(new HttpRequestException("Network error"));

            // Act
            var result = await _kepwareApiClient.GetAdminSettingsAsync();

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
        public async Task GetAdminSettingsAsync_ShouldReturnNull_WhenApiReturnsNotFound()
        {
            // Arrange
            _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, $"{TEST_ENDPOINT}{ENDPOINT_ADMIN}")
                .ReturnsResponse(HttpStatusCode.NotFound, "Not Found");

            // Act
            var result = await _kepwareApiClient.GetAdminSettingsAsync();

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
        public async Task GetAdminSettingsAsync_ShouldReturnNull_WhenApiReturnsUnauthorized()
        {
            // Arrange
            _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, $"{TEST_ENDPOINT}{ENDPOINT_ADMIN}")
                .ReturnsResponse(HttpStatusCode.Unauthorized, "Unauthorized");

            // Act
            var result = await _kepwareApiClient.GetAdminSettingsAsync();

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

        #region SetAdminSettingsAsync Tests

        [Fact]
        public async Task SetAdminSettingsAsync_ShouldReturnTrue_WhenUpdateSuccessful()
        {
            // Arrange
            var currentSettings = CreateTestAdminSettings();
            var newSettings = CreateTestAdminSettings();
            newSettings.EventLogMaxRecords = 30000;
            newSettings.LicenseServer.Port = 8901;

            _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, $"{TEST_ENDPOINT}{ENDPOINT_ADMIN}")
                .ReturnsResponse(HttpStatusCode.OK, JsonSerializer.Serialize(currentSettings), "application/json");

            _httpMessageHandlerMock.SetupRequest(HttpMethod.Put, $"{TEST_ENDPOINT}{ENDPOINT_ADMIN}")
                .ReturnsResponse(HttpStatusCode.OK);

            // Act
            var result = await _kepwareApiClient.SetAdminSettingsAsync(newSettings);

            // Assert
            result.ShouldBeTrue();
            _httpMessageHandlerMock.VerifyRequest(HttpMethod.Get, $"{TEST_ENDPOINT}{ENDPOINT_ADMIN}", Times.Once());
            _httpMessageHandlerMock.VerifyRequest(HttpMethod.Put, $"{TEST_ENDPOINT}{ENDPOINT_ADMIN}", Times.Once());
        }

        [Fact]
        public async Task SetAdminSettingsAsync_ShouldThrowInvalidOperationException_WhenGetCurrentSettingsFails()
        {
            // Arrange
            var newSettings = CreateTestAdminSettings();
            _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, $"{TEST_ENDPOINT}{ENDPOINT_ADMIN}")
                .ReturnsResponse(HttpStatusCode.InternalServerError, "Internal Server Error");

            // Act & Assert
            await Should.ThrowAsync<InvalidOperationException>(async () => 
                await _kepwareApiClient.SetAdminSettingsAsync(newSettings));

            _httpMessageHandlerMock.VerifyRequest(HttpMethod.Get, $"{TEST_ENDPOINT}{ENDPOINT_ADMIN}", Times.Once());
            _httpMessageHandlerMock.VerifyRequest(HttpMethod.Put, $"{TEST_ENDPOINT}{ENDPOINT_ADMIN}", Times.Never());
        }

        [Fact]
        public async Task SetAdminSettingsAsync_ShouldReturnFalse_WhenUpdateFails()
        {
            // Arrange
            var currentSettings = CreateTestAdminSettings();
            var newSettings = CreateTestAdminSettings();
            newSettings.EventLogMaxRecords = 30000;

            _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, $"{TEST_ENDPOINT}{ENDPOINT_ADMIN}")
                .ReturnsResponse(HttpStatusCode.OK, JsonSerializer.Serialize(currentSettings), "application/json");

            _httpMessageHandlerMock.SetupRequest(HttpMethod.Put, $"{TEST_ENDPOINT}{ENDPOINT_ADMIN}")
                .ReturnsResponse(HttpStatusCode.BadRequest, "Invalid setting value");

            // Act
            var result = await _kepwareApiClient.SetAdminSettingsAsync(newSettings);

            // Assert
            result.ShouldBeFalse();
            _httpMessageHandlerMock.VerifyRequest(HttpMethod.Get, $"{TEST_ENDPOINT}{ENDPOINT_ADMIN}", Times.Once());
            _httpMessageHandlerMock.VerifyRequest(HttpMethod.Put, $"{TEST_ENDPOINT}{ENDPOINT_ADMIN}", Times.Once());
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
        public async Task SetAdminSettingsAsync_ShouldReturnFalse_OnHttpRequestException()
        {
            // Arrange
            var currentSettings = CreateTestAdminSettings();
            var newSettings = CreateTestAdminSettings();
            newSettings.EventLogMaxRecords = 30000;

            _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, $"{TEST_ENDPOINT}{ENDPOINT_ADMIN}")
                .ReturnsResponse(HttpStatusCode.OK, JsonSerializer.Serialize(currentSettings), "application/json");

            _httpMessageHandlerMock.SetupRequest(HttpMethod.Put, $"{TEST_ENDPOINT}{ENDPOINT_ADMIN}")
                .ThrowsAsync(new HttpRequestException("Network error during update"));

            // Act
            var result = await _kepwareApiClient.SetAdminSettingsAsync(newSettings);

            // Assert
            result.ShouldBeFalse();
            _httpMessageHandlerMock.VerifyRequest(HttpMethod.Get, $"{TEST_ENDPOINT}{ENDPOINT_ADMIN}", Times.Once());
            _httpMessageHandlerMock.VerifyRequest(HttpMethod.Put, $"{TEST_ENDPOINT}{ENDPOINT_ADMIN}", Times.Once());
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
        public async Task SetAdminSettingsAsync_ShouldReturnFalse_WhenApiReturnsUnauthorized()
        {
            // Arrange
            var currentSettings = CreateTestAdminSettings();
            var newSettings = CreateTestAdminSettings();
            newSettings.EventLogMaxRecords = 30000;

            _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, $"{TEST_ENDPOINT}{ENDPOINT_ADMIN}")
                .ReturnsResponse(HttpStatusCode.OK, JsonSerializer.Serialize(currentSettings), "application/json");

            _httpMessageHandlerMock.SetupRequest(HttpMethod.Put, $"{TEST_ENDPOINT}{ENDPOINT_ADMIN}")
                .ReturnsResponse(HttpStatusCode.Unauthorized, "Unauthorized");

            // Act
            var result = await _kepwareApiClient.SetAdminSettingsAsync(newSettings);

            // Assert
            result.ShouldBeFalse();
            _httpMessageHandlerMock.VerifyRequest(HttpMethod.Get, $"{TEST_ENDPOINT}{ENDPOINT_ADMIN}", Times.Once());
            _httpMessageHandlerMock.VerifyRequest(HttpMethod.Put, $"{TEST_ENDPOINT}{ENDPOINT_ADMIN}", Times.Once());
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
        public async Task SetAdminSettingsAsync_ShouldReturnFalse_WhenApiReturnsForbidden()
        {
            // Arrange
            var currentSettings = CreateTestAdminSettings();
            var newSettings = CreateTestAdminSettings();
            newSettings.EventLogMaxRecords = 30000;

            _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, $"{TEST_ENDPOINT}{ENDPOINT_ADMIN}")
                .ReturnsResponse(HttpStatusCode.OK, JsonSerializer.Serialize(currentSettings), "application/json");

            _httpMessageHandlerMock.SetupRequest(HttpMethod.Put, $"{TEST_ENDPOINT}{ENDPOINT_ADMIN}")
                .ReturnsResponse(HttpStatusCode.Forbidden, "Forbidden");

            // Act
            var result = await _kepwareApiClient.SetAdminSettingsAsync(newSettings);

            // Assert
            result.ShouldBeFalse();
            _httpMessageHandlerMock.VerifyRequest(HttpMethod.Get, $"{TEST_ENDPOINT}{ENDPOINT_ADMIN}", Times.Once());
            _httpMessageHandlerMock.VerifyRequest(HttpMethod.Put, $"{TEST_ENDPOINT}{ENDPOINT_ADMIN}", Times.Once());
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

        private static AdminSettings CreateTestAdminSettings()
        {
            var settings = new AdminSettings
            {
                EventLogConnectionPort = 39461,
                EventLogMaxRecords = 25000,
                EventLogPersistence = 1,
                EventLogLogFilePath = "C:\\ProgramData\\Kepware\\Logs\\Event",
                EventLogMaxSingleFileSizeKb = 1024,
                EventLogMinDaysToPreserve = 30,
                OpcDiagnosticsPersistence = 1,
                OpcDiagnosticsMaxRecords = 1000,
                OpcDiagnosticsLogFilePath = "C:\\ProgramData\\Kepware\\Logs\\OpcDiagnostics",
                OpcDiagnosticsMaxSingleFileSizeKb = 1024,
                OpcDiagnosticsMinDaysToPreserve = 30
            };

            // Set license server properties
            settings.LicenseServer.Name = "licenseserver.example.com";
            settings.LicenseServer.Enable = true;
            settings.LicenseServer.Port = 8765;
            settings.LicenseServer.SslPort = 8443;
            settings.LicenseServer.AllowInsecureComms = true;
            settings.LicenseServer.AllowSelfSignedCerts = true;
            settings.LicenseServer.ClientAlias = "TestClient";
            settings.LicenseServer.RecheckIntervalMinutes = 60;

            return settings;
        }

        #endregion
    }
}
