using Kepware.Api.Model;
using Kepware.Api.Serializer;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Contrib.HttpClient;
using Shouldly;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;
using System.Net.Http.Json;

namespace Kepware.Api.TestIntg.ApiClient
{
    public class ProjectPropertiesTests : TestIntgApiClientBase
    {

        #region GetProjectPropertiesAsync Tests

        [Fact]
        public async Task GetProjectPropertiesAsync_ShouldReturnProjectProperties_WhenApiRespondsSuccessfully()
        {

            // Act
            var result = await _kepwareApiClient.Project.GetProjectPropertiesAsync();

            // Assert
            result.ShouldNotBeNull();
            result.ProjectProperties.OpcUaEnableDiagnostics.ShouldNotBeNull();
            result.ProjectProperties.OpcUaEnableDiagnostics.Value.ShouldBeOfType<bool>();
            result.ProjectProperties.ThingWorxMaxDatastoreSize.ShouldBeOfType<ThingWorxDataStoreMaxSize>();
            result.ProjectProperties.EnableOpcDa1.ShouldNotBeNull();
            result.ProjectProperties.EnableOpcDa1.Value.ShouldBeOfType<bool>();
        }

        [Fact]
        public async Task GetProjectPropertiesAsync_ShouldReturnNull_WhenApiReturnsUnauthorized()
        {

            // Act
            var result = await _badCredKepwareApiClient.Project.GetProjectPropertiesAsync();

            // Assert
            result.ShouldBeNull();
        }

        #endregion

        #region SetProjectPropertiesAsync Tests

        [Fact]
        public async Task SetProjectPropertiesAsync_ShouldReturnTrue_WhenUpdateSuccessful()
        {
            // Arrange
            var newSettings = new Project();
            if (_productInfo.ProductId != "013")
            {
                newSettings.ProjectProperties.OpcDaMaxConnections = new Random().Next(1, 4001);
            }
            newSettings.ProjectProperties.OpcUaMaxConnections = new Random().Next(1, 257);

            // Act
            var result = await _kepwareApiClient.Project.SetProjectPropertiesAsync(newSettings);

            // Assert
            result.ShouldBeTrue();

            // Verify the settings were actually applied
            var updatedSettings = await _kepwareApiClient.Project.GetProjectPropertiesAsync();
            updatedSettings.ShouldNotBeNull();
            if (_productInfo.ProductId != "013")
            {
                updatedSettings.ProjectProperties.OpcDaMaxConnections.ShouldBe(newSettings.ProjectProperties.OpcDaMaxConnections);
            }
            updatedSettings.ProjectProperties.OpcUaMaxConnections.ShouldBe(newSettings.ProjectProperties.OpcUaMaxConnections);
        }

        [Fact]
        public async Task SetProjectPropertiesAsync_ShouldReturnFalse_WhenUpdateFails()
        {
            // Arrange
            var newSettings = new Project();
            newSettings.ProjectProperties.OpcDaMaxConnections = 5000; // Invalid value, should be between 1 and 4000

            // Act
            var result = await _kepwareApiClient.Project.SetProjectPropertiesAsync(newSettings);

            // Assert
            result.ShouldBeFalse();
        }

        [Fact]
        public async Task SetProjectPropertiesAsync_ShouldReturnFalse_WhenApiReturnsUnauthorized()
        {
            // Arrange
            var newSettings = new Project();
            newSettings.ProjectProperties.OpcDaMaxConnections = new Random().Next(1, 4001);
            newSettings.ProjectProperties.OpcUaMaxConnections = new Random().Next(1, 257);
            
            // Act and Assert
            await Should.ThrowAsync<InvalidOperationException>(async () =>
            {
                await _badCredKepwareApiClient.Project.SetProjectPropertiesAsync(newSettings);
            });
        }

        #endregion
    }
}
