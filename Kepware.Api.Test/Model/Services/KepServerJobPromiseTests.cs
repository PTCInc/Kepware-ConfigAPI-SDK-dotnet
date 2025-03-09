using Kepware.Api.Model;
using Kepware.Api.Model.Services;
using Moq;
using Moq.Contrib.HttpClient;
using Shouldly;
using System;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Kepware.Api.Test.Model.Services
{
    public class KepServerJobPromiseTests
    {
        private const string TEST_ENDPOINT = "http://localhost";
        private const string JOB_ENDPOINT = "/config/v1/project/services/ReinitializeRuntime/jobs/job123";
        private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
        private readonly HttpClient _httpClient;

        public KepServerJobPromiseTests()
        {
            _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
            _httpClient = _httpMessageHandlerMock.CreateClient();
            _httpClient.BaseAddress = new Uri(TEST_ENDPOINT);
        }

        [Fact]
        public async Task AwaitCompletionAsync_ShouldReturnSuccess_WhenJobCompletesSuccessfully()
        {
            // Arrange
            var jobResponse = new JobResponseMessage { ResponseStatusCode = (int)ApiResponseCode.Accepted, JobId = JOB_ENDPOINT };
            var jobStatus = new JobStatusMessage { Completed = true };
            _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, $"{TEST_ENDPOINT}{JOB_ENDPOINT}")
                .ReturnsResponse(HttpStatusCode.OK, JsonSerializer.Serialize(jobStatus), "application/json");

            var promise = new KepServerJobPromise("endpoint", TimeSpan.FromSeconds(30), jobResponse, _httpClient);

            // Act
            var result = await promise.AwaitCompletionAsync();

            // Assert
            result.Value.ShouldBeTrue();
            result.IsSuccess.ShouldBeTrue();
        }

        [Fact]
        public async Task AwaitCompletionAsync_ShouldReturnTimeout_WhenJobDoesNotCompleteInTime()
        {
            // Arrange
            var jobResponse = new JobResponseMessage { ResponseStatusCode = (int)ApiResponseCode.Accepted, JobId = JOB_ENDPOINT };
            _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, $"{TEST_ENDPOINT}{JOB_ENDPOINT}")
                .ReturnsResponse(HttpStatusCode.OK, JsonSerializer.Serialize(new JobStatusMessage { Completed = false }), "application/json");

            var promise = new KepServerJobPromise("endpoint", TimeSpan.FromSeconds(1), jobResponse, _httpClient);

            // Act
            var result = await promise.AwaitCompletionAsync();

            // Assert
            result.Value.ShouldBeFalse();
            result.ResponseCode.ShouldBe(ApiResponseCode.Timeout);
        }

        [Fact]
        public void Dispose_ShouldDisposeResources()
        {
            // Arrange
            var jobResponse = new JobResponseMessage { ResponseStatusCode = (int)ApiResponseCode.Accepted, JobId = JOB_ENDPOINT };
            var promise = new KepServerJobPromise(JOB_ENDPOINT, TimeSpan.FromSeconds(30), jobResponse, _httpClient);

            // Act
            promise.Dispose();

            // Assert
            Should.Throw<ObjectDisposedException>(() => promise.AwaitCompletionAsync().GetAwaiter().GetResult());
        }
    }
}
