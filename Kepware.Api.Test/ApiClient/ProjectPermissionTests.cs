using Kepware.Api.Model.Admin;
using Moq;
using Moq.Contrib.HttpClient;
using Shouldly;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace Kepware.Api.Test.ApiClient
{
    public class ProjectPermissionTests : TestApiClientBase
    {
        private const string ENDPOINT_PROJECT_PERMISSION = "/config/v1/admin/server_usergroups/{groupName}/project_permissions/{permissionName}";

        [Fact]
        public async Task GetProjectPermissionAsync_ShouldReturnProjectPermission_WhenApiRespondsSuccessfully()
        {
            // Arrange
            var serverUserGroup = new ServerUserGroup { Name = "TestGroup" };
            var projectPermissionName = ProjectPermissionName.ServermainAlias;
            var projectPermission = new ProjectPermission
            {
                Name = projectPermissionName,
                AddObject = true,
                EditObject = true,
                DeleteObject = false
            };

            _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, $"{TEST_ENDPOINT}{ENDPOINT_PROJECT_PERMISSION.Replace("{groupName}", serverUserGroup.Name).Replace("{permissionName}", projectPermissionName.ToString())}")
                .ReturnsResponse(HttpStatusCode.OK, JsonSerializer.Serialize(projectPermission), "application/json");

            // Act
            var result = await _kepwareApiClient.Admin.GetProjectPermissionAsync(serverUserGroup, projectPermissionName);

            // Assert
            result.ShouldNotBeNull();
            result.Name.ShouldBe(projectPermissionName);
            result.AddObject.ShouldBeTrue();
            result.EditObject.ShouldBeTrue();
            result.DeleteObject.ShouldBeFalse();
        }

        [Fact]
        public async Task GetProjectPermissionAsync_ShouldReturnNull_WhenApiReturnsNotFound()
        {
            // Arrange
            var serverUserGroup = new ServerUserGroup { Name = "TestGroup" };
            var projectPermissionName = ProjectPermissionName.ServermainAlias;

            _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, $"{TEST_ENDPOINT}{ENDPOINT_PROJECT_PERMISSION.Replace("{groupName}", serverUserGroup.Name).Replace("{permissionName}", projectPermissionName.ToString())}")
                .ReturnsResponse(HttpStatusCode.NotFound, "Not Found");

            // Act
            var result = await _kepwareApiClient.Admin.GetProjectPermissionAsync(serverUserGroup, projectPermissionName);

            // Assert
            result.ShouldBeNull();
        }

        [Fact]
        public async Task UpdateProjectPermissionAsync_ShouldReturnTrue_WhenUpdateSuccessful()
        {
            // Arrange
            var serverUserGroup = new ServerUserGroup { Name = "TestGroup" };
            var projectPermission = new ProjectPermission
            {
                Name = ProjectPermissionName.ServermainAlias,
                AddObject = true,
                EditObject = true,
                DeleteObject = false
            };

            _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, $"{TEST_ENDPOINT}{ENDPOINT_PROJECT_PERMISSION.Replace("{groupName}", serverUserGroup.Name).Replace("{permissionName}", projectPermission.Name.ToString())}")
                .ReturnsResponse(HttpStatusCode.OK, JsonSerializer.Serialize(projectPermission), "application/json");

            _httpMessageHandlerMock.SetupRequest(HttpMethod.Put, $"{TEST_ENDPOINT}{ENDPOINT_PROJECT_PERMISSION.Replace("{groupName}", serverUserGroup.Name).Replace("{permissionName}", projectPermission.Name.ToString())}")
                .ReturnsResponse(HttpStatusCode.OK);

            // Act
            var result = await _kepwareApiClient.Admin.UpdateProjectPermissionAsync(serverUserGroup, projectPermission);

            // Assert
            result.ShouldBeTrue();
        }

        [Fact]
        public async Task UpdateProjectPermissionAsync_ShouldThrowInvalidOperationException_WhenProjectPermissionNotFound()
        {
            // Arrange
            var serverUserGroup = new ServerUserGroup { Name = "TestGroup" };
            var projectPermission = new ProjectPermission
            {
                Name = ProjectPermissionName.ServermainAlias,
                AddObject = true,
                EditObject = true,
                DeleteObject = false
            };

            _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, $"{TEST_ENDPOINT}{ENDPOINT_PROJECT_PERMISSION.Replace("{groupName}", serverUserGroup.Name).Replace("{permissionName}", projectPermission.Name.ToString())}")
                .ReturnsResponse(HttpStatusCode.NotFound, "Not Found");

            // Act & Assert
            await Should.ThrowAsync<InvalidOperationException>(async () =>
                await _kepwareApiClient.Admin.UpdateProjectPermissionAsync(serverUserGroup, projectPermission));
        }

        [Fact]
        public async Task UpdateProjectPermissionAsync_ShouldReturnFalse_WhenUpdateFails()
        {
            // Arrange
            var serverUserGroup = new ServerUserGroup { Name = "TestGroup" };
            var projectPermission = new ProjectPermission
            {
                Name = ProjectPermissionName.ServermainAlias,
                AddObject = true,
                EditObject = true,
                DeleteObject = false
            };

            _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, $"{TEST_ENDPOINT}{ENDPOINT_PROJECT_PERMISSION.Replace("{groupName}", serverUserGroup.Name).Replace("{permissionName}", projectPermission.Name.ToString())}")
                .ReturnsResponse(HttpStatusCode.OK, JsonSerializer.Serialize(projectPermission), "application/json");

            _httpMessageHandlerMock.SetupRequest(HttpMethod.Put, $"{TEST_ENDPOINT}{ENDPOINT_PROJECT_PERMISSION.Replace("{groupName}", serverUserGroup.Name).Replace("{permissionName}", projectPermission.Name.ToString())}")
                .ReturnsResponse(HttpStatusCode.BadRequest, "Invalid setting value");

            // Act
            var result = await _kepwareApiClient.Admin.UpdateProjectPermissionAsync(serverUserGroup, projectPermission);

            // Assert
            result.ShouldBeFalse();
        }
    }
}
