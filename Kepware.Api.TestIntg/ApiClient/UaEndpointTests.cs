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
using Xunit.Extensions.Ordering;

namespace Kepware.Api.TestIntg.ApiClient
{
    public class UaEndpointTests : TestIntgApiClientBase
    {

        [SkippableFact]
        public async Task CreateOrUpdateUaEndpointAsync_ShouldCreateUaEndpoint_WhenItDoesNotExist()
        {
            // Skip the test if the product is not "Edge" productId
            Skip.If(_productInfo.ProductId != "013", "Test only applicable for Edge productIds");

            // Arrange
            var uaEndpoint = CreateTestUaEndpoint();

            // Act
            var result = await _kepwareApiClient.Admin.CreateOrUpdateUaEndpointAsync(uaEndpoint);

            // Assert
            result.ShouldBeTrue();

            // Clean up
            await _kepwareApiClient.Admin.DeleteUaEndpointAsync(uaEndpoint.Name);

        }

        [SkippableFact]
        public async Task CreateOrUpdateUaEndpointAsync_ShouldUpdateUaEndpoint_WhenItExists()
        {
            // Skip the test if the product is not "Edge" productId
            Skip.If(_productInfo.ProductId != "013", "Test only applicable for Edge productIds");

            // Arrange
            var uaEndpoint = CreateTestUaEndpoint();
            await _kepwareApiClient.GenericConfig.InsertItemAsync<UaEndpoint>(uaEndpoint);

            uaEndpoint = await _kepwareApiClient.GenericConfig.LoadEntityAsync<UaEndpoint>(uaEndpoint.Name);
            uaEndpoint.ShouldNotBeNull();
            uaEndpoint.Port = 4840;

            // Act
            var result = await _kepwareApiClient.Admin.CreateOrUpdateUaEndpointAsync(uaEndpoint);

            // Assert
            result.ShouldBeTrue();

            // Clean up
            await _kepwareApiClient.Admin.DeleteUaEndpointAsync(uaEndpoint.Name);
        }

        [SkippableFact]
        public async Task GetUaEndpointAsync_ShouldReturnUaEndpoint_WhenApiRespondsSuccessfully()
        {
            // Skip the test if the product is not "Edge" productId
            Skip.If(_productInfo.ProductId != "013", "Test only applicable for Edge productIds");

            // Arrange
            var uaEndpoint = CreateTestUaEndpoint();
            await _kepwareApiClient.GenericConfig.InsertItemAsync<UaEndpoint>(uaEndpoint);

            // Act
            var result = await _kepwareApiClient.Admin.GetUaEndpointAsync(uaEndpoint.Name);

            // Assert
            result.ShouldNotBeNull();
            result.Name.ShouldBe(uaEndpoint.Name);
            result.Port.ShouldBeOfType<int>();

            // Clean up
            await _kepwareApiClient.Admin.DeleteUaEndpointAsync(uaEndpoint.Name);

        }

        [SkippableFact]
        public async Task GetUaEndpointsAsync_ShouldReturnUaEndpointCollection_WhenApiRespondsSuccessfully()
        {
            // Skip the test if the product is not "Edge" productId
            Skip.If(_productInfo.ProductId != "013", "Test only applicable for Edge productIds");

            // Act
            var result = await _kepwareApiClient.Admin.GetUaEndpointListAsync();

            // Assert
            result.ShouldNotBeNull();
        }


        [SkippableFact]
        public async Task DeleteUaEndpointAsync_ShouldReturnTrue_WhenDeleteSuccessful()
        {
            // Skip the test if the product is not "Edge" productId
            Skip.If(_productInfo.ProductId != "013", "Test only applicable for Edge productIds");

            // Arrange
            var uaEndpoint = CreateTestUaEndpoint();
            await _kepwareApiClient.GenericConfig.InsertItemAsync<UaEndpoint>(uaEndpoint);

            // Act
            var result = await _kepwareApiClient.Admin.DeleteUaEndpointAsync(uaEndpoint.Name);

            // Assert
            result.ShouldBeTrue();
        }

        [SkippableFact]
        public async Task DeleteUaEndpointAsync_ShouldReturnFalse_WhenDeleteFails()
        {
            // Skip the test if the product is not "Edge" productId
            Skip.If(_productInfo.ProductId != "013", "Test only applicable for Edge productIds");

            // Arrange
            var uaEndpoint = CreateTestUaEndpoint();

            // Act
            var result = await _kepwareApiClient.Admin.DeleteUaEndpointAsync(uaEndpoint.Name);

            // Assert
            result.ShouldBeFalse();
        }

        private static UaEndpoint CreateTestUaEndpoint(string endpointName = "TestEndpoint")
        {
            return new UaEndpoint
            {
                Name = endpointName,
                Enabled = true,
                Adapter = "Default",
                Port = 49500,
                SecurityNone = false,
                SecurityBasic256Sha256 = UaEndpointSecurityMode.SignAndEncrypt
            };
        }
    }
}
