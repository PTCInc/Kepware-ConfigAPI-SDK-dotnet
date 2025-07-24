using Kepware.Api.Model.Admin;
using Moq;
using Moq.Contrib.HttpClient;
using Shouldly;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace Kepware.Api.TestIntg.ApiClient
{
    public class ProjectPermissionTests : TestApiClientBase
    {

        [Fact]
        public async Task GetProjectPermissionAsync_ShouldReturnProjectPermission_WhenApiRespondsSuccessfully()
        {
            // Arrange
            var serverUserGroup = new ServerUserGroup { Name = "Administrators" };
            var projectPermissionName = ProjectPermissionName.ServermainAlias;
            var projectPermission = new ProjectPermission
            {
                Name = projectPermissionName,
                AddObject = true,
                EditObject = true,
                DeleteObject = false
            };

            // Act
            var result = await _kepwareApiClient.Admin.GetProjectPermissionAsync(serverUserGroup, projectPermissionName);

            // Assert
            result.ShouldNotBeNull();
            result.Name.ShouldBe(projectPermissionName);
            result.AddObject.ShouldBeTrue();
            result.EditObject.ShouldBeTrue();
            result.DeleteObject.ShouldBeTrue();
        }

        //[Fact]
        //public async Task GetProjectPermissionAsync_ShouldReturnNull_WhenApiReturnsNotFound()
        //{
        //    // Arrange
        //    var serverUserGroup = new ServerUserGroup { Name = "Anonymous Clients" };
        //    var projectPermissionName = ProjectPermissionName.ServermainProject;

        //    _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, $"{TEST_ENDPOINT}{ENDPOINT_PROJECT_PERMISSION.Replace("{groupName}", serverUserGroup.Name).Replace("{permissionName}", projectPermissionName.ToString())}")
        //        .ReturnsResponse(HttpStatusCode.NotFound, "Not Found");

        //    // Act
        //    var result = await _kepwareApiClient.Admin.GetProjectPermissionAsync(serverUserGroup, projectPermissionName);

        //    // Assert
        //    result.ShouldBeNull();
        //}

        [Fact]
        public async Task UpdateProjectPermissionAsync_ShouldReturnTrue_WhenUpdateSuccessful()
        {
            // Arrange
            var serverUserGroup = new ServerUserGroup { Name = "TestGroup" };

            await _kepwareApiClient.Admin.CreateOrUpdateServerUserGroupAsync(serverUserGroup);

            var projectPermission = new ProjectPermission
            {
                Name = ProjectPermissionName.ServermainAlias,
                AddObject = true,
                EditObject = true,
                DeleteObject = false
            };

            // Act
            var result = await _kepwareApiClient.Admin.UpdateProjectPermissionAsync(serverUserGroup, projectPermission);

            // Assert
            result.ShouldBeTrue();

            // Clean up
            await _kepwareApiClient.Admin.DeleteServerUserGroupAsync(serverUserGroup.Name);
        }

        //[Fact]
        //public async Task UpdateProjectPermissionAsync_ShouldThrowInvalidOperationException_WhenProjectPermissionNotFound()
        //{
        //    // Arrange
        //    var serverUserGroup = new ServerUserGroup { Name = "TestGroup" };
        //    var projectPermission = new ProjectPermission
        //    {
        //        Name = ProjectPermissionName.ServermainAlias,
        //        AddObject = true,
        //        EditObject = true,
        //        DeleteObject = false
        //    };

        //    _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, $"{TEST_ENDPOINT}{ENDPOINT_PROJECT_PERMISSION.Replace("{groupName}", serverUserGroup.Name).Replace("{permissionName}", projectPermission.Name.ToString())}")
        //        .ReturnsResponse(HttpStatusCode.NotFound, "Not Found");

        //    // Act & Assert
        //    await Should.ThrowAsync<InvalidOperationException>(async () =>
        //        await _kepwareApiClient.Admin.UpdateProjectPermissionAsync(serverUserGroup, projectPermission));
        //}

        [Fact]
        public async Task UpdateProjectPermissionAsync_ShouldReturnFalse_WhenUpdateFails()
        {
            // TODO: Currently fails. Unsure of expected behavior from Kepware when an update fails. As of v6.18
            // Kepware returns a 200 OK with content that indicates a "not applied" key in the payload ,
            // which is not consistent with other endpoints.
            
            // Arrange
            var serverUserGroup = new ServerUserGroup { Name = "Administrators" };
            var projectPermission = new ProjectPermission
            {
                Name = ProjectPermissionName.ServermainAlias,
                AddObject = true,
                EditObject = true,
                DeleteObject = false
            };

            // Act
            var result = await _kepwareApiClient.Admin.UpdateProjectPermissionAsync(serverUserGroup, projectPermission);

            // Assert
            result.ShouldBeFalse("Currently fails. Unsure of expected behavior from Kepware when an update fails. See comments in test.");
        }
    }
}
