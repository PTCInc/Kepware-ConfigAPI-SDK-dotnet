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
    [TestCaseOrderer("Xunit.Extensions.Ordering.TestCaseOrderer", "Xunit.Extensions.Ordering")]
    public class ServerUserTests : TestApiClientBase
    {

        [Fact]
        public async Task GetServerUserAsync_ShouldReturnServerUser_WhenApiRespondsSuccessfully()
        {
            // Arrange
            var user = CreateTestServerUser("Administrators", "Administrator");

            // Act
            var result = await _kepwareApiClient.Admin.GetServerUserAsync(user.Name);

            // Assert
            result.ShouldNotBeNull();
            result.Name.ShouldBe(user.Name);
            result.Enabled.ShouldBe(user.Enabled);
        }

        [Fact]
        public async Task GetServerUserAsync_ShouldReturnNull_WhenApiReturnsNotFound()
        {
            // Arrange
            var userName = "NonExistentUser";

            // Act
            var result = await _kepwareApiClient.Admin.GetServerUserAsync(userName);

            // Assert
            result.ShouldBeNull();
        }

        [Fact]
        public async Task CreateOrUpdateServerUserAsync_ShouldCreateServerUser_WhenItDoesNotExist()
        {
            // Arrange
            var userGroup = new ServerUserGroup
            {
                Name = "TestGroup",
                Enabled = true
            };

            await _kepwareApiClient.Admin.CreateOrUpdateServerUserGroupAsync(userGroup);
            var user = CreateTestServerUser(userGroup.Name);

            // Act
            var result = await _kepwareApiClient.Admin.CreateOrUpdateServerUserAsync(user);

            // Assert
            result.ShouldBeTrue();

            // Clean up
            await _kepwareApiClient.Admin.DeleteServerUserGroupAsync(userGroup.Name);
        }

        [Fact]
        public async Task CreateOrUpdateServerUserAsync_ShouldUpdateServerUser_WhenItExists()
        {
            // Arrange
            var userGroup = new ServerUserGroup
            {
                Name = "TestGroup",
                Enabled = true
            };

            await _kepwareApiClient.Admin.CreateOrUpdateServerUserGroupAsync(userGroup);
            var user = CreateTestServerUser(userGroup.Name);
            await _kepwareApiClient.GenericConfig.InsertItemAsync(user);
            user.Description = "Updated Description";

            // Act
            var result = await _kepwareApiClient.Admin.CreateOrUpdateServerUserAsync(user);

            // Assert
            result.ShouldBeTrue();

            // Clean up
            await _kepwareApiClient.Admin.DeleteServerUserGroupAsync(userGroup.Name);
        }

        [Fact]
        public async Task CreateServerUserAsync_ShouldThrowArgumentException_WhenPasswordIsInvalid()
        {
            // Arrange
            var userGroup = new ServerUserGroup
            {
                Name = "TestGroup",
                Enabled = true
            };

            await _kepwareApiClient.Admin.CreateOrUpdateServerUserGroupAsync(userGroup);
            var user = CreateTestServerUser(userGroup.Name);
            user.Password = "short"; // Invalid password (too short)

            // Act & Assert
            await Should.ThrowAsync<ArgumentException>(async () =>
                await _kepwareApiClient.Admin.CreateOrUpdateServerUserAsync(user));

            // Clean up
            await _kepwareApiClient.Admin.DeleteServerUserGroupAsync(userGroup.Name);
        }

        [Fact]
        public async Task CreateServerUserAsync_ShouldThrowArgumentException_WhenPasswordIsEmpty()
        {
            // Arrange
            var userGroup = new ServerUserGroup
            {
                Name = "TestGroup",
                Enabled = true
            };

            await _kepwareApiClient.Admin.CreateOrUpdateServerUserGroupAsync(userGroup);
            var user = CreateTestServerUser(userGroup.Name);
            user.Password = string.Empty; // Empty password

            // Act & Assert
            await Should.ThrowAsync<ArgumentException>(async () =>
                await _kepwareApiClient.Admin.CreateOrUpdateServerUserAsync(user));

            // Clean up
            await _kepwareApiClient.Admin.DeleteServerUserGroupAsync(userGroup.Name);
        }

        [Fact]
        public async Task UpdateServerUserAsync_ShouldThrowArgumentException_WhenPasswordIsInvalid()
        {
            // Arrange
            var userGroup = new ServerUserGroup
            {
                Name = "TestGroup",
                Enabled = true
            };

            await _kepwareApiClient.Admin.CreateOrUpdateServerUserGroupAsync(userGroup);
            var user = CreateTestServerUser(userGroup.Name);
            await _kepwareApiClient.GenericConfig.InsertItemAsync(user);
            user.Password = "short";

            // Act & Assert
            await Should.ThrowAsync<ArgumentException>(async () =>
                await _kepwareApiClient.Admin.CreateOrUpdateServerUserAsync(user));

            // Clean up
            await _kepwareApiClient.Admin.DeleteServerUserGroupAsync(userGroup.Name);
        }

        [Fact]
        public async Task UpdateServerUserAsync_ShouldNotThrowArgumentException_WhenPasswordIsEmpty()
        {
            // Arrange
            var userGroup = new ServerUserGroup
            {
                Name = "TestGroup",
                Enabled = true
            };

            await _kepwareApiClient.Admin.CreateOrUpdateServerUserGroupAsync(userGroup);
            var user = CreateTestServerUser(userGroup.Name);
            await _kepwareApiClient.GenericConfig.InsertItemAsync(user);

            user.Password = null;

            // Act
            var result = await _kepwareApiClient.Admin.CreateOrUpdateServerUserAsync(user);

            // Assert
            result.ShouldBeTrue();

            // Clean up
            await _kepwareApiClient.Admin.DeleteServerUserGroupAsync(userGroup.Name);
        }

        [Fact]
        public async Task DeleteServerUserAsync_ShouldReturnTrue_WhenDeleteSuccessful()
        {
            // Arrange
            var userGroup = new ServerUserGroup
            {
                Name = "TestGroup",
                Enabled = true
            };

            await _kepwareApiClient.Admin.CreateOrUpdateServerUserGroupAsync(userGroup);
            var user = CreateTestServerUser(userGroup.Name);
            await _kepwareApiClient.GenericConfig.InsertItemAsync(user);

            // Act
            var result = await _kepwareApiClient.Admin.DeleteServerUserAsync(user.Name);

            // Assert
            result.ShouldBeTrue();

            // Clean up
            await _kepwareApiClient.Admin.DeleteServerUserGroupAsync(userGroup.Name);
        }

        [Fact]
        public async Task DeleteServerUserAsync_ShouldReturnFalse_WhenDeleteFails()
        {
            //Arrange
            var userGroup = new ServerUserGroup
            {
                Name = "TestGroup",
                Enabled = true
            };
            var user = CreateTestServerUser(userGroup.Name);


            // Act
            var result = await _kepwareApiClient.Admin.DeleteServerUserAsync(user.Name);

            // Assert
            result.ShouldBeFalse();
        }

        [Fact]
        public async Task GetServerUsersAsync_ShouldReturnServerUserCollection_WhenApiRespondsSuccessfully()
        {
            // Arrange
            var userGroup = new ServerUserGroup
            {
                Name = "TestGroup",
                Enabled = true
            };

            await _kepwareApiClient.Admin.CreateOrUpdateServerUserGroupAsync(userGroup);
            var users = new ServerUserCollection
                {
                    CreateTestServerUser(userGroup.Name, "User1"),
                    CreateTestServerUser(userGroup.Name, "User2")
                };
            await _kepwareApiClient.GenericConfig.InsertItemsAsync<ServerUserCollection, ServerUser>(users);


            // Act
            var result = await _kepwareApiClient.Admin.GetServerUserListAsync();

            // Assert
            result.ShouldNotBeNull();
            result.Count.ShouldBeGreaterThan(2);
            result.ShouldContain(u => u.Name == "User1");
            result.ShouldContain(u => u.Name == "User2");

            // Clean up
            await _kepwareApiClient.Admin.DeleteServerUserGroupAsync(userGroup.Name);
        }

        private static ServerUser CreateTestServerUser(string group, string name = "TestUser")
        {
            return new ServerUser
            {
                Name = name,
                Enabled = true,
                UserGroupName = group,
                Password = "ValidPassword123!",
                UserType = 0
            };
        }
    }
}


