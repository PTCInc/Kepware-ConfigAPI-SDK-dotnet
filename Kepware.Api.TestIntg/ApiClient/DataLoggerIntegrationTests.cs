using Kepware.Api.Model;
using Shouldly;

namespace Kepware.Api.TestIntg.ApiClient;

public class DataLoggerIntegrationTests : TestIntgApiClientBase
{
    /// <summary>
    /// Checks whether the DataLogger plug-in is available on the connected server.
    /// </summary>
    private bool HasDataLoggerPlugIn()
    {
        if (_productInfo.ProductType == ProductType.KepwareEdge)
        {
            return false;
        }

        try
        {
            var groups = _kepwareApiClient.Project.DataLogger.GetLogGroupsAsync().GetAwaiter().GetResult();
            return true; // endpoint responded — plug-in is present
        }
        catch
        {
            return false;
        }
    }

    #region Log Group CRUD Tests

    [SkippableFact]
    public async Task CreateLogGroup_ShouldReturnLogGroup()
    {
        Skip.If(!HasDataLoggerPlugIn(), "Requires DataLogger plug-in");

        // Act
        var result = await _kepwareApiClient.Project.DataLogger.CreateLogGroupAsync("TestLogGroup");

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe("TestLogGroup");

        // Clean up
        await DeleteAllLogGroupsAsync();
    }

    [SkippableFact]
    public async Task GetLogGroup_WhenExists_ShouldReturnLogGroup()
    {
        Skip.If(!HasDataLoggerPlugIn(), "Requires DataLogger plug-in");

        // Arrange
        await AddTestLogGroup("TestLogGroup");

        // Act
        var result = await _kepwareApiClient.Project.DataLogger.GetLogGroupAsync("TestLogGroup");

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe("TestLogGroup");

        // Clean up
        await DeleteAllLogGroupsAsync();
    }

    [SkippableFact]
    public async Task GetLogGroups_ShouldReturnCollection()
    {
        Skip.If(!HasDataLoggerPlugIn(), "Requires DataLogger plug-in");

        // Arrange
        await AddTestLogGroup("TestLogGroup");

        // Act
        var result = await _kepwareApiClient.Project.DataLogger.GetLogGroupsAsync();

        // Assert
        result.ShouldNotBeNull();
        result.Count.ShouldBeGreaterThanOrEqualTo(1);

        // Clean up
        await DeleteAllLogGroupsAsync();
    }

    [SkippableFact]
    public async Task UpdateLogGroup_ShouldReturnTrue()
    {
        Skip.If(!HasDataLoggerPlugIn(), "Requires DataLogger plug-in");

        // Arrange
        var group = await AddTestLogGroup("TestLogGroup");
        group.Enabled = false;

        // Act
        var result = await _kepwareApiClient.Project.DataLogger.UpdateLogGroupAsync(group);

        // Assert
        result.ShouldBeTrue();

        // Clean up
        await DeleteAllLogGroupsAsync();
    }

    [SkippableFact]
    public async Task DeleteLogGroup_ByEntity_ShouldReturnTrue()
    {
        Skip.If(!HasDataLoggerPlugIn(), "Requires DataLogger plug-in");

        // Arrange
        var group = await AddTestLogGroup("TestLogGroup");

        // Act
        var result = await _kepwareApiClient.Project.DataLogger.DeleteLogGroupAsync(group);

        // Assert
        result.ShouldBeTrue();
    }

    [SkippableFact]
    public async Task DeleteLogGroup_ByName_ShouldReturnTrue()
    {
        Skip.If(!HasDataLoggerPlugIn(), "Requires DataLogger plug-in");

        // Arrange
        await AddTestLogGroup("TestLogGroup");

        // Act
        var result = await _kepwareApiClient.Project.DataLogger.DeleteLogGroupAsync("TestLogGroup");

        // Assert
        result.ShouldBeTrue();
    }

    #endregion

    #region Log Item CRUD Tests

    [SkippableFact]
    public async Task CreateLogItem_ShouldReturnLogItem()
    {
        Skip.If(!HasDataLoggerPlugIn(), "Requires DataLogger plug-in");

        // Arrange
        var group = await AddTestLogGroup("TestLogGroup");
        var item = new LogItem("TestItem")
        {
            ServerItem = "_System._Time"
        };
        var properties = item.DynamicProperties.ToDictionary(kvp => kvp.Key, kvp => (object)kvp.Value);

        // Act
        var result = await _kepwareApiClient.Project.DataLogger.CreateLogItemAsync("TestItem", group, properties: properties);

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe("TestItem");

        // Clean up
        await DeleteAllLogGroupsAsync();
    }

    [SkippableFact]
    public async Task GetLogItem_WhenExists_ShouldReturnLogItem()
    {
        Skip.If(!HasDataLoggerPlugIn(), "Requires DataLogger plug-in");

        // Arrange
        var group = await AddTestLogGroup("TestLogGroup");

        var item = new LogItem("TestItem")
        {
            ServerItem = "_System._Time"
        };
        var properties = item.DynamicProperties.ToDictionary(kvp => kvp.Key, kvp => (object)kvp.Value);
        await _kepwareApiClient.Project.DataLogger.CreateLogItemAsync("TestItem", group, properties: properties);

        // Act
        var result = await _kepwareApiClient.Project.DataLogger.GetLogItemAsync("TestItem", group);

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe("TestItem");

        // Clean up
        await DeleteAllLogGroupsAsync();
    }

    [SkippableFact]
    public async Task GetLogItems_ShouldReturnCollection()
    {
        Skip.If(!HasDataLoggerPlugIn(), "Requires DataLogger plug-in");

        // Arrange
        var group = await AddTestLogGroup("TestLogGroup");
        var item = new LogItem("TestItem")
        {
            ServerItem = "_System._Time"
        };
        var properties = item.DynamicProperties.ToDictionary(kvp => kvp.Key, kvp => (object)kvp.Value);
        await _kepwareApiClient.Project.DataLogger.CreateLogItemAsync("TestItem", group, properties: properties);

        // Act
        var result = await _kepwareApiClient.Project.DataLogger.GetLogItemsAsync(group);

        // Assert
        result.ShouldNotBeNull();
        result.Count.ShouldBeGreaterThanOrEqualTo(1);

        // Clean up
        await DeleteAllLogGroupsAsync();
    }

    [SkippableFact]
    public async Task DeleteLogItem_ByEntity_ShouldReturnTrue()
    {
        Skip.If(!HasDataLoggerPlugIn(), "Requires DataLogger plug-in");

        // Arrange
        var group = await AddTestLogGroup("TestLogGroup");
        var item = new LogItem("TestItem")
        {
            ServerItem = "_System._Time"
        };
        var properties = item.DynamicProperties.ToDictionary(kvp => kvp.Key, kvp => (object)kvp.Value);
        var logItem = await _kepwareApiClient.Project.DataLogger.GetOrCreateLogItemAsync("TestItem", group, properties: properties);

        // Act
        var result = await _kepwareApiClient.Project.DataLogger.DeleteLogItemAsync(logItem, group);

        // Assert
        result.ShouldBeTrue();

        // Clean up
        await DeleteAllLogGroupsAsync();
    }

    [SkippableFact]
    public async Task DeleteLogItem_ByName_ShouldReturnTrue()
    {
        Skip.If(!HasDataLoggerPlugIn(), "Requires DataLogger plug-in");

        // Arrange
        var group = await AddTestLogGroup("TestLogGroup");
        var item = new LogItem("TestItem")
        {
            ServerItem = "_System._Time"
        };
        var properties = item.DynamicProperties.ToDictionary(kvp => kvp.Key, kvp => (object)kvp.Value);
        await _kepwareApiClient.Project.DataLogger.GetOrCreateLogItemAsync("TestItem", group, properties: properties);

        // Act
        var result = await _kepwareApiClient.Project.DataLogger.DeleteLogItemAsync("TestItem", group);

        // Assert
        result.ShouldBeTrue();

        // Clean up
        await DeleteAllLogGroupsAsync();
    }

    #endregion

    #region Column Mapping Tests

    [SkippableFact]
    public async Task GetColumnMappings_ShouldReturnCollection()
    {
        Skip.If(!HasDataLoggerPlugIn(), "Requires DataLogger plug-in");

        // Arrange — create a group with a log item (server auto-generates column mappings)
        var group = await AddTestLogGroup("TestLogGroup");
        var item = new LogItem("TestItem")
        {
            ServerItem = "_System._Time"
        };
        var properties = item.DynamicProperties.ToDictionary(kvp => kvp.Key, kvp => (object)kvp.Value);
        await _kepwareApiClient.Project.DataLogger.GetOrCreateLogItemAsync("TestItem", group, properties: properties);

        // Act
        var result = await _kepwareApiClient.Project.DataLogger.GetColumnMappingsAsync(group);

        // Assert
        result.ShouldNotBeNull();
        result.Count.ShouldBeGreaterThanOrEqualTo(1);

        // Clean up
        await DeleteAllLogGroupsAsync();
    }

    [SkippableFact]
    public async Task GetColumnMapping_WhenExists_ShouldReturnMapping()
    {
        Skip.If(!HasDataLoggerPlugIn(), "Requires DataLogger plug-in");

        // Arrange — create a group with a log item and find the auto-generated mapping
        var group = await AddTestLogGroup("TestLogGroup");
        var item = new LogItem("TestItem")
        {
            ServerItem = "_System._Time"
        };
        var properties = item.DynamicProperties.ToDictionary(kvp => kvp.Key, kvp => (object)kvp.Value);
        var logItem = await _kepwareApiClient.Project.DataLogger.GetOrCreateLogItemAsync("TestItem", group, properties: properties);
        var mappings = await _kepwareApiClient.Project.DataLogger.GetColumnMappingsAsync(group);
        var mappingName = mappings![0].Name;

        // Act
        var result = await _kepwareApiClient.Project.DataLogger.GetColumnMappingAsync(mappingName, group);

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe(mappingName);

        // Clean up
        await DeleteAllLogGroupsAsync();
    }

    [SkippableFact]
    public async Task UpdateColumnMapping_ShouldReturnTrue()
    {
        Skip.If(!HasDataLoggerPlugIn(), "Requires DataLogger plug-in");

        // Arrange
        var group = await AddTestLogGroup("TestLogGroup");
        var item = new LogItem("TestItem")
        {
            ServerItem = "_System._Time"
        };
        var properties = item.DynamicProperties.ToDictionary(kvp => kvp.Key, kvp => (object)kvp.Value);
        await _kepwareApiClient.Project.DataLogger.CreateLogItemAsync("TestItem", group, properties: properties);
        var mappings = await _kepwareApiClient.Project.DataLogger.GetColumnMappingsAsync(group);
        var mapping = mappings![0];
        mapping.Owner = group;

        // Act
        var result = await _kepwareApiClient.Project.DataLogger.UpdateColumnMappingAsync(mapping, group);

        // Assert
        result.ShouldBeTrue();

        // Clean up
        await DeleteAllLogGroupsAsync();
    }

    #endregion

    #region Trigger CRUD Tests

    [SkippableFact]
    public async Task CreateTrigger_ShouldReturnTrigger()
    {
        Skip.If(!HasDataLoggerPlugIn(), "Requires DataLogger plug-in");

        // Arrange
        var group = await AddTestLogGroup("TestLogGroup");

        // Act
        var result = await _kepwareApiClient.Project.DataLogger.CreateTriggerAsync("TestTrigger", group);

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe("TestTrigger");

        // Clean up
        await DeleteAllLogGroupsAsync();
    }

    [SkippableFact]
    public async Task GetTrigger_WhenExists_ShouldReturnTrigger()
    {
        Skip.If(!HasDataLoggerPlugIn(), "Requires DataLogger plug-in");

        // Arrange
        var group = await AddTestLogGroup("TestLogGroup");
        await _kepwareApiClient.Project.DataLogger.CreateTriggerAsync("TestTrigger", group);

        // Act
        var result = await _kepwareApiClient.Project.DataLogger.GetTriggerAsync("TestTrigger", group);

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe("TestTrigger");

        // Clean up
        await DeleteAllLogGroupsAsync();
    }

    [SkippableFact]
    public async Task GetTriggers_ShouldReturnCollection()
    {
        Skip.If(!HasDataLoggerPlugIn(), "Requires DataLogger plug-in");

        // Arrange
        var group = await AddTestLogGroup("TestLogGroup");
        await _kepwareApiClient.Project.DataLogger.CreateTriggerAsync("TestTrigger", group);

        // Act
        var result = await _kepwareApiClient.Project.DataLogger.GetTriggersAsync(group);

        // Assert
        result.ShouldNotBeNull();
        result.Count.ShouldBeGreaterThanOrEqualTo(1);

        // Clean up
        await DeleteAllLogGroupsAsync();
    }

    [SkippableFact]
    public async Task DeleteTrigger_ByEntity_ShouldReturnTrue()
    {
        Skip.If(!HasDataLoggerPlugIn(), "Requires DataLogger plug-in");

        // Arrange
        var group = await AddTestLogGroup("TestLogGroup");
        var trigger = await _kepwareApiClient.Project.DataLogger.GetOrCreateTriggerAsync("TestTrigger", group);

        // Act
        var result = await _kepwareApiClient.Project.DataLogger.DeleteTriggerAsync(trigger, group);

        // Assert
        result.ShouldBeTrue();

        // Clean up
        await DeleteAllLogGroupsAsync();
    }

    [SkippableFact]
    public async Task DeleteTrigger_ByName_ShouldReturnTrue()
    {
        Skip.If(!HasDataLoggerPlugIn(), "Requires DataLogger plug-in");

        // Arrange
        var group = await AddTestLogGroup("TestLogGroup");
        await _kepwareApiClient.Project.DataLogger.CreateTriggerAsync("TestTrigger", group);

        // Act
        var result = await _kepwareApiClient.Project.DataLogger.DeleteTriggerAsync("TestTrigger", group);

        // Assert
        result.ShouldBeTrue();

        // Clean up
        await DeleteAllLogGroupsAsync();
    }

    #endregion

    #region ResetColumnMapping Tests

    [SkippableFact]
    public async Task ResetColumnMapping_ShouldReturnSuccessfulJobPromise()
    {
        Skip.If(!HasDataLoggerPlugIn(), "Requires DataLogger plug-in");

        // Arrange
        var group = await AddTestLogGroup("TestLogGroup");
        var item = new LogItem("TestItem")
        {
            ServerItem = "_System._Time"
        };
        var properties = item.DynamicProperties.ToDictionary(kvp => kvp.Key, kvp => (object)kvp.Value);
        await _kepwareApiClient.Project.DataLogger.CreateLogItemAsync("TestItem", group, properties: properties);

        // Act
        var promise = await _kepwareApiClient.Project.DataLogger.ResetColumnMappingAsync(group);
        var result = await promise.AwaitCompletionAsync();

        // Assert
        promise.ShouldNotBeNull();
        result.IsSuccess.ShouldBeTrue();

        // Clean up
        await DeleteAllLogGroupsAsync();
    }

    #endregion

    #region CompareAndApply Tests

    [SkippableFact]
    public async Task CompareAndApply_RoundTrip_ShouldApplyChanges()
    {
        Skip.If(!HasDataLoggerPlugIn(), "Requires DataLogger plug-in");

        try
        {
            // ── Seed the server with two log groups ────────────────────────────────
            // GroupA: will stay unchanged at group level but children will be added/updated/deleted
            // GroupB: enabled, autoDisable will temporarily disable it while children change
            var groupA = await AddTestLogGroup("GroupA");
            groupA.Enabled = false; 
            await _kepwareApiClient.Project.DataLogger.UpdateLogGroupAsync(groupA);

            var groupB = await AddTestLogGroup("GroupB");
            groupB.Enabled = true;
            await _kepwareApiClient.Project.DataLogger.UpdateLogGroupAsync(groupB);

            // Add log items and triggers to both groups on the server
            var itemA1Props = new LogItem("ItemA_Keep") { ServerItem = "_System._Time" }
                .DynamicProperties.ToDictionary(kvp => kvp.Key, kvp => (object)kvp.Value);
            await _kepwareApiClient.Project.DataLogger.CreateLogItemAsync("ItemA_Keep", groupA, properties: itemA1Props);

            var itemA2Props = new LogItem("ItemA_Delete") { ServerItem = "_System._Date" }
                .DynamicProperties.ToDictionary(kvp => kvp.Key, kvp => (object)kvp.Value);
            await _kepwareApiClient.Project.DataLogger.CreateLogItemAsync("ItemA_Delete", groupA, properties: itemA2Props);

            await _kepwareApiClient.Project.DataLogger.CreateTriggerAsync("TriggerA_Keep", groupA);
            await _kepwareApiClient.Project.DataLogger.CreateTriggerAsync("TriggerA_Delete", groupA);

            var itemB1Props = new LogItem("ItemB_Keep") { ServerItem = "_System._Time" }
                .DynamicProperties.ToDictionary(kvp => kvp.Key, kvp => (object)kvp.Value);
            await _kepwareApiClient.Project.DataLogger.CreateLogItemAsync("ItemB_Keep", groupB, properties: itemB1Props);

            await _kepwareApiClient.Project.DataLogger.CreateTriggerAsync("TriggerB_Keep", groupB);

            // ── Load current state from server (with all children) ─────────────────
            var current = await LoadDataLoggerContainerAsync();

            // ── Build source state from a second server load ────────────────────────
            // Loading twice gives us separate object graphs with identical hashes.
            // GroupA: same group properties → hash match → "unchanged" group with child changes
            // GroupB: same group properties → hash match → "unchanged" group with child changes + autoDisable
            var sourceLoad = await LoadDataLoggerContainerAsync();

            var srcGroupA = sourceLoad.LogGroups!.First(g => g.Name == "GroupA");
            // Replace children: keep ItemA_Keep (updated), drop ItemA_Delete, add ItemA_New
            srcGroupA.LogItems = new LogItemCollection();
            srcGroupA.ColumnMappings = null; // let server manage column mappings

            var srcItemAKeep = new LogItem("ItemA_Keep") { Owner = srcGroupA, ServerItem = "_System._Time" };
            srcItemAKeep.Description ="updated by CompareAndApply";
            srcGroupA.LogItems.Add(srcItemAKeep);

            var srcItemANew = new LogItem("ItemA_New") { Owner = srcGroupA, ServerItem = "_System._Date" };
            srcGroupA.LogItems.Add(srcItemANew);

            // Replace triggers: keep TriggerA_Keep (updated), drop TriggerA_Delete, add TriggerA_New
            srcGroupA.Triggers = new TriggerCollection();

            var srcTriggerAKeep = new Trigger("TriggerA_Keep") { Owner = srcGroupA };
            srcTriggerAKeep.Description ="updated trigger";
            srcGroupA.Triggers.Add(srcTriggerAKeep);

            var srcTriggerANew = new Trigger("TriggerA_New") { Owner = srcGroupA };
            srcGroupA.Triggers.Add(srcTriggerANew);

            // GroupB: children change → autoDisable should disable then re-enable
            // Keep ItemB_Keep, add ItemB_New, remove TriggerB_Keep
            var srcGroupB = sourceLoad.LogGroups!.First(g => g.Name == "GroupB");
            srcGroupB.LogItems = new LogItemCollection();
            srcGroupB.ColumnMappings = null;

            var srcItemBKeep = new LogItem("ItemB_Keep") { Owner = srcGroupB, ServerItem = "_System._Time" };
            srcGroupB.LogItems.Add(srcItemBKeep);

            var srcItemBNew = new LogItem("ItemB_New") { Owner = srcGroupB, ServerItem = "_System._Date" };
            srcGroupB.LogItems.Add(srcItemBNew);

            // TriggerB_Keep deliberately absent → will be deleted
            srcGroupB.Triggers = new TriggerCollection();

            var source = new DataLoggerContainer
            {
                LogGroups = new LogGroupCollection { srcGroupA, srcGroupB }
            };

            // ── Act ────────────────────────────────────────────────────────────────
            var result = await _kepwareApiClient.Project.DataLogger.CompareAndApplyAsync(
                source, current, autoDisable: true);

            // ── Assert — verify the result counters ────────────────────────────────
            result.ShouldNotBeNull();
            result.Failures.ShouldBe(0, "no operations should have failed");

            // ── Assert — verify actual server state ────────────────────────────────

            // Reload groups from the server
            var serverGroupA = await _kepwareApiClient.Project.DataLogger.GetLogGroupAsync("GroupA");
            var serverGroupB = await _kepwareApiClient.Project.DataLogger.GetLogGroupAsync("GroupB");

            // Both groups should still be enabled (autoDisable re-enables after apply)
            serverGroupA.ShouldNotBeNull();
            serverGroupA.Enabled.ShouldBe(false, "GroupA should remain disabled after apply");
            serverGroupB.ShouldNotBeNull();
            serverGroupB.Enabled.ShouldBe(true, "GroupB should be re-enabled after autoDisable apply");

            // GroupA log items: ItemA_Keep (updated) + ItemA_New; ItemA_Delete gone
            var serverItemsA = await _kepwareApiClient.Project.DataLogger.GetLogItemsAsync(serverGroupA);
            serverItemsA.ShouldNotBeNull();
            serverItemsA.Select(i => i.Name).ShouldContain("ItemA_Keep");
            serverItemsA.Select(i => i.Name).ShouldContain("ItemA_New");
            serverItemsA.Select(i => i.Name).ShouldNotContain("ItemA_Delete");

            // GroupA triggers: TriggerA_Keep (updated) + TriggerA_New; TriggerA_Delete gone
            var serverTriggersA = await _kepwareApiClient.Project.DataLogger.GetTriggersAsync(serverGroupA);
            serverTriggersA.ShouldNotBeNull();
            serverTriggersA.Select(t => t.Name).ShouldContain("TriggerA_Keep");
            serverTriggersA.Select(t => t.Name).ShouldContain("TriggerA_New");
            serverTriggersA.Select(t => t.Name).ShouldNotContain("TriggerA_Delete");

            // GroupB log items: ItemB_Keep + ItemB_New
            var serverItemsB = await _kepwareApiClient.Project.DataLogger.GetLogItemsAsync(serverGroupB);
            serverItemsB.ShouldNotBeNull();
            serverItemsB.Select(i => i.Name).ShouldContain("ItemB_Keep");
            serverItemsB.Select(i => i.Name).ShouldContain("ItemB_New");

            // GroupB triggers: TriggerB_Keep should be deleted
            var serverTriggersB = await _kepwareApiClient.Project.DataLogger.GetTriggersAsync(serverGroupB);
            serverTriggersB.ShouldNotBeNull();
            serverTriggersB.Select(t => t.Name).ShouldNotContain("TriggerB_Keep");
        }
        finally
        {
            // Clean up
            await DeleteAllLogGroupsAsync();
        }
    }

    /// <summary>
    /// Loads the full DataLogger container from the server, including all child collections
    /// (log items, column mappings, triggers) for each log group.
    /// </summary>
    private async Task<DataLoggerContainer> LoadDataLoggerContainerAsync()
    {
        var logGroups = await _kepwareApiClient.Project.DataLogger.GetLogGroupsAsync();
        if (logGroups != null)
        {
            foreach (var group in logGroups)
            {
                group.LogItems = await _kepwareApiClient.Project.DataLogger.GetLogItemsAsync(group);
                group.ColumnMappings = await _kepwareApiClient.Project.DataLogger.GetColumnMappingsAsync(group);
                group.Triggers = await _kepwareApiClient.Project.DataLogger.GetTriggersAsync(group);
            }
        }

        return new DataLoggerContainer { LogGroups = logGroups ?? new LogGroupCollection() };
    }

    #endregion
}
