using Kepware.Api.ClientHandler;
using Kepware.Api.Model;
using Kepware.Api.Model.Services;
using Kepware.Api.Test.Util;
using Moq;
using Moq.Contrib.HttpClient;
using Moq.Protected;
using Shouldly;
using System.Net;
using System.Net.Http;
using System.Text.Json;

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

    #region Column Mapping Tests

    private const string TEST_MAPPING_NAME = "TestMapping";
    private string ColumnMappingsEndpoint => $"{TEST_ENDPOINT}/config/v1/project/_datalogger/log_groups/{TEST_GROUP_NAME}/column_mappings";
    private string ColumnMappingEndpoint => $"{TEST_ENDPOINT}/config/v1/project/_datalogger/log_groups/{TEST_GROUP_NAME}/column_mappings/{TEST_MAPPING_NAME}";

    [Fact]
    public async Task GetColumnMappings_ShouldLoadFromParentEndpoint()
    {
        // Arrange
        var parent = new LogGroup(TEST_GROUP_NAME);
        var mappingsJson = $$"""[ { "common.ALLTYPES_NAME": "{{TEST_MAPPING_NAME}}" } ]""";

        _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, ColumnMappingsEndpoint)
            .ReturnsResponse(HttpStatusCode.OK, mappingsJson, "application/json");

        // Act
        var result = await _kepwareApiClient.Project.DataLogger.GetColumnMappingsAsync(parent);

        // Assert
        result.ShouldNotBeNull();
        result.Count.ShouldBe(1);
        result[0].Name.ShouldBe(TEST_MAPPING_NAME);
    }

    [Fact]
    public async Task GetColumnMapping_ShouldLoadSingleEntityFromParentEndpoint()
    {
        // Arrange
        var parent = new LogGroup(TEST_GROUP_NAME);
        var mappingJson = $$"""{ "common.ALLTYPES_NAME": "{{TEST_MAPPING_NAME}}" }""";

        _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, ColumnMappingEndpoint)
            .ReturnsResponse(HttpStatusCode.OK, mappingJson, "application/json");

        // Act
        var result = await _kepwareApiClient.Project.DataLogger.GetColumnMappingAsync(TEST_MAPPING_NAME, parent);

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe(TEST_MAPPING_NAME);
    }

    [Fact]
    public async Task UpdateColumnMapping_ShouldPutToCorrectEndpoint()
    {
        // Arrange
        var parent = new LogGroup(TEST_GROUP_NAME);
        var mapping = new ColumnMapping(TEST_MAPPING_NAME) { Owner = parent };

        var mappingJson = $$"""{ "common.ALLTYPES_NAME": "{{TEST_MAPPING_NAME}}" }""";
        _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, ColumnMappingEndpoint)
            .ReturnsResponse(HttpStatusCode.OK, mappingJson, "application/json");

        _httpMessageHandlerMock.SetupRequest(HttpMethod.Put, ColumnMappingEndpoint)
            .ReturnsResponse(HttpStatusCode.OK);

        // Act
        var result = await _kepwareApiClient.Project.DataLogger.UpdateColumnMappingAsync(mapping, parent);

        // Assert
        result.ShouldBeTrue();
        _httpMessageHandlerMock.VerifyRequest(HttpMethod.Put, ColumnMappingEndpoint, Times.Once());
    }

    [Fact]
    public async Task UpdateColumnMapping_WithAutoDisableTrue_ShouldDisableThenReEnable()
    {
        // Arrange — group starts enabled
        var parent = new LogGroup(TEST_GROUP_NAME) { Enabled = true };
        var mapping = new ColumnMapping(TEST_MAPPING_NAME) { Owner = parent };

        var groupJson = $$"""{ "common.ALLTYPES_NAME": "{{TEST_GROUP_NAME}}", "datalogger.LOG_GROUP_ENABLED": true }""";
        _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, LogGroupEndpoint)
            .ReturnsResponse(HttpStatusCode.OK, groupJson, "application/json");

        var mappingJson = $$"""{ "common.ALLTYPES_NAME": "{{TEST_MAPPING_NAME}}" }""";
        _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, ColumnMappingEndpoint)
            .ReturnsResponse(HttpStatusCode.OK, mappingJson, "application/json");

        _httpMessageHandlerMock.SetupRequest(HttpMethod.Put, ColumnMappingEndpoint)
            .ReturnsResponse(HttpStatusCode.OK);

        // Capture PUT bodies on the log group to verify disable then re-enable
        var groupPutBodies = new List<string>();
        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.Is<HttpRequestMessage>(r =>
                    r.Method == HttpMethod.Put &&
                    r.RequestUri!.ToString() == LogGroupEndpoint),
                ItExpr.IsAny<CancellationToken>())
            .Returns<HttpRequestMessage, CancellationToken>(async (req, ct) =>
            {
                groupPutBodies.Add(await req.Content!.ReadAsStringAsync(ct));
                return new HttpResponseMessage(HttpStatusCode.OK);
            });

        // Act
        var result = await _kepwareApiClient.Project.DataLogger.UpdateColumnMappingAsync(mapping, parent, autoDisable: true);

        // Assert
        result.ShouldBeTrue();
        _httpMessageHandlerMock.VerifyRequest(HttpMethod.Put, ColumnMappingEndpoint, Times.Once());

        groupPutBodies.Count.ShouldBe(2);
        groupPutBodies[0].ShouldContain("\"datalogger.LOG_GROUP_ENABLED\": false"); // disable
        groupPutBodies[1].ShouldContain("\"datalogger.LOG_GROUP_ENABLED\": true");  // re-enable
    }

    #endregion

    #region Trigger Tests

    private const string TEST_TRIGGER_NAME = "TestTrigger";
    private string TriggersEndpoint => $"{TEST_ENDPOINT}/config/v1/project/_datalogger/log_groups/{TEST_GROUP_NAME}/triggers";
    private string TriggerEndpoint => $"{TEST_ENDPOINT}/config/v1/project/_datalogger/log_groups/{TEST_GROUP_NAME}/triggers/{TEST_TRIGGER_NAME}";

    [Fact]
    public async Task CreateTrigger_ShouldPostToCorrectEndpoint()
    {
        // Arrange
        var parent = new LogGroup(TEST_GROUP_NAME);

        _httpMessageHandlerMock.SetupRequest(HttpMethod.Post, TriggersEndpoint)
            .ReturnsResponse(HttpStatusCode.OK);

        // Act
        var result = await _kepwareApiClient.Project.DataLogger.CreateTriggerAsync(TEST_TRIGGER_NAME, parent);

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe(TEST_TRIGGER_NAME);
        _httpMessageHandlerMock.VerifyRequest(HttpMethod.Post, TriggersEndpoint, Times.Once());
    }

    [Fact]
    public async Task CreateTrigger_WithAutoDisableTrue_ShouldDisableThenReEnable()
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

        _httpMessageHandlerMock.SetupRequest(HttpMethod.Post, TriggersEndpoint)
            .ReturnsResponse(HttpStatusCode.OK);

        // Act
        var result = await _kepwareApiClient.Project.DataLogger.CreateTriggerAsync(TEST_TRIGGER_NAME, parent, autoDisable: true);

        // Assert — 2 PUTs: first disables, second re-enables
        result.ShouldNotBeNull();
        result.Name.ShouldBe(TEST_TRIGGER_NAME);
        _httpMessageHandlerMock.VerifyRequest(HttpMethod.Post, TriggersEndpoint, Times.Once());

        putBodies.Count.ShouldBe(2);
        putBodies[0].ShouldContain("\"datalogger.LOG_GROUP_ENABLED\": false"); // disable
        putBodies[1].ShouldContain("\"datalogger.LOG_GROUP_ENABLED\": true");  // re-enable
    }

    [Fact]
    public async Task UpdateTrigger_ShouldPutToCorrectEndpoint()
    {
        // Arrange
        var parent = new LogGroup(TEST_GROUP_NAME);
        var trigger = new Trigger(TEST_TRIGGER_NAME) { Owner = parent };

        var triggerJson = $$"""{ "common.ALLTYPES_NAME": "{{TEST_TRIGGER_NAME}}" }""";
        _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, TriggerEndpoint)
            .ReturnsResponse(HttpStatusCode.OK, triggerJson, "application/json");

        _httpMessageHandlerMock.SetupRequest(HttpMethod.Put, TriggerEndpoint)
            .ReturnsResponse(HttpStatusCode.OK);

        // Act
        var result = await _kepwareApiClient.Project.DataLogger.UpdateTriggerAsync(trigger, parent);

        // Assert
        result.ShouldBeTrue();
        _httpMessageHandlerMock.VerifyRequest(HttpMethod.Put, TriggerEndpoint, Times.Once());
    }

    [Fact]
    public async Task DeleteTrigger_ShouldDeleteFromCorrectEndpoint()
    {
        // Arrange
        var parent = new LogGroup(TEST_GROUP_NAME);
        var trigger = new Trigger(TEST_TRIGGER_NAME) { Owner = parent };

        _httpMessageHandlerMock.SetupRequest(HttpMethod.Delete, TriggerEndpoint)
            .ReturnsResponse(HttpStatusCode.OK);

        // Act
        var result = await _kepwareApiClient.Project.DataLogger.DeleteTriggerAsync(trigger, parent);

        // Assert
        result.ShouldBeTrue();
        _httpMessageHandlerMock.VerifyRequest(HttpMethod.Delete, TriggerEndpoint, Times.Once());
    }

    [Fact]
    public async Task DeleteTrigger_ByName_ShouldDeleteFromCorrectEndpoint()
    {
        // Arrange
        var parent = new LogGroup(TEST_GROUP_NAME);

        _httpMessageHandlerMock.SetupRequest(HttpMethod.Delete, TriggerEndpoint)
            .ReturnsResponse(HttpStatusCode.OK);

        // Act
        var result = await _kepwareApiClient.Project.DataLogger.DeleteTriggerAsync(TEST_TRIGGER_NAME, parent);

        // Assert
        result.ShouldBeTrue();
        _httpMessageHandlerMock.VerifyRequest(HttpMethod.Delete, TriggerEndpoint, Times.Once());
    }

    [Fact]
    public async Task GetTriggers_ShouldLoadFromParentEndpoint()
    {
        // Arrange
        var parent = new LogGroup(TEST_GROUP_NAME);
        var triggersJson = $$"""[ { "common.ALLTYPES_NAME": "{{TEST_TRIGGER_NAME}}" } ]""";

        _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, TriggersEndpoint)
            .ReturnsResponse(HttpStatusCode.OK, triggersJson, "application/json");

        // Act
        var result = await _kepwareApiClient.Project.DataLogger.GetTriggersAsync(parent);

        // Assert
        result.ShouldNotBeNull();
        result.Count.ShouldBe(1);
        result[0].Name.ShouldBe(TEST_TRIGGER_NAME);
    }

    [Fact]
    public async Task GetOrCreateTrigger_WhenExists_ShouldReturnExisting()
    {
        // Arrange
        var parent = new LogGroup(TEST_GROUP_NAME);
        var triggerJson = $$"""{ "common.ALLTYPES_NAME": "{{TEST_TRIGGER_NAME}}" }""";

        _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, TriggerEndpoint)
            .ReturnsResponse(HttpStatusCode.OK, triggerJson, "application/json");

        // Act
        var result = await _kepwareApiClient.Project.DataLogger.GetOrCreateTriggerAsync(TEST_TRIGGER_NAME, parent);

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe(TEST_TRIGGER_NAME);
        _httpMessageHandlerMock.VerifyRequest(HttpMethod.Post, TriggersEndpoint, Times.Never());
    }

    [Fact]
    public async Task GetOrCreateTrigger_WhenNotExists_ShouldCreateAndReturn()
    {
        // Arrange
        var parent = new LogGroup(TEST_GROUP_NAME);

        _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, TriggerEndpoint)
            .ReturnsResponse(HttpStatusCode.NotFound, "Not Found");

        _httpMessageHandlerMock.SetupRequest(HttpMethod.Post, TriggersEndpoint)
            .ReturnsResponse(HttpStatusCode.OK);

        // Act
        var result = await _kepwareApiClient.Project.DataLogger.GetOrCreateTriggerAsync(TEST_TRIGGER_NAME, parent);

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe(TEST_TRIGGER_NAME);
        _httpMessageHandlerMock.VerifyRequest(HttpMethod.Post, TriggersEndpoint, Times.Once());
    }

    #endregion

    #region ResetColumnMapping Tests

    private const string JOB_ID_RESET = $"/config/v1/project/_datalogger/log_groups/{TEST_GROUP_NAME}/services/ResetColumnMapping/jobs/job123";
    private string ResetColumnMappingEndpoint => $"{TEST_ENDPOINT}/config/v1/project/_datalogger/log_groups/{TEST_GROUP_NAME}/services/ResetColumnMapping";
    private string JobEndpointReset => $"{TEST_ENDPOINT}{JOB_ID_RESET}";

    [Fact]
    public async Task ResetColumnMapping_ShouldPutToCorrectServiceEndpoint()
    {
        // Arrange
        var group = new LogGroup(TEST_GROUP_NAME);
        var jobResponse = new JobResponseMessage { ResponseStatusCode = (int)ApiResponseCode.Accepted, JobId = JOB_ID_RESET };

        _httpMessageHandlerMock.SetupRequest(HttpMethod.Put, ResetColumnMappingEndpoint)
            .ReturnsResponse(HttpStatusCode.Accepted, JsonSerializer.Serialize(jobResponse), "application/json");

        // Act
        var result = await _kepwareApiClient.Project.DataLogger.ResetColumnMappingAsync(group);

        // Assert
        result.ShouldNotBeNull();
        _httpMessageHandlerMock.VerifyRequest(HttpMethod.Put, ResetColumnMappingEndpoint, Times.Once());
        _httpMessageHandlerMock.VerifyRequest(HttpMethod.Post, ResetColumnMappingEndpoint, Times.Never());
    }

    [Fact]
    public async Task ResetColumnMapping_ShouldReturnKepServerJobPromise()
    {
        // Arrange
        var group = new LogGroup(TEST_GROUP_NAME);
        var jobId = $"/config/v1/project/_datalogger/log_groups/{TEST_GROUP_NAME}/services/ResetColumnMapping/jobs/job123";
        var jobResponse = new JobResponseMessage { ResponseStatusCode = (int)ApiResponseCode.Accepted, JobId = jobId };

        _httpMessageHandlerMock.SetupRequest(HttpMethod.Put, ResetColumnMappingEndpoint)
            .ReturnsResponse(HttpStatusCode.Accepted, JsonSerializer.Serialize(jobResponse), "application/json");

        // Act
        var result = await _kepwareApiClient.Project.DataLogger.ResetColumnMappingAsync(group, TimeSpan.FromSeconds(30));

        // Assert
        result.ShouldNotBeNull();
        result.Endpoint.ShouldBe($"/config/v1/project/_datalogger/log_groups/{TEST_GROUP_NAME}/services/ResetColumnMapping");
        result.JobTimeToLive.ShouldBe(TimeSpan.FromSeconds(30));
    }

    [Fact]
    public async Task ResetColumnMapping_WithAutoDisableTrue_ShouldDisableThenReEnable()
    {
        // Arrange — group starts enabled
        var group = new LogGroup(TEST_GROUP_NAME) { Enabled = true };

        var groupJson = $$"""{ "common.ALLTYPES_NAME": "{{TEST_GROUP_NAME}}", "datalogger.LOG_GROUP_ENABLED": true }""";
        _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, LogGroupEndpoint)
            .ReturnsResponse(HttpStatusCode.OK, groupJson, "application/json");

        var jobResponse = new JobResponseMessage { ResponseStatusCode = (int)ApiResponseCode.Accepted, JobId = JOB_ID_RESET };
        _httpMessageHandlerMock.SetupRequest(HttpMethod.Put, ResetColumnMappingEndpoint)
            .ReturnsResponse(HttpStatusCode.Accepted, JsonSerializer.Serialize(jobResponse), "application/json");

        // Capture log group PUT bodies to verify disable then re-enable
        var groupPutBodies = new List<string>();
        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.Is<HttpRequestMessage>(r =>
                    r.Method == HttpMethod.Put &&
                    r.RequestUri!.ToString() == LogGroupEndpoint),
                ItExpr.IsAny<CancellationToken>())
            .Returns<HttpRequestMessage, CancellationToken>(async (req, ct) =>
            {
                groupPutBodies.Add(await req.Content!.ReadAsStringAsync(ct));
                return new HttpResponseMessage(HttpStatusCode.OK);
            });

        // Act
        var result = await _kepwareApiClient.Project.DataLogger.ResetColumnMappingAsync(group, autoDisable: true);

        // Assert
        result.ShouldNotBeNull();
        _httpMessageHandlerMock.VerifyRequest(HttpMethod.Put, ResetColumnMappingEndpoint, Times.Once());

        groupPutBodies.Count.ShouldBe(2);
        groupPutBodies[0].ShouldContain("\"datalogger.LOG_GROUP_ENABLED\": false"); // disable
        groupPutBodies[1].ShouldContain("\"datalogger.LOG_GROUP_ENABLED\": true");  // re-enable
    }

    [Fact]
    public async Task ResetColumnMapping_WhenServerReturnsError_ShouldReturnFailedJobPromise()
    {
        // Arrange
        var group = new LogGroup(TEST_GROUP_NAME);

        _httpMessageHandlerMock.SetupRequest(HttpMethod.Put, ResetColumnMappingEndpoint)
            .ReturnsResponse(HttpStatusCode.BadRequest, "Bad Request");

        // Act
        var result = await _kepwareApiClient.Project.DataLogger.ResetColumnMappingAsync(group);
        var jobResult = await result.AwaitCompletionAsync();

        // Assert
        result.ShouldNotBeNull();
        jobResult.IsSuccess.ShouldBeFalse();
        jobResult.ResponseCode.ShouldBe(ApiResponseCode.BadRequest);
    }

    [Fact]
    public async Task ResetColumnMapping_ShouldThrowException_WhenHttpClientThrowsException()
    {
        // Arrange
        var group = new LogGroup(TEST_GROUP_NAME);

        _httpMessageHandlerMock.SetupRequest(HttpMethod.Put, ResetColumnMappingEndpoint)
            .Throws(new HttpRequestException("Network error"));

        // Act & Assert
        await Should.ThrowAsync<HttpRequestException>(async () =>
        {
            await _kepwareApiClient.Project.DataLogger.ResetColumnMappingAsync(group);
        });
    }

    [Fact]
    public async Task ResetColumnMapping_ShouldHandleTimeout_WhenOperationTimesOut()
    {
        // Arrange
        var group = new LogGroup(TEST_GROUP_NAME);

        _httpMessageHandlerMock.SetupRequest(HttpMethod.Put, ResetColumnMappingEndpoint)
            .ReturnsResponse(HttpStatusCode.RequestTimeout, "Request Timeout");

        // Act
        var result = await _kepwareApiClient.Project.DataLogger.ResetColumnMappingAsync(group, TimeSpan.FromSeconds(30));

        // Assert
        result.ShouldNotBeNull();
        result.Endpoint.ShouldBe($"/config/v1/project/_datalogger/log_groups/{TEST_GROUP_NAME}/services/ResetColumnMapping");
        result.JobTimeToLive.ShouldBe(TimeSpan.FromSeconds(30));
    }

    [Fact]
    public async Task ResetColumnMapping_ShouldThrowException_WhenTimeToLiveIsNegative()
    {
        // Arrange
        var group = new LogGroup(TEST_GROUP_NAME);

        // Act & Assert
        await Should.ThrowAsync<ArgumentOutOfRangeException>(async () =>
        {
            await _kepwareApiClient.Project.DataLogger.ResetColumnMappingAsync(group, TimeSpan.FromSeconds(-1));
        });
    }

    [Fact]
    public async Task ResetColumnMapping_ShouldReturnSuccess_WhenJobCompletesSuccessfullyAfterFirstGet()
    {
        // Arrange
        var group = new LogGroup(TEST_GROUP_NAME);
        var jobResponse = new JobResponseMessage { ResponseStatusCode = (int)ApiResponseCode.Accepted, JobId = JOB_ID_RESET };
        var jobStatus = new JobStatusMessage { Completed = true };

        _httpMessageHandlerMock.SetupRequest(HttpMethod.Put, ResetColumnMappingEndpoint)
            .ReturnsResponse(HttpStatusCode.Accepted, JsonSerializer.Serialize(jobResponse), "application/json");
        _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, JobEndpointReset)
            .ReturnsResponse(HttpStatusCode.OK, JsonSerializer.Serialize(jobStatus), "application/json");

        // Act
        var result = await _kepwareApiClient.Project.DataLogger.ResetColumnMappingAsync(group, TimeSpan.FromSeconds(30));
        var completionResult = await result.AwaitCompletionAsync();

        // Assert
        completionResult.Value.ShouldBeTrue();
        completionResult.IsSuccess.ShouldBeTrue();
        completionResult.ResponseCode.ShouldBe(ApiResponseCode.Success);
    }

    [Fact]
    public async Task ResetColumnMapping_ShouldReturnSuccess_WhenJobCompletesSuccessfullyAfterMultipleGets()
    {
        // Arrange
        var group = new LogGroup(TEST_GROUP_NAME);
        var jobResponse = new JobResponseMessage { ResponseStatusCode = (int)ApiResponseCode.Accepted, JobId = JOB_ID_RESET };
        var jobStatusIncomplete = new JobStatusMessage { Completed = false };
        var jobStatusComplete = new JobStatusMessage { Completed = true };

        _httpMessageHandlerMock.SetupRequest(HttpMethod.Put, ResetColumnMappingEndpoint)
            .ReturnsResponse(HttpStatusCode.Accepted, JsonSerializer.Serialize(jobResponse), "application/json");
        _httpMessageHandlerMock.SetupSequenceRequest(HttpMethod.Get, JobEndpointReset)
            .ReturnsResponse(HttpStatusCode.OK, JsonSerializer.Serialize(jobStatusIncomplete), "application/json")
            .ReturnsResponse(HttpStatusCode.OK, JsonSerializer.Serialize(jobStatusComplete), "application/json");

        // Act
        var result = await _kepwareApiClient.Project.DataLogger.ResetColumnMappingAsync(group, TimeSpan.FromSeconds(30));
        var completionResult = await result.AwaitCompletionAsync();

        // Assert
        completionResult.Value.ShouldBeTrue();
        completionResult.IsSuccess.ShouldBeTrue();
        completionResult.ResponseCode.ShouldBe(ApiResponseCode.Success);
    }

    [Fact]
    public async Task ResetColumnMapping_ShouldReturnFailure_WhenJobFailsAfterFirstGet()
    {
        // Arrange
        var group = new LogGroup(TEST_GROUP_NAME);
        var jobResponse = new JobResponseMessage { ResponseStatusCode = (int)ApiResponseCode.Accepted, JobId = JOB_ID_RESET };
        var jobStatus = new JobStatusMessage { Completed = false };

        _httpMessageHandlerMock.SetupRequest(HttpMethod.Put, ResetColumnMappingEndpoint)
            .ReturnsResponse(HttpStatusCode.Accepted, JsonSerializer.Serialize(jobResponse), "application/json");
        _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, JobEndpointReset)
            .ReturnsResponse(HttpStatusCode.OK, JsonSerializer.Serialize(jobStatus), "application/json");

        // Act
        var result = await _kepwareApiClient.Project.DataLogger.ResetColumnMappingAsync(group, TimeSpan.FromSeconds(1));
        var completionResult = await result.AwaitCompletionAsync(TimeSpan.FromMilliseconds(100));

        // Assert
        completionResult.Value.ShouldBeFalse();
        completionResult.IsSuccess.ShouldBeFalse();
        completionResult.ResponseCode.ShouldBe(ApiResponseCode.Timeout);
    }

    [Fact]
    public async Task ResetColumnMapping_ShouldReturnFailure_WhenJobFailsAfterMultipleGets()
    {
        // Arrange
        var group = new LogGroup(TEST_GROUP_NAME);
        var jobResponse = new JobResponseMessage { ResponseStatusCode = (int)ApiResponseCode.Accepted, JobId = JOB_ID_RESET };
        var jobStatusIncomplete = new JobStatusMessage { Completed = false };
        var jobStatusFailed = new JobStatusMessage { Completed = true, Message = "Job failed" };

        _httpMessageHandlerMock.SetupRequest(HttpMethod.Put, ResetColumnMappingEndpoint)
            .ReturnsResponse(HttpStatusCode.Accepted, JsonSerializer.Serialize(jobResponse), "application/json");
        _httpMessageHandlerMock.SetupSequenceRequest(HttpMethod.Get, JobEndpointReset)
            .ReturnsResponse(HttpStatusCode.OK, JsonSerializer.Serialize(jobStatusIncomplete), "application/json")
            .ReturnsResponse(HttpStatusCode.OK, JsonSerializer.Serialize(jobStatusIncomplete), "application/json")
            .ReturnsResponse(HttpStatusCode.ServiceUnavailable, JsonSerializer.Serialize(jobStatusFailed), "application/json");

        // Act
        var result = await _kepwareApiClient.Project.DataLogger.ResetColumnMappingAsync(group, TimeSpan.FromSeconds(5));
        var completionResult = await result.AwaitCompletionAsync(TimeSpan.FromMilliseconds(100));

        // Assert
        completionResult.Value.ShouldBeFalse();
        completionResult.IsSuccess.ShouldBeFalse();
        completionResult.ResponseCode.ShouldBe(ApiResponseCode.ServiceUnavailable);
        completionResult.Message.ShouldBe(jobStatusFailed.Message);
    }

    #endregion
}
