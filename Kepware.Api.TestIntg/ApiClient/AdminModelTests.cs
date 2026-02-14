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
using Kepware.Api.Model.Admin;

namespace Kepware.Api.TestIntg.ApiClient
{
    public class AdminModelTests : TestIntgApiClientBase
    {

        #region GetAdminSettingsAsync Tests

        [Fact]
        public async Task GetAdminSettingsAsync_ShouldReturnAdminSettings_WhenApiRespondsSuccessfully()
        {

            // Act
            var result = await _kepwareApiClient.Admin.GetAdminSettingsAsync();

            // Assert
            result.ShouldNotBeNull();
            result.EventLogConnectionPort.ShouldNotBe(default(int));
            result.EventLogMaxRecords.ShouldNotBe(default(int));
            if (_productInfo.ProductId == "013")
            {
                result.LicenseServer.ShouldNotBeNull();
                result.LicenseServer.RecheckIntervalMinutes.ShouldNotBe(default(int));
                result.LicenseServer.Enable.ShouldNotBeNull();
            }
            
        }

        [Fact]
        public async Task GetAdminSettingsAsync_ShouldReturnNull_WhenApiReturnsUnauthorized()
        {

            // Act
            var result = await _badCredKepwareApiClient.Admin.GetAdminSettingsAsync();

            // Assert
            result.ShouldBeNull();

        }

        #endregion

        #region SetAdminSettingsAsync Tests

        [Fact]
        public async Task SetAdminSettingsAsync_ShouldReturnTrue_WhenUpdateSuccessful()
        {
            // Arrange
            var newSettings = new AdminSettings();
            newSettings.EventLogMaxRecords = new Random().Next(10000, 30001);
            if (_productInfo.ProductId != "013")
            {
                newSettings.LicenseServer.Port = new Random().Next(10000, 30001);
            }

            // Act
            var result = await _kepwareApiClient.Admin.SetAdminSettingsAsync(newSettings);

            // Assert
            result.ShouldBeTrue();

        }

        [Fact]
        public async Task SetAdminSettingsAsync_ShouldReturnFalse_WhenUpdateFails()
        {
            // Arrange
            var newSettings = new AdminSettings();
            newSettings.EventLogConnectionPort = 1000;


            // Act
            var result = await _kepwareApiClient.Admin.SetAdminSettingsAsync(newSettings);

            // Assert
            result.ShouldBeFalse();
        }

        [Fact]
        public async Task SetAdminSettingsAsync_ShouldThrowException_WhenApiReturnsUnauthorized()
        {
            // Arrange
            var newSettings = new AdminSettings();
            newSettings.EventLogMaxRecords = new Random().Next(10000, 30001);

            // Act and Assert
            await Should.ThrowAsync<InvalidOperationException>(async () =>
            {
                await _badCredKepwareApiClient.Admin.SetAdminSettingsAsync(newSettings);
            });

        }

        #endregion
    }
}
