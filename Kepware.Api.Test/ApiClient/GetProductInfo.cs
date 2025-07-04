using Kepware.Api.Model;
using Moq;
using Moq.Contrib.HttpClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Kepware.Api.Test.ApiClient
{
    public class GetProductInfo : TestApiClientBase
    {

        [Fact]
        public async Task GetProductInfoAsync_ShouldReturnProductInfo_WhenApiRespondsSuccessfully()
        {
            // Arrange
            ConfigureConnectedClient();

            // Act
            var result = await _kepwareApiClient.GetProductInfoAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal("012", result.ProductId);
            Assert.Equal("KEPServerEX", result.ProductName);
            Assert.Equal("V6.17.240.0", result.ProductVersion);
            Assert.Equal(6, result.ProductVersionMajor);
            Assert.Equal(17, result.ProductVersionMinor);
            Assert.Equal(240, result.ProductVersionBuild);
            Assert.Equal(0, result.ProductVersionPatch);

        }

        #region GetProductInfoAsync - SupportsJsonProjectLoadService

        //TODO: Add more test cases for TKS versions as well. Different product name
        [Theory]
        [InlineData("KEPServerEX", "12", 6, 17, true)]  // Supports JSON Project Load Service (6.17+)
        [InlineData("KEPServerEX", "12", 6, 16, false)] // Does not support it (6.16)
        [InlineData("ThingWorxKepwareEdge", "13", 1, 10, true)] // Supports it (1.10+)
        [InlineData("ThingWorxKepwareEdge", "13", 1, 9, false)] // Does not support it (1.9)
        [InlineData("UnknownProduct", "99", 10, 0, false)] // Unknown product, should be false
        public async Task GetProductInfoAsync_ShouldReturnCorrect_SupportsJsonProjectLoadService(
            string productName, string productId, int majorVersion, int minorVersion, bool expectedResult)
        {
            // Arrange
            ConfigureConnectedClient(productName, productId, majorVersion, minorVersion);

            // Act
            var result = await _kepwareApiClient.GetProductInfoAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedResult, result.SupportsJsonProjectLoadService);
        }

        #endregion

        #region GetProductInfoAsync - ProductType

        [Theory]
        [InlineData("KEPServerEX", "12", ProductType.KEPServerEX)]
        [InlineData("ThingWorxKepwareEdge", "13", ProductType.ThingWorxKepwareEdge)]
        [InlineData("UnknownProduct", "99", ProductType.Unknown)]
        [InlineData("InvalidProduct", "abc", ProductType.Unknown)] // Invalid ID, should be Unknown
        public async Task GetProductInfoAsync_ShouldReturnCorrect_ProductType(
            string productName, string productId, ProductType expectedType)
        {
            // Arrange
            ConfigureConnectedClient(productName, productId);

            // Act
            var result = await _kepwareApiClient.GetProductInfoAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedType, result.ProductType);
        }

        #endregion

        #region GetProductInfoAsync - error handling
        [Fact]
        public async Task GetProductInfoAsync_ShouldReturnNull_WhenApiReturnsError()
        {
            // Arrange: Mock for erroneous API response (e.g., 500 Internal Server Error)
            _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, TEST_ENDPOINT + "/config/v1/about")
                                   .ReturnsResponse(HttpStatusCode.InternalServerError);

            // Act
            var result = await _kepwareApiClient.GetProductInfoAsync();

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetProductInfoAsync_ShouldReturnNull_WhenApiReturnsInvalidJson()
        {
            // Arrange: Mock for invalid JSON response
            var invalidJson = "{ invalid_json }";
            _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, TEST_ENDPOINT + "/config/v1/about")
                                   .ReturnsResponse(invalidJson, "application/json");

            // Act
            var result = await _kepwareApiClient.GetProductInfoAsync();

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetProductInfoAsync_ShouldReturnNull_OnHttpRequestException()
        {
            // Arrange: Simulated exception (e.g., network error)
            _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, TEST_ENDPOINT + "/config/v1/about")
                                   .ThrowsAsync(new HttpRequestException("Network error"));

            // Act
            var result = await _kepwareApiClient.GetProductInfoAsync();

            // Assert
            Assert.Null(result);
        }
        #endregion
    }
}
