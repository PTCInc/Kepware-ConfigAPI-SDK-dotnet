using Kepware.Api.Model;
using Moq;
using Moq.Contrib.HttpClient;
using Shouldly;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Xunit;

namespace Kepware.Api.Test.ApiClient;

public class ProjectApiHandlerDataLoggerTests : TestApiClientBase
{
    private const string LOG_GROUPS_ENDPOINT = "/config/v1/project/_datalogger/log_groups";
    private const string TEST_GROUP_NAME = "Group1";
    private const string FULL_PROJECT_ENDPOINT = "/config/v1/project?content=serialize";

    private string LogGroupsEndpoint      => $"{TEST_ENDPOINT}{LOG_GROUPS_ENDPOINT}";
    private string LogItemsEndpoint       => $"{TEST_ENDPOINT}{LOG_GROUPS_ENDPOINT}/{TEST_GROUP_NAME}/log_items";
    private string ColumnMappingsEndpoint => $"{TEST_ENDPOINT}{LOG_GROUPS_ENDPOINT}/{TEST_GROUP_NAME}/column_mappings";
    private string TriggersEndpoint       => $"{TEST_ENDPOINT}{LOG_GROUPS_ENDPOINT}/{TEST_GROUP_NAME}/triggers";

    // ── helpers ────────────────────────────────────────────────────────────────

    private static LogGroup CreateGroup(string name = TEST_GROUP_NAME, bool enabled = true)
    {
        var g = new LogGroup(name);
        g.Enabled = enabled;
        return g;
    }

    /// <summary>
    /// Overrides the <c>/config/v1/project</c> mock set by <see cref="ConfigureConnectedClient"/>
    /// to return a controlled project properties body.
    /// </summary>
    private void MockProjectEndpoint(string json = "{}")
    {
        _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, $"{TEST_ENDPOINT}/config/v1/project")
            .ReturnsResponse(HttpStatusCode.OK, json, "application/json");
    }

    private void MockChannelsEmpty()
    {
        _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, $"{TEST_ENDPOINT}/config/v1/project/channels")
            .ReturnsResponse(HttpStatusCode.OK, "[]", "application/json");
    }

    /// <summary>
    /// Sets up the DataLogger endpoints so that one group with empty children is returned.
    /// </summary>
    private void MockDataLoggerGroupOnly()
    {
        var groupJson = $$"""[{"common.ALLTYPES_NAME": "{{TEST_GROUP_NAME}}", "datalogger.LOG_GROUP_ENABLED": true}]""";
        _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, LogGroupsEndpoint)
            .ReturnsResponse(HttpStatusCode.OK, groupJson, "application/json");
        _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, LogItemsEndpoint)
            .ReturnsResponse(HttpStatusCode.OK, "[]", "application/json");
        _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, ColumnMappingsEndpoint)
            .ReturnsResponse(HttpStatusCode.OK, "[]", "application/json");
        _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, TriggersEndpoint)
            .ReturnsResponse(HttpStatusCode.OK, "[]", "application/json");
    }

    // ── tests ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task LoadProjectAsync_WithDataLogger_ShouldLoadLogGroups()
    {
        // Arrange — use non-JsonProjectLoad path (v6.16) with a single log group
        ConfigureConnectedClient(majorVersion: 6, minorVersion: 16);
        MockProjectEndpoint();
        MockChannelsEmpty();
        MockDataLoggerGroupOnly();

        // Act
        var project = await _kepwareApiClient.Project.LoadProjectAsync(blnLoadFullProject: true);

        // Assert
        project.DataLogger.ShouldNotBeNull();
        project.DataLogger.LogGroups.ShouldNotBeNull();
        project.DataLogger.LogGroups.Count.ShouldBe(1);
        project.DataLogger.LogGroups[0].Name.ShouldBe(TEST_GROUP_NAME);
    }

    [Fact]
    public async Task LoadProjectAsync_WithDataLogger_ShouldLoadLogItemsColumnMappingsTriggers()
    {
        // Arrange — non-JsonProjectLoad path, group has one entry in each child collection
        ConfigureConnectedClient(majorVersion: 6, minorVersion: 16);
        MockProjectEndpoint();
        MockChannelsEmpty();

        var groupJson = $$"""[{"common.ALLTYPES_NAME": "{{TEST_GROUP_NAME}}", "datalogger.LOG_GROUP_ENABLED": true}]""";
        _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, LogGroupsEndpoint)
            .ReturnsResponse(HttpStatusCode.OK, groupJson, "application/json");

        var logItemsJson = """[{"common.ALLTYPES_NAME": "Item1", "datalogger.LOG_ITEM_SERVER_TAG": "Channel1.Device1.Tag1"}]""";
        _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, LogItemsEndpoint)
            .ReturnsResponse(HttpStatusCode.OK, logItemsJson, "application/json");

        var cmJson = """[{"common.ALLTYPES_NAME": "Item1", "datalogger.COLUMN_MAP_FIELD_NAME": "Item1"}]""";
        _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, ColumnMappingsEndpoint)
            .ReturnsResponse(HttpStatusCode.OK, cmJson, "application/json");

        var triggerJson = """[{"common.ALLTYPES_NAME": "Trigger1"}]""";
        _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, TriggersEndpoint)
            .ReturnsResponse(HttpStatusCode.OK, triggerJson, "application/json");

        // Act
        var project = await _kepwareApiClient.Project.LoadProjectAsync(blnLoadFullProject: true);

        // Assert
        var logGroup = project.DataLogger?.LogGroups?.FirstOrDefault();
        logGroup.ShouldNotBeNull();
        logGroup.LogItems.ShouldNotBeNull();
        logGroup.LogItems.Count.ShouldBe(1);
        logGroup.LogItems[0].Name.ShouldBe("Item1");
        logGroup.ColumnMappings.ShouldNotBeNull();
        logGroup.ColumnMappings.Count.ShouldBe(1);
        logGroup.Triggers.ShouldNotBeNull();
        logGroup.Triggers.Count.ShouldBe(1);
    }

    [Fact]
    public async Task LoadProjectAsync_WhenDataLoggerNotInstalled_ShouldNotThrow()
    {
        // Arrange — DataLogger plug-in absent: log_groups returns 404; must not throw
        ConfigureConnectedClient(majorVersion: 6, minorVersion: 16);
        MockProjectEndpoint();
        MockChannelsEmpty();

        _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, LogGroupsEndpoint)
            .ReturnsResponse(HttpStatusCode.NotFound, "Not Found");

        // Act
        var project = await _kepwareApiClient.Project.LoadProjectAsync(blnLoadFullProject: true);

        // Assert
        project.ShouldNotBeNull();
        project.DataLogger.ShouldBeNull();
    }

    [Fact]
    public async Task LoadProjectAsync_OptimizedPath_WithDataLogger_ShouldLoadLogGroups()
    {
        // Arrange — v6.17 (JsonProjectLoad supported), project tags > limit → optimized recursion path
        ConfigureConnectedClient(majorVersion: 6, minorVersion: 17);

        // 1000 tags with projectLoadTagLimit=1 → 1000 > 1 → optimized recursion
        MockProjectEndpoint("""{"servermain.PROJECT_TAGS_DEFINED": "1000"}""");
        MockChannelsEmpty();
        MockDataLoggerGroupOnly();

        // Act — pass a low limit so any tag count triggers the optimized path
        var project = await _kepwareApiClient.Project.LoadProjectAsync(blnLoadFullProject: true, projectLoadTagLimit: 1);

        // Assert
        project.DataLogger.ShouldNotBeNull();
        project.DataLogger.LogGroups.ShouldNotBeNull();
        project.DataLogger.LogGroups.Count.ShouldBe(1);
        project.DataLogger.LogGroups[0].Name.ShouldBe(TEST_GROUP_NAME);
    }

    [Fact]
    public async Task LoadProjectAsync_ViaSerializeContent_ShouldFlattenLogItemGroupsToLogItems()
    {
        // Arrange — v6.17, no TagsDefined → falls into content=serialize path
        ConfigureConnectedClient(majorVersion: 6, minorVersion: 17);
        MockProjectEndpoint(); // no TagsDefined → int.TryParse fails → serialize path

        // Full project JSON: _datalogger uses the array-envelope format with log_item_groups wrapper
        var fullProjectJson = $$"""
            {
                "project": {
                    "common.ALLTYPES_NAME": "Default Project",
                    "_datalogger": [
                        {
                            "common.ALLTYPES_NAME": "_DataLogger",
                            "log_groups": [
                                {
                                    "common.ALLTYPES_NAME": "{{TEST_GROUP_NAME}}",
                                    "datalogger.LOG_GROUP_ENABLED": true,
                                    "log_item_groups": [
                                        {
                                            "common.ALLTYPES_NAME": "Log Items",
                                            "log_items": [
                                                {
                                                    "common.ALLTYPES_NAME": "Item1",
                                                    "datalogger.LOG_ITEM_SERVER_TAG": "Channel1.Device1.Tag1"
                                                }
                                            ]
                                        }
                                    ]
                                }
                            ]
                        }
                    ]
                }
            }
            """;

        _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, $"{TEST_ENDPOINT}{FULL_PROJECT_ENDPOINT}")
            .ReturnsResponse(HttpStatusCode.OK, fullProjectJson, "application/json");

        // Act
        var project = await _kepwareApiClient.Project.LoadProjectAsync(blnLoadFullProject: true);

        // Assert — SetOwnersFullProject flattens LogItemGroups[0].LogItems → LogGroup.LogItems
        project.DataLogger.ShouldNotBeNull();
        var logGroup = project.DataLogger.LogGroups?.FirstOrDefault();
        logGroup.ShouldNotBeNull();
        logGroup.LogItems.ShouldNotBeNull();
        logGroup.LogItems.Count.ShouldBe(1);
        logGroup.LogItems[0].Name.ShouldBe("Item1");
    }

    [Fact]
    public async Task CompareAndApplyAsync_WithMixedDataLoggerOperations_ShouldApplyAllOperationTypes()
    {
        // Arrange — exercises all three operation types in one call via ProjectApiHandler:
        //   source : Group1 (changed description + Item1 child)   — no Group3
        //   current: Group1 (no description, no children)          + Group3 (only in current)
        // Expected: 1 delete (Group3), 1 update (Group1 props), 1 insert (Item1)
        var sourceGroup = CreateGroup(TEST_GROUP_NAME, enabled: true);
        sourceGroup.SetDynamicProperty("common.ALLTYPES_DESCRIPTION", "updated description");
        sourceGroup.LogItems = new LogItemCollection { new LogItem("Item1") };

        var currentGroup = CreateGroup(TEST_GROUP_NAME, enabled: true); // no description → different hash
        currentGroup.LogItems = new LogItemCollection();

        var group3 = CreateGroup("Group3", enabled: true);

        var source  = new Project { DataLogger = new DataLoggerContainer { LogGroups = new LogGroupCollection { sourceGroup } } };
        var current = new Project { DataLogger = new DataLoggerContainer { LogGroups = new LogGroupCollection { currentGroup, group3 } } };

        // DELETE Group3 (only in current)
        _httpMessageHandlerMock.SetupRequest(HttpMethod.Delete, $"{LogGroupsEndpoint}/Group3")
            .ReturnsResponse(HttpStatusCode.OK);

        // GET Group1 then PUT Group1 (UpdateItemAsync fetches current state before PUT)
        var groupJson = $$"""{ "common.ALLTYPES_NAME": "{{TEST_GROUP_NAME}}", "datalogger.LOG_GROUP_ENABLED": true }""";
        _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, $"{LogGroupsEndpoint}/{TEST_GROUP_NAME}")
            .ReturnsResponse(HttpStatusCode.OK, groupJson, "application/json");

        _httpMessageHandlerMock.SetupRequest(HttpMethod.Put, $"{LogGroupsEndpoint}/{TEST_GROUP_NAME}")
            .ReturnsResponse(HttpStatusCode.OK);

        // POST Item1 (only in source group)
        _httpMessageHandlerMock.SetupRequest(HttpMethod.Post, LogItemsEndpoint)
            .ReturnsResponse(HttpStatusCode.OK);

        // GET column_mappings (re-fetched after log item insert)
        _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, ColumnMappingsEndpoint)
            .ReturnsResponse(HttpStatusCode.OK, "[]", "application/json");

        // Act
        var (inserts, updates, deletes) = await _kepwareApiClient.Project.CompareAndApplyAsync(source, current);

        // Assert
        inserts.ShouldBe(1);
        updates.ShouldBe(1);
        deletes.ShouldBe(1);
        _httpMessageHandlerMock.VerifyRequest(HttpMethod.Delete, $"{LogGroupsEndpoint}/Group3",        Times.Once());
        _httpMessageHandlerMock.VerifyRequest(HttpMethod.Put,    $"{LogGroupsEndpoint}/{TEST_GROUP_NAME}", Times.Once());
        _httpMessageHandlerMock.VerifyRequest(HttpMethod.Post,   LogItemsEndpoint,                     Times.Once());
    }

    [Fact]
    public async Task CompareAndApplyAsync_WithDataLoggerChanges_ShouldApplyInOrder()
    {
        // Arrange — source has Group1 + Item1; current has Group1 with no log items.
        //           Group properties are identical (UnchangedItems), but children differ.
        var sourceGroup = CreateGroup();
        sourceGroup.LogItems = new LogItemCollection { new LogItem("Item1") };

        var currentGroup = CreateGroup();
        currentGroup.LogItems = new LogItemCollection();

        var source  = new Project { DataLogger = new DataLoggerContainer { LogGroups = new LogGroupCollection { sourceGroup } } };
        var current = new Project { DataLogger = new DataLoggerContainer { LogGroups = new LogGroupCollection { currentGroup } } };

        // POST for the inserted log item
        _httpMessageHandlerMock.SetupRequest(HttpMethod.Post, LogItemsEndpoint)
            .ReturnsResponse(HttpStatusCode.OK);

        // GET column_mappings (re-fetched because log item was inserted)
        _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, ColumnMappingsEndpoint)
            .ReturnsResponse(HttpStatusCode.OK, "[]", "application/json");

        // Act
        var (inserts, updates, deletes) = await _kepwareApiClient.Project.CompareAndApplyAsync(source, current);

        // Assert
        inserts.ShouldBe(1);
        updates.ShouldBe(0);
        deletes.ShouldBe(0);
        _httpMessageHandlerMock.VerifyRequest(HttpMethod.Post, LogItemsEndpoint, Times.Once());
    }
}
