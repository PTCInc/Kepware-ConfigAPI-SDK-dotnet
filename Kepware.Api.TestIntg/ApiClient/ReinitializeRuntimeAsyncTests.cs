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

namespace Kepware.Api.TestIntg.ApiClient
{
    public class ReinitializeRuntimeAsyncTests : TestApiClientBase
    {

        [Fact]
        public async Task ReinitializeRuntimeAsync_ShouldReturnKepServerJobPromise_WhenApiResponseIsInvalid()
        {

            // Act
            var result = await _badCredKepwareApiClient.ApiServices.ReinitializeRuntimeAsync(TimeSpan.FromSeconds(30));

            // Assert
            result.ShouldNotBeNull();
            result.Endpoint.ShouldBe("/config/v1/project/services/ReinitializeRuntime");
            result.JobTimeToLive.ShouldBe(TimeSpan.FromSeconds(30));
        }


        [Fact]
        public async Task ReinitializeRuntimeAsync_ShouldReturnSuccess_WhenJobCompletesSuccessfully()
        {

            // Act
            var result = await _kepwareApiClient.ApiServices.ReinitializeRuntimeAsync(TimeSpan.FromSeconds(30));
            // Assert
            result.ShouldNotBeNull();
            result.Endpoint.ShouldBe("/config/v1/project/services/ReinitializeRuntime");
            result.JobTimeToLive.ShouldBe(TimeSpan.FromSeconds(30));

            // Wait for the job to complete
            var completionResult = await result.AwaitCompletionAsync();

            // Assert
            completionResult.Value.ShouldBeTrue();
            completionResult.IsSuccess.ShouldBeTrue();
        }
    }
}
