using Moq;
using Moq.Contrib.HttpClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kepware.Api.TestIntg.ApiClient
{
    public class TestConnection : TestIntgApiClientBase
    {
        [Fact]
        public async Task TestConnectionAsync_ShouldReturnTrue_WhenApiIsHealthy()
        {

            // Act
            var result = await _kepwareApiClient.TestConnectionAsync();

            // Assert
            Assert.True(result);
        }

    }
}
