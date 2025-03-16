using Kepware.Api;
using Kepware.Api.Model;
using Kepware.Api.Model.Services;
using Kepware.Api.Test.Util;
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

namespace Kepware.Api.Test.ApiClient
{
    public class ReinitializeRuntimeAsyncTests : TestApiClientBase
    {
        private const string ENDPOINT_REINITIALIZE_RUNTIME = "/config/v1/project/services/ReinitializeRuntime";
        private const string JOB_ENDPOINT = "/config/v1/project/services/ReinitializeRuntime/jobs/job123";

        [Fact]
        public async Task ReinitializeRuntimeAsync_ShouldReturnKepServerJobPromise_WhenApiRespondsSuccessfully()
        {
            // Arrange
            var jobResponse = new JobResponseMessage { ResponseStatusCode = (int)ApiResponseCode.Accepted, JobId = JOB_ENDPOINT };
            _httpMessageHandlerMock.SetupRequest(HttpMethod.Put, $"{TEST_ENDPOINT}{ENDPOINT_REINITIALIZE_RUNTIME}")
                .ReturnsResponse(HttpStatusCode.Accepted, JsonSerializer.Serialize(jobResponse), "application/json");

            // Act
            var result = await _kepwareApiClient.ApiServices.ReinitializeRuntimeAsync(TimeSpan.FromSeconds(30));

            // Assert
            result.ShouldNotBeNull();
            result.Endpoint.ShouldBe("/config/v1/project/services/ReinitializeRuntime");
            result.JobTimeToLive.ShouldBe(TimeSpan.FromSeconds(30));
        }

        [Fact]
        public async Task ReinitializeRuntimeAsync_ShouldReturnKepServerJobPromise_WhenApiResponseIsInvalid()
        {
            // Arrange
            _httpMessageHandlerMock.SetupRequest(HttpMethod.Put, $"{TEST_ENDPOINT}{ENDPOINT_REINITIALIZE_RUNTIME}")
                .ReturnsResponse(HttpStatusCode.BadRequest, "Bad Request");

            // Act
            var result = await _kepwareApiClient.ApiServices.ReinitializeRuntimeAsync(TimeSpan.FromSeconds(30));

            // Assert
            result.ShouldNotBeNull();
            result.Endpoint.ShouldBe("/config/v1/project/services/ReinitializeRuntime");
            result.JobTimeToLive.ShouldBe(TimeSpan.FromSeconds(30));
        }

        [Fact]
        public async Task ReinitializeRuntimeAsync_ShouldThrowException_WhenHttpClientThrowsException()
        {
            // Arrange
            _httpMessageHandlerMock.SetupRequest(HttpMethod.Put, $"{TEST_ENDPOINT}{ENDPOINT_REINITIALIZE_RUNTIME}")
                .Throws(new HttpRequestException("Network error"));

            // Act & Assert
            await Should.ThrowAsync<HttpRequestException>(async () =>
            {
                await _kepwareApiClient.ApiServices.ReinitializeRuntimeAsync(TimeSpan.FromSeconds(30));
            });
        }

        [Fact]
        public async Task ReinitializeRuntimeAsync_ShouldHandleTimeout_WhenOperationTimesOut()
        {
            // Arrange
            _httpMessageHandlerMock.SetupRequest(HttpMethod.Put, $"{TEST_ENDPOINT}{ENDPOINT_REINITIALIZE_RUNTIME}")
                .ReturnsResponse(HttpStatusCode.RequestTimeout, "Request Timeout");

            // Act
            var result = await _kepwareApiClient.ApiServices.ReinitializeRuntimeAsync(TimeSpan.FromSeconds(30));

            // Assert
            result.ShouldNotBeNull();
            result.Endpoint.ShouldBe("/config/v1/project/services/ReinitializeRuntime");
            result.JobTimeToLive.ShouldBe(TimeSpan.FromSeconds(30));
        }

        [Fact]
        public async Task ReinitializeRuntimeAsync_ShouldHandleInvalidInput_WhenTimeToLiveIsNegative()
        {
            // Act & Assert
            await Should.ThrowAsync<ArgumentOutOfRangeException>(async () =>
            {
                await _kepwareApiClient.ApiServices.ReinitializeRuntimeAsync(TimeSpan.FromSeconds(-1));
            });
        }

        [Fact]
        public async Task ReinitializeRuntimeAsync_ShouldReturnSuccess_WhenJobCompletesSuccessfullyAfterFirstGet()
        {
            // Arrange
            var jobResponse = new JobResponseMessage { ResponseStatusCode = (int)ApiResponseCode.Accepted, JobId = JOB_ENDPOINT };
            var jobStatus = new JobStatusMessage { Completed = true };
            _httpMessageHandlerMock.SetupRequest(HttpMethod.Put, $"{TEST_ENDPOINT}{ENDPOINT_REINITIALIZE_RUNTIME}")
                .ReturnsResponse(HttpStatusCode.Accepted, JsonSerializer.Serialize(jobResponse), "application/json");
            _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, $"{TEST_ENDPOINT}{JOB_ENDPOINT}")
                .ReturnsResponse(HttpStatusCode.OK, JsonSerializer.Serialize(jobStatus), "application/json");

            // Act
            var result = await _kepwareApiClient.ApiServices.ReinitializeRuntimeAsync(TimeSpan.FromSeconds(30));
            var completionResult = await result.AwaitCompletionAsync();

            // Assert
            completionResult.Value.ShouldBeTrue();
            completionResult.IsSuccess.ShouldBeTrue();
        }

        [Fact]
        public async Task ReinitializeRuntimeAsync_ShouldReturnSuccess_WhenJobCompletesSuccessfullyAfterMultipleGets()
        {
            // Arrange
            var jobResponse = new JobResponseMessage { ResponseStatusCode = (int)ApiResponseCode.Accepted, JobId = JOB_ENDPOINT };
            var jobStatusIncomplete = new JobStatusMessage { Completed = false };
            var jobStatusComplete = new JobStatusMessage { Completed = true };
            _httpMessageHandlerMock.SetupRequest(HttpMethod.Put, $"{TEST_ENDPOINT}{ENDPOINT_REINITIALIZE_RUNTIME}")
                .ReturnsResponse(HttpStatusCode.Accepted, JsonSerializer.Serialize(jobResponse), "application/json");
            _httpMessageHandlerMock.SetupSequenceRequest(HttpMethod.Get, $"{TEST_ENDPOINT}{JOB_ENDPOINT}")
                .ReturnsResponse(HttpStatusCode.OK, JsonSerializer.Serialize(jobStatusIncomplete), "application/json")
                .ReturnsResponse(HttpStatusCode.OK, JsonSerializer.Serialize(jobStatusComplete), "application/json");

            // Act
            var result = await _kepwareApiClient.ApiServices.ReinitializeRuntimeAsync(TimeSpan.FromSeconds(30));
            var completionResult = await result.AwaitCompletionAsync();

            // Assert
            completionResult.Value.ShouldBeTrue();
            completionResult.IsSuccess.ShouldBeTrue();
        }

        [Fact]
        public async Task ReinitializeRuntimeAsync_ShouldReturnFailure_WhenJobFailsAfterFirstGet()
        {
            // Arrange
            var jobResponse = new JobResponseMessage { ResponseStatusCode = (int)ApiResponseCode.Accepted, JobId = JOB_ENDPOINT };
            var jobStatus = new JobStatusMessage { Completed = false };
            _httpMessageHandlerMock.SetupRequest(HttpMethod.Put, $"{TEST_ENDPOINT}{ENDPOINT_REINITIALIZE_RUNTIME}")
                .ReturnsResponse(HttpStatusCode.Accepted, JsonSerializer.Serialize(jobResponse), "application/json");
            _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, $"{TEST_ENDPOINT}{JOB_ENDPOINT}")
                .ReturnsResponse(HttpStatusCode.OK, JsonSerializer.Serialize(jobStatus), "application/json");

            // Act
            var result = await _kepwareApiClient.ApiServices.ReinitializeRuntimeAsync(TimeSpan.FromSeconds(2));
            var completionResult = await result.AwaitCompletionAsync();

            // Assert
            completionResult.Value.ShouldBeFalse();
            completionResult.IsSuccess.ShouldBeFalse();
        }

        [Fact]
        public async Task ReinitializeRuntimeAsync_ShouldReturnFailure_WhenJobFailsAfterMultipleGets()
        {
            // Arrange
            var jobResponse = new JobResponseMessage { ResponseStatusCode = (int)ApiResponseCode.Accepted, JobId = JOB_ENDPOINT };
            var jobStatusIncomplete = new JobStatusMessage { Completed = false };

            var jobStatusFailed = new JobStatusMessage { Completed = true, Message = "Job failed" };

            _httpMessageHandlerMock.SetupRequest(HttpMethod.Put, $"{TEST_ENDPOINT}{ENDPOINT_REINITIALIZE_RUNTIME}")
                .ReturnsResponse(HttpStatusCode.Accepted, JsonSerializer.Serialize(jobResponse), "application/json");
            _httpMessageHandlerMock.SetupSequenceRequest(HttpMethod.Get, $"{TEST_ENDPOINT}{JOB_ENDPOINT}")
                .ReturnsResponse(HttpStatusCode.OK, JsonSerializer.Serialize(jobStatusIncomplete), "application/json")
                .ReturnsResponse(HttpStatusCode.OK, JsonSerializer.Serialize(jobStatusIncomplete), "application/json")
                .ReturnsResponse(HttpStatusCode.ServiceUnavailable, JsonSerializer.Serialize(jobStatusFailed), "application/json");

            // Act
            var result = await _kepwareApiClient.ApiServices.ReinitializeRuntimeAsync(TimeSpan.FromSeconds(5));
            var completionResult = await result.AwaitCompletionAsync();

            // Assert
            completionResult.Value.ShouldBeFalse();
            completionResult.IsSuccess.ShouldBeFalse();
        }
    }
}
