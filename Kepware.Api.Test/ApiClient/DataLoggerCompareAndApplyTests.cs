using Kepware.Api.Model;
using Moq;
using Moq.Contrib.HttpClient;
using Moq.Protected;
using Shouldly;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Kepware.Api.Test.ApiClient;

public class DataLoggerCompareAndApplyTests : TestApiClientBase
{
    private const string LOG_GROUPS_ENDPOINT = "/config/v1/project/_datalogger/log_groups";
    private const string TEST_GROUP_NAME = "TestGroup";

    private string LogGroupsEndpoint      => $"{TEST_ENDPOINT}{LOG_GROUPS_ENDPOINT}";
    private string LogGroupEndpoint       => $"{TEST_ENDPOINT}{LOG_GROUPS_ENDPOINT}/{TEST_GROUP_NAME}";
    private string LogItemsEndpoint       => $"{TEST_ENDPOINT}{LOG_GROUPS_ENDPOINT}/{TEST_GROUP_NAME}/log_items";
    private string ColumnMappingsEndpoint => $"{TEST_ENDPOINT}{LOG_GROUPS_ENDPOINT}/{TEST_GROUP_NAME}/column_mappings";
    private string TriggersEndpoint       => $"{TEST_ENDPOINT}{LOG_GROUPS_ENDPOINT}/{TEST_GROUP_NAME}/triggers";

    // ── helpers ──────────────────────────────────────────────────────────────

    private static LogGroup CreateGroup(string name, bool enabled = true, string? description = null)
    {
        var g = new LogGroup(name);
        g.Enabled = enabled;
        if (description != null)
            g.SetDynamicProperty("common.ALLTYPES_DESCRIPTION", description);
        return g;
    }

    private static DataLoggerContainer ContainerWith(params LogGroup[] groups)
    {
        var col = new LogGroupCollection();
        foreach (var g in groups) col.Add(g);
        return new DataLoggerContainer { LogGroups = col };
    }

    // ── tests ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task CompareAndApply_NoChanges_ShouldPerformNoOperations()
    {
        // Arrange — identical source and current produce the same hash → UnchangedItems
        var source  = ContainerWith(CreateGroup(TEST_GROUP_NAME, enabled: true));
        var current = ContainerWith(CreateGroup(TEST_GROUP_NAME, enabled: true));

        // Act
        var result = await _kepwareApiClient.Project.DataLogger.CompareAndApplyAsync(source, current);

        // Assert
        result.Inserts.ShouldBe(0);
        result.Updates.ShouldBe(0);
        result.Deletes.ShouldBe(0);
        result.Failures.ShouldBe(0);
        _httpMessageHandlerMock.VerifyRequest(HttpMethod.Post,   LogGroupsEndpoint, Times.Never());
        _httpMessageHandlerMock.VerifyRequest(HttpMethod.Put,    LogGroupEndpoint,  Times.Never());
        _httpMessageHandlerMock.VerifyRequest(HttpMethod.Delete, LogGroupEndpoint,  Times.Never());
    }

    [Fact]
    public async Task CompareAndApply_NewLogGroup_ShouldInsertGroup()
    {
        // Arrange — source has a new group; current is empty
        var source  = ContainerWith(CreateGroup(TEST_GROUP_NAME));
        var current = new DataLoggerContainer { LogGroups = new LogGroupCollection() };

        _httpMessageHandlerMock.SetupRequest(HttpMethod.Post, LogGroupsEndpoint)
            .ReturnsResponse(HttpStatusCode.OK);

        // Act
        var result = await _kepwareApiClient.Project.DataLogger.CompareAndApplyAsync(source, current);

        // Assert
        result.Inserts.ShouldBe(1);
        result.Deletes.ShouldBe(0);
        _httpMessageHandlerMock.VerifyRequest(HttpMethod.Post, LogGroupsEndpoint, Times.Once());
    }

    [Fact]
    public async Task CompareAndApply_NewLogGroup_WithChildren_ShouldSerializeChildrenInInsertBody()
    {
        // Arrange — source has a new log group with log items and triggers already populated.
        // InsertItemAsync serializes the full LogGroup (including children) as the POST body,
        // so the server receives one request that creates the group and all its children.
        // No separate POST to /log_items or /triggers should occur.
        var sourceGroup = CreateGroup(TEST_GROUP_NAME, enabled: true);

        var item = new LogItem("Item1") { Owner = sourceGroup };
        item.SetDynamicProperty("datalogger.LOG_ITEM_SERVER_TAG", "Channel1.Device1.Tag1");
        sourceGroup.LogItems = new LogItemCollection { item };

        var trigger = new Trigger("T1") { Owner = sourceGroup };
        trigger.SetDynamicProperty("datalogger.TRIGGER_TYPE", 0);
        sourceGroup.Triggers = new TriggerCollection { trigger };

        var source  = ContainerWith(sourceGroup);
        var current = new DataLoggerContainer { LogGroups = new LogGroupCollection() };

        string? postBody = null;
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.Is<HttpRequestMessage>(r => r.Method == HttpMethod.Post
                    && r.RequestUri!.ToString() == LogGroupsEndpoint),
                ItExpr.IsAny<System.Threading.CancellationToken>())
            .Returns<HttpRequestMessage, System.Threading.CancellationToken>(async (req, ct) =>
            {
                postBody = await req.Content!.ReadAsStringAsync(ct);
                return new HttpResponseMessage(HttpStatusCode.OK);
            });

        // Act
        var result = await _kepwareApiClient.Project.DataLogger.CompareAndApplyAsync(source, current);

        // Assert — one insert (the group) and children embedded in the POST body
        result.Inserts.ShouldBe(1);
        postBody.ShouldNotBeNull();
        postBody!.ShouldContain("\"log_items\"");
        postBody!.ShouldContain("\"Item1\"");
        postBody!.ShouldContain("\"triggers\"");
        postBody!.ShouldContain("\"T1\"");

        // No separate child endpoint POSTs — children travel with the group POST
        _httpMessageHandlerMock.VerifyRequest(HttpMethod.Post, LogGroupsEndpoint, Times.Once());
        _httpMessageHandlerMock.VerifyRequest(HttpMethod.Post, LogItemsEndpoint,  Times.Never());
        _httpMessageHandlerMock.VerifyRequest(HttpMethod.Post, TriggersEndpoint,  Times.Never());
    }

    [Fact]
    public async Task CompareAndApply_RemovedLogGroup_ShouldDeleteFromCorrectEndpoint()
    {
        // Arrange — current has a group that is absent from source
        var source  = new DataLoggerContainer { LogGroups = new LogGroupCollection() };
        var current = ContainerWith(CreateGroup(TEST_GROUP_NAME));

        _httpMessageHandlerMock.SetupRequest(HttpMethod.Delete, LogGroupEndpoint)
            .ReturnsResponse(HttpStatusCode.OK);

        // Act
        var result = await _kepwareApiClient.Project.DataLogger.CompareAndApplyAsync(source, current);

        // Assert
        result.Deletes.ShouldBe(1);
        result.Inserts.ShouldBe(0);
        _httpMessageHandlerMock.VerifyRequest(HttpMethod.Delete, LogGroupEndpoint, Times.Once());
    }

    [Fact]
    public async Task CompareAndApply_ChangedLogItem_ShouldApplyInOrder_LogGroupThenLogItemThenColumnMappingThenTrigger()
    {
        // Arrange — all four levels have a difference so all four PUT types fire
        var sourceGroup = CreateGroup(TEST_GROUP_NAME, enabled: true, description: "new");

        var srcItem = new LogItem("Item1") { Owner = sourceGroup };
        srcItem.SetDynamicProperty("datalogger.LOG_ITEM_DESCRIPTION", "new");
        sourceGroup.LogItems = new LogItemCollection { srcItem };

        var srcCm = new ColumnMapping("CM1") { Owner = sourceGroup };
        srcCm.SetDynamicProperty("datalogger.COLUMN_DESCRIPTION", "new");
        sourceGroup.ColumnMappings = new ColumnMappingCollection { srcCm };

        var srcT = new Trigger("T1") { Owner = sourceGroup };
        srcT.SetDynamicProperty("datalogger.TRIGGER_DESCRIPTION", "new");
        sourceGroup.Triggers = new TriggerCollection { srcT };

        var currentGroup = CreateGroup(TEST_GROUP_NAME, enabled: true); // no description → different hash

        var curItem = new LogItem("Item1") { Owner = currentGroup };
        curItem.SetDynamicProperty("datalogger.LOG_ITEM_DESCRIPTION", "old");
        currentGroup.LogItems = new LogItemCollection { curItem };

        var curCm = new ColumnMapping("CM1") { Owner = currentGroup };
        curCm.SetDynamicProperty("datalogger.COLUMN_DESCRIPTION", "old");
        currentGroup.ColumnMappings = new ColumnMappingCollection { curCm };

        var curT = new Trigger("T1") { Owner = currentGroup };
        curT.SetDynamicProperty("datalogger.TRIGGER_DESCRIPTION", "old");
        currentGroup.Triggers = new TriggerCollection { curT };

        var source  = ContainerWith(sourceGroup);
        var current = ContainerWith(currentGroup);

        // Capture the relative path of every PUT in arrival order
        var putPaths = new List<string>();
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.Is<HttpRequestMessage>(r => r.Method == HttpMethod.Put),
                ItExpr.IsAny<System.Threading.CancellationToken>())
            .Returns<HttpRequestMessage, System.Threading.CancellationToken>((req, _) =>
            {
                putPaths.Add(req.RequestUri!.AbsolutePath);
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
            });

        // Provide GET responses for entities that UpdateItemAsync fetches before PUT
        var groupJson = $$"""{ "common.ALLTYPES_NAME": "{{TEST_GROUP_NAME}}", "datalogger.LOG_GROUP_ENABLED": true }""";
        _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, LogGroupEndpoint)
            .ReturnsResponse(HttpStatusCode.OK, groupJson, "application/json");

        _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, $"{LogItemsEndpoint}/Item1")
            .ReturnsResponse(HttpStatusCode.OK, """{ "common.ALLTYPES_NAME": "Item1" }""", "application/json");

        _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, $"{TriggersEndpoint}/T1")
            .ReturnsResponse(HttpStatusCode.OK, """{ "common.ALLTYPES_NAME": "T1" }""", "application/json");

        _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, $"{ColumnMappingsEndpoint}/CM1")
            .ReturnsResponse(HttpStatusCode.OK, """{ "common.ALLTYPES_NAME": "CM1" }""", "application/json");

        // Act
        await _kepwareApiClient.Project.DataLogger.CompareAndApplyAsync(source, current);

        // Assert ordering: log group before log item before trigger; CM fits between item and trigger
        var groupPath  = $"{LOG_GROUPS_ENDPOINT}/{TEST_GROUP_NAME}";
        var itemPath   = $"{LOG_GROUPS_ENDPOINT}/{TEST_GROUP_NAME}/log_items/Item1";
        var cmPath     = $"{LOG_GROUPS_ENDPOINT}/{TEST_GROUP_NAME}/column_mappings/CM1";
        var trigPath   = $"{LOG_GROUPS_ENDPOINT}/{TEST_GROUP_NAME}/triggers/T1";

        int groupIdx  = putPaths.IndexOf(groupPath);
        int itemIdx   = putPaths.IndexOf(itemPath);
        int cmIdx     = putPaths.IndexOf(cmPath);
        int trigIdx   = putPaths.IndexOf(trigPath);

        groupIdx.ShouldBeGreaterThanOrEqualTo(0, "log group should receive a PUT");
        itemIdx.ShouldBeGreaterThanOrEqualTo(0,  "log item should receive a PUT");
        cmIdx.ShouldBeGreaterThanOrEqualTo(0,    "column mapping should receive a PUT");
        trigIdx.ShouldBeGreaterThanOrEqualTo(0,  "trigger should receive a PUT");

        groupIdx.ShouldBeLessThan(itemIdx,  "log group PUT must precede log item PUT");
        itemIdx.ShouldBeLessThan(cmIdx,     "log item PUT must precede column mapping PUT");
        cmIdx.ShouldBeLessThan(trigIdx,     "column mapping PUT must precede trigger PUT");
    }

    [Fact]
    public async Task CompareAndApply_WithAutoDisable_ShouldDisableBeforeAndReEnableAfter()
    {
        // Arrange — groups differ (description), both Enabled=true → ChangedItems → auto-disable fires
        var sourceGroup  = CreateGroup(TEST_GROUP_NAME, enabled: true, description: "new");
        var currentGroup = CreateGroup(TEST_GROUP_NAME, enabled: true); // no description → different hash

        var source  = ContainerWith(sourceGroup);
        var current = ContainerWith(currentGroup);

        var putBodies = new List<string>();
        var groupJson = $$"""{ "common.ALLTYPES_NAME": "{{TEST_GROUP_NAME}}", "datalogger.LOG_GROUP_ENABLED": true }""";

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.Is<HttpRequestMessage>(r => r.Method == HttpMethod.Get
                    && r.RequestUri!.ToString() == LogGroupEndpoint),
                ItExpr.IsAny<System.Threading.CancellationToken>())
            .Returns<HttpRequestMessage, System.Threading.CancellationToken>((_, _) =>
                Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(groupJson, Encoding.UTF8, "application/json"),
                }));

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.Is<HttpRequestMessage>(r => r.Method == HttpMethod.Put
                    && r.RequestUri!.ToString() == LogGroupEndpoint),
                ItExpr.IsAny<System.Threading.CancellationToken>())
            .Returns<HttpRequestMessage, System.Threading.CancellationToken>(async (req, ct) =>
            {
                putBodies.Add(await req.Content!.ReadAsStringAsync(ct));
                return new HttpResponseMessage(HttpStatusCode.OK);
            });

        // Act
        var result = await _kepwareApiClient.Project.DataLogger.CompareAndApplyAsync(
            source, current, autoDisable: true);

        // Assert — folded disable+update PUT then re-enable PUT (2 total, not 3)
        putBodies.Count.ShouldBe(2);
        putBodies[0].ShouldContain("\"datalogger.LOG_GROUP_ENABLED\": false"); // folded disable+update
        putBodies[1].ShouldContain("\"datalogger.LOG_GROUP_ENABLED\": true");  // re-enable
    }

    [Fact]
    public async Task CompareAndApply_RemovedTrigger_ShouldDeleteFromCorrectEndpoint()
    {
        // Arrange — source group has no triggers; current has T1
        var sourceGroup = CreateGroup(TEST_GROUP_NAME, enabled: true);
        sourceGroup.Triggers = new TriggerCollection();

        var currentGroup = CreateGroup(TEST_GROUP_NAME, enabled: true);
        var t1 = new Trigger("T1") { Owner = currentGroup };
        currentGroup.Triggers = new TriggerCollection { t1 };

        var source  = ContainerWith(sourceGroup);
        var current = ContainerWith(currentGroup);

        _httpMessageHandlerMock.SetupRequest(HttpMethod.Delete, $"{TriggersEndpoint}/T1")
            .ReturnsResponse(HttpStatusCode.OK);

        // Act
        var result = await _kepwareApiClient.Project.DataLogger.CompareAndApplyAsync(source, current);

        // Assert
        result.Deletes.ShouldBe(1);
        _httpMessageHandlerMock.VerifyRequest(HttpMethod.Delete, $"{TriggersEndpoint}/T1", Times.Once());
    }

    [Fact]
    public async Task CompareAndApply_AfterLogItemChanges_ShouldRefetchColumnMappingsBeforeApplying()
    {
        // Arrange — source adds a new log item; the column_mappings endpoint must be re-fetched
        // after the insert (server auto-generates mappings when log items are added)
        var sourceGroup = CreateGroup(TEST_GROUP_NAME, enabled: true);
        var newItem = new LogItem("Item1") { Owner = sourceGroup };
        sourceGroup.LogItems = new LogItemCollection { newItem };
        var srcCm = new ColumnMapping("CM1") { Owner = sourceGroup };
        srcCm.SetDynamicProperty("datalogger.COLUMN_DESCRIPTION", "from source");
        sourceGroup.ColumnMappings = new ColumnMappingCollection { srcCm };

        var currentGroup = CreateGroup(TEST_GROUP_NAME, enabled: true);
        currentGroup.LogItems        = new LogItemCollection();        // no items yet
        currentGroup.ColumnMappings  = new ColumnMappingCollection(); // no mappings yet

        var source  = ContainerWith(sourceGroup);
        var current = ContainerWith(currentGroup);

        // Mock POST for the new log item insert
        _httpMessageHandlerMock.SetupRequest(HttpMethod.Post, LogItemsEndpoint)
            .ReturnsResponse(HttpStatusCode.OK);

        // Mock GET for the required column_mappings re-fetch
        _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, ColumnMappingsEndpoint)
            .ReturnsResponse(HttpStatusCode.OK, "[]", "application/json");

        // Act
        var result = await _kepwareApiClient.Project.DataLogger.CompareAndApplyAsync(source, current);

        // Assert — log item inserted AND column_mappings was re-fetched exactly once
        result.Inserts.ShouldBe(1);
        _httpMessageHandlerMock.VerifyRequest(HttpMethod.Post, LogItemsEndpoint,       Times.Once());
        _httpMessageHandlerMock.VerifyRequest(HttpMethod.Get,  ColumnMappingsEndpoint, Times.Once());
    }

    [Fact]
    public async Task CompareAndApply_ColumnMappings_ShouldNeverCallDeleteOrPost()
    {
        // Arrange — source and current have the same group (same hash = UnchangedItems) but
        // different column mapping properties → only a PUT is issued, never DELETE or POST
        var sourceGroup  = CreateGroup(TEST_GROUP_NAME, enabled: true);
        var srcCm = new ColumnMapping("CM1") { Owner = sourceGroup };
        srcCm.SetDynamicProperty("datalogger.COLUMN_DESCRIPTION", "new");
        sourceGroup.ColumnMappings = new ColumnMappingCollection { srcCm };

        var currentGroup = CreateGroup(TEST_GROUP_NAME, enabled: true);
        var curCm = new ColumnMapping("CM1") { Owner = currentGroup };
        curCm.SetDynamicProperty("datalogger.COLUMN_DESCRIPTION", "old");
        currentGroup.ColumnMappings = new ColumnMappingCollection { curCm };

        var source  = ContainerWith(sourceGroup);
        var current = ContainerWith(currentGroup);

        // UpdateItemAsync always GETs before PUT
        _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, $"{ColumnMappingsEndpoint}/CM1")
            .ReturnsResponse(HttpStatusCode.OK, """{ "common.ALLTYPES_NAME": "CM1" }""", "application/json");

        // Only the PUT for the changed mapping should fire
        _httpMessageHandlerMock.SetupRequest(HttpMethod.Put, $"{ColumnMappingsEndpoint}/CM1")
            .ReturnsResponse(HttpStatusCode.OK);

        // Act
        var result = await _kepwareApiClient.Project.DataLogger.CompareAndApplyAsync(source, current);

        // Assert — exactly one update, zero deletes, and no POST or DELETE on the CM endpoints
        result.Updates.ShouldBe(1);
        _httpMessageHandlerMock.VerifyRequest(HttpMethod.Put,    $"{ColumnMappingsEndpoint}/CM1", Times.Once());
        _httpMessageHandlerMock.VerifyRequest(HttpMethod.Delete, $"{ColumnMappingsEndpoint}/CM1", Times.Never());
        _httpMessageHandlerMock.VerifyRequest(HttpMethod.Post,   ColumnMappingsEndpoint,          Times.Never());
    }

    [Fact]
    public async Task CompareAndApply_UnchangedGroupWithChildChanges_WithAutoDisable_ShouldDisableBeforeAndReEnableAfter()
    {
        // Arrange — source and current groups are identical (same hash → UnchangedItems) but a
        // log item differs.  With autoDisable=true the unchanged-but-enabled group must still be
        // disabled before child operations and re-enabled afterward.
        var sourceGroup  = CreateGroup(TEST_GROUP_NAME, enabled: true);
        var currentGroup = CreateGroup(TEST_GROUP_NAME, enabled: true); // same hash as source

        var srcItem = new LogItem("Item1") { Owner = sourceGroup };
        srcItem.SetDynamicProperty("datalogger.LOG_ITEM_DESCRIPTION", "new");
        sourceGroup.LogItems = new LogItemCollection { srcItem };

        var curItem = new LogItem("Item1") { Owner = currentGroup };
        curItem.SetDynamicProperty("datalogger.LOG_ITEM_DESCRIPTION", "old");
        currentGroup.LogItems = new LogItemCollection { curItem };

        var source  = ContainerWith(sourceGroup);
        var current = ContainerWith(currentGroup);

        // Capture every PUT URL + body so we can verify disable → item update → re-enable order.
        var putPaths       = new List<string>();
        var groupPutBodies = new List<string>();
        var groupPath      = $"{LOG_GROUPS_ENDPOINT}/{TEST_GROUP_NAME}";

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.Is<HttpRequestMessage>(r => r.Method == HttpMethod.Put),
                ItExpr.IsAny<System.Threading.CancellationToken>())
            .Returns<HttpRequestMessage, System.Threading.CancellationToken>(async (req, ct) =>
            {
                string path = req.RequestUri!.AbsolutePath;
                putPaths.Add(path);
                if (path == groupPath)
                    groupPutBodies.Add(await req.Content!.ReadAsStringAsync(ct));
                return new HttpResponseMessage(HttpStatusCode.OK);
            });

        var groupJson = $$"""{ "common.ALLTYPES_NAME": "{{TEST_GROUP_NAME}}", "datalogger.LOG_GROUP_ENABLED": true }""";
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.Is<HttpRequestMessage>(r => r.Method == HttpMethod.Get
                    && r.RequestUri!.ToString() == LogGroupEndpoint),
                ItExpr.IsAny<System.Threading.CancellationToken>())
            .Returns<HttpRequestMessage, System.Threading.CancellationToken>((_, _) =>
                Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(groupJson, Encoding.UTF8, "application/json"),
                }));

        _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, $"{LogItemsEndpoint}/Item1")
            .ReturnsResponse(HttpStatusCode.OK, """{ "common.ALLTYPES_NAME": "Item1" }""", "application/json");

        // Act
        var result = await _kepwareApiClient.Project.DataLogger.CompareAndApplyAsync(
            source, current, autoDisable: true);

        // Assert — ordering: group disable PUT first, log item PUT in middle, group re-enable PUT last
        putPaths.Count.ShouldBeGreaterThanOrEqualTo(3, "expected disable PUT, item PUT, re-enable PUT");
        putPaths.First().ShouldBe(groupPath, "group must be disabled before children are applied");
        putPaths.Last().ShouldBe(groupPath,  "group must be re-enabled after children are applied");

        groupPutBodies.Count.ShouldBe(2);
        groupPutBodies[0].ShouldContain("\"datalogger.LOG_GROUP_ENABLED\": false"); // disable
        groupPutBodies[1].ShouldContain("\"datalogger.LOG_GROUP_ENABLED\": true");  // re-enable

        result.Updates.ShouldBe(1); // one log item updated
    }

    [Fact]
    public async Task CompareAndApply_RemovedLogItem_ShouldDeleteFromCorrectEndpoint()
    {
        // Arrange — source group has 0 log items; current has 1
        var sourceGroup = CreateGroup(TEST_GROUP_NAME, enabled: true);
        sourceGroup.LogItems = new LogItemCollection();

        var currentGroup = CreateGroup(TEST_GROUP_NAME, enabled: true);
        var curItem = new LogItem("Item1") { Owner = currentGroup };
        currentGroup.LogItems = new LogItemCollection { curItem };

        var source  = ContainerWith(sourceGroup);
        var current = ContainerWith(currentGroup);

        _httpMessageHandlerMock.SetupRequest(HttpMethod.Delete, $"{LogItemsEndpoint}/Item1")
            .ReturnsResponse(HttpStatusCode.OK);

        // After log item delete, column mappings are re-fetched
        _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, ColumnMappingsEndpoint)
            .ReturnsResponse(HttpStatusCode.OK, "[]", "application/json");

        // Act
        var result = await _kepwareApiClient.Project.DataLogger.CompareAndApplyAsync(source, current);

        // Assert
        result.Deletes.ShouldBe(1);
        _httpMessageHandlerMock.VerifyRequest(HttpMethod.Delete, $"{LogItemsEndpoint}/Item1", Times.Once());
        _httpMessageHandlerMock.VerifyRequest(HttpMethod.Get, ColumnMappingsEndpoint, Times.Once());
    }

    [Fact]
    public async Task CompareAndApply_AfterLogItemDeletes_ShouldRefetchColumnMappingsAndApplyChanges()
    {
        // Arrange — deleting a log item causes the server to regenerate column mappings;
        // the handler must re-fetch and then apply updates to the new mappings
        var sourceGroup = CreateGroup(TEST_GROUP_NAME, enabled: true);
        sourceGroup.LogItems = new LogItemCollection(); // no items → item will be deleted
        var srcCm = new ColumnMapping("CM_Remaining") { Owner = sourceGroup };
        srcCm.SetDynamicProperty("datalogger.COLUMN_DESCRIPTION", "updated");
        sourceGroup.ColumnMappings = new ColumnMappingCollection { srcCm };

        var currentGroup = CreateGroup(TEST_GROUP_NAME, enabled: true);
        var curItem = new LogItem("Item1") { Owner = currentGroup };
        currentGroup.LogItems = new LogItemCollection { curItem };
        var curCm = new ColumnMapping("CM_Remaining") { Owner = currentGroup };
        curCm.SetDynamicProperty("datalogger.COLUMN_DESCRIPTION", "old");
        currentGroup.ColumnMappings = new ColumnMappingCollection { curCm };

        var source  = ContainerWith(sourceGroup);
        var current = ContainerWith(currentGroup);

        // Mock DELETE for the removed log item
        _httpMessageHandlerMock.SetupRequest(HttpMethod.Delete, $"{LogItemsEndpoint}/Item1")
            .ReturnsResponse(HttpStatusCode.OK);

        // Mock GET for column mapping re-fetch (server regenerated mappings after delete)
        var refetchedCmJson = """[{ "common.ALLTYPES_NAME": "CM_Remaining", "datalogger.COLUMN_DESCRIPTION": "old" }]""";
        _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, ColumnMappingsEndpoint)
            .ReturnsResponse(HttpStatusCode.OK, refetchedCmJson, "application/json");

        // Mock GET+PUT for the column mapping update
        _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, $"{ColumnMappingsEndpoint}/CM_Remaining")
            .ReturnsResponse(HttpStatusCode.OK, """{ "common.ALLTYPES_NAME": "CM_Remaining" }""", "application/json");
        _httpMessageHandlerMock.SetupRequest(HttpMethod.Put, $"{ColumnMappingsEndpoint}/CM_Remaining")
            .ReturnsResponse(HttpStatusCode.OK);

        // Act
        var result = await _kepwareApiClient.Project.DataLogger.CompareAndApplyAsync(source, current);

        // Assert — re-fetch happened and the column mapping was updated
        _httpMessageHandlerMock.VerifyRequest(HttpMethod.Get, ColumnMappingsEndpoint, Times.Once());
        _httpMessageHandlerMock.VerifyRequest(HttpMethod.Put, $"{ColumnMappingsEndpoint}/CM_Remaining", Times.Once());
        result.Deletes.ShouldBeGreaterThanOrEqualTo(1);
        result.Updates.ShouldBeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task CompareAndApply_WhenInsertFails_ShouldIncrementFailureCount()
    {
        // Arrange — source has a new group; POST returns 500
        var source  = ContainerWith(CreateGroup(TEST_GROUP_NAME));
        var current = new DataLoggerContainer { LogGroups = new LogGroupCollection() };

        _httpMessageHandlerMock.SetupRequest(HttpMethod.Post, LogGroupsEndpoint)
            .ReturnsResponse(HttpStatusCode.InternalServerError, "Internal Server Error");

        // Act
        var result = await _kepwareApiClient.Project.DataLogger.CompareAndApplyAsync(source, current);

        // Assert
        result.Inserts.ShouldBe(0);
        result.Failures.ShouldBeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task CompareAndApply_WhenDeleteFails_ShouldIncrementFailureCount()
    {
        // Arrange — current has a group not in source; DELETE returns 500
        var source  = new DataLoggerContainer { LogGroups = new LogGroupCollection() };
        var current = ContainerWith(CreateGroup(TEST_GROUP_NAME));

        _httpMessageHandlerMock.SetupRequest(HttpMethod.Delete, LogGroupEndpoint)
            .ReturnsResponse(HttpStatusCode.InternalServerError, "Internal Server Error");

        // Act
        var result = await _kepwareApiClient.Project.DataLogger.CompareAndApplyAsync(source, current);

        // Assert
        result.Deletes.ShouldBe(0);
        result.Failures.ShouldBeGreaterThanOrEqualTo(1);
    }
}
