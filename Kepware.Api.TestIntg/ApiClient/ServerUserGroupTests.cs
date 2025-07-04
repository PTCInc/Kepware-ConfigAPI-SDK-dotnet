using Kepware.Api.Model.Admin;
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
    public class ServerUserGroupTests : TestApiClientBase
    {

        [Fact]
        public async Task GetServerUserGroupAsync_ShouldReturnServerUserGroup_WhenApiRespondsSuccessfully()
        {
            // Arrange
            var userGroup = new ServerUserGroup{ Name= "Administrators"};

            // Act
            var result = await _kepwareApiClient.Admin.GetServerUserGroupAsync(userGroup.Name);

            // Assert
            result.ShouldNotBeNull();
            result.Name.ShouldBe(userGroup.Name);
            result.Enabled!.Value.ShouldBeTrue();
        }

        [Fact]
        public async Task GetServerUserGroupAsync_ShouldReturnNull_WhenApiReturnsNotFound()
        {
            // Arrange
            var groupName = "NonExistentGroup";

            // Act
            var result = await _kepwareApiClient.Admin.GetServerUserGroupAsync(groupName);

            // Assert
            result.ShouldBeNull();
        }

        [Fact]
        public async Task CreateOrUpdateServerUserGroupAsync_ShouldCreateServerUserGroup_WhenItDoesNotExist()
        {
            // Arrange
            var userGroup = CreateTestServerUserGroup();

            // Act
            var result = await _kepwareApiClient.Admin.CreateOrUpdateServerUserGroupAsync(userGroup);

            // Assert
            result.ShouldBeTrue();

            // Clean up
            await _kepwareApiClient.GenericConfig.DeleteItemAsync(userGroup);
        }

        [Fact]
        public async Task CreateOrUpdateServerUserGroupAsync_ShouldUpdateServerUserGroup_WhenItExists()
        {
            // Arrange
            var userGroup = CreateTestServerUserGroup();
            await _kepwareApiClient.GenericConfig.InsertItemAsync(userGroup);
            userGroup.Enabled = false;

            // Act
            var result = await _kepwareApiClient.Admin.CreateOrUpdateServerUserGroupAsync(userGroup);

            // Assert
            result.ShouldBeTrue();

            // Clean up
            await _kepwareApiClient.GenericConfig.DeleteItemAsync(userGroup);
        }

        [Fact]
        public async Task DeleteServerUserGroupAsync_ShouldReturnTrue_WhenDeleteSuccessful()
        {
            // Arrange
            var userGroup = CreateTestServerUserGroup();
            await _kepwareApiClient.GenericConfig.InsertItemAsync(userGroup);

            // Act
            var result = await _kepwareApiClient.Admin.DeleteServerUserGroupAsync(userGroup.Name);

            // Assert
            result.ShouldBeTrue();

            // Clean up
            await _kepwareApiClient.GenericConfig.DeleteItemAsync(userGroup);
        }

        [Fact]
        public async Task DeleteServerUserGroupAsync_ShouldReturnFalse_WhenDeleteFails()
        {
            // Arrange
            // Cannot delete the default Administrators group
            var groupName = "Administrators";

            // Act
            var result = await _kepwareApiClient.Admin.DeleteServerUserGroupAsync(groupName);

            // Assert
            result.ShouldBeFalse();
        }

        [Fact]
        public async Task GetServerUserGroupsAsync_ShouldReturnServerUserGroupCollection_WhenApiRespondsSuccessfully()
        {

            // Act
            var result = await _kepwareApiClient.Admin.GetServerUserGroupListAsync();

            // Assert
            result.ShouldNotBeNull();
            result.Count.ShouldBeGreaterThan(2);
        }

        private static ServerUserGroup CreateTestServerUserGroup(string name = "TestGroup")
        {
            return new ServerUserGroup
            {
                Name = name,
                Enabled = true,
                IoTagRead = true,
                IoTagWrite = true,
                IoTagDynamicAddressing = true,
                SystemTagRead = true,
                SystemTagWrite = true,
                ManageLicenses = true,
                ModifyServerSettings = true,
                DisconnectClients = true,
                ReplaceRuntimeProject = true,
                ResetEventLog = true,
                BrowseNamespace = true,
                ProjectModificationAdd = true,
                ProjectModificationEdit = true,
                ProjectModificationDelete = true,
                ResetOpcDiagsLog = true,
                ResetCommDiagsLog = true,
                ConfigApiLogAccess = true,
                ViewEventLogSecurity = true,
                ViewEventLogError = true,
                ViewEventLogWarning = true,
                ViewEventLogInfo = true
            };
        }
    }
}

