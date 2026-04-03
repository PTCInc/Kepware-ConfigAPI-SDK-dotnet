using System.Text.Json;
using Kepware.Api.Model;
using Kepware.Api.Serializer;

namespace Kepware.Api.Test.Model.DataLogger
{
    public class DataLoggerContainerTests
    {
        [Fact]
        public void DataLoggerContainer_IsEmpty_WithNullLogGroups_ShouldBeTrue()
        {
            var container = new DataLoggerContainer();

            Assert.True(container.IsEmpty);
        }

        [Fact]
        public void DataLoggerContainer_IsEmpty_WithEmptyCollection_ShouldBeTrue()
        {
            var container = new DataLoggerContainer
            {
                LogGroups = new LogGroupCollection()
            };

            Assert.True(container.IsEmpty);
        }

        [Fact]
        public void DataLoggerContainer_IsEmpty_WithLogGroups_ShouldBeFalse()
        {
            var container = new DataLoggerContainer
            {
                LogGroups = new LogGroupCollection { new LogGroup("TestGroup") }
            };

            Assert.False(container.IsEmpty);
        }

        [Fact]
        public void ProjectWithDataLogger_ShouldDeserializeFromJson()
        {
            var json = """
            {
                "_datalogger": [
                    {
                        "common.ALLTYPES_NAME": "_DataLogger",
                        "log_groups": [
                            {
                                "common.ALLTYPES_NAME": "NarrowGroup",
                                "datalogger.LOG_GROUP_ENABLED": false,
                                "datalogger.LOG_GROUP_TABLE_FORMAT": 0
                            },
                            {
                                "common.ALLTYPES_NAME": "WideGroup",
                                "datalogger.LOG_GROUP_ENABLED": true,
                                "datalogger.LOG_GROUP_TABLE_FORMAT": 1
                            }
                        ]
                    }
                ]
            }
            """;

            var project = JsonSerializer.Deserialize(json, KepJsonContext.Default.Project);

            Assert.NotNull(project);
            Assert.NotNull(project.DataLogger);
            Assert.False(project.DataLogger.IsEmpty);
            Assert.NotNull(project.DataLogger.LogGroups);
            Assert.Equal(2, project.DataLogger.LogGroups.Count);
            Assert.Equal("NarrowGroup", project.DataLogger.LogGroups[0].Name);
            Assert.False(project.DataLogger.LogGroups[0].Enabled);
            Assert.Equal(0, project.DataLogger.LogGroups[0].TableFormat);
            Assert.Equal("WideGroup", project.DataLogger.LogGroups[1].Name);
            Assert.True(project.DataLogger.LogGroups[1].Enabled);
            Assert.Equal(1, project.DataLogger.LogGroups[1].TableFormat);
        }

        [Fact]
        public void ProjectWithoutDataLogger_ShouldDeserializeCorrectly()
        {
            var json = """
            {
                "common.ALLTYPES_DESCRIPTION": "Test project"
            }
            """;

            var project = JsonSerializer.Deserialize(json, KepJsonContext.Default.Project);

            Assert.NotNull(project);
            Assert.Null(project.DataLogger);
            Assert.True(project.IsEmpty);
        }

        [Fact]
        public void ProjectWithDataLogger_RoundTrip_ShouldPreserveLogGroups()
        {
            var project = new Project
            {
                DataLogger = new DataLoggerContainer
                {
                    LogGroups = new LogGroupCollection
                    {
                        new LogGroup("NarrowGroup") { Enabled = false, TableFormat = 0 },
                        new LogGroup("WideGroup")   { Enabled = true,  TableFormat = 1 },
                    }
                }
            };

            var json = JsonSerializer.Serialize(project, KepJsonContext.Default.Project);
            var restored = JsonSerializer.Deserialize(json, KepJsonContext.Default.Project);

            Assert.NotNull(restored);
            Assert.NotNull(restored.DataLogger);
            Assert.False(restored.DataLogger.IsEmpty);
            Assert.NotNull(restored.DataLogger.LogGroups);
            Assert.Equal(2, restored.DataLogger.LogGroups.Count);
            Assert.Equal("NarrowGroup", restored.DataLogger.LogGroups[0].Name);
            Assert.False(restored.DataLogger.LogGroups[0].Enabled);
            Assert.Equal(0, restored.DataLogger.LogGroups[0].TableFormat);
            Assert.Equal("WideGroup", restored.DataLogger.LogGroups[1].Name);
            Assert.True(restored.DataLogger.LogGroups[1].Enabled);
            Assert.Equal(1, restored.DataLogger.LogGroups[1].TableFormat);
        }
    }
}
