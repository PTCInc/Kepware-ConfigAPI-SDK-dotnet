using Kepware.Api.Model;
using Kepware.Api.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kepware.Api.Test.Util
{
    public class EndpointResolverTests
    {
        #region ResolveEndpoint - Standard Endpoints

        [Fact]
        public void ResolveEndpoint_ShouldReturnCorrectEndpoint_ForStandardEntity()
        {
            // Arrange & Act
            var endpoint = EndpointResolver.ResolveEndpoint<ChannelCollection>();

            // Assert
            Assert.Equal("/config/v1/project/channels", endpoint);
        }

        [Fact]
        public void ResolveEndpoint_ShouldReturnCorrectEndpoint_ForEntityWithName()
        {
            // Arrange & Act
            var endpoint = EndpointResolver.ResolveEndpoint<Channel>(["Channel1"]);

            // Assert
            Assert.Equal("/config/v1/project/channels/Channel1", endpoint);
        }

        [Fact]
        public void ResolveEndpoint_ShouldReturnCorrectEndpoint_WithOwnerHierarchy()
        {
            // Arrange
            var owner = new NamedEntity { Name = "Data Type Examples" };

            // Act
            var endpoint = EndpointResolver.ResolveEndpoint<Device>(owner, "16 Bit Device");

            // Assert
            Assert.Equal("/config/v1/project/channels/Data%20Type%20Examples/devices/16%20Bit%20Device", endpoint);
        }

        #endregion

        #region ResolveEndpoint - Recursive Endpoints

        [Fact]
        public void ResolveEndpoint_ShouldReturnCorrectRecursiveEndpoint()
        {
            // Arrange
            var owner = new DeviceTagGroup("Values", new DeviceTagGroup("B Registers", new Device("16 Bit Device", new Channel("Data Type Examples"))));

            // Act
            var endpoint = EndpointResolver.ResolveEndpoint<DeviceTagCollection>(owner);

            // Assert
            Assert.Equal("/config/v1/project/channels/Data%20Type%20Examples/devices/16%20Bit%20Device/tag_groups/B%20Registers/tag_groups/Values/tags", endpoint);
        }

        [Fact]
        public void ResolveEndpoint_ShouldReturnCorrectRecursiveEndpoint_WithItem()
        {
            // Arrange
            var owner = new DeviceTagGroup("B Registers", new Device("16 Bit Device", new Channel("Data Type Examples")));

            // Act
            var endpoint = EndpointResolver.ResolveEndpoint<Tag>(owner, "Boolean1");

            // Assert
            Assert.Equal("/config/v1/project/channels/Data%20Type%20Examples/devices/16%20Bit%20Device/tag_groups/B%20Registers/tags/Boolean1", endpoint);
        }

        #endregion

        #region ResolveEndpoint - Fehlerfälle

        [Fact]
        public void ResolveEndpoint_ShouldThrowException_WhenNoEndpointDefined()
        {
            // Arrange & Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() =>
                EndpointResolver.ResolveEndpoint<DefaultEntity>()
            );

            Assert.Equal("No endpoint defined for DefaultEntity", exception.Message);
        }

        [Fact]
        public void ResolveEndpoint_ShouldThrowException_WhenRecursiveEndpointDoesNotSupportItemName()
        {
            // Arrange
            var owner = new DeviceTagGroup("B Registers", new Device("16 Bit Device", new Channel("Data Type Examples")));

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() =>
                EndpointResolver.ResolveEndpoint<DeviceTagCollection>(owner, "Boolean1")
            );

            Assert.Equal("Recursive endpoint does not support item name", exception.Message);
        }

        [Fact]
        public void ResolveEndpoint_ShouldThrowException_WhenPlaceholderCountMismatch()
        {
            // Arrange & Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() =>
                EndpointResolver.ResolveEndpoint<ChannelCollection>(new[] { "ExtraValue" })
            );

            Assert.StartsWith("The number of placeholders in the template", exception.Message);
        }

        #endregion
    }
}
