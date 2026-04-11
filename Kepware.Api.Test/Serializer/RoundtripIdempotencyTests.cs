using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Kepware.Api.Model;
using Kepware.Api.Serializer;
using Kepware.Api.Util;
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;

namespace Kepware.Api.Test.Serializer
{
    public class RoundtripIdempotencyTests
    {
        private static YamlSerializer CreateYamlSerializer() =>
            new(Mock.Of<ILogger<YamlSerializer>>());

        private static CsvTagSerializer CreateCsvTagSerializer() =>
            new(Mock.Of<ILogger<CsvTagSerializer>>());

        private static DataTypeEnumConverterProvider CreateDataTypeConverterProvider() => new();

        [Fact]
        public async Task YamlSerializer_Roundtrip_ProjectEntities_ShouldPreserveHashes()
        {
            var serializer = CreateYamlSerializer();
            var tempRoot = Path.Combine(Path.GetTempPath(), nameof(YamlSerializer_Roundtrip_ProjectEntities_ShouldPreserveHashes), Path.GetRandomFileName());

            try
            {
                var project = CreateProjectEntity();
                var channel = CreateChannelEntity();
                var device = CreateDeviceEntity();
                var projectFile = Path.Combine(tempRoot, "project", "project.yaml");
                var channelFile = Path.Combine(tempRoot, channel.Name, "channel.yaml");
                var deviceFile = Path.Combine(tempRoot, channel.Name, device.Name, "device.yaml");

                await serializer.SaveAsYaml(projectFile, project);
                await serializer.SaveAsYaml(channelFile, channel);
                await serializer.SaveAsYaml(deviceFile, device);

                var savedChannelYaml = await File.ReadAllTextAsync(channelFile);
                var savedDeviceYaml = await File.ReadAllTextAsync(deviceFile);

                var loadedProject = await serializer.LoadFromYaml<Project>(projectFile);
                var loadedChannel = await serializer.LoadFromYaml<Channel>(channelFile);
                var loadedDevice = await serializer.LoadFromYaml<Device>(deviceFile);

                loadedProject.Description.ShouldBe(project.Description);
                loadedProject.ProjectProperties.Title.ShouldBe(project.ProjectProperties.Title);

                loadedChannel.Hash.ShouldBe(channel.Hash);
                loadedChannel.Name.ShouldBe(channel.Name);
                loadedChannel.Description.ShouldBe(channel.Description);
                loadedChannel.DeviceDriver.ShouldBe(channel.DeviceDriver);

                loadedDevice.Hash.ShouldBe(device.Hash);
                loadedDevice.Name.ShouldBe(device.Name);
                loadedDevice.Description.ShouldBe(device.Description);
                loadedDevice.GetDynamicProperty<string>(Properties.Channel.DeviceDriver).ShouldBe(device.GetDynamicProperty<string>(Properties.Channel.DeviceDriver));

                await serializer.SaveAsYaml(channelFile, loadedChannel);
                await serializer.SaveAsYaml(deviceFile, loadedDevice);

                (await File.ReadAllTextAsync(channelFile)).ShouldBe(savedChannelYaml);
                (await File.ReadAllTextAsync(deviceFile)).ShouldBe(savedDeviceYaml);
            }
            finally
            {
                if (Directory.Exists(tempRoot))
                {
                    Directory.Delete(tempRoot, recursive: true);
                }
            }
        }

        [Fact]
        public async Task CsvTagSerializer_Roundtrip_Tags_ShouldNotIntroduceHashDifferences()
        {
            var serializer = CreateCsvTagSerializer();
            var converter = CreateDataTypeConverterProvider().GetDataTypeEnumConverter("Simulator");
            var tempRoot = Path.Combine(Path.GetTempPath(), nameof(CsvTagSerializer_Roundtrip_Tags_ShouldNotIntroduceHashDifferences), Path.GetRandomFileName());
            var tagsFile = Path.Combine(tempRoot, "tags.csv");
            var secondTagsFile = Path.Combine(tempRoot, "tags-roundtrip.csv");

            Directory.CreateDirectory(tempRoot);

            try
            {
                var sourceTags = new DeviceTagCollection
                {
                    CreateScaledTag("ScaledTag"),
                    CreateUnscaledTag("DiscreteTag")
                };

                await serializer.ExportTagsAsync(tagsFile, sourceTags.ToList(), converter);
                var importedTags = await serializer.ImportTagsAsync(tagsFile, converter);
                await serializer.ExportTagsAsync(secondTagsFile, importedTags, converter);

                importedTags.Count.ShouldBe(sourceTags.Count);

                foreach (var sourceTag in sourceTags)
                {
                    var importedTag = importedTags.Single(tag => tag.Name == sourceTag.Name);
                    importedTag.Hash.ShouldBe(sourceTag.Hash);
                    importedTag.Description.ShouldBe(sourceTag.Description);
                    importedTag.TagAddress.ShouldBe(sourceTag.TagAddress);
                    importedTag.DataType.ShouldBe(sourceTag.DataType);
                    importedTag.ReadWriteAccess.ShouldBe(sourceTag.ReadWriteAccess);
                    importedTag.ScanRateMilliseconds.ShouldBe(sourceTag.ScanRateMilliseconds);
                    importedTag.ScalingType.ShouldBe(sourceTag.ScalingType);
                    importedTag.ScalingUnits.ShouldBe(sourceTag.ScalingUnits);
                    importedTag.ScalingClampLow.ShouldBe(sourceTag.ScalingClampLow);
                    importedTag.ScalingClampHigh.ShouldBe(sourceTag.ScalingClampHigh);
                    importedTag.ScalingNegateValue.ShouldBe(sourceTag.ScalingNegateValue);
                }

                var compareResult = EntityCompare.Compare<DeviceTagCollection, Tag>(sourceTags, [.. importedTags]);
                compareResult.ChangedItems.ShouldBeEmpty();
                compareResult.ItemsOnlyInLeft.ShouldBeEmpty();
                compareResult.ItemsOnlyInRight.ShouldBeEmpty();
                compareResult.UnchangedItems.Count.ShouldBe(sourceTags.Count);

                (await File.ReadAllTextAsync(secondTagsFile)).ShouldBe(await File.ReadAllTextAsync(tagsFile));
            }
            finally
            {
                if (Directory.Exists(tempRoot))
                {
                    Directory.Delete(tempRoot, recursive: true);
                }
            }
        }

        private static Project CreateProjectEntity()
        {
            var project = new Project
            {
                Description = "Project description"
            };
            project.ProjectProperties.Title = "Project title";
            project.SetDynamicProperty(Properties.ProjectSettings.OpcDa.EnableOpcDa3, true);
            return project;
        }

        private static Channel CreateChannelEntity()
        {
            var channel = new Channel
            {
                Name = "Channel-01",
                Description = "Channel description",
                DeviceDriver = "Simulator",
                DiagnosticsCapture = true,
            };
            channel.SetDynamicProperty(Properties.NonUpdatable.ChannelUniqueId, 101L);
            return channel;
        }

        private static Device CreateDeviceEntity()
        {
            var device = new Device
            {
                Name = "Device-01",
                Description = "Device description",
            };
            device.SetDynamicProperty(Properties.NonUpdatable.DeviceUniqueId, 201L);
            device.SetDynamicProperty(Properties.Channel.DeviceDriver, "Simulator");
            device.SetDynamicProperty(Properties.Device.DeviceDriver, "Simulator");
            return device;
        }

        private static Tag CreateScaledTag(string name)
        {
            var tag = new Tag
            {
                Name = name,
                Description = "Scaled tag"
            };

            tag.TagAddress = "RAMP";
            tag.DataType = 8;
            tag.ReadWriteAccess = 1;
            tag.ScanRateMilliseconds = 250;
            tag.ScalingType = 1;
            tag.ScalingRawLow = 0;
            tag.ScalingRawHigh = 100;
            tag.ScalingScaledLow = 0;
            tag.ScalingScaledHigh = 1000;
            tag.ScalingScaledDataType = 8;
            tag.ScalingClampLow = true;
            tag.ScalingClampHigh = false;
            tag.ScalingUnits = "psi";
            tag.ScalingNegateValue = true;

            return tag;
        }

        private static Tag CreateUnscaledTag(string name)
        {
            var tag = new Tag
            {
                Name = name,
                Description = "Discrete tag"
            };

            tag.TagAddress = "SWITCH";
            tag.DataType = 1;
            tag.ReadWriteAccess = 0;
            tag.ScanRateMilliseconds = 100;
            tag.ScalingType = 0;
            return tag;
        }
    }
}
