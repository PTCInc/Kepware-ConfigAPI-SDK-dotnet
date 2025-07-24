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
    public class AutomaticTagGenerationAsyncTests : TestApiClientBase
    {
        private const string UNIT_TEST_CHANNEL = "unitTestChannel";
        private const string UNIT_TEST_DEVICE = "unitTestDevice";
        private const string ENDPOINT_TAG_GENERATION = $"/config/v1/project/channels/{UNIT_TEST_CHANNEL}/devices/{UNIT_TEST_DEVICE}/services/TagGeneration";
        private const string JOB_ENDPOINT = $"/config/v1/project/channels/{UNIT_TEST_CHANNEL}/devices/{UNIT_TEST_DEVICE}/services/TagGeneration/jobs/job123";

        [Fact]
        public async Task AutomaticTagGenerationAsync_ShouldReturnKepServerJobPromise_WhenApiRespondsSuccessfully()
        {
            // Arrange
            var jobResponse = new JobResponseMessage { ResponseStatusCode = (int)ApiResponseCode.Accepted, JobId = JOB_ENDPOINT };
            _httpMessageHandlerMock.SetupRequest(HttpMethod.Put, $"{TEST_ENDPOINT}{ENDPOINT_TAG_GENERATION}")
                .ReturnsResponse(HttpStatusCode.Accepted, JsonSerializer.Serialize(jobResponse), "application/json");

            // Act
            var result = await _kepwareApiClient.ApiServices.AutomaticTagGenerationAsync(UNIT_TEST_CHANNEL, UNIT_TEST_DEVICE, TimeSpan.FromSeconds(30));

            // Assert
            result.ShouldNotBeNull();
            result.Endpoint.ShouldBe(ENDPOINT_TAG_GENERATION);
            result.JobTimeToLive.ShouldBe(TimeSpan.FromSeconds(30));
        }

        [Fact]
        public async Task AutomaticTagGenerationAsync_ShouldReturnKepServerJobPromise_WhenApiResponseIsInvalid()
        {
            // Arrange
            _httpMessageHandlerMock.SetupRequest(HttpMethod.Put, $"{TEST_ENDPOINT}{ENDPOINT_TAG_GENERATION}")
                .ReturnsResponse(HttpStatusCode.BadRequest, "Bad Request");

            // Act
            var result = await _kepwareApiClient.ApiServices.AutomaticTagGenerationAsync(UNIT_TEST_CHANNEL, UNIT_TEST_DEVICE, TimeSpan.FromSeconds(30));
            var jobResult = await result.AwaitCompletionAsync();

            // Assert
            result.ShouldNotBeNull();
            result.Endpoint.ShouldBe(ENDPOINT_TAG_GENERATION);
            result.JobTimeToLive.ShouldBe(TimeSpan.FromSeconds(30));
            jobResult.Value.ShouldBeFalse();
            jobResult.IsSuccess.ShouldBeFalse();
            jobResult.ResponseCode.ShouldBe(ApiResponseCode.BadRequest);
        }

        [Fact]
        public async Task AutomaticTagGenerationAsync_ShouldThrowException_WhenHttpClientThrowsException()
        {
            // Arrange
            _httpMessageHandlerMock.SetupRequest(HttpMethod.Put, $"{TEST_ENDPOINT}{ENDPOINT_TAG_GENERATION}")
                .Throws(new HttpRequestException("Network error"));

            // Act & Assert
            await Should.ThrowAsync<HttpRequestException>(async () =>
            {
                await _kepwareApiClient.ApiServices.AutomaticTagGenerationAsync(UNIT_TEST_CHANNEL, UNIT_TEST_DEVICE, TimeSpan.FromSeconds(30));
            });
        }

        [Fact]
        public async Task AutomaticTagGenerationAsync_ShouldHandleTimeout_WhenOperationTimesOut()
        {
            // Arrange
            _httpMessageHandlerMock.SetupRequest(HttpMethod.Put, $"{TEST_ENDPOINT}{ENDPOINT_TAG_GENERATION}")
                .ReturnsResponse(HttpStatusCode.RequestTimeout, "Request Timeout");

            // Act
            var result = await _kepwareApiClient.ApiServices.AutomaticTagGenerationAsync(UNIT_TEST_CHANNEL, UNIT_TEST_DEVICE, TimeSpan.FromSeconds(30));

            // Assert
            result.ShouldNotBeNull();
            result.Endpoint.ShouldBe(ENDPOINT_TAG_GENERATION);
            result.JobTimeToLive.ShouldBe(TimeSpan.FromSeconds(30));
        }

        [Fact]
        public async Task AutomaticTagGenerationAsync_ShouldHandleInvalidInput_WhenTimeToLiveIsNegative()
        {
            // Act & Assert
            await Should.ThrowAsync<ArgumentOutOfRangeException>(async () =>
            {
                await _kepwareApiClient.ApiServices.AutomaticTagGenerationAsync(UNIT_TEST_CHANNEL, UNIT_TEST_DEVICE, TimeSpan.FromSeconds(-1));
            });
        }

        [Fact]
        public async Task AutomaticTagGenerationAsync_ShouldReturnSuccess_WhenJobCompletesSuccessfullyAfterFirstGet()
        {
            // Arrange
            var jobResponse = new JobResponseMessage { ResponseStatusCode = (int)ApiResponseCode.Accepted, JobId = JOB_ENDPOINT };
            var jobStatus = new JobStatusMessage { Completed = true };
            _httpMessageHandlerMock.SetupRequest(HttpMethod.Put, $"{TEST_ENDPOINT}{ENDPOINT_TAG_GENERATION}")
                .ReturnsResponse(HttpStatusCode.Accepted, JsonSerializer.Serialize(jobResponse), "application/json");
            _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, $"{TEST_ENDPOINT}{JOB_ENDPOINT}")
                .ReturnsResponse(HttpStatusCode.OK, JsonSerializer.Serialize(jobStatus), "application/json");

            // Act
            var result = await _kepwareApiClient.ApiServices.AutomaticTagGenerationAsync(UNIT_TEST_CHANNEL, UNIT_TEST_DEVICE, TimeSpan.FromSeconds(30));
            var completionResult = await result.AwaitCompletionAsync();

            // Assert
            completionResult.Value.ShouldBeTrue();
            completionResult.IsSuccess.ShouldBeTrue();
            completionResult.ResponseCode.ShouldBe(ApiResponseCode.Success);
        }

        [Fact]
        public async Task AutomaticTagGenerationAsync_ShouldReturnSuccess_WhenJobCompletesSuccessfullyAfterMultipleGets()
        {
            // Arrange
            var jobResponse = new JobResponseMessage { ResponseStatusCode = (int)ApiResponseCode.Accepted, JobId = JOB_ENDPOINT };
            var jobStatusIncomplete = new JobStatusMessage { Completed = false };
            var jobStatusComplete = new JobStatusMessage { Completed = true };
            _httpMessageHandlerMock.SetupRequest(HttpMethod.Put, $"{TEST_ENDPOINT}{ENDPOINT_TAG_GENERATION}")
                .ReturnsResponse(HttpStatusCode.Accepted, JsonSerializer.Serialize(jobResponse), "application/json");
            _httpMessageHandlerMock.SetupSequenceRequest(HttpMethod.Get, $"{TEST_ENDPOINT}{JOB_ENDPOINT}")
                .ReturnsResponse(HttpStatusCode.OK, JsonSerializer.Serialize(jobStatusIncomplete), "application/json")
                .ReturnsResponse(HttpStatusCode.OK, JsonSerializer.Serialize(jobStatusComplete), "application/json");

            // Act
            var result = await _kepwareApiClient.ApiServices.AutomaticTagGenerationAsync(UNIT_TEST_CHANNEL, UNIT_TEST_DEVICE, TimeSpan.FromSeconds(30));
            var completionResult = await result.AwaitCompletionAsync();

            // Assert
            completionResult.Value.ShouldBeTrue();
            completionResult.IsSuccess.ShouldBeTrue();
            completionResult.ResponseCode.ShouldBe(ApiResponseCode.Success);
        }

        [Fact]
        public async Task AutomaticTagGenerationAsync_ShouldReturnFailure_WhenJobFailsAfterFirstGet()
        {
            // Arrange
            var jobResponse = new JobResponseMessage { ResponseStatusCode = (int)ApiResponseCode.Accepted, JobId = JOB_ENDPOINT };
            var jobStatus = new JobStatusMessage { Completed = false };
            _httpMessageHandlerMock.SetupRequest(HttpMethod.Put, $"{TEST_ENDPOINT}{ENDPOINT_TAG_GENERATION}")
                .ReturnsResponse(HttpStatusCode.Accepted, JsonSerializer.Serialize(jobResponse), "application/json");
            _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, $"{TEST_ENDPOINT}{JOB_ENDPOINT}")
                .ReturnsResponse(HttpStatusCode.OK, JsonSerializer.Serialize(jobStatus), "application/json");

            // Act
            var result = await _kepwareApiClient.ApiServices.AutomaticTagGenerationAsync(UNIT_TEST_CHANNEL, UNIT_TEST_DEVICE, TimeSpan.FromSeconds(1));
            var completionResult = await result.AwaitCompletionAsync(TimeSpan.FromMilliseconds(100));

            // Assert
            completionResult.Value.ShouldBeFalse();
            completionResult.IsSuccess.ShouldBeFalse();
            completionResult.ResponseCode.ShouldBe(ApiResponseCode.Timeout);
        }

        [Fact]
        public async Task AutomaticTagGenerationAsync_ShouldReturnFailure_WhenJobFailsAfterMultipleGets()
        {
            // Arrange
            var jobResponse = new JobResponseMessage { ResponseStatusCode = (int)ApiResponseCode.Accepted, JobId = JOB_ENDPOINT };
            var jobStatusIncomplete = new JobStatusMessage { Completed = false };

            var jobStatusFailed = new JobStatusMessage { Completed = true, Message = "Job failed" };

            _httpMessageHandlerMock.SetupRequest(HttpMethod.Put, $"{TEST_ENDPOINT}{ENDPOINT_TAG_GENERATION}")
                .ReturnsResponse(HttpStatusCode.Accepted, JsonSerializer.Serialize(jobResponse), "application/json");
            _httpMessageHandlerMock.SetupSequenceRequest(HttpMethod.Get, $"{TEST_ENDPOINT}{JOB_ENDPOINT}")
                .ReturnsResponse(HttpStatusCode.OK, JsonSerializer.Serialize(jobStatusIncomplete), "application/json")
                .ReturnsResponse(HttpStatusCode.OK, JsonSerializer.Serialize(jobStatusIncomplete), "application/json")
                .ReturnsResponse(HttpStatusCode.ServiceUnavailable, JsonSerializer.Serialize(jobStatusFailed), "application/json");

            // Act
            var result = await _kepwareApiClient.ApiServices.AutomaticTagGenerationAsync(UNIT_TEST_CHANNEL, UNIT_TEST_DEVICE, TimeSpan.FromSeconds(5));
            var completionResult = await result.AwaitCompletionAsync(TimeSpan.FromMilliseconds(100));

            // Assert
            completionResult.Value.ShouldBeFalse();
            completionResult.IsSuccess.ShouldBeFalse();
            completionResult.ResponseCode.ShouldBe(ApiResponseCode.ServiceUnavailable);
            completionResult.Message.ShouldBe(jobStatusFailed.Message);
        }
    }
}
