using System.Text.Json;
using Kepware.Api.Model;
using Kepware.Api.Serializer;

namespace Kepware.Api.Test.Model.DataLogger
{
    public class LogGroupModelTests
    {
        [Fact]
        public void LogGroup_Deserialize_ShouldPopulateProperties()
        {
            var json = """
            {
                "common.ALLTYPES_NAME": "TestGroup",
                "common.ALLTYPES_DESCRIPTION": "A test log group",
                "datalogger.LOG_GROUP_ENABLED": false,
                "datalogger.LOG_GROUP_UPDATE_RATE_MSEC": 100,
                "datalogger.LOG_GROUP_UPDATE_RATE_UNITS": 0,
                "datalogger.LOG_GROUP_MAP_NUMERIC_ID_TO_VARCHAR": false,
                "datalogger.LOG_GROUP_USE_LOCAL_TIME_FOR_TIMESTAMP_INSERTS": true,
                "datalogger.LOG_GROUP_STORE_AND_FORWARD_ENABLED": false,
                "datalogger.LOG_GROUP_MAX_ROW_BUFFER_SIZE": 1000,
                "datalogger.LOG_GROUP_DSN": "MyDSN",
                "datalogger.LOG_GROUP_DSN_USERNAME": "sa",
                "datalogger.LOG_GROUP_DSN_PASSWORD": "secret",
                "datalogger.LOG_GROUP_DSN_LOGIN_TIMEOUT": 10,
                "datalogger.LOG_GROUP_DSN_QUERY_TIMEOUT": 15,
                "datalogger.LOG_GROUP_TABLE_SELECTION": 0,
                "datalogger.LOG_GROUP_TABLE_NAME": "LogTable",
                "datalogger.LOG_GROUP_TABLE_FORMAT": 0,
                "datalogger.LOG_GROUP_BATCH_ID_ITEM": "",
                "datalogger.LOG_GROUP_BATCH_ID_ITEM_TYPE": "Default",
                "datalogger.LOG_GROUP_BATCH_ID_UPDATE_RATE": 1000,
                "datalogger.LOG_GROUP_BATCH_ID_UPDATE_RATE_UNITS": 0,
                "datalogger.LOG_GROUP_REGENERATE_ALIAS_TABLE_ON_DSN_CHANGE": true,
                "datalogger.LOG_GROUP_REGENERATE_ALIAS_TABLE_ON_BATCH_ID_CHANGE": true,
                "datalogger.LOG_GROUP_REGENERATE_ALIAS_TABLE_ON_TABLE_NAME_CHANGE": false,
                "datalogger.LOG_GROUP_REGENERATE_ALIAS_TABLE_ON_TABLE_SELECTION_CHANGE": false
            }
            """;

            var group = JsonSerializer.Deserialize<LogGroup>(json, KepJsonContext.Default.LogGroup);

            Assert.NotNull(group);
            Assert.Equal("TestGroup", group.Name);
            Assert.Equal("A test log group", group.Description);
            Assert.False(group.Enabled);
            Assert.Equal(100, group.UpdateRate);
            Assert.Equal(0, group.UpdateRateUnits);
            Assert.False(group.MapNumericIdToVarchar);
            Assert.True(group.UseLocalTimeForTimestamp);
            Assert.False(group.StoreAndForwardEnabled);
            Assert.Equal(1000, group.MaxRowBufferSize);
            Assert.Equal("MyDSN", group.Dsn);
            Assert.Equal("sa", group.DsnUsername);
            Assert.Equal("secret", group.DsnPassword);
            Assert.Equal(10, group.DsnLoginTimeout);
            Assert.Equal(15, group.DsnQueryTimeout);
            Assert.Equal(0, group.TableSelection);
            Assert.Equal("LogTable", group.TableName);
            Assert.Equal(0, group.TableFormat);
            Assert.Equal("Default", group.BatchIdItemType);
            Assert.Equal(1000, group.BatchIdUpdateRate);
            Assert.True(group.RegenerateOnDsnChange);
            Assert.True(group.RegenerateOnBatchIdChange);
            Assert.False(group.RegenerateOnTableNameChange);
            Assert.False(group.RegenerateOnTableSelectionChange);
        }

        [Fact]
        public void LogGroup_SetProperties_ShouldUpdateDynamicProperties()
        {
            var group = new LogGroup("TestGroup");

            group.Enabled = true;
            group.UpdateRate = 500;
            group.UpdateRateUnits = 1;
            group.MapNumericIdToVarchar = true;
            group.UseLocalTimeForTimestamp = false;
            group.StoreAndForwardEnabled = true;
            group.StoreAndForwardStorageDirectory = @"C:\Logs";
            group.StoreAndForwardMaxStorageSizeMb = 50;
            group.MaxRowBufferSize = 2000;
            group.Dsn = "ProductionDSN";
            group.DsnUsername = "admin";
            group.DsnPassword = "pass123";
            group.DsnLoginTimeout = 30;
            group.DsnQueryTimeout = 60;
            group.TableSelection = 1;
            group.TableName = "DataLog";
            group.TableFormat = 1;
            group.BatchIdItem = "Channel1.Device1.BatchTag";
            group.BatchIdUpdateRate = 2000;
            group.BatchIdUpdateRateUnits = 0;
            group.RegenerateOnDsnChange = false;
            group.RegenerateOnBatchIdChange = false;
            group.RegenerateOnTableNameChange = true;
            group.RegenerateOnTableSelectionChange = true;

            Assert.Equal("TestGroup", group.Name);
            Assert.True(group.Enabled);
            Assert.Equal(500, group.UpdateRate);
            Assert.Equal(1, group.UpdateRateUnits);
            Assert.True(group.MapNumericIdToVarchar);
            Assert.False(group.UseLocalTimeForTimestamp);
            Assert.True(group.StoreAndForwardEnabled);
            Assert.Equal(@"C:\Logs", group.StoreAndForwardStorageDirectory);
            Assert.Equal(50, group.StoreAndForwardMaxStorageSizeMb);
            Assert.Equal(2000, group.MaxRowBufferSize);
            Assert.Equal("ProductionDSN", group.Dsn);
            Assert.Equal("admin", group.DsnUsername);
            Assert.Equal("pass123", group.DsnPassword);
            Assert.Equal(30, group.DsnLoginTimeout);
            Assert.Equal(60, group.DsnQueryTimeout);
            Assert.Equal(1, group.TableSelection);
            Assert.Equal("DataLog", group.TableName);
            Assert.Equal(1, group.TableFormat);
            Assert.Equal("Channel1.Device1.BatchTag", group.BatchIdItem);
            Assert.Equal(2000, group.BatchIdUpdateRate);
            Assert.Equal(0, group.BatchIdUpdateRateUnits);
            Assert.False(group.RegenerateOnDsnChange);
            Assert.False(group.RegenerateOnBatchIdChange);
            Assert.True(group.RegenerateOnTableNameChange);
            Assert.True(group.RegenerateOnTableSelectionChange);
        }

        [Fact]
        public void LogGroup_RoundTrip_ShouldPreserveProperties()
        {
            var group = new LogGroup("RoundTripGroup");
            group.Enabled = true;
            group.UpdateRate = 250;
            group.UpdateRateUnits = 0;
            group.Dsn = "TestDSN";
            group.DsnUsername = "user";
            group.DsnPassword = "pwd";
            group.TableName = "MyTable";
            group.TableFormat = 1;
            group.RegenerateOnDsnChange = false;

            var json = JsonSerializer.Serialize(group, KepJsonContext.Default.LogGroup);
            var deserialized = JsonSerializer.Deserialize<LogGroup>(json, KepJsonContext.Default.LogGroup);

            Assert.NotNull(deserialized);
            Assert.Equal("RoundTripGroup", deserialized.Name);
            Assert.True(deserialized.Enabled);
            Assert.Equal(250, deserialized.UpdateRate);
            Assert.Equal(0, deserialized.UpdateRateUnits);
            Assert.Equal("TestDSN", deserialized.Dsn);
            Assert.Equal("user", deserialized.DsnUsername);
            Assert.Equal("pwd", deserialized.DsnPassword);
            Assert.Equal("MyTable", deserialized.TableName);
            Assert.Equal(1, deserialized.TableFormat);
            Assert.False(deserialized.RegenerateOnDsnChange);
        }

        [Fact]
        public void LogGroup_WithDirectChildren_Deserialize_ShouldPopulateCollections()
        {
            var json = """
            {
                "common.ALLTYPES_NAME": "GroupWithChildren",
                "datalogger.LOG_GROUP_ENABLED": true,
                "log_items": [
                    { "common.ALLTYPES_NAME": "Item1" }
                ],
                "column_mappings": [
                    { "common.ALLTYPES_NAME": "Mapping1" }
                ],
                "triggers": [
                    { "common.ALLTYPES_NAME": "Trigger1" }
                ]
            }
            """;

            var group = JsonSerializer.Deserialize<LogGroup>(json, KepJsonContext.Default.LogGroup);

            Assert.NotNull(group);
            Assert.Equal("GroupWithChildren", group.Name);
            Assert.NotNull(group.LogItems);
            Assert.Single(group.LogItems);
            Assert.Equal("Item1", group.LogItems[0].Name);
            Assert.NotNull(group.ColumnMappings);
            Assert.Single(group.ColumnMappings);
            Assert.Equal("Mapping1", group.ColumnMappings[0].Name);
            Assert.NotNull(group.Triggers);
            Assert.Single(group.Triggers);
            Assert.Equal("Trigger1", group.Triggers[0].Name);
        }

        [Fact]
        public void LogGroup_WithGroupedChildren_Deserialize_ShouldPopulateGroupCollections()
        {
            var json = """
            {
                "common.ALLTYPES_NAME": "GroupWithGroupedChildren",
                "datalogger.LOG_GROUP_ENABLED": false,
                "log_item_groups": [
                    {
                        "common.ALLTYPES_NAME": "Log Items",
                        "log_items": [
                            { "common.ALLTYPES_NAME": "Item1" },
                            { "common.ALLTYPES_NAME": "Item2" }
                        ]
                    }
                ],
                "column_mapping_groups": [
                    {
                        "common.ALLTYPES_NAME": "Column Mappings",
                        "column_mappings": [
                            { "common.ALLTYPES_NAME": "Mapping1" }
                        ]
                    }
                ],
                "trigger_groups": [
                    {
                        "common.ALLTYPES_NAME": "Triggers",
                        "triggers": [
                            { "common.ALLTYPES_NAME": "Trigger1" }
                        ]
                    }
                ]
            }
            """;

            var group = JsonSerializer.Deserialize<LogGroup>(json, KepJsonContext.Default.LogGroup);

            Assert.NotNull(group);
            Assert.NotNull(group.LogItemGroups);
            Assert.Single(group.LogItemGroups);
            Assert.Equal("Log Items", group.LogItemGroups[0].Name);
            Assert.NotNull(group.LogItemGroups[0].LogItems);
            Assert.Equal(2, group.LogItemGroups[0].LogItems!.Count);
            Assert.Equal("Item1", group.LogItemGroups[0].LogItems![0].Name);

            Assert.NotNull(group.ColumnMappingGroups);
            Assert.Single(group.ColumnMappingGroups);
            Assert.NotNull(group.ColumnMappingGroups[0].ColumnMappings);
            Assert.Single(group.ColumnMappingGroups[0].ColumnMappings!);

            Assert.NotNull(group.TriggerGroups);
            Assert.Single(group.TriggerGroups);
            Assert.NotNull(group.TriggerGroups[0].Triggers);
            Assert.Single(group.TriggerGroups[0].Triggers!);
            Assert.Equal("Trigger1", group.TriggerGroups[0].Triggers![0].Name);
        }
    }
}
