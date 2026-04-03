using System.Text.Json;
using Kepware.Api.Model;
using Kepware.Api.Serializer;
using Kepware.Api.Util;

namespace Kepware.Api.Test.Model.DataLogger
{
    public class TriggerModelTests
    {
        [Fact]
        public void Trigger_Deserialize_ShouldPopulateProperties()
        {
            var json = """
            {
                "common.ALLTYPES_NAME": "TimeTrigger",
                "common.ALLTYPES_DESCRIPTION": "A time-based trigger",
                "datalogger.TRIGGER_TYPE": 1,
                "datalogger.TRIGGER_LOG_ON_STATIC_INTERVAL": true,
                "datalogger.TRIGGER_STATIC_INTERVAL": 500,
                "datalogger.TRIGGER_STATIC_INTERVAL_UNITS": 0,
                "datalogger.TRIGGER_LOG_ON_DATA_CHANGE": false,
                "datalogger.TRIGGER_LOG_ALL_ITEMS": true,
                "datalogger.TRIGGER_MONITOR_ITEM_ID": "Channel1.Device1.MonitorTag",
                "datalogger.TRIGGER_MONITOR_ITEM_UPDATE_RATE": 1000,
                "datalogger.TRIGGER_MONITOR_ITEM_UPDATE_UNITS": 0,
                "datalogger.TRIGGER_MONITOR_ITEM_DATA_TYPE": "Boolean",
                "datalogger.TRIGGER_MONITOR_ITEM_DEADBAND_TYPE": 0,
                "datalogger.TRIGGER_MONITOR_ITEM_DEADBAND_VALUE": 0.0,
                "datalogger.TRIGGER_MONITOR_ITEM_DEADBAND_LO_RANGE": 0.0,
                "datalogger.TRIGGER_MONITOR_ITEM_DEADBAND_HI_RANGE": 100.0,
                "datalogger.TRIGGER_ABSOLUTE_START_TIME": "08:00:00",
                "datalogger.TRIGGER_ABSOLUTE_STOP_TIME": "17:00:00",
                "datalogger.TRIGGER_DAYS_SUNDAY": false,
                "datalogger.TRIGGER_DAYS_MONDAY": true,
                "datalogger.TRIGGER_DAYS_TUESDAY": true,
                "datalogger.TRIGGER_DAYS_WEDNESDAY": true,
                "datalogger.TRIGGER_DAYS_THURSDAY": true,
                "datalogger.TRIGGER_DAYS_FRIDAY": true,
                "datalogger.TRIGGER_DAYS_SATURDAY": false,
                "datalogger.TRIGGER_CONDITION_START_ITEM_ID": "Channel1.Device1.StartTag",
                "datalogger.TRIGGER_CONDITION_START_ITEM_DATA_TYPE": "Boolean",
                "datalogger.TRIGGER_CONDITION_START_ITEM_UPDATE_RATE": 100,
                "datalogger.TRIGGER_CONDITION_START_ITEM_UPDATE_UNITS": 0,
                "datalogger.TRIGGER_CONDITION_START_CONDITION_TYPE": 1,
                "datalogger.TRIGGER_CONDITION_START_CONDITION_DATA": "1",
                "datalogger.TRIGGER_CONDITION_STOP_ITEM_ID": "Channel1.Device1.StopTag",
                "datalogger.TRIGGER_CONDITION_STOP_ITEM_DATA_TYPE": "Boolean",
                "datalogger.TRIGGER_CONDITION_STOP_ITEM_UPDATE_RATE": 100,
                "datalogger.TRIGGER_CONDITION_STOP_ITEM_UPDATE_UNITS": 0,
                "datalogger.TRIGGER_CONDITION_STOP_CONDITION_TYPE": 1,
                "datalogger.TRIGGER_CONDITION_STOP_CONDITION_DATA": "0",
                "datalogger.TRIGGER_ABSOLUTE_LOG_ALL_ITEMS_START": true,
                "datalogger.TRIGGER_ABSOLUTE_LOG_ALL_ITEMS_STOP": false
            }
            """;

            var trigger = JsonSerializer.Deserialize<Trigger>(json, KepJsonContext.Default.Trigger);

            Assert.NotNull(trigger);
            Assert.Equal("TimeTrigger", trigger.Name);
            Assert.Equal(1, trigger.TriggerType);
            Assert.True(trigger.LogOnStaticInterval);
            Assert.Equal(500, trigger.StaticInterval);
            Assert.Equal(0, trigger.StaticIntervalUnits);
            Assert.False(trigger.LogOnDataChange);
            Assert.True(trigger.LogAllItems);
            Assert.Equal("Channel1.Device1.MonitorTag", trigger.MonitorItemId);
            Assert.Equal(1000, trigger.MonitorItemUpdateRate);
            Assert.Equal("Boolean", trigger.MonitorItemDataType);
            Assert.Equal(0, trigger.MonitorItemDeadbandType);
            Assert.Equal("08:00:00", trigger.AbsoluteStartTime);
            Assert.Equal("17:00:00", trigger.AbsoluteStopTime);
            Assert.False(trigger.DaysSunday);
            Assert.True(trigger.DaysMonday);
            Assert.True(trigger.DaysFriday);
            Assert.False(trigger.DaysSaturday);
            Assert.Equal("Channel1.Device1.StartTag", trigger.ConditionStartItemId);
            Assert.Equal("Boolean", trigger.ConditionStartItemDataType);
            Assert.Equal(1, trigger.ConditionStartConditionType);
            Assert.Equal("1", trigger.ConditionStartConditionData);
            Assert.Equal("Channel1.Device1.StopTag", trigger.ConditionStopItemId);
            Assert.Equal("Boolean", trigger.ConditionStopItemDataType);
            Assert.Equal("0", trigger.ConditionStopConditionData);
            Assert.True(trigger.LogAllItemsOnStart);
            Assert.False(trigger.LogAllItemsOnStop);
        }

        [Fact]
        public void Trigger_SetProperties_ShouldUpdateDynamicProperties()
        {
            var trigger = new Trigger("ConditionTrigger");

            trigger.TriggerType = 2;
            trigger.LogOnDataChange = true;
            trigger.LogAllItems = false;
            trigger.ConditionStartItemId = "Channel1.Device1.StartTag";
            trigger.ConditionStartItemUpdateRate = 250;
            trigger.ConditionStartConditionType = 0;
            trigger.ConditionStartConditionData = "1";
            trigger.ConditionStopItemId = "Channel1.Device1.StopTag";
            trigger.ConditionStopConditionData = "0";
            trigger.DaysMonday = true;
            trigger.DaysSaturday = false;
            trigger.LogAllItemsOnStart = true;
            trigger.LogAllItemsOnStop = true;

            Assert.Equal("ConditionTrigger", trigger.Name);
            Assert.Equal(2, trigger.TriggerType);
            Assert.True(trigger.LogOnDataChange);
            Assert.False(trigger.LogAllItems);
            Assert.Equal("Channel1.Device1.StartTag", trigger.ConditionStartItemId);
            Assert.Equal(250, trigger.ConditionStartItemUpdateRate);
            Assert.Equal("1", trigger.ConditionStartConditionData);
            Assert.Equal("Channel1.Device1.StopTag", trigger.ConditionStopItemId);
            Assert.Equal("0", trigger.ConditionStopConditionData);
            Assert.True(trigger.DaysMonday);
            Assert.False(trigger.DaysSaturday);
            Assert.True(trigger.LogAllItemsOnStart);
            Assert.True(trigger.LogAllItemsOnStop);
        }

        [Fact]
        public void Trigger_RoundTrip_ShouldPreserveProperties()
        {
            var trigger = new Trigger("RoundTripTrigger");
            trigger.TriggerType = 1;
            trigger.LogOnStaticInterval = true;
            trigger.StaticInterval = 100;
            trigger.StaticIntervalUnits = 1;
            trigger.DaysMonday = true;
            trigger.DaysTuesday = true;
            trigger.DaysWednesday = true;
            trigger.DaysThursday = true;
            trigger.DaysFriday = true;
            trigger.DaysSunday = false;
            trigger.DaysSaturday = false;
            trigger.AbsoluteStartTime = "06:00:00";
            trigger.AbsoluteStopTime = "22:00:00";
            trigger.LogAllItemsOnStart = false;
            trigger.LogAllItemsOnStop = false;

            var json = JsonSerializer.Serialize(trigger, KepJsonContext.Default.Trigger);
            var deserialized = JsonSerializer.Deserialize<Trigger>(json, KepJsonContext.Default.Trigger);

            Assert.NotNull(deserialized);
            Assert.Equal("RoundTripTrigger", deserialized.Name);
            Assert.Equal(1, deserialized.TriggerType);
            Assert.True(deserialized.LogOnStaticInterval);
            Assert.Equal(100, deserialized.StaticInterval);
            Assert.Equal(1, deserialized.StaticIntervalUnits);
            Assert.True(deserialized.DaysMonday);
            Assert.False(deserialized.DaysSunday);
            Assert.Equal("06:00:00", deserialized.AbsoluteStartTime);
            Assert.Equal("22:00:00", deserialized.AbsoluteStopTime);
        }

        [Fact]
        public void TriggerCollection_Endpoint_ShouldContainLogGroupName()
        {
            var logGroup = new LogGroup("ProductionGroup");

            var collectionEndpoint = EndpointResolver.ResolveEndpoint<TriggerCollection>(logGroup);
            var itemEndpoint = EndpointResolver.ResolveEndpoint<Trigger>(logGroup, "MyTrigger");

            Assert.Equal("/config/v1/project/_datalogger/log_groups/ProductionGroup/triggers", collectionEndpoint);
            Assert.Equal("/config/v1/project/_datalogger/log_groups/ProductionGroup/triggers/MyTrigger", itemEndpoint);
        }
    }
}
