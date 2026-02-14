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
using Xunit.Extensions.Ordering;

namespace Kepware.Api.TestIntg.ApiClient
{
    public class AutomaticTagGenerationAsyncTests : TestIntgApiClientBase
    {

        [Fact]
        public async Task AutomaticTagGenerationAsync_ShouldReturnKepServerJobPromise_WhenApiResponseIsInvalid()
        {
            // Arrange
            // Create channel that doesn't support ATG
            var channel = await AddTestChannel();
            var device = await AddTestDevice(channel);

            // Act
            var result = await _kepwareApiClient.ApiServices.AutomaticTagGenerationAsync(channel.Name, device.Name, TimeSpan.FromSeconds(30));
            // Assert
            result.ShouldNotBeNull();
            //result.Endpoint.ShouldBe(ENDPOINT_TAG_GENERATION);
            result.JobTimeToLive.ShouldBe(TimeSpan.FromSeconds(30));

            // Clean up
            await DeleteAllChannelsAsync();
        }

        //    [Fact]
        //    public async Task AutomaticTagGenerationAsync_ShouldThrowException_WhenHttpClientThrowsException()
        //    {
        //        // Arrange
        //        _httpMessageHandlerMock.SetupRequest(HttpMethod.Put, $"{TEST_ENDPOINT}{ENDPOINT_TAG_GENERATION}")
        //            .Throws(new HttpRequestException("Network error"));

        //        // Act & Assert
        //        await Should.ThrowAsync<HttpRequestException>(async () =>
        //        {
        //            await _kepwareApiClient.ApiServices.AutomaticTagGenerationAsync(UNIT_TEST_CHANNEL, UNIT_TEST_DEVICE, TimeSpan.FromSeconds(30));
        //        });
        //    }

        //    [Fact]
        //    public async Task AutomaticTagGenerationAsync_ShouldReturnSuccess_WhenJobCompletesSuccessfullyAfterFirstGet()
        //    {
        //        // Arrange
        //        var jobResponse = new JobResponseMessage { ResponseStatusCode = (int)ApiResponseCode.Accepted, JobId = JOB_ENDPOINT };
        //        var jobStatus = new JobStatusMessage { Completed = true };
        //        _httpMessageHandlerMock.SetupRequest(HttpMethod.Put, $"{TEST_ENDPOINT}{ENDPOINT_TAG_GENERATION}")
        //            .ReturnsResponse(HttpStatusCode.Accepted, JsonSerializer.Serialize(jobResponse), "application/json");
        //        _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, $"{TEST_ENDPOINT}{JOB_ENDPOINT}")
        //            .ReturnsResponse(HttpStatusCode.OK, JsonSerializer.Serialize(jobStatus), "application/json");

        //        // Act
        //        var result = await _kepwareApiClient.ApiServices.AutomaticTagGenerationAsync(UNIT_TEST_CHANNEL, UNIT_TEST_DEVICE, TimeSpan.FromSeconds(30));
        //        var completionResult = await result.AwaitCompletionAsync();

        //        // Assert
        //        completionResult.Value.ShouldBeTrue();
        //        completionResult.IsSuccess.ShouldBeTrue();
        //    }

        [Fact]
        public async Task AutomaticTagGenerationAsync_ShouldReturnSuccess_WhenJobCompletesSuccessfully()
        {
            // Arrange
            var channel = await AddTestChannel(driver: "Allen-Bradley ControlLogix Ethernet");
            var device = await AddAtgTestDevice(channel);

            // Act
            var result = await _kepwareApiClient.ApiServices.AutomaticTagGenerationAsync(channel.Name, device.Name, TimeSpan.FromSeconds(30));

            // Assert
            result.ShouldNotBeNull();
            result.JobTimeToLive.ShouldBe(TimeSpan.FromSeconds(30));

            // Wait for job completion
            var completionResult = await result.AwaitCompletionAsync();

            // Assert
            completionResult.Value.ShouldBeTrue();
            completionResult.IsSuccess.ShouldBeTrue();

            // Clean up
            await DeleteAllChannelsAsync();
        }

        //    [Fact]
        //    public async Task AutomaticTagGenerationAsync_ShouldReturnFailure_WhenJobFailsAfterFirstGet()
        //    {
        //        // Arrange
        //        var jobResponse = new JobResponseMessage { ResponseStatusCode = (int)ApiResponseCode.Accepted, JobId = JOB_ENDPOINT };
        //        var jobStatus = new JobStatusMessage { Completed = false };
        //        _httpMessageHandlerMock.SetupRequest(HttpMethod.Put, $"{TEST_ENDPOINT}{ENDPOINT_TAG_GENERATION}")
        //            .ReturnsResponse(HttpStatusCode.Accepted, JsonSerializer.Serialize(jobResponse), "application/json");
        //        _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, $"{TEST_ENDPOINT}{JOB_ENDPOINT}")
        //            .ReturnsResponse(HttpStatusCode.OK, JsonSerializer.Serialize(jobStatus), "application/json");

        //        // Act
        //        var result = await _kepwareApiClient.ApiServices.AutomaticTagGenerationAsync(UNIT_TEST_CHANNEL, UNIT_TEST_DEVICE, TimeSpan.FromSeconds(1));
        //        var completionResult = await result.AwaitCompletionAsync(TimeSpan.FromMilliseconds(100));

        //        // Assert
        //        completionResult.Value.ShouldBeFalse();
        //        completionResult.IsSuccess.ShouldBeFalse();
        //    }

        //    [Fact]
        //    public async Task AutomaticTagGenerationAsync_ShouldReturnFailure_WhenJobFailsAfterMultipleGets()
        //    {
        //        // Arrange
        //        var jobResponse = new JobResponseMessage { ResponseStatusCode = (int)ApiResponseCode.Accepted, JobId = JOB_ENDPOINT };
        //        var jobStatusIncomplete = new JobStatusMessage { Completed = false };

        //        var jobStatusFailed = new JobStatusMessage { Completed = true, Message = "Job failed" };

        //        _httpMessageHandlerMock.SetupRequest(HttpMethod.Put, $"{TEST_ENDPOINT}{ENDPOINT_TAG_GENERATION}")
        //            .ReturnsResponse(HttpStatusCode.Accepted, JsonSerializer.Serialize(jobResponse), "application/json");
        //        _httpMessageHandlerMock.SetupSequenceRequest(HttpMethod.Get, $"{TEST_ENDPOINT}{JOB_ENDPOINT}")
        //            .ReturnsResponse(HttpStatusCode.OK, JsonSerializer.Serialize(jobStatusIncomplete), "application/json")
        //            .ReturnsResponse(HttpStatusCode.OK, JsonSerializer.Serialize(jobStatusIncomplete), "application/json")
        //            .ReturnsResponse(HttpStatusCode.ServiceUnavailable, JsonSerializer.Serialize(jobStatusFailed), "application/json");

        //        // Act
        //        var result = await _kepwareApiClient.ApiServices.AutomaticTagGenerationAsync(UNIT_TEST_CHANNEL, UNIT_TEST_DEVICE, TimeSpan.FromSeconds(5));
        //        var completionResult = await result.AwaitCompletionAsync(TimeSpan.FromMilliseconds(100));

        //        // Assert
        //        completionResult.Value.ShouldBeFalse();
        //        completionResult.IsSuccess.ShouldBeFalse();
        //    }
    }
}
