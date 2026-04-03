using System.Text.Json;
using Kepware.Api.Model;
using Kepware.Api.Serializer;
using Kepware.Api.Util;

namespace Kepware.Api.Test.Model.DataLogger
{
    public class LogItemModelTests
    {
        [Fact]
        public void LogItem_Deserialize_ShouldPopulateProperties()
        {
            var json = """
            {
                "common.ALLTYPES_NAME": "TestItem",
                "common.ALLTYPES_DESCRIPTION": "A test log item",
                "datalogger.LOG_ITEM_ID": "Channel1.Device1.Tag1",
                "datalogger.LOG_ITEM_NUMERIC_ID": 42,
                "datalogger.LOG_ITEM_DATA_TYPE": "Long",
                "datalogger.LOG_ITEM_DEADBAND_TYPE": 1,
                "datalogger.LOG_ITEM_DEADBAND_VALUE": 0.5,
                "datalogger.LOG_ITEM_DEADBAND_LO_RANGE": 0.0,
                "datalogger.LOG_ITEM_DEADBAND_HI_RANGE": 100.0
            }
            """;

            var item = JsonSerializer.Deserialize<LogItem>(json, KepJsonContext.Default.LogItem);

            Assert.NotNull(item);
            Assert.Equal("TestItem", item.Name);
            Assert.Equal("A test log item", item.Description);
            Assert.Equal("Channel1.Device1.Tag1", item.ServerItem);
            Assert.Equal(42L, item.NumericId);
            Assert.Equal("Long", item.DataType);
            Assert.Equal(1, item.DeadbandType);
            Assert.Equal(0.5, item.DeadbandValue);
            Assert.Equal(0.0, item.DeadbandLoRange);
            Assert.Equal(100.0, item.DeadbandHiRange);
        }

        [Fact]
        public void LogItem_SetProperties_ShouldUpdateDynamicProperties()
        {
            var item = new LogItem("MyItem");

            item.ServerItem = "Channel1.Device1.Tag2";
            item.NumericId = 99L;
            item.DeadbandType = 2;
            item.DeadbandValue = 1.5;
            item.DeadbandLoRange = -10.0;
            item.DeadbandHiRange = 200.0;

            Assert.Equal("MyItem", item.Name);
            Assert.Equal("Channel1.Device1.Tag2", item.ServerItem);
            Assert.Equal(99L, item.NumericId);
            Assert.Equal(2, item.DeadbandType);
            Assert.Equal(1.5, item.DeadbandValue);
            Assert.Equal(-10.0, item.DeadbandLoRange);
            Assert.Equal(200.0, item.DeadbandHiRange);
        }

        [Fact]
        public void LogItem_RoundTrip_ShouldPreserveProperties()
        {
            var item = new LogItem("RoundTripItem");
            item.ServerItem = "Channel1.Device1.Tag3";
            item.NumericId = 7L;
            item.DeadbandType = 0;
            item.DeadbandValue = 0.0;

            var json = JsonSerializer.Serialize(item, KepJsonContext.Default.LogItem);
            var deserialized = JsonSerializer.Deserialize<LogItem>(json, KepJsonContext.Default.LogItem);

            Assert.NotNull(deserialized);
            Assert.Equal("RoundTripItem", deserialized.Name);
            Assert.Equal("Channel1.Device1.Tag3", deserialized.ServerItem);
            Assert.Equal(7L, deserialized.NumericId);
            Assert.Equal(0, deserialized.DeadbandType);
            Assert.Equal(0.0, deserialized.DeadbandValue);
        }

        [Fact]
        public void LogItem_WithParentEndpoint_ShouldResolveCorrectUrl()
        {
            var logGroup = new LogGroup("TestGroup");

            var collectionEndpoint = EndpointResolver.ResolveEndpoint<LogItemCollection>(logGroup);
            var itemEndpoint = EndpointResolver.ResolveEndpoint<LogItem>(logGroup, "Item1");

            Assert.Equal("/config/v1/project/_datalogger/log_groups/TestGroup/log_items", collectionEndpoint);
            Assert.Equal("/config/v1/project/_datalogger/log_groups/TestGroup/log_items/Item1", itemEndpoint);
        }
    }
}
