using Kepware.Api.ClientHandler;
using Kepware.Api.Model;
using Moq;
using Moq.Contrib.HttpClient;
using Moq.Protected;
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

        // Capture PUT bodies in order to verify disable → update → re-enable sequence
        var putBodies = new List<string>();
        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.Is<HttpRequestMessage>(r =>
                    r.Method == HttpMethod.Put &&
                    r.RequestUri!.ToString() == LogGroupEndpoint),
                ItExpr.IsAny<CancellationToken>())
            .Returns<HttpRequestMessage, CancellationToken>(async (req, ct) =>
            {
                putBodies.Add(await req.Content!.ReadAsStringAsync(ct));
                return new HttpResponseMessage(HttpStatusCode.OK);
            });

        // Act
        var result = await _kepwareApiClient.Project.DataLogger.UpdateLogGroupAsync(group, autoDisable: true);

        // Assert — 3 PUTs: disable, update, re-enable
        result.ShouldBeTrue();
        putBodies.Count.ShouldBe(3);
        putBodies[0].ShouldContain("\"datalogger.LOG_GROUP_ENABLED\": false"); // disable
        putBodies[2].ShouldContain("\"datalogger.LOG_GROUP_ENABLED\": true");  // re-enable
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

    #region Log Item Tests

    private const string TEST_ITEM_NAME = "TestItem";
    private string LogItemsEndpoint => $"{TEST_ENDPOINT}/config/v1/project/_datalogger/log_groups/{TEST_GROUP_NAME}/log_items";
    private string LogItemEndpoint => $"{TEST_ENDPOINT}/config/v1/project/_datalogger/log_groups/{TEST_GROUP_NAME}/log_items/{TEST_ITEM_NAME}";

    [Fact]
    public async Task CreateLogItem_WithAutoDisableFalse_ShouldNotDisableGroup()
    {
        // Arrange
        var parent = new LogGroup(TEST_GROUP_NAME) { Enabled = true };

        _httpMessageHandlerMock.SetupRequest(HttpMethod.Post, LogItemsEndpoint)
            .ReturnsResponse(HttpStatusCode.OK);

        // Act
        var result = await _kepwareApiClient.Project.DataLogger.CreateLogItemAsync(TEST_ITEM_NAME, parent, autoDisable: false);

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe(TEST_ITEM_NAME);
        _httpMessageHandlerMock.VerifyRequest(HttpMethod.Post, LogItemsEndpoint, Times.Once());
        _httpMessageHandlerMock.VerifyRequest(HttpMethod.Put, LogGroupEndpoint, Times.Never());
    }

    [Fact]
    public async Task CreateLogItem_WithAutoDisableTrue_WhenGroupEnabled_ShouldDisableThenReEnable()
    {
        // Arrange — group starts enabled
        var parent = new LogGroup(TEST_GROUP_NAME) { Enabled = true };

        var groupJson = $$"""{ "common.ALLTYPES_NAME": "{{TEST_GROUP_NAME}}", "datalogger.LOG_GROUP_ENABLED": true }""";
        _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, LogGroupEndpoint)
            .ReturnsResponse(HttpStatusCode.OK, groupJson, "application/json");

        // Capture PUT bodies in order to verify disable then re-enable sequence
        var putBodies = new List<string>();
        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.Is<HttpRequestMessage>(r =>
                    r.Method == HttpMethod.Put &&
                    r.RequestUri!.ToString() == LogGroupEndpoint),
                ItExpr.IsAny<CancellationToken>())
            .Returns<HttpRequestMessage, CancellationToken>(async (req, ct) =>
            {
                putBodies.Add(await req.Content!.ReadAsStringAsync(ct));
                return new HttpResponseMessage(HttpStatusCode.OK);
            });

        _httpMessageHandlerMock.SetupRequest(HttpMethod.Post, LogItemsEndpoint)
            .ReturnsResponse(HttpStatusCode.OK);

        // Act
        var result = await _kepwareApiClient.Project.DataLogger.CreateLogItemAsync(TEST_ITEM_NAME, parent, autoDisable: true);

        // Assert — 2 PUTs: first disables, second re-enables
        result.ShouldNotBeNull();
        result.Name.ShouldBe(TEST_ITEM_NAME);
        _httpMessageHandlerMock.VerifyRequest(HttpMethod.Post, LogItemsEndpoint, Times.Once());

        putBodies.Count.ShouldBe(2);
        putBodies[0].ShouldContain("\"datalogger.LOG_GROUP_ENABLED\": false");
        putBodies[1].ShouldContain("\"datalogger.LOG_GROUP_ENABLED\": true");
    }

    [Fact]
    public async Task CreateLogItem_WithAutoDisableTrue_WhenGroupAlreadyDisabled_ShouldNotReEnable()
    {
        // Arrange — group starts disabled
        var parent = new LogGroup(TEST_GROUP_NAME) { Enabled = false };

        _httpMessageHandlerMock.SetupRequest(HttpMethod.Post, LogItemsEndpoint)
            .ReturnsResponse(HttpStatusCode.OK);

        // Act
        var result = await _kepwareApiClient.Project.DataLogger.CreateLogItemAsync(TEST_ITEM_NAME, parent, autoDisable: true);

        // Assert — 0 PUTs (group already disabled, no disable/re-enable)
        result.ShouldNotBeNull();
        result.Name.ShouldBe(TEST_ITEM_NAME);
        
        _httpMessageHandlerMock.VerifyRequest(HttpMethod.Put, LogGroupEndpoint, Times.Never());
        _httpMessageHandlerMock.VerifyRequest(HttpMethod.Post, LogItemsEndpoint, Times.Once());
    }

    [Fact]
    public async Task UpdateLogItem_ShouldPutToCorrectEndpoint()
    {
        // Arrange
        var parent = new LogGroup(TEST_GROUP_NAME);
        var item = new LogItem(TEST_ITEM_NAME) { Owner = parent };

        var itemJson = $$"""{ "common.ALLTYPES_NAME": "{{TEST_ITEM_NAME}}" }""";
        _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, LogItemEndpoint)
            .ReturnsResponse(HttpStatusCode.OK, itemJson, "application/json");

        _httpMessageHandlerMock.SetupRequest(HttpMethod.Put, LogItemEndpoint)
            .ReturnsResponse(HttpStatusCode.OK);

        // Act
        var result = await _kepwareApiClient.Project.DataLogger.UpdateLogItemAsync(item, parent);

        // Assert
        result.ShouldBeTrue();
        _httpMessageHandlerMock.VerifyRequest(HttpMethod.Put, LogItemEndpoint, Times.Once());
    }

    [Fact]
    public async Task DeleteLogItem_ShouldDeleteFromCorrectEndpoint()
    {
        // Arrange
        var parent = new LogGroup(TEST_GROUP_NAME);
        var item = new LogItem(TEST_ITEM_NAME) { Owner = parent };

        _httpMessageHandlerMock.SetupRequest(HttpMethod.Delete, LogItemEndpoint)
            .ReturnsResponse(HttpStatusCode.OK);

        // Act
        var result = await _kepwareApiClient.Project.DataLogger.DeleteLogItemAsync(item, parent);

        // Assert
        result.ShouldBeTrue();
        _httpMessageHandlerMock.VerifyRequest(HttpMethod.Delete, LogItemEndpoint, Times.Once());
    }

    [Fact]
    public async Task GetLogItems_ShouldLoadFromParentEndpoint()
    {
        // Arrange
        var parent = new LogGroup(TEST_GROUP_NAME);
        var itemsJson = $$"""[ { "common.ALLTYPES_NAME": "{{TEST_ITEM_NAME}}" } ]""";

        _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, LogItemsEndpoint)
            .ReturnsResponse(HttpStatusCode.OK, itemsJson, "application/json");

        // Act
        var result = await _kepwareApiClient.Project.DataLogger.GetLogItemsAsync(parent);

        // Assert
        result.ShouldNotBeNull();
        result.Count.ShouldBe(1);
        result[0].Name.ShouldBe(TEST_ITEM_NAME);
    }

    [Fact]
    public async Task GetOrCreateLogItem_WhenExists_ShouldReturnExisting()
    {
        // Arrange
        var parent = new LogGroup(TEST_GROUP_NAME);
        var itemJson = $$"""{ "common.ALLTYPES_NAME": "{{TEST_ITEM_NAME}}" }""";

        _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, LogItemEndpoint)
            .ReturnsResponse(HttpStatusCode.OK, itemJson, "application/json");

        // Act
        var result = await _kepwareApiClient.Project.DataLogger.GetOrCreateLogItemAsync(TEST_ITEM_NAME, parent);

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe(TEST_ITEM_NAME);
        _httpMessageHandlerMock.VerifyRequest(HttpMethod.Post, LogItemsEndpoint, Times.Never());
    }

    [Fact]
    public async Task GetOrCreateLogItem_WhenNotExists_ShouldCreateAndReturn()
    {
        // Arrange
        var parent = new LogGroup(TEST_GROUP_NAME);

        _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, LogItemEndpoint)
            .ReturnsResponse(HttpStatusCode.NotFound, "Not Found");

        _httpMessageHandlerMock.SetupRequest(HttpMethod.Post, LogItemsEndpoint)
            .ReturnsResponse(HttpStatusCode.OK);

        // Act
        var result = await _kepwareApiClient.Project.DataLogger.GetOrCreateLogItemAsync(TEST_ITEM_NAME, parent);

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe(TEST_ITEM_NAME);
        _httpMessageHandlerMock.VerifyRequest(HttpMethod.Post, LogItemsEndpoint, Times.Once());
    }

    #endregion
}
