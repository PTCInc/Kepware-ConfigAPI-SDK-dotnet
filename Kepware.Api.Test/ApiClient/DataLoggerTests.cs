using Kepware.Api.ClientHandler;
using Kepware.Api.Model;
using Moq;
using Moq.Contrib.HttpClient;
using Shouldly;
using System.Net;

namespace Kepware.Api.Test.ApiClient;

public class DataLoggerTests : TestApiClientBase
{
    private const string LOG_GROUPS_ENDPOINT = "/config/v1/project/_datalogger/log_groups";
    private const string TEST_GROUP_NAME = "TestGroup";
    private string LogGroupsEndpoint => $"{TEST_ENDPOINT}{LOG_GROUPS_ENDPOINT}";
    private string LogGroupEndpoint => $"{TEST_ENDPOINT}{LOG_GROUPS_ENDPOINT}/{TEST_GROUP_NAME}";

    #region Log Group Tests

    [Fact]
    public async Task GetLogGroup_WhenExists_ShouldReturnLogGroup()
    {
        // Arrange
        var groupJson = $$"""
            {
                "common.ALLTYPES_NAME": "{{TEST_GROUP_NAME}}",
                "datalogger.LOG_GROUP_ENABLED": true
            }
            """;

        _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, LogGroupEndpoint)
            .ReturnsResponse(HttpStatusCode.OK, groupJson, "application/json");

        // Act
        var result = await _kepwareApiClient.Project.DataLogger.GetLogGroupAsync(TEST_GROUP_NAME);

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe(TEST_GROUP_NAME);
        result.Enabled.ShouldBe(true);
    }

    [Fact]
    public async Task GetOrCreateLogGroup_WhenNotExists_ShouldCreateAndReturn()
    {
        // Arrange
        _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, LogGroupEndpoint)
            .ReturnsResponse(HttpStatusCode.NotFound, "Not Found");

        _httpMessageHandlerMock.SetupRequest(HttpMethod.Post, LogGroupsEndpoint)
            .ReturnsResponse(HttpStatusCode.OK);

        // Act
        var result = await _kepwareApiClient.Project.DataLogger.GetOrCreateLogGroupAsync(TEST_GROUP_NAME);

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe(TEST_GROUP_NAME);
        _httpMessageHandlerMock.VerifyRequest(HttpMethod.Post, LogGroupsEndpoint, Times.Once());
    }

    [Fact]
    public async Task GetOrCreateLogGroup_WhenExists_ShouldReturnExisting()
    {
        // Arrange
        var groupJson = $$"""
            {
                "common.ALLTYPES_NAME": "{{TEST_GROUP_NAME}}",
                "datalogger.LOG_GROUP_ENABLED": false
            }
            """;

        _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, LogGroupEndpoint)
            .ReturnsResponse(HttpStatusCode.OK, groupJson, "application/json");

        // Act
        var result = await _kepwareApiClient.Project.DataLogger.GetOrCreateLogGroupAsync(TEST_GROUP_NAME);

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe(TEST_GROUP_NAME);
        result.Enabled.ShouldBe(false);
        _httpMessageHandlerMock.VerifyRequest(HttpMethod.Post, LogGroupsEndpoint, Times.Never());
    }

    [Fact]
    public async Task CreateLogGroup_ShouldPostToCorrectEndpoint()
    {
        // Arrange
        _httpMessageHandlerMock.SetupRequest(HttpMethod.Post, LogGroupsEndpoint)
            .ReturnsResponse(HttpStatusCode.OK);

        // Act
        var result = await _kepwareApiClient.Project.DataLogger.CreateLogGroupAsync(TEST_GROUP_NAME);

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe(TEST_GROUP_NAME);
        _httpMessageHandlerMock.VerifyRequest(HttpMethod.Post, LogGroupsEndpoint, Times.Once());
    }

    [Fact]
    public async Task UpdateLogGroup_ShouldPutToCorrectEndpoint()
    {
        // Arrange
        var group = new LogGroup(TEST_GROUP_NAME);
        group.Enabled = false;

        var groupJson = $$"""{ "common.ALLTYPES_NAME": "{{TEST_GROUP_NAME}}", "datalogger.LOG_GROUP_ENABLED": false }""";
        _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, LogGroupEndpoint)
            .ReturnsResponse(HttpStatusCode.OK, groupJson, "application/json");

        _httpMessageHandlerMock.SetupRequest(HttpMethod.Put, LogGroupEndpoint)
            .ReturnsResponse(HttpStatusCode.OK);

        // Act
        var result = await _kepwareApiClient.Project.DataLogger.UpdateLogGroupAsync(group);

        // Assert
        result.ShouldBeTrue();
        _httpMessageHandlerMock.VerifyRequest(HttpMethod.Put, LogGroupEndpoint, Times.Once());
    }

    [Fact]
    public async Task UpdateLogGroup_WithAutoDisableTrue_WhenEnabled_ShouldDisableThenReEnable()
    {
        // Arrange — group starts enabled
        var group = new LogGroup(TEST_GROUP_NAME);
        group.Enabled = true;

        // GET returns the current state (called 3 times: disable, update, re-enable)
        var groupJson = $$"""{ "common.ALLTYPES_NAME": "{{TEST_GROUP_NAME}}", "datalogger.LOG_GROUP_ENABLED": true }""";
        _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, LogGroupEndpoint)
            .ReturnsResponse(HttpStatusCode.OK, groupJson, "application/json");

        _httpMessageHandlerMock.SetupRequest(HttpMethod.Put, LogGroupEndpoint)
            .ReturnsResponse(HttpStatusCode.OK);

        // Act
        var result = await _kepwareApiClient.Project.DataLogger.UpdateLogGroupAsync(group, autoDisable: true);

        // Assert — 3 PUTs: disable, update, re-enable
        result.ShouldBeTrue();
        _httpMessageHandlerMock.VerifyRequest(HttpMethod.Put, LogGroupEndpoint, Times.Exactly(3));
    }

    [Fact]
    public async Task UpdateLogGroup_WithAutoDisableTrue_WhenAlreadyDisabled_ShouldNotReEnable()
    {
        // Arrange — group starts disabled
        var group = new LogGroup(TEST_GROUP_NAME);
        group.Enabled = false;

        // GET returns the current state (called once for the single update)
        var groupJson = $$"""{ "common.ALLTYPES_NAME": "{{TEST_GROUP_NAME}}", "datalogger.LOG_GROUP_ENABLED": false }""";
        _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, LogGroupEndpoint)
            .ReturnsResponse(HttpStatusCode.OK, groupJson, "application/json");

        _httpMessageHandlerMock.SetupRequest(HttpMethod.Put, LogGroupEndpoint)
            .ReturnsResponse(HttpStatusCode.OK);

        // Act
        var result = await _kepwareApiClient.Project.DataLogger.UpdateLogGroupAsync(group, autoDisable: true);

        // Assert — only 1 PUT: the update itself (no disable/re-enable)
        result.ShouldBeTrue();
        _httpMessageHandlerMock.VerifyRequest(HttpMethod.Put, LogGroupEndpoint, Times.Once());
    }

    [Fact]
    public async Task DeleteLogGroup_ShouldDeleteFromCorrectEndpoint()
    {
        // Arrange
        var group = new LogGroup(TEST_GROUP_NAME);

        _httpMessageHandlerMock.SetupRequest(HttpMethod.Delete, LogGroupEndpoint)
            .ReturnsResponse(HttpStatusCode.OK);

        // Act
        var result = await _kepwareApiClient.Project.DataLogger.DeleteLogGroupAsync(group);

        // Assert
        result.ShouldBeTrue();
        _httpMessageHandlerMock.VerifyRequest(HttpMethod.Delete, LogGroupEndpoint, Times.Once());
    }

    [Fact]
    public async Task DeleteLogGroup_ByName_ShouldDeleteFromCorrectEndpoint()
    {
        // Arrange
        _httpMessageHandlerMock.SetupRequest(HttpMethod.Delete, LogGroupEndpoint)
            .ReturnsResponse(HttpStatusCode.OK);

        // Act
        var result = await _kepwareApiClient.Project.DataLogger.DeleteLogGroupAsync(TEST_GROUP_NAME);

        // Assert
        result.ShouldBeTrue();
        _httpMessageHandlerMock.VerifyRequest(HttpMethod.Delete, LogGroupEndpoint, Times.Once());
    }

    #endregion
}
