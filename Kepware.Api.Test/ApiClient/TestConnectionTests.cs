using Moq;
using Moq.Contrib.HttpClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kepware.Api.Test.ApiClient
{
    public class TestConnection : TestApiClientBase
    {
        [Fact]
        public async Task TestConnectionAsync_ShouldReturnTrue_WhenApiIsHealthy()
        {
            ConfigureConnectedClient();

            // Act
            var result = await _kepwareApiClient.TestConnectionAsync();

            // Assert
            Assert.True(result);
        }


        [Fact]
        public async Task TestConnectionAsync_ShouldReturnFalse_WhenApiIsUnhealthy()
        {
            // Arrange: Mock for the status endpoint (Healthy = false)
            var statusResponse = "[{\"Name\": \"ConfigAPI REST Service\", \"Healthy\": false}]";
            _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, TEST_ENDPOINT + "/config/v1/status")
                                   .ReturnsResponse(statusResponse, "application/json");

            // Act
            var result = await _kepwareApiClient.TestConnectionAsync();

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task TestConnectionAsync_ShouldReturnFalse_OnHttpRequestException()
        {
            // Arrange: Simulated exception for the status endpoint
            _httpMessageHandlerMock.SetupAnyRequest()
                                   .ThrowsAsync(new HttpRequestException("Network error"));

            // Act
            var result = await _kepwareApiClient.TestConnectionAsync();

            // Assert
            Assert.False(result);
        }
    }
}
