using Kepware.Api.ClientHandler;
using Kepware.Api.Model;
using Kepware.Api.Serializer;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Contrib.HttpClient;
using Shouldly;
using System.Net;
using System.Text.Json;

namespace Kepware.Api.Test.ApiClient;

public class IotGatewayTests : TestApiClientBase
{
    #region ServerTagToItemName Tests

    [Theory]
    [InlineData("Channel1.Device1.Tag1", "Channel1_Device1_Tag1")]
    [InlineData("_System._Time", "System__Time")]
    [InlineData("_System._Date", "System__Date")]
    [InlineData("Channel1.Device1._InternalTag", "Channel1_Device1__InternalTag")]
    [InlineData("SimpleTag", "SimpleTag")]
    [InlineData("_LeadingUnderscore", "LeadingUnderscore")]
    public void ServerTagToItemName_ShouldConvertCorrectly(string serverTag, string expectedName)
    {
        var result = IotGatewayApiHandler.ServerTagToItemName(serverTag);
        result.ShouldBe(expectedName);
    }

    #endregion

    #region MQTT Client Agent Tests

    [Fact]
    public async Task GetOrCreateMqttClientAgent_WhenNotExists_ShouldCreateAgent()
    {
        // Arrange
        var getEndpoint = "/config/v1/project/_iot_gateway/mqtt_clients/TestAgent";
        var postEndpoint = "/config/v1/project/_iot_gateway/mqtt_clients";

        _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, $"{TEST_ENDPOINT}{getEndpoint}")
            .ReturnsResponse(HttpStatusCode.NotFound, "Not Found");

        _httpMessageHandlerMock.SetupRequest(HttpMethod.Post, $"{TEST_ENDPOINT}{postEndpoint}")
            .ReturnsResponse(HttpStatusCode.OK);

        // Act
        var result = await _kepwareApiClient.Project.IotGateway.GetOrCreateMqttClientAgentAsync("TestAgent");

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe("TestAgent");
        _httpMessageHandlerMock.VerifyRequest(HttpMethod.Post, $"{TEST_ENDPOINT}{postEndpoint}", Times.Once());
    }

    [Fact]
    public async Task GetOrCreateMqttClientAgent_WhenExists_ShouldReturnExistingAgent()
    {
        // Arrange
        var agentJson = """
            {
                "common.ALLTYPES_NAME": "TestAgent",
                "iot_gateway.AGENTTYPES_ENABLED": true,
                "iot_gateway.MQTT_CLIENT_URL": "tcp://localhost:1883"
            }
            """;
        var getEndpoint = "/config/v1/project/_iot_gateway/mqtt_clients/TestAgent";

        _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, $"{TEST_ENDPOINT}{getEndpoint}")
            .ReturnsResponse(HttpStatusCode.OK, agentJson, "application/json");

        // Act
        var result = await _kepwareApiClient.Project.IotGateway.GetOrCreateMqttClientAgentAsync("TestAgent");

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe("TestAgent");
        result.Url.ShouldBe("tcp://localhost:1883");
        // Should NOT have called POST
        _httpMessageHandlerMock.VerifyRequest(HttpMethod.Post, $"{TEST_ENDPOINT}/config/v1/project/_iot_gateway/mqtt_clients", Times.Never());
    }

    [Fact]
    public async Task GetOrCreateMqttClientAgent_WhenCreateFails_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var getEndpoint = "/config/v1/project/_iot_gateway/mqtt_clients/TestAgent";
        var postEndpoint = "/config/v1/project/_iot_gateway/mqtt_clients";

        _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, $"{TEST_ENDPOINT}{getEndpoint}")
            .ReturnsResponse(HttpStatusCode.NotFound, "Not Found");

        _httpMessageHandlerMock.SetupRequest(HttpMethod.Post, $"{TEST_ENDPOINT}{postEndpoint}")
            .ReturnsResponse(HttpStatusCode.InternalServerError, "Server Error");

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(async () =>
            await _kepwareApiClient.Project.IotGateway.GetOrCreateMqttClientAgentAsync("TestAgent"));
    }

    [Fact]
    public async Task GetOrCreateMqttClientAgent_WithEmptyName_ShouldThrowArgumentException()
    {
        await Should.ThrowAsync<ArgumentException>(async () =>
            await _kepwareApiClient.Project.IotGateway.GetOrCreateMqttClientAgentAsync(""));
    }


    [Fact]
    public async Task CreateMqttClientAgent_WhenSuccessful_ShouldReturnAgent()
    {
        // Arrange
        var endpoint = "/config/v1/project/_iot_gateway/mqtt_clients";

        _httpMessageHandlerMock.SetupRequest(HttpMethod.Post, $"{TEST_ENDPOINT}{endpoint}")
            .ReturnsResponse(HttpStatusCode.OK);

        // Act
        var result = await _kepwareApiClient.Project.IotGateway.CreateMqttClientAgentAsync("TestAgent");

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe("TestAgent");
        _httpMessageHandlerMock.VerifyRequest(HttpMethod.Post, $"{TEST_ENDPOINT}{endpoint}", Times.Once());
    }

    [Fact]
    public async Task CreateMqttClientAgent_WithProperties_ShouldSetProperties()
    {
        // Arrange
        var endpoint = "/config/v1/project/_iot_gateway/mqtt_clients";

        _httpMessageHandlerMock.SetupRequest(HttpMethod.Post, $"{TEST_ENDPOINT}{endpoint}")
            .ReturnsResponse(HttpStatusCode.OK);

        var properties = new Dictionary<string, object>
        {
            { Properties.MqttClientAgent.Url, "tcp://localhost:1883" },
            { Properties.MqttClientAgent.Topic, "test/topic" }
        };

        // Act
        var result = await _kepwareApiClient.Project.IotGateway.CreateMqttClientAgentAsync("TestAgent", properties);

        // Assert
        result.ShouldNotBeNull();
        result.Url.ShouldBe("tcp://localhost:1883");
        result.Topic.ShouldBe("test/topic");
    }

    [Fact]
    public async Task CreateMqttClientAgent_WithHttpError_ShouldReturnNull()
    {
        // Arrange
        var endpoint = "/config/v1/project/_iot_gateway/mqtt_clients";

        _httpMessageHandlerMock.SetupRequest(HttpMethod.Post, $"{TEST_ENDPOINT}{endpoint}")
            .ReturnsResponse(HttpStatusCode.InternalServerError, "Server Error");

        // Act
        var result = await _kepwareApiClient.Project.IotGateway.CreateMqttClientAgentAsync("TestAgent");

        // Assert
        result.ShouldBeNull();
        _loggerMockGeneric.Verify(logger =>
            logger.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
            Times.Once);
    }

    [Fact]
    public async Task CreateMqttClientAgent_WithEmptyName_ShouldThrowArgumentException()
    {
        await Should.ThrowAsync<ArgumentException>(async () =>
            await _kepwareApiClient.Project.IotGateway.CreateMqttClientAgentAsync(""));
    }

    [Fact]
    public async Task GetMqttClientAgent_WhenExists_ShouldReturnAgent()
    {
        // Arrange
        var agentJson = """
            {
                "common.ALLTYPES_NAME": "TestAgent",
                "iot_gateway.AGENTTYPES_ENABLED": true,
                "iot_gateway.MQTT_CLIENT_URL": "tcp://localhost:1883"
            }
            """;
        var endpoint = "/config/v1/project/_iot_gateway/mqtt_clients/TestAgent";

        _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, $"{TEST_ENDPOINT}{endpoint}")
            .ReturnsResponse(HttpStatusCode.OK, agentJson, "application/json");

        // Act
        var result = await _kepwareApiClient.Project.IotGateway.GetMqttClientAgentAsync("TestAgent");

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe("TestAgent");
        result.Enabled.ShouldBe(true);
        result.Url.ShouldBe("tcp://localhost:1883");
    }

    [Fact]
    public async Task GetMqttClientAgent_WhenNotFound_ShouldReturnNull()
    {
        // Arrange
        var endpoint = "/config/v1/project/_iot_gateway/mqtt_clients/NonExistent";

        _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, $"{TEST_ENDPOINT}{endpoint}")
            .ReturnsResponse(HttpStatusCode.NotFound, "Not Found");

        // Act
        var result = await _kepwareApiClient.Project.IotGateway.GetMqttClientAgentAsync("NonExistent");

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public async Task DeleteMqttClientAgent_ByEntity_WhenSuccessful_ShouldReturnTrue()
    {
        // Arrange
        var agent = new MqttClientAgent("TestAgent");
        var endpoint = "/config/v1/project/_iot_gateway/mqtt_clients/TestAgent";

        _httpMessageHandlerMock.SetupRequest(HttpMethod.Delete, $"{TEST_ENDPOINT}{endpoint}")
            .ReturnsResponse(HttpStatusCode.OK);

        // Act
        var result = await _kepwareApiClient.Project.IotGateway.DeleteMqttClientAgentAsync(agent);

        // Assert
        result.ShouldBeTrue();
        _httpMessageHandlerMock.VerifyRequest(HttpMethod.Delete, $"{TEST_ENDPOINT}{endpoint}", Times.Once());
    }

    [Fact]
    public async Task DeleteMqttClientAgent_ByName_WhenSuccessful_ShouldReturnTrue()
    {
        // Arrange
        var endpoint = "/config/v1/project/_iot_gateway/mqtt_clients/TestAgent";

        _httpMessageHandlerMock.SetupRequest(HttpMethod.Delete, $"{TEST_ENDPOINT}{endpoint}")
            .ReturnsResponse(HttpStatusCode.OK);

        // Act
        var result = await _kepwareApiClient.Project.IotGateway.DeleteMqttClientAgentAsync("TestAgent");

        // Assert
        result.ShouldBeTrue();
        _httpMessageHandlerMock.VerifyRequest(HttpMethod.Delete, $"{TEST_ENDPOINT}{endpoint}", Times.Once());
    }

    [Fact]
    public async Task DeleteMqttClientAgent_WithHttpError_ShouldReturnFalse()
    {
        // Arrange
        var endpoint = "/config/v1/project/_iot_gateway/mqtt_clients/TestAgent";

        _httpMessageHandlerMock.SetupRequest(HttpMethod.Delete, $"{TEST_ENDPOINT}{endpoint}")
            .ReturnsResponse(HttpStatusCode.InternalServerError, "Server Error");

        // Act
        var result = await _kepwareApiClient.Project.IotGateway.DeleteMqttClientAgentAsync("TestAgent");

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public async Task DeleteMqttClientAgent_WithConnectionError_ShouldReturnFalse()
    {
        // Arrange
        var endpoint = "/config/v1/project/_iot_gateway/mqtt_clients/TestAgent";

        _httpMessageHandlerMock.SetupRequest(HttpMethod.Delete, $"{TEST_ENDPOINT}{endpoint}")
            .Throws(new HttpRequestException("Connection error"));

        // Act
        var result = await _kepwareApiClient.Project.IotGateway.DeleteMqttClientAgentAsync("TestAgent");

        // Assert
        result.ShouldBeFalse();
    }

    #endregion

    #region REST Client Agent Tests

    [Fact]
    public async Task GetOrCreateRestClientAgent_WhenNotExists_ShouldCreateAgent()
    {
        // Arrange
        var getEndpoint = "/config/v1/project/_iot_gateway/rest_clients/TestRestClient";
        var postEndpoint = "/config/v1/project/_iot_gateway/rest_clients";

        _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, $"{TEST_ENDPOINT}{getEndpoint}")
            .ReturnsResponse(HttpStatusCode.NotFound, "Not Found");

        _httpMessageHandlerMock.SetupRequest(HttpMethod.Post, $"{TEST_ENDPOINT}{postEndpoint}")
            .ReturnsResponse(HttpStatusCode.OK);

        // Act
        var result = await _kepwareApiClient.Project.IotGateway.GetOrCreateRestClientAgentAsync("TestRestClient");

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe("TestRestClient");
    }

    [Fact]
    public async Task GetOrCreateRestClientAgent_WhenExists_ShouldReturnAgent()
    {
        // Arrange
        var agentJson = """
            {
                "common.ALLTYPES_NAME": "TestRestClient",
                "iot_gateway.AGENTTYPES_ENABLED": true,
                "iot_gateway.REST_CLIENT_URL": "https://api.example.com"
            }
            """;
        var endpoint = "/config/v1/project/_iot_gateway/rest_clients/TestRestClient";

        _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, $"{TEST_ENDPOINT}{endpoint}")
            .ReturnsResponse(HttpStatusCode.OK, agentJson, "application/json");

        // Act
        var result = await _kepwareApiClient.Project.IotGateway.GetOrCreateRestClientAgentAsync("TestRestClient");

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe("TestRestClient");
        result.Url.ShouldBe("https://api.example.com");
    }

    [Fact]
    public async Task GetOrCreateRestClientAgent_WhenCreateFails_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var getEndpoint = "/config/v1/project/_iot_gateway/rest_clients/TestRestClient";
        var postEndpoint = "/config/v1/project/_iot_gateway/rest_clients";

        _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, $"{TEST_ENDPOINT}{getEndpoint}")
            .ReturnsResponse(HttpStatusCode.NotFound, "Not Found");

        _httpMessageHandlerMock.SetupRequest(HttpMethod.Post, $"{TEST_ENDPOINT}{postEndpoint}")
            .ReturnsResponse(HttpStatusCode.InternalServerError, "Server Error");

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(async () =>
            await _kepwareApiClient.Project.IotGateway.GetOrCreateRestClientAgentAsync("TestRestClient"));
    }

    [Fact]
    public async Task GetOrCreateRestClientAgent_WithEmptyName_ShouldThrowArgumentException()
    {
        await Should.ThrowAsync<ArgumentException>(async () =>
            await _kepwareApiClient.Project.IotGateway.GetOrCreateRestClientAgentAsync(""));
    }

    [Fact]
    public async Task CreateRestClientAgent_WhenSuccessful_ShouldReturnAgent()
    {
        // Arrange
        var endpoint = "/config/v1/project/_iot_gateway/rest_clients";

        _httpMessageHandlerMock.SetupRequest(HttpMethod.Post, $"{TEST_ENDPOINT}{endpoint}")
            .ReturnsResponse(HttpStatusCode.OK);

        // Act
        var result = await _kepwareApiClient.Project.IotGateway.CreateRestClientAgentAsync("TestRestClient");

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe("TestRestClient");
    }

    [Fact]
    public async Task CreateRestClientAgent_WithProperties_ShouldSetProperties()
    {
        // Arrange
        var endpoint = "/config/v1/project/_iot_gateway/rest_clients";

        _httpMessageHandlerMock.SetupRequest(HttpMethod.Post, $"{TEST_ENDPOINT}{endpoint}")
            .ReturnsResponse(HttpStatusCode.OK);

        var properties = new Dictionary<string, object>
        {
            { Properties.RestClientAgent.Url, "https://api.example.com" }
        };

        // Act
        var result = await _kepwareApiClient.Project.IotGateway.CreateRestClientAgentAsync("TestRestClient", properties);

        // Assert
        result.ShouldNotBeNull();
        result.Url.ShouldBe("https://api.example.com");
    }

    [Fact]
    public async Task CreateRestClientAgent_WithHttpError_ShouldReturnNull()
    {
        // Arrange
        var endpoint = "/config/v1/project/_iot_gateway/rest_clients";

        _httpMessageHandlerMock.SetupRequest(HttpMethod.Post, $"{TEST_ENDPOINT}{endpoint}")
            .ReturnsResponse(HttpStatusCode.InternalServerError, "Server Error");

        // Act
        var result = await _kepwareApiClient.Project.IotGateway.CreateRestClientAgentAsync("TestRestClient");

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public async Task CreateRestClientAgent_WithEmptyName_ShouldThrowArgumentException()
    {
        await Should.ThrowAsync<ArgumentException>(async () =>
            await _kepwareApiClient.Project.IotGateway.CreateRestClientAgentAsync(""));
    }

    [Fact]
    public async Task GetRestClientAgent_WhenExists_ShouldReturnAgent()
    {
        // Arrange
        var agentJson = """
            {
                "common.ALLTYPES_NAME": "TestRestClient",
                "iot_gateway.AGENTTYPES_ENABLED": true,
                "iot_gateway.REST_CLIENT_URL": "https://api.example.com"
            }
            """;
        var endpoint = "/config/v1/project/_iot_gateway/rest_clients/TestRestClient";

        _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, $"{TEST_ENDPOINT}{endpoint}")
            .ReturnsResponse(HttpStatusCode.OK, agentJson, "application/json");

        // Act
        var result = await _kepwareApiClient.Project.IotGateway.GetRestClientAgentAsync("TestRestClient");

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe("TestRestClient");
        result.Url.ShouldBe("https://api.example.com");
    }

    [Fact]
    public async Task GetRestClientAgent_WhenNotFound_ShouldReturnNull()
    {
        // Arrange
        var endpoint = "/config/v1/project/_iot_gateway/rest_clients/NonExistent";

        _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, $"{TEST_ENDPOINT}{endpoint}")
            .ReturnsResponse(HttpStatusCode.NotFound, "Not Found");

        // Act
        var result = await _kepwareApiClient.Project.IotGateway.GetRestClientAgentAsync("NonExistent");

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public async Task DeleteRestClientAgent_ByEntity_WhenSuccessful_ShouldReturnTrue()
    {
        // Arrange
        var agent = new RestClientAgent("TestRestClient");
        var endpoint = "/config/v1/project/_iot_gateway/rest_clients/TestRestClient";

        _httpMessageHandlerMock.SetupRequest(HttpMethod.Delete, $"{TEST_ENDPOINT}{endpoint}")
            .ReturnsResponse(HttpStatusCode.OK);

        // Act
        var result = await _kepwareApiClient.Project.IotGateway.DeleteRestClientAgentAsync(agent);

        // Assert
        result.ShouldBeTrue();
        _httpMessageHandlerMock.VerifyRequest(HttpMethod.Delete, $"{TEST_ENDPOINT}{endpoint}", Times.Once());
    }

    [Fact]
    public async Task DeleteRestClientAgent_ByName_WhenSuccessful_ShouldReturnTrue()
    {
        // Arrange
        var endpoint = "/config/v1/project/_iot_gateway/rest_clients/TestRestClient";

        _httpMessageHandlerMock.SetupRequest(HttpMethod.Delete, $"{TEST_ENDPOINT}{endpoint}")
            .ReturnsResponse(HttpStatusCode.OK);

        // Act
        var result = await _kepwareApiClient.Project.IotGateway.DeleteRestClientAgentAsync("TestRestClient");

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public async Task DeleteRestClientAgent_WithHttpError_ShouldReturnFalse()
    {
        // Arrange
        var endpoint = "/config/v1/project/_iot_gateway/rest_clients/TestRestClient";

        _httpMessageHandlerMock.SetupRequest(HttpMethod.Delete, $"{TEST_ENDPOINT}{endpoint}")
            .ReturnsResponse(HttpStatusCode.InternalServerError, "Server Error");

        // Act
        var result = await _kepwareApiClient.Project.IotGateway.DeleteRestClientAgentAsync("TestRestClient");

        // Assert
        result.ShouldBeFalse();
    }

    #endregion

    #region REST Server Agent Tests

    [Fact]
    public async Task GetOrCreateRestServerAgent_WhenNotExists_ShouldCreateAgent()
    {
        // Arrange
        var getEndpoint = "/config/v1/project/_iot_gateway/rest_servers/TestRestServer";
        var postEndpoint = "/config/v1/project/_iot_gateway/rest_servers";

        _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, $"{TEST_ENDPOINT}{getEndpoint}")
            .ReturnsResponse(HttpStatusCode.NotFound, "Not Found");

        _httpMessageHandlerMock.SetupRequest(HttpMethod.Post, $"{TEST_ENDPOINT}{postEndpoint}")
            .ReturnsResponse(HttpStatusCode.OK);

        // Act
        var result = await _kepwareApiClient.Project.IotGateway.GetOrCreateRestServerAgentAsync("TestRestServer");

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe("TestRestServer");
    }

    [Fact]
    public async Task GetOrCreateRestServerAgent_WhenExists_ShouldReturnAgent()
    {
        // Arrange
        var agentJson = """
            {
                "common.ALLTYPES_NAME": "TestRestServer",
                "iot_gateway.AGENTTYPES_ENABLED": true,
                "iot_gateway.REST_SERVER_PORT_NUMBER": 39320
            }
            """;
        var endpoint = "/config/v1/project/_iot_gateway/rest_servers/TestRestServer";

        _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, $"{TEST_ENDPOINT}{endpoint}")
            .ReturnsResponse(HttpStatusCode.OK, agentJson, "application/json");

        // Act
        var result = await _kepwareApiClient.Project.IotGateway.GetOrCreateRestServerAgentAsync("TestRestServer");

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe("TestRestServer");
        result.PortNumber.ShouldBe(39320);
    }

    [Fact]
    public async Task GetOrCreateRestServerAgent_WhenCreateFails_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var getEndpoint = "/config/v1/project/_iot_gateway/rest_servers/TestRestServer";
        var postEndpoint = "/config/v1/project/_iot_gateway/rest_servers";

        _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, $"{TEST_ENDPOINT}{getEndpoint}")
            .ReturnsResponse(HttpStatusCode.NotFound, "Not Found");

        _httpMessageHandlerMock.SetupRequest(HttpMethod.Post, $"{TEST_ENDPOINT}{postEndpoint}")
            .ReturnsResponse(HttpStatusCode.InternalServerError, "Server Error");

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(async () =>
            await _kepwareApiClient.Project.IotGateway.GetOrCreateRestServerAgentAsync("TestRestServer"));
    }

    [Fact]
    public async Task GetOrCreateRestServerAgent_WithEmptyName_ShouldThrowArgumentException()
    {
        await Should.ThrowAsync<ArgumentException>(async () =>
            await _kepwareApiClient.Project.IotGateway.GetOrCreateRestServerAgentAsync(""));
    }

    [Fact]
    public async Task CreateRestServerAgent_WhenSuccessful_ShouldReturnAgent()
    {
        // Arrange
        var endpoint = "/config/v1/project/_iot_gateway/rest_servers";

        _httpMessageHandlerMock.SetupRequest(HttpMethod.Post, $"{TEST_ENDPOINT}{endpoint}")
            .ReturnsResponse(HttpStatusCode.OK);

        // Act
        var result = await _kepwareApiClient.Project.IotGateway.CreateRestServerAgentAsync("TestRestServer");

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe("TestRestServer");
    }

    [Fact]
    public async Task CreateRestServerAgent_WithProperties_ShouldSetProperties()
    {
        // Arrange
        var endpoint = "/config/v1/project/_iot_gateway/rest_servers";

        _httpMessageHandlerMock.SetupRequest(HttpMethod.Post, $"{TEST_ENDPOINT}{endpoint}")
            .ReturnsResponse(HttpStatusCode.OK);

        var properties = new Dictionary<string, object>
        {
            { Properties.RestServerAgent.PortNumber, 39320 }
        };

        // Act
        var result = await _kepwareApiClient.Project.IotGateway.CreateRestServerAgentAsync("TestRestServer", properties);

        // Assert
        result.ShouldNotBeNull();
        result.PortNumber.ShouldBe(39320);
    }

    [Fact]
    public async Task CreateRestServerAgent_WithHttpError_ShouldReturnNull()
    {
        // Arrange
        var endpoint = "/config/v1/project/_iot_gateway/rest_servers";

        _httpMessageHandlerMock.SetupRequest(HttpMethod.Post, $"{TEST_ENDPOINT}{endpoint}")
            .ReturnsResponse(HttpStatusCode.InternalServerError, "Server Error");

        // Act
        var result = await _kepwareApiClient.Project.IotGateway.CreateRestServerAgentAsync("TestRestServer");

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public async Task CreateRestServerAgent_WithEmptyName_ShouldThrowArgumentException()
    {
        await Should.ThrowAsync<ArgumentException>(async () =>
            await _kepwareApiClient.Project.IotGateway.CreateRestServerAgentAsync(""));
    }

    [Fact]
    public async Task GetRestServerAgent_WhenExists_ShouldReturnAgent()
    {
        // Arrange
        var agentJson = """
            {
                "common.ALLTYPES_NAME": "TestRestServer",
                "iot_gateway.AGENTTYPES_ENABLED": true,
                "iot_gateway.REST_SERVER_PORT_NUMBER": 39320
            }
            """;
        var endpoint = "/config/v1/project/_iot_gateway/rest_servers/TestRestServer";

        _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, $"{TEST_ENDPOINT}{endpoint}")
            .ReturnsResponse(HttpStatusCode.OK, agentJson, "application/json");

        // Act
        var result = await _kepwareApiClient.Project.IotGateway.GetRestServerAgentAsync("TestRestServer");

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe("TestRestServer");
        result.PortNumber.ShouldBe(39320);
    }

    [Fact]
    public async Task GetRestServerAgent_WhenNotFound_ShouldReturnNull()
    {
        // Arrange
        var endpoint = "/config/v1/project/_iot_gateway/rest_servers/NonExistent";

        _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, $"{TEST_ENDPOINT}{endpoint}")
            .ReturnsResponse(HttpStatusCode.NotFound, "Not Found");

        // Act
        var result = await _kepwareApiClient.Project.IotGateway.GetRestServerAgentAsync("NonExistent");

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public async Task DeleteRestServerAgent_ByEntity_WhenSuccessful_ShouldReturnTrue()
    {
        // Arrange
        var agent = new RestServerAgent("TestRestServer");
        var endpoint = "/config/v1/project/_iot_gateway/rest_servers/TestRestServer";

        _httpMessageHandlerMock.SetupRequest(HttpMethod.Delete, $"{TEST_ENDPOINT}{endpoint}")
            .ReturnsResponse(HttpStatusCode.OK);

        // Act
        var result = await _kepwareApiClient.Project.IotGateway.DeleteRestServerAgentAsync(agent);

        // Assert
        result.ShouldBeTrue();
        _httpMessageHandlerMock.VerifyRequest(HttpMethod.Delete, $"{TEST_ENDPOINT}{endpoint}", Times.Once());
    }

    [Fact]
    public async Task DeleteRestServerAgent_ByName_WhenSuccessful_ShouldReturnTrue()
    {
        // Arrange
        var endpoint = "/config/v1/project/_iot_gateway/rest_servers/TestRestServer";

        _httpMessageHandlerMock.SetupRequest(HttpMethod.Delete, $"{TEST_ENDPOINT}{endpoint}")
            .ReturnsResponse(HttpStatusCode.OK);

        // Act
        var result = await _kepwareApiClient.Project.IotGateway.DeleteRestServerAgentAsync("TestRestServer");

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public async Task DeleteRestServerAgent_WithHttpError_ShouldReturnFalse()
    {
        // Arrange
        var endpoint = "/config/v1/project/_iot_gateway/rest_servers/TestRestServer";

        _httpMessageHandlerMock.SetupRequest(HttpMethod.Delete, $"{TEST_ENDPOINT}{endpoint}")
            .ReturnsResponse(HttpStatusCode.InternalServerError, "Server Error");

        // Act
        var result = await _kepwareApiClient.Project.IotGateway.DeleteRestServerAgentAsync("TestRestServer");

        // Assert
        result.ShouldBeFalse();
    }

    #endregion

    #region IoT Item Tests

        [Fact]
    public async Task GetOrCreateIotItem_WhenNotExists_ShouldCreateWithDerivedName()
    {
        // Arrange - server tag "Channel1.Device1.Tag1" should query with name "Channel1_Device1_Tag1"
        var parentAgent = new MqttClientAgent("ParentAgent");
        var getEndpoint = "/config/v1/project/_iot_gateway/mqtt_clients/ParentAgent/iot_items/Channel1_Device1_Tag1";
        var postEndpoint = "/config/v1/project/_iot_gateway/mqtt_clients/ParentAgent/iot_items";

        _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, $"{TEST_ENDPOINT}{getEndpoint}")
            .ReturnsResponse(HttpStatusCode.NotFound, "Not Found");

        _httpMessageHandlerMock.SetupRequest(HttpMethod.Post, $"{TEST_ENDPOINT}{postEndpoint}")
            .ReturnsResponse(HttpStatusCode.OK);

        // Act
        var result = await _kepwareApiClient.Project.IotGateway.GetOrCreateIotItemAsync("Channel1.Device1.Tag1", parentAgent);

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe("Channel1_Device1_Tag1");
        result.ServerTag.ShouldBe("Channel1.Device1.Tag1");
    }

    [Fact]
    public async Task GetOrCreateIotItem_WhenExists_ShouldReturnExistingItem()
    {
        // Arrange
        var parentAgent = new MqttClientAgent("ParentAgent");
        var itemJson = """
            {
                "common.ALLTYPES_NAME": "Channel1_Device1_Tag1",
                "iot_gateway.IOT_ITEM_SERVER_TAG": "Channel1.Device1.Tag1",
                "iot_gateway.IOT_ITEM_ENABLED": true
            }
            """;
        var getEndpoint = "/config/v1/project/_iot_gateway/mqtt_clients/ParentAgent/iot_items/Channel1_Device1_Tag1";

        _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, $"{TEST_ENDPOINT}{getEndpoint}")
            .ReturnsResponse(HttpStatusCode.OK, itemJson, "application/json");

        // Act
        var result = await _kepwareApiClient.Project.IotGateway.GetOrCreateIotItemAsync("Channel1.Device1.Tag1", parentAgent);

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe("Channel1_Device1_Tag1");
        // Should NOT have called POST
        _httpMessageHandlerMock.VerifyRequest(HttpMethod.Post, $"{TEST_ENDPOINT}/config/v1/project/_iot_gateway/mqtt_clients/ParentAgent/iot_items", Times.Never());
    }

    [Fact]
    public async Task GetOrCreateIotItem_WhenCreateFails_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var parentAgent = new MqttClientAgent("ParentAgent");
        var getEndpoint = "/config/v1/project/_iot_gateway/mqtt_clients/ParentAgent/iot_items/Channel1_Device1_Tag1";
        var postEndpoint = "/config/v1/project/_iot_gateway/mqtt_clients/ParentAgent/iot_items";

        _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, $"{TEST_ENDPOINT}{getEndpoint}")
            .ReturnsResponse(HttpStatusCode.NotFound, "Not Found");

        _httpMessageHandlerMock.SetupRequest(HttpMethod.Post, $"{TEST_ENDPOINT}{postEndpoint}")
            .ReturnsResponse(HttpStatusCode.InternalServerError, "Server Error");

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(async () =>
            await _kepwareApiClient.Project.IotGateway.GetOrCreateIotItemAsync("Channel1.Device1.Tag1", parentAgent));
    }

    [Fact]
    public async Task CreateIotItem_ShouldDeriveNameFromServerTag()
    {
        // Arrange - "Channel1.Device1.Tag1" should produce item named "Channel1_Device1_Tag1"
        var parentAgent = new MqttClientAgent("ParentAgent");
        var endpoint = "/config/v1/project/_iot_gateway/mqtt_clients/ParentAgent/iot_items";

        _httpMessageHandlerMock.SetupRequest(HttpMethod.Post, $"{TEST_ENDPOINT}{endpoint}")
            .ReturnsResponse(HttpStatusCode.OK);

        // Act
        var result = await _kepwareApiClient.Project.IotGateway.CreateIotItemAsync("Channel1.Device1.Tag1", parentAgent);

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe("Channel1_Device1_Tag1");
        result.ServerTag.ShouldBe("Channel1.Device1.Tag1");
    }

    [Fact]
    public async Task CreateIotItem_WithSystemTag_ShouldStripLeadingUnderscore()
    {
        // Arrange - "_System._Time" should produce item named "System__Time"
        var parentAgent = new MqttClientAgent("ParentAgent");
        var endpoint = "/config/v1/project/_iot_gateway/mqtt_clients/ParentAgent/iot_items";

        _httpMessageHandlerMock.SetupRequest(HttpMethod.Post, $"{TEST_ENDPOINT}{endpoint}")
            .ReturnsResponse(HttpStatusCode.OK);

        // Act
        var result = await _kepwareApiClient.Project.IotGateway.CreateIotItemAsync("_System._Time", parentAgent);

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe("System__Time");
        result.ServerTag.ShouldBe("_System._Time");
    }

    [Fact]
    public async Task CreateIotItem_WithHttpError_ShouldReturnNull()
    {
        // Arrange
        var parentAgent = new MqttClientAgent("ParentAgent");
        var endpoint = "/config/v1/project/_iot_gateway/mqtt_clients/ParentAgent/iot_items";

        _httpMessageHandlerMock.SetupRequest(HttpMethod.Post, $"{TEST_ENDPOINT}{endpoint}")
            .ReturnsResponse(HttpStatusCode.InternalServerError, "Server Error");

        // Act
        var result = await _kepwareApiClient.Project.IotGateway.CreateIotItemAsync("Channel1.Device1.Tag1", parentAgent);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public async Task CreateIotItem_WithEmptyServerTag_ShouldThrowArgumentException()
    {
        var parentAgent = new MqttClientAgent("ParentAgent");

        await Should.ThrowAsync<ArgumentException>(async () =>
            await _kepwareApiClient.Project.IotGateway.CreateIotItemAsync("", parentAgent));
    }

    [Fact]
    public async Task CreateIotItem_WithNullParent_ShouldThrowArgumentNullException()
    {
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await _kepwareApiClient.Project.IotGateway.CreateIotItemAsync("Channel1.Device1.Tag1", null!));
    }

    [Fact]
    public async Task GetIotItem_ShouldTranslateServerTagToItemName()
    {
        // Arrange - querying by server tag "Channel1.Device1.Tag1" should GET using name "Channel1_Device1_Tag1"
        var parentAgent = new MqttClientAgent("ParentAgent");
        var itemJson = """
            {
                "common.ALLTYPES_NAME": "Channel1_Device1_Tag1",
                "iot_gateway.IOT_ITEM_SERVER_TAG": "Channel1.Device1.Tag1",
                "iot_gateway.IOT_ITEM_ENABLED": true
            }
            """;
        var endpoint = "/config/v1/project/_iot_gateway/mqtt_clients/ParentAgent/iot_items/Channel1_Device1_Tag1";

        _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, $"{TEST_ENDPOINT}{endpoint}")
            .ReturnsResponse(HttpStatusCode.OK, itemJson, "application/json");

        // Act
        var result = await _kepwareApiClient.Project.IotGateway.GetIotItemAsync("Channel1.Device1.Tag1", parentAgent);

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe("Channel1_Device1_Tag1");
        result.ServerTag.ShouldBe("Channel1.Device1.Tag1");
        result.Enabled.ShouldBe(true);
    }

    [Fact]
    public async Task DeleteIotItem_ByEntity_WhenSuccessful_ShouldReturnTrue()
    {
        // Arrange
        var parentAgent = new MqttClientAgent("ParentAgent");
        var item = new IotItem("Channel1_Device1_Tag1") { Owner = parentAgent };
        var endpoint = "/config/v1/project/_iot_gateway/mqtt_clients/ParentAgent/iot_items/Channel1_Device1_Tag1";

        _httpMessageHandlerMock.SetupRequest(HttpMethod.Delete, $"{TEST_ENDPOINT}{endpoint}")
            .ReturnsResponse(HttpStatusCode.OK);

        // Act
        var result = await _kepwareApiClient.Project.IotGateway.DeleteIotItemAsync(item);

        // Assert
        result.ShouldBeTrue();
        _httpMessageHandlerMock.VerifyRequest(HttpMethod.Delete, $"{TEST_ENDPOINT}{endpoint}", Times.Once());
    }

    [Fact]
    public async Task DeleteIotItem_ByServerTag_ShouldTranslateToItemName()
    {
        // Arrange - deleting by server tag "Channel1.Device1.Tag1" should DELETE using name "Channel1_Device1_Tag1"
        var parentAgent = new MqttClientAgent("ParentAgent");
        var endpoint = "/config/v1/project/_iot_gateway/mqtt_clients/ParentAgent/iot_items/Channel1_Device1_Tag1";

        _httpMessageHandlerMock.SetupRequest(HttpMethod.Delete, $"{TEST_ENDPOINT}{endpoint}")
            .ReturnsResponse(HttpStatusCode.OK);

        // Act
        var result = await _kepwareApiClient.Project.IotGateway.DeleteIotItemAsync("Channel1.Device1.Tag1", parentAgent);

        // Assert
        result.ShouldBeTrue();
        _httpMessageHandlerMock.VerifyRequest(HttpMethod.Delete, $"{TEST_ENDPOINT}{endpoint}", Times.Once());
    }

    [Fact]
    public async Task DeleteIotItem_WithHttpError_ShouldReturnFalse()
    {
        // Arrange
        var parentAgent = new MqttClientAgent("ParentAgent");
        var item = new IotItem("Channel1_Device1_Tag1") { Owner = parentAgent };
        var endpoint = "/config/v1/project/_iot_gateway/mqtt_clients/ParentAgent/iot_items/Channel1_Device1_Tag1";

        _httpMessageHandlerMock.SetupRequest(HttpMethod.Delete, $"{TEST_ENDPOINT}{endpoint}")
            .ReturnsResponse(HttpStatusCode.InternalServerError, "Server Error");

        // Act
        var result = await _kepwareApiClient.Project.IotGateway.DeleteIotItemAsync(item);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public async Task DeleteIotItem_WithConnectionError_ShouldReturnFalse()
    {
        // Arrange
        var parentAgent = new MqttClientAgent("ParentAgent");
        var item = new IotItem("Channel1_Device1_Tag1") { Owner = parentAgent };
        var endpoint = "/config/v1/project/_iot_gateway/mqtt_clients/ParentAgent/iot_items/Channel1_Device1_Tag1";

        _httpMessageHandlerMock.SetupRequest(HttpMethod.Delete, $"{TEST_ENDPOINT}{endpoint}")
            .Throws(new HttpRequestException("Connection error"));

        // Act
        var result = await _kepwareApiClient.Project.IotGateway.DeleteIotItemAsync(item);

        // Assert
        result.ShouldBeFalse();
    }

    #endregion

    #region Argument Validation Tests

    [Fact]
    public async Task GetMqttClientAgent_WithEmptyName_ShouldThrowArgumentException()
    {
        await Should.ThrowAsync<ArgumentException>(async () =>
            await _kepwareApiClient.Project.IotGateway.GetMqttClientAgentAsync(""));
    }

    [Fact]
    public async Task GetRestClientAgent_WithEmptyName_ShouldThrowArgumentException()
    {
        await Should.ThrowAsync<ArgumentException>(async () =>
            await _kepwareApiClient.Project.IotGateway.GetRestClientAgentAsync(""));
    }

    [Fact]
    public async Task GetRestServerAgent_WithEmptyName_ShouldThrowArgumentException()
    {
        await Should.ThrowAsync<ArgumentException>(async () =>
            await _kepwareApiClient.Project.IotGateway.GetRestServerAgentAsync(""));
    }

    [Fact]
    public async Task DeleteMqttClientAgent_WithNullEntity_ShouldThrowArgumentNullException()
    {
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await _kepwareApiClient.Project.IotGateway.DeleteMqttClientAgentAsync((MqttClientAgent)null!));
    }

    [Fact]
    public async Task DeleteRestClientAgent_WithNullEntity_ShouldThrowArgumentNullException()
    {
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await _kepwareApiClient.Project.IotGateway.DeleteRestClientAgentAsync((RestClientAgent)null!));
    }

    [Fact]
    public async Task DeleteRestServerAgent_WithNullEntity_ShouldThrowArgumentNullException()
    {
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await _kepwareApiClient.Project.IotGateway.DeleteRestServerAgentAsync((RestServerAgent)null!));
    }

    [Fact]
    public async Task GetIotItem_WithEmptyServerTag_ShouldThrowArgumentException()
    {
        var parentAgent = new MqttClientAgent("ParentAgent");

        await Should.ThrowAsync<ArgumentException>(async () =>
            await _kepwareApiClient.Project.IotGateway.GetIotItemAsync("", parentAgent));
    }

    [Fact]
    public async Task GetIotItem_WithNullParent_ShouldThrowArgumentNullException()
    {
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await _kepwareApiClient.Project.IotGateway.GetIotItemAsync("Channel1.Device1.Tag1", null!));
    }

    [Fact]
    public async Task DeleteIotItem_WithNullEntity_ShouldThrowArgumentNullException()
    {
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await _kepwareApiClient.Project.IotGateway.DeleteIotItemAsync((IotItem)null!));
    }

    [Fact]
    public async Task GetOrCreateIotItem_WithEmptyServerTag_ShouldThrowArgumentException()
    {
        var parentAgent = new MqttClientAgent("ParentAgent");

        await Should.ThrowAsync<ArgumentException>(async () =>
            await _kepwareApiClient.Project.IotGateway.GetOrCreateIotItemAsync("", parentAgent));
    }

    [Fact]
    public async Task GetOrCreateIotItem_WithNullParent_ShouldThrowArgumentNullException()
    {
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await _kepwareApiClient.Project.IotGateway.GetOrCreateIotItemAsync("Channel1.Device1.Tag1", null!));
    }

    [Fact]
    public async Task DeleteIotItem_ByServerTag_WithEmptyTag_ShouldThrowArgumentException()
    {
        var parentAgent = new MqttClientAgent("ParentAgent");

        await Should.ThrowAsync<ArgumentException>(async () =>
            await _kepwareApiClient.Project.IotGateway.DeleteIotItemAsync("", parentAgent));
    }

    [Fact]
    public async Task DeleteIotItem_ByServerTag_WithNullParent_ShouldThrowArgumentNullException()
    {
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await _kepwareApiClient.Project.IotGateway.DeleteIotItemAsync("Channel1.Device1.Tag1", null!));
    }

    #endregion
}
