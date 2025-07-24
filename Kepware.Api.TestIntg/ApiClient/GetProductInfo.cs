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
    public class GetProductInfo : TestApiClientBase
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

        }

    }
}
