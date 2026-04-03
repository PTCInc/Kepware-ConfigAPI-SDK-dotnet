using System.Collections.Generic;
using Kepware.Api.Model;

namespace Kepware.Api.Test.Model.DataLogger
{
    public class DataLoggerPropertiesTests
    {
        [Fact]
        public void Properties_LogGroup_Enabled_ShouldHaveCorrectKey()
        {
            Assert.Equal("datalogger.LOG_GROUP_ENABLED", Properties.DataLogger.LogGroup.Enabled);
        }

        [Fact]
        public void Properties_LogGroup_BatchIdItemType_ShouldBeInNonUpdatableSet()
        {
            Assert.Contains(Properties.DataLogger.LogGroup.BatchIdItemType, (IEnumerable<string>)Properties.NonUpdatable.AsHashSet);
        }

        [Fact]
        public void Properties_LogGroup_DsnPassword_ShouldNotBeInNonSerializedSet()
        {
            Assert.DoesNotContain(Properties.DataLogger.LogGroup.DsnPassword, (IEnumerable<string>)Properties.NonSerialized.AsHashSet);
        }

        [Fact]
        public void Properties_LogItem_DataType_ShouldBeInNonUpdatableSet()
        {
            Assert.Contains(Properties.DataLogger.LogItem.DataType, (IEnumerable<string>)Properties.NonUpdatable.AsHashSet);
        }

        [Fact]
        public void Properties_Trigger_MonitorItemDataType_ShouldBeInNonUpdatableSet()
        {
            Assert.Contains(Properties.DataLogger.Trigger.MonitorItemDataType, (IEnumerable<string>)Properties.NonUpdatable.AsHashSet);
        }

        [Fact]
        public void Properties_Trigger_ConditionStartItemDataType_ShouldBeInNonUpdatableSet()
        {
            Assert.Contains(Properties.DataLogger.Trigger.ConditionStartItemDataType, (IEnumerable<string>)Properties.NonUpdatable.AsHashSet);
        }

        [Fact]
        public void Properties_Trigger_ConditionStopItemDataType_ShouldBeInNonUpdatableSet()
        {
            Assert.Contains(Properties.DataLogger.Trigger.ConditionStopItemDataType, (IEnumerable<string>)Properties.NonUpdatable.AsHashSet);
        }
    }
}
