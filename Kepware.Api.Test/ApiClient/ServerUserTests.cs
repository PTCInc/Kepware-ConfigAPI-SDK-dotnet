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
    public class ServerUserTests : TestApiClientBase
    {
        private const string ENDPOINT_USER = "/config/v1/admin/server_users";

        [Fact]
        public async Task GetServerUserAsync_ShouldReturnServerUser_WhenApiRespondsSuccessfully()
        {
            // Arrange
            var user = CreateTestServerUser();
            _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, $"{TEST_ENDPOINT}{ENDPOINT_USER}/{user.Name}")
                .ReturnsResponse(HttpStatusCode.OK, JsonSerializer.Serialize(user), "application/json");

            // Act
            var result = await _kepwareApiClient.GetServerUserAsync(user.Name);

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
            _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, $"{TEST_ENDPOINT}{ENDPOINT_USER}/{userName}")
                .ReturnsResponse(HttpStatusCode.NotFound, "Not Found");

            // Act
            var result = await _kepwareApiClient.GetServerUserAsync(userName);

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
        public async Task CreateOrUpdateServerUserAsync_ShouldCreateServerUser_WhenItDoesNotExist()
        {
            // Arrange
            var user = CreateTestServerUser();
            _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, $"{TEST_ENDPOINT}{ENDPOINT_USER}/{user.Name}")
                .ReturnsResponse(HttpStatusCode.NotFound, "Not Found");

            _httpMessageHandlerMock.SetupRequest(HttpMethod.Post, $"{TEST_ENDPOINT}{ENDPOINT_USER}")
                .ReturnsResponse(HttpStatusCode.Created);

            // Act
            var result = await _kepwareApiClient.CreateOrUpdateServerUserAsync(user);

            // Assert
            result.ShouldBeTrue();
            _httpMessageHandlerMock.VerifyRequest(HttpMethod.Get, $"{TEST_ENDPOINT}{ENDPOINT_USER}/{user.Name}", Times.Once());
            _httpMessageHandlerMock.VerifyRequest(HttpMethod.Post, $"{TEST_ENDPOINT}{ENDPOINT_USER}", Times.Once());
        }

        [Fact]
        public async Task CreateOrUpdateServerUserAsync_ShouldUpdateServerUser_WhenItExists()
        {
            // Arrange
            var user = CreateTestServerUser();
            var currentUser = CreateTestServerUser();
            currentUser.Enabled = false;

            _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, $"{TEST_ENDPOINT}{ENDPOINT_USER}/{user.Name}")
                .ReturnsResponse(HttpStatusCode.OK, JsonSerializer.Serialize(currentUser), "application/json");

            _httpMessageHandlerMock.SetupRequest(HttpMethod.Put, $"{TEST_ENDPOINT}{ENDPOINT_USER}/{user.Name}")
                .ReturnsResponse(HttpStatusCode.OK);

            // Act
            var result = await _kepwareApiClient.CreateOrUpdateServerUserAsync(user);

            // Assert
            result.ShouldBeTrue();
            _httpMessageHandlerMock.VerifyRequest(HttpMethod.Get, $"{TEST_ENDPOINT}{ENDPOINT_USER}/{user.Name}", Times.Once());
            _httpMessageHandlerMock.VerifyRequest(HttpMethod.Put, $"{TEST_ENDPOINT}{ENDPOINT_USER}/{user.Name}", Times.Once());
        }

        [Fact]
        public async Task CreateServerUserAsync_ShouldThrowArgumentException_WhenPasswordIsInvalid()
        {
            // Arrange
            var user = CreateTestServerUser();
            user.Password = "short";

            _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, $"{TEST_ENDPOINT}{ENDPOINT_USER}/{user.Name}")
               .ReturnsResponse(HttpStatusCode.NotFound, "Not Found");

            // Act & Assert
            await Should.ThrowAsync<ArgumentException>(async () => 
                await _kepwareApiClient.CreateOrUpdateServerUserAsync(user));
        }


        [Fact]
        public async Task CreateServerUserAsync_ShouldThrowArgumentException_WhenPasswordIsEmpty()
        {
            // Arrange
            var user = CreateTestServerUser();
            user.Password = null;

            _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, $"{TEST_ENDPOINT}{ENDPOINT_USER}/{user.Name}")
               .ReturnsResponse(HttpStatusCode.NotFound, "Not Found");

            // Act & Assert
            await Should.ThrowAsync<ArgumentException>(async () =>
                await _kepwareApiClient.CreateOrUpdateServerUserAsync(user));
        }


        [Fact]
        public async Task UpdateServerUserAsync_ShouldThrowArgumentException_WhenPasswordIsInvalid()
        {
            // Arrange
            var user = CreateTestServerUser();
            
            _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, $"{TEST_ENDPOINT}{ENDPOINT_USER}/{user.Name}")
                  .ReturnsResponse(HttpStatusCode.OK, JsonSerializer.Serialize(user), "application/json");

            user.Password = "short";
            // Act & Assert
            await Should.ThrowAsync<ArgumentException>(async () =>
                await _kepwareApiClient.CreateOrUpdateServerUserAsync(user));
        }


        [Fact]
        public async Task UpdateServerUserAsync_ShouldNotThrowArgumentException_WhenPasswordIsEmpty()
        {
            // Arrange
            var user = CreateTestServerUser();
            
            _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, $"{TEST_ENDPOINT}{ENDPOINT_USER}/{user.Name}")
                  .ReturnsResponse(HttpStatusCode.OK, JsonSerializer.Serialize(user), "application/json");

            _httpMessageHandlerMock.SetupRequest(HttpMethod.Put, $"{TEST_ENDPOINT}{ENDPOINT_USER}/{user.Name}")
              .ReturnsResponse(HttpStatusCode.OK);

            user.Password = null;

            // Act
            var result = await _kepwareApiClient.CreateOrUpdateServerUserAsync(user);

            // Assert
            result.ShouldBeTrue();
            _httpMessageHandlerMock.VerifyRequest(HttpMethod.Get, $"{TEST_ENDPOINT}{ENDPOINT_USER}/{user.Name}", Times.Once());
            _httpMessageHandlerMock.VerifyRequest(HttpMethod.Put, $"{TEST_ENDPOINT}{ENDPOINT_USER}/{user.Name}", Times.Once());
        }

        [Fact]
        public async Task DeleteServerUserAsync_ShouldReturnTrue_WhenDeleteSuccessful()
        {
            // Arrange
            var userName = "TestUser";
            _httpMessageHandlerMock.SetupRequest(HttpMethod.Delete, $"{TEST_ENDPOINT}{ENDPOINT_USER}/{userName}")
                .ReturnsResponse(HttpStatusCode.OK);

            // Act
            var result = await _kepwareApiClient.DeleteServerUserAsync(userName);

            // Assert
            result.ShouldBeTrue();
            _httpMessageHandlerMock.VerifyRequest(HttpMethod.Delete, $"{TEST_ENDPOINT}{ENDPOINT_USER}/{userName}", Times.Once());
        }

        [Fact]
        public async Task DeleteServerUserAsync_ShouldReturnFalse_WhenDeleteFails()
        {
            // Arrange
            var userName = "TestUser";
            _httpMessageHandlerMock.SetupRequest(HttpMethod.Delete, $"{TEST_ENDPOINT}{ENDPOINT_USER}/{userName}")
                .ReturnsResponse(HttpStatusCode.InternalServerError, "Internal Server Error");

            // Act
            var result = await _kepwareApiClient.DeleteServerUserAsync(userName);

            // Assert
            result.ShouldBeFalse();
            _httpMessageHandlerMock.VerifyRequest(HttpMethod.Delete, $"{TEST_ENDPOINT}{ENDPOINT_USER}/{userName}", Times.Once());
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
        public async Task GetServerUsersAsync_ShouldReturnServerUserCollection_WhenApiRespondsSuccessfully()
        {
            // Arrange
            var users = new ServerUserCollection
            {
                CreateTestServerUser("User1"),
                CreateTestServerUser("User2")
            };
            _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, $"{TEST_ENDPOINT}{ENDPOINT_USER}")
                .ReturnsResponse(HttpStatusCode.OK, JsonSerializer.Serialize(users), "application/json");

            // Act
            var result = await _kepwareApiClient.GetServerUserListAsync();

            // Assert
            result.ShouldNotBeNull();
            result.Count.ShouldBe(2);
        }

        [Fact]
        public async Task GetServerUsersAsync_ShouldReturnNull_WhenApiReturnsError()
        {
            // Arrange
            _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, $"{TEST_ENDPOINT}{ENDPOINT_USER}")
                .ReturnsResponse(HttpStatusCode.InternalServerError, "Internal Server Error");

            // Act
            var result = await _kepwareApiClient.GetServerUserListAsync();

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

        private static ServerUser CreateTestServerUser(string name = "TestUser")
        {
            return new ServerUser
            {
                Name = name,
                Enabled = true,
                UserGroupName = "TestGroup",
                Password = "ValidPassword123!"
            };
        }
    }
}


