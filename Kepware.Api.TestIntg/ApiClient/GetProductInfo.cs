using Kepware.Api.Model;
using Moq;
using Moq.Contrib.HttpClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Kepware.Api.TestIntg.ApiClient
{
    public class GetProductInfo : TestIntgApiClientBase
    {

        [Fact]
        public async Task GetProductInfoAsync_ShouldReturnProductInfo_WhenApiRespondsSuccessfully()
        {

            // Act
            var result = await _kepwareApiClient.GetProductInfoAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(_productInfo.ProductId, result.ProductId);
            Assert.Equal(_productInfo.ProductName, result.ProductName);
            Assert.Equal(_productInfo.ProductVersion, result.ProductVersion);
            Assert.Equal(_productInfo.ProductVersionMajor, result.ProductVersionMajor);
            Assert.Equal(_productInfo.ProductVersionMinor, result.ProductVersionMinor);
            Assert.Equal(_productInfo.ProductVersionBuild, result.ProductVersionBuild);
            Assert.Equal(_productInfo.ProductVersionPatch, result.ProductVersionPatch);

            // Also verify that the ProductInfo property on the client is updated
            Assert.NotNull(_kepwareApiClient.ProductInfo);
            Assert.Equal(_productInfo.ProductId, _kepwareApiClient.ProductInfo.ProductId);
            Assert.Equal(_productInfo.ProductName, _kepwareApiClient.ProductInfo.ProductName);
            Assert.Equal(_productInfo.ProductVersion, _kepwareApiClient.ProductInfo.ProductVersion);
            Assert.Equal(_productInfo.ProductVersionMajor, _kepwareApiClient.ProductInfo.ProductVersionMajor);
            Assert.Equal(_productInfo.ProductVersionMinor, _kepwareApiClient.ProductInfo.ProductVersionMinor);
            Assert.Equal(_productInfo.ProductVersionBuild, _kepwareApiClient.ProductInfo.ProductVersionBuild);
            Assert.Equal(_productInfo.ProductVersionPatch, _kepwareApiClient.ProductInfo.ProductVersionPatch);
        }

    }
}
