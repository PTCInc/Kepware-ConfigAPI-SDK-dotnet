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

namespace Kepware.Api.Test.ApiClient
{
    public class ServerUserGroupTests : TestApiClientBase
    {
        private const string ENDPOINT_USER_GROUP = "/config/v1/admin/server_user_groups";

        [Fact]
        public async Task GetServerUserGroupAsync_ShouldReturnServerUserGroup_WhenApiRespondsSuccessfully()
        {
            // Arrange
            var userGroup = CreateTestServerUserGroup();
            _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, $"{TEST_ENDPOINT}{ENDPOINT_USER_GROUP}/{userGroup.Name}")
                .ReturnsResponse(HttpStatusCode.OK, JsonSerializer.Serialize(userGroup), "application/json");

            // Act
            var result = await _kepwareApiClient.GetServerUserGroupAsync(userGroup.Name);

            // Assert
            result.ShouldNotBeNull();
            result.Name.ShouldBe(userGroup.Name);
            result.Enabled.ShouldBe(userGroup.Enabled);
        }

        [Fact]
        public async Task GetServerUserGroupAsync_ShouldReturnNull_WhenApiReturnsNotFound()
        {
            // Arrange
            var groupName = "NonExistentGroup";
            _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, $"{TEST_ENDPOINT}{ENDPOINT_USER_GROUP}/{groupName}")
                .ReturnsResponse(HttpStatusCode.NotFound, "Not Found");

            // Act
            var result = await _kepwareApiClient.GetServerUserGroupAsync(groupName);

            // Assert
            result.ShouldBeNull();
            _loggerMock.Verify(logger => 
                logger.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => true),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)), 
                Times.Once);
        }

        [Fact]
        public async Task CreateOrUpdateServerUserGroupAsync_ShouldCreateServerUserGroup_WhenItDoesNotExist()
        {
            // Arrange
            var userGroup = CreateTestServerUserGroup();
            _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, $"{TEST_ENDPOINT}{ENDPOINT_USER_GROUP}/{userGroup.Name}")
                .ReturnsResponse(HttpStatusCode.NotFound, "Not Found");

            _httpMessageHandlerMock.SetupRequest(HttpMethod.Post, $"{TEST_ENDPOINT}{ENDPOINT_USER_GROUP}")
                .ReturnsResponse(HttpStatusCode.Created);

            // Act
            var result = await _kepwareApiClient.CreateOrUpdateServerUserGroupAsync(userGroup);

            // Assert
            result.ShouldBeTrue();
            _httpMessageHandlerMock.VerifyRequest(HttpMethod.Get, $"{TEST_ENDPOINT}{ENDPOINT_USER_GROUP}/{userGroup.Name}", Times.Once());
            _httpMessageHandlerMock.VerifyRequest(HttpMethod.Post, $"{TEST_ENDPOINT}{ENDPOINT_USER_GROUP}", Times.Once());
        }

        [Fact]
        public async Task CreateOrUpdateServerUserGroupAsync_ShouldUpdateServerUserGroup_WhenItExists()
        {
            // Arrange
            var userGroup = CreateTestServerUserGroup();
            var currentGroup = CreateTestServerUserGroup();
            currentGroup.Enabled = false;

            _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, $"{TEST_ENDPOINT}{ENDPOINT_USER_GROUP}/{userGroup.Name}")
                .ReturnsResponse(HttpStatusCode.OK, JsonSerializer.Serialize(currentGroup), "application/json");

            _httpMessageHandlerMock.SetupRequest(HttpMethod.Put, $"{TEST_ENDPOINT}{ENDPOINT_USER_GROUP}/{userGroup.Name}")
                .ReturnsResponse(HttpStatusCode.OK);

            // Act
            var result = await _kepwareApiClient.CreateOrUpdateServerUserGroupAsync(userGroup);

            // Assert
            result.ShouldBeTrue();
            _httpMessageHandlerMock.VerifyRequest(HttpMethod.Get, $"{TEST_ENDPOINT}{ENDPOINT_USER_GROUP}/{userGroup.Name}", Times.Once());
            _httpMessageHandlerMock.VerifyRequest(HttpMethod.Put, $"{TEST_ENDPOINT}{ENDPOINT_USER_GROUP}/{userGroup.Name}", Times.Once());
        }

        [Fact]
        public async Task DeleteServerUserGroupAsync_ShouldReturnTrue_WhenDeleteSuccessful()
        {
            // Arrange
            var groupName = "TestGroup";
            _httpMessageHandlerMock.SetupRequest(HttpMethod.Delete, $"{TEST_ENDPOINT}{ENDPOINT_USER_GROUP}/{groupName}")
                .ReturnsResponse(HttpStatusCode.OK);

            // Act
            var result = await _kepwareApiClient.DeleteServerUserGroupAsync(groupName);

            // Assert
            result.ShouldBeTrue();
            _httpMessageHandlerMock.VerifyRequest(HttpMethod.Delete, $"{TEST_ENDPOINT}{ENDPOINT_USER_GROUP}/{groupName}", Times.Once());
        }

        [Fact]
        public async Task DeleteServerUserGroupAsync_ShouldReturnFalse_WhenDeleteFails()
        {
            // Arrange
            var groupName = "TestGroup";
            _httpMessageHandlerMock.SetupRequest(HttpMethod.Delete, $"{TEST_ENDPOINT}{ENDPOINT_USER_GROUP}/{groupName}")
                .ReturnsResponse(HttpStatusCode.InternalServerError, "Internal Server Error");

            // Act
            var result = await _kepwareApiClient.DeleteServerUserGroupAsync(groupName);

            // Assert
            result.ShouldBeFalse();
            _httpMessageHandlerMock.VerifyRequest(HttpMethod.Delete, $"{TEST_ENDPOINT}{ENDPOINT_USER_GROUP}/{groupName}", Times.Once());
            _loggerMock.Verify(logger => 
                logger.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => true),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)), 
                Times.Once);
        }

        [Fact]
        public async Task GetServerUserGroupsAsync_ShouldReturnServerUserGroupCollection_WhenApiRespondsSuccessfully()
        {
            // Arrange
            var userGroups = new ServerUserGroupCollection
            {
                CreateTestServerUserGroup("Group1"),
                CreateTestServerUserGroup("Group2")
            };
            _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, $"{TEST_ENDPOINT}{ENDPOINT_USER_GROUP}")
                .ReturnsResponse(HttpStatusCode.OK, JsonSerializer.Serialize(userGroups), "application/json");

            // Act
            var result = await _kepwareApiClient.GetServerUserGroupListAsync();

            // Assert
            result.ShouldNotBeNull();
            result.Count.ShouldBe(2);
        }

        [Fact]
        public async Task GetServerUserGroupsAsync_ShouldReturnNull_WhenApiReturnsError()
        {
            // Arrange
            _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, $"{TEST_ENDPOINT}{ENDPOINT_USER_GROUP}")
                .ReturnsResponse(HttpStatusCode.InternalServerError, "Internal Server Error");

            // Act
            var result = await _kepwareApiClient.GetServerUserGroupListAsync();

            // Assert
            result.ShouldBeNull();
            _loggerMock.Verify(logger => 
                logger.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => true),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)), 
                Times.Once);
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
                ProjectModificationDelete = true
            };
        }
    }
}

