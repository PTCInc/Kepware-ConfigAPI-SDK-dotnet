using Kepware.Api.ClientHandler;
using Kepware.Api.Model;
using Kepware.Api.Serializer;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Contrib.HttpClient;
using Shouldly;
using System.Net;
using System.Text.Json;

namespace Kepware.Api.TestIntg.ApiClient;

public class IotGatewayTests : TestIntgApiClientBase
{

    #region MQTT Client Agent Tests

    [Fact]
    public async Task GetOrCreateMqttClientAgent_WhenNotExists_ShouldCreateAgent()
    {

        // Act
        var result = await _kepwareApiClient.Project.IotGateway.GetOrCreateMqttClientAgentAsync("TestMqttAgent");

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe("TestMqttAgent");

        // Clean up
        await DeleteAllIoTAgentsAsync();

    }

    [Fact]
    public async Task GetOrCreateMqttClientAgent_WhenExists_ShouldReturnExistingAgent()
    {
        // Arrange
        var agentProperties = new Dictionary<string, JsonElement> { { "iot_gateway.AGENTTYPES_ENABLED", JsonDocument.Parse("true").RootElement }, 
                    {"iot_gateway.MQTT_CLIENT_URL", JsonDocument.Parse($"\"tcp://localhost:1883\"").RootElement} };
        var agent = await AddTestMqttClientAgent("TestMqttAgent", agentProperties);

        // Act
        var result = await _kepwareApiClient.Project.IotGateway.GetOrCreateMqttClientAgentAsync("TestMqttAgent");

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe(agent.Name);
        result.Url.ShouldBe(agent.Url);
        result.Enabled.ShouldBe(agent.Enabled);

        // Clean up
        await DeleteAllIoTAgentsAsync();

    }


    [Fact]
    public async Task CreateMqttClientAgent_WhenSuccessful_ShouldReturnAgent()
    {
        // Act
        var result = await _kepwareApiClient.Project.IotGateway.CreateMqttClientAgentAsync("TestMqttAgent");

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe("TestMqttAgent");

        // Clean up
        await DeleteAllIoTAgentsAsync();
    }

    [Fact]
    public async Task CreateMqttClientAgent_WithProperties_ShouldSetProperties()
    {
        // Arrange

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

        // Clean up
        await DeleteAllIoTAgentsAsync();

    }

    [Fact]
    public async Task CreateMqttClientAgent_WithHttpError_AlreadyExists_ShouldReturnNull()
    {
        // Arrange
        var agentProperties = new Dictionary<string, JsonElement> { { "iot_gateway.AGENTTYPES_ENABLED", JsonDocument.Parse("true").RootElement }, 
                    {"iot_gateway.MQTT_CLIENT_URL", JsonDocument.Parse($"\"tcp://localhost:1883\"").RootElement} };
        var agent = await AddTestMqttClientAgent("TestMqttAgent", agentProperties);

        // Act
        var result = await _kepwareApiClient.Project.IotGateway.CreateMqttClientAgentAsync("TestMqttAgent");

        // Assert
        result.ShouldBeNull();

        // Clean up
        await DeleteAllIoTAgentsAsync();

    }

    [Fact]
    public async Task GetMqttClientAgent_WhenExists_ShouldReturnAgent()
    {
        // Arrange
        var agentProperties = new Dictionary<string, JsonElement> { { "iot_gateway.AGENTTYPES_ENABLED", JsonDocument.Parse("true").RootElement }, 
                    {"iot_gateway.MQTT_CLIENT_URL", JsonDocument.Parse($"\"tcp://localhost:1883\"").RootElement} };
        var agent = await AddTestMqttClientAgent("TestMqttAgent", agentProperties);

        // Act
        var result = await _kepwareApiClient.Project.IotGateway.GetMqttClientAgentAsync("TestMqttAgent");

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe("TestMqttAgent");
        result.Enabled.ShouldBe(true);
        result.Url.ShouldBe("tcp://localhost:1883");

        // Clean up
        await DeleteAllIoTAgentsAsync();

    }

    [Fact]
    public async Task GetMqttClientAgent_WhenNotFound_ShouldReturnNull()
    {

        // Act
        var result = await _kepwareApiClient.Project.IotGateway.GetMqttClientAgentAsync("NonExistent");

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public async Task DeleteMqttClientAgent_ByEntity_WhenSuccessful_ShouldReturnTrue()
    {
        // Arrange
        var agentProperties = new Dictionary<string, JsonElement> { { "iot_gateway.AGENTTYPES_ENABLED", JsonDocument.Parse("true").RootElement }, 
                    {"iot_gateway.MQTT_CLIENT_URL", JsonDocument.Parse($"\"tcp://localhost:1883\"").RootElement} };
        var agent = await AddTestMqttClientAgent("TestMqttAgent", agentProperties);

        // Act
        var result = await _kepwareApiClient.Project.IotGateway.DeleteMqttClientAgentAsync(agent);

        // Assert
        result.ShouldBeTrue();

        // Clean up
        await DeleteAllIoTAgentsAsync();
    }

    [Fact]
    public async Task DeleteMqttClientAgent_ByName_WhenSuccessful_ShouldReturnTrue()
    {
        // Arrange
        var agentProperties = new Dictionary<string, JsonElement> { { "iot_gateway.AGENTTYPES_ENABLED", JsonDocument.Parse("true").RootElement }, 
                    {"iot_gateway.MQTT_CLIENT_URL", JsonDocument.Parse($"\"tcp://localhost:1883\"").RootElement} };
        var agent = await AddTestMqttClientAgent("TestMqttAgent", agentProperties);

        // Act
        var result = await _kepwareApiClient.Project.IotGateway.DeleteMqttClientAgentAsync("TestMqttAgent");

        // Assert
        result.ShouldBeTrue();

        // Clean up
        await DeleteAllIoTAgentsAsync();

    }

    [Fact]
    public async Task DeleteMqttClientAgent_WithHttpError_ShouldReturnFalse()
    {

        // Act
        var result = await _kepwareApiClient.Project.IotGateway.DeleteMqttClientAgentAsync("TestMqttAgent");

        // Assert
        result.ShouldBeFalse();
    }


    #endregion

    #region REST Client Agent Tests


    [SkippableFact]
    public async Task GetOrCreateRestClientAgent_WhenNotExists_ShouldCreateAgent()
    {
        // Skip the test if the product is not "Server" productId
        Skip.If(_productInfo.ProductId != "012", "Test only applicable for Server productIds");
        // Act
        var result = await _kepwareApiClient.Project.IotGateway.GetOrCreateRestClientAgentAsync("TestRestClient");

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe("TestRestClient");

        // Clean up
        await DeleteAllIoTAgentsAsync();

    }

    [SkippableFact]
    public async Task GetOrCreateRestClientAgent_WhenExists_ShouldReturnExistingAgent()
    {
        // Skip the test if the product is not "Server" productId
        Skip.If(_productInfo.ProductId != "012", "Test only applicable for Server productIds");

        // Arrange
        var agentProperties = new Dictionary<string, JsonElement> { { "iot_gateway.AGENTTYPES_ENABLED", JsonDocument.Parse("true").RootElement }, 
                    {"iot_gateway.REST_CLIENT_URL", JsonDocument.Parse($"\"https://api.example.com\"").RootElement} };
        var agent = await AddTestRestClientAgent("TestRestClient", agentProperties);

        // Act
        var result = await _kepwareApiClient.Project.IotGateway.GetOrCreateRestClientAgentAsync("TestRestClient");

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe(agent.Name);
        result.Url.ShouldBe(agent.Url);
        result.Enabled.ShouldBe(agent.Enabled);

        // Clean up
        await DeleteAllIoTAgentsAsync();

    }


    [SkippableFact]
    public async Task CreateRestClientAgent_WhenSuccessful_ShouldReturnAgent()
    {
        // Skip the test if the product is not "Server" productId
        Skip.If(_productInfo.ProductId != "012", "Test only applicable for Server productIds");

        // Act
        var result = await _kepwareApiClient.Project.IotGateway.CreateRestClientAgentAsync("TestRestClient");

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe("TestRestClient");

        // Clean up
        await DeleteAllIoTAgentsAsync();

    }

    [SkippableFact]
    public async Task CreateRestClientAgent_WithProperties_ShouldSetProperties()
    {
        // Skip the test if the product is not "Server" productId
        Skip.If(_productInfo.ProductId != "012", "Test only applicable for Server productIds");

        // Arrange

        var properties = new Dictionary<string, object>
        {
            { Properties.RestClientAgent.Url, "https://api.example.com" }
        };

        // Act
        var result = await _kepwareApiClient.Project.IotGateway.CreateRestClientAgentAsync("TestRestClient", properties);

        // Assert
        result.ShouldNotBeNull();
        result.Url.ShouldBe("https://api.example.com");

        // Clean up
        await DeleteAllIoTAgentsAsync();

    }

    [SkippableFact]
    public async Task CreateRestClientAgent_WithHttpError_AlreadyExists_ShouldReturnNull()
    {
        // Skip the test if the product is not "Server" productId
        Skip.If(_productInfo.ProductId != "012", "Test only applicable for Server productIds");

        // Arrange
        var agentProperties = new Dictionary<string, JsonElement> { { "iot_gateway.AGENTTYPES_ENABLED", JsonDocument.Parse("true").RootElement }, 
                    {"iot_gateway.REST_CLIENT_URL", JsonDocument.Parse($"\"https://api.example.com\"").RootElement} };
        var agent = await AddTestRestClientAgent("TestRestClient", agentProperties);

        // Act
        var result = await _kepwareApiClient.Project.IotGateway.CreateRestClientAgentAsync("TestRestClient");

        // Assert
        result.ShouldBeNull();
        
        // Clean up
        await DeleteAllIoTAgentsAsync();

    }


    [SkippableFact]
    public async Task GetRestClientAgent_WhenExists_ShouldReturnAgent()
    {
        // Skip the test if the product is not "Server" productId
        Skip.If(_productInfo.ProductId != "012", "Test only applicable for Server productIds");

        // Arrange
        var agentProperties = new Dictionary<string, JsonElement> { { "iot_gateway.AGENTTYPES_ENABLED", JsonDocument.Parse("true").RootElement }, 
                    {"iot_gateway.REST_CLIENT_URL", JsonDocument.Parse($"\"https://api.example.com\"").RootElement} };
        var agent = await AddTestRestClientAgent("TestRestClient", agentProperties);

        // Act
        var result = await _kepwareApiClient.Project.IotGateway.GetRestClientAgentAsync("TestRestClient");

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe("TestRestClient");
        result.Url.ShouldBe("https://api.example.com");

        // Clean up
        await DeleteAllIoTAgentsAsync();

    }

    [SkippableFact]
    public async Task GetRestClientAgent_WhenNotFound_ShouldReturnNull()
    {
        // Skip the test if the product is not "Server" productId
        Skip.If(_productInfo.ProductId != "012", "Test only applicable for Server productIds");

        // Act
        var result = await _kepwareApiClient.Project.IotGateway.GetRestClientAgentAsync("NonExistent");

        // Assert
        result.ShouldBeNull();
    }

    [SkippableFact]
    public async Task DeleteRestClientAgent_ByEntity_WhenSuccessful_ShouldReturnTrue()
    {
        // Skip the test if the product is not "Server" productId
        Skip.If(_productInfo.ProductId != "012", "Test only applicable for Server productIds");

        // Arrange
        var agentProperties = new Dictionary<string, JsonElement> { { "iot_gateway.AGENTTYPES_ENABLED", JsonDocument.Parse("true").RootElement }, 
                    {"iot_gateway.REST_CLIENT_URL", JsonDocument.Parse($"\"https://api.example.com\"").RootElement} };
        var agent = await AddTestRestClientAgent("TestRestClient", agentProperties);

        // Act
        var result = await _kepwareApiClient.Project.IotGateway.DeleteRestClientAgentAsync(agent);

        // Assert
        result.ShouldBeTrue();

        // Clean up
        await DeleteAllIoTAgentsAsync();

    }

    [SkippableFact]
    public async Task DeleteRestClientAgent_ByName_WhenSuccessful_ShouldReturnTrue()
    {
        // Skip the test if the product is not "Server" productId
        Skip.If(_productInfo.ProductId != "012", "Test only applicable for Server productIds");

        // Arrange
        var agentProperties = new Dictionary<string, JsonElement> { { "iot_gateway.AGENTTYPES_ENABLED", JsonDocument.Parse("true").RootElement }, 
                    {"iot_gateway.REST_CLIENT_URL", JsonDocument.Parse($"\"https://api.example.com\"").RootElement} };
        var agent = await AddTestRestClientAgent("TestRestClient", agentProperties);

        // Act
        var result = await _kepwareApiClient.Project.IotGateway.DeleteRestClientAgentAsync("TestRestClient");

        // Assert
        result.ShouldBeTrue();

        // Clean up
        await DeleteAllIoTAgentsAsync();
    }

    [SkippableFact]
    public async Task DeleteRestClientAgent_WithHttpError_ShouldReturnFalse()
    {
        // Skip the test if the product is not "Server" productId
        Skip.If(_productInfo.ProductId != "012", "Test only applicable for Server productIds");

        // Act
        var result = await _kepwareApiClient.Project.IotGateway.DeleteRestClientAgentAsync("TestRestClient");

        // Assert
        result.ShouldBeFalse();
    }

    #endregion

    #region REST Server Agent Tests

    [SkippableFact]
    public async Task GetOrCreateRestServerAgent_WhenNotExists_ShouldCreateAgent()
    {
        // Skip the test if the product is not "Server" productId
        Skip.If(_productInfo.ProductId != "012", "Test only applicable for Server productIds");

        // Act
        var result = await _kepwareApiClient.Project.IotGateway.GetOrCreateRestServerAgentAsync("TestRestServer");

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe("TestRestServer");

        // Clean up
        await DeleteAllIoTAgentsAsync();

    }

    [SkippableFact]
    public async Task GetOrCreateRestServerAgent_WhenExists_ShouldReturnExistingAgent()
    {
        // Skip the test if the product is not "Server" productId
        Skip.If(_productInfo.ProductId != "012", "Test only applicable for Server productIds");
        
        // Arrange
        
        var agentProperties = new Dictionary<string, JsonElement> { { "iot_gateway.AGENTTYPES_ENABLED", JsonDocument.Parse("true").RootElement }, 
                    {"iot_gateway.REST_SERVER_PORT_NUMBER", JsonDocument.Parse("39320").RootElement} };
        var agent = await AddTestRestServerAgent("TestRestServer", agentProperties);

        // Act
        var result = await _kepwareApiClient.Project.IotGateway.GetOrCreateRestServerAgentAsync("TestRestServer");

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe(agent.Name);
        result.PortNumber.ShouldBe(agent.PortNumber);
        result.Enabled.ShouldBe(agent.Enabled);

        // Clean up
        await DeleteAllIoTAgentsAsync();

    }


    [SkippableFact]
    public async Task CreateRestServerAgent_WhenSuccessful_ShouldReturnAgent()
    {
        // Skip the test if the product is not "Server" productId
        Skip.If(_productInfo.ProductId != "012", "Test only applicable for Server productIds");

        // Act
        var result = await _kepwareApiClient.Project.IotGateway.CreateRestServerAgentAsync("TestRestServer");

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe("TestRestServer");

        // Clean up
        await DeleteAllIoTAgentsAsync();

    }

    [SkippableFact]
    public async Task CreateRestServerAgent_WithProperties_ShouldSetProperties()
    {
        // Skip the test if the product is not "Server" productId
        Skip.If(_productInfo.ProductId != "012", "Test only applicable for Server productIds");

        // Arrange

        var properties = new Dictionary<string, object>
        {
            { Properties.RestServerAgent.PortNumber, 39320 }
        };

        // Act
        var result = await _kepwareApiClient.Project.IotGateway.CreateRestServerAgentAsync("TestRestServer", properties);

        // Assert
        result.ShouldNotBeNull();
        result.PortNumber.ShouldBe(39320);
        
        // Clean up
        await DeleteAllIoTAgentsAsync();
    }

    [SkippableFact]
    public async Task CreateRestServerAgent_WithHttpError_AlreadyExists_ShouldReturnNull()
    {
        // Skip the test if the product is not "Server" productId
        Skip.If(_productInfo.ProductId != "012", "Test only applicable for Server productIds");

        // Arrange
        var agentProperties = new Dictionary<string, JsonElement> { { "iot_gateway.AGENTTYPES_ENABLED", JsonDocument.Parse("true").RootElement }, 
                    {"iot_gateway.REST_SERVER_PORT_NUMBER", JsonDocument.Parse("39320").RootElement} };
        var agent = await AddTestRestServerAgent("TestRestServer", agentProperties);

        // Act
        var result = await _kepwareApiClient.Project.IotGateway.CreateRestServerAgentAsync("TestRestServer");

        // Assert
        result.ShouldBeNull();

        // Clean up
        await DeleteAllIoTAgentsAsync();

    }


    [SkippableFact]
    public async Task GetRestServerAgent_WhenExists_ShouldReturnAgent()
    {
        // Skip the test if the product is not "Server" productId
        Skip.If(_productInfo.ProductId != "012", "Test only applicable for Server productIds");

        // Arrange
        var agentProperties = new Dictionary<string, JsonElement> { { "iot_gateway.AGENTTYPES_ENABLED", JsonDocument.Parse("true").RootElement }, 
                    {"iot_gateway.REST_SERVER_PORT_NUMBER", JsonDocument.Parse("39320").RootElement} };
        var agent = await AddTestRestServerAgent("TestRestServer", agentProperties);

        // Act
        var result = await _kepwareApiClient.Project.IotGateway.GetRestServerAgentAsync("TestRestServer");

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe("TestRestServer");
        result.PortNumber.ShouldBe(39320);

        // Clean up
        await DeleteAllIoTAgentsAsync();

    }

    [SkippableFact]
    public async Task GetRestServerAgent_WhenNotFound_ShouldReturnNull()
    {
        // Skip the test if the product is not "Server" productId
        Skip.If(_productInfo.ProductId != "012", "Test only applicable for Server productIds");

        // Act
        var result = await _kepwareApiClient.Project.IotGateway.GetRestServerAgentAsync("NonExistent");

        // Assert
        result.ShouldBeNull();
    }

    [SkippableFact]
    public async Task DeleteRestServerAgent_ByEntity_WhenSuccessful_ShouldReturnTrue()
    {
        // Skip the test if the product is not "Server" productId
        Skip.If(_productInfo.ProductId != "012", "Test only applicable for Server productIds");

        // Arrange
        var agentProperties = new Dictionary<string, JsonElement> { { "iot_gateway.AGENTTYPES_ENABLED", JsonDocument.Parse("true").RootElement }, 
                    {"iot_gateway.REST_SERVER_PORT_NUMBER", JsonDocument.Parse("39320").RootElement} };
        var agent = await AddTestRestServerAgent("TestRestServer", agentProperties);

        // Act
        var result = await _kepwareApiClient.Project.IotGateway.DeleteRestServerAgentAsync(agent);

        // Assert
        result.ShouldBeTrue();

        // Clean up
        await DeleteAllIoTAgentsAsync();
    }

    [SkippableFact]
    public async Task DeleteRestServerAgent_ByName_WhenSuccessful_ShouldReturnTrue()
    {
        // Skip the test if the product is not "Server" productId
        Skip.If(_productInfo.ProductId != "012", "Test only applicable for Server productIds");

        // Arrange
        var agentProperties = new Dictionary<string, JsonElement> { { "iot_gateway.AGENTTYPES_ENABLED", JsonDocument.Parse("true").RootElement }, 
                    {"iot_gateway.REST_SERVER_PORT_NUMBER", JsonDocument.Parse("39320").RootElement} };
        var agent = await AddTestRestServerAgent("TestRestServer", agentProperties);

        // Act
        var result = await _kepwareApiClient.Project.IotGateway.DeleteRestServerAgentAsync("TestRestServer");

        // Assert
        result.ShouldBeTrue();

        // Clean up
        await DeleteAllIoTAgentsAsync();
    }

    [SkippableFact]
    public async Task DeleteRestServerAgent_WithHttpError_ShouldReturnFalse()
    {
        // Skip the test if the product is not "Server" productId
        Skip.If(_productInfo.ProductId != "012", "Test only applicable for Server productIds");

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
        var channel = await AddTestChannel("Channel1");
        var device = await AddTestDevice(channel, "Device1");
        var tag = await AddSimulatorTestTag(device, "Tag1");
        
        var parentMqttAgent = await AddTestMqttClientAgent("ParentMqttAgent", new Dictionary<string, JsonElement> { { "iot_gateway.AGENTTYPES_ENABLED", JsonDocument.Parse("false").RootElement }});
        
        // Act
        var resultMqttClient = await _kepwareApiClient.Project.IotGateway.GetOrCreateIotItemAsync("Channel1.Device1.Tag1", parentMqttAgent);
        
        
        // Act - create a system tag item to test the leading underscore logic
        var systemTagItem = await _kepwareApiClient.Project.IotGateway.GetOrCreateIotItemAsync("_System._Time", parentMqttAgent);

        // Assert
        resultMqttClient.ShouldNotBeNull();
        resultMqttClient.Name.ShouldBe("Channel1_Device1_Tag1");
        resultMqttClient.ServerTag.ShouldBe("Channel1.Device1.Tag1");

        systemTagItem.ShouldNotBeNull();
        systemTagItem.Name.ShouldBe("System__Time");
        systemTagItem.ServerTag.ShouldBe("_System._Time");


        // If Kepware Server, test that REST Client and REST Server agents also create items with the same name and server tag
        if (_productInfo.ProductId == "012")
        {   
            // Arrange
            var parentRestClientAgent = await AddTestRestClientAgent("ParentRestClientAgent", new Dictionary<string, JsonElement> { { "iot_gateway.AGENTTYPES_ENABLED", JsonDocument.Parse("false").RootElement }});
            var parentRestServerAgent = await AddTestRestServerAgent("ParentRestServerAgent", new Dictionary<string, JsonElement> { { "iot_gateway.AGENTTYPES_ENABLED", JsonDocument.Parse("false").RootElement }});

            //Act
            var resultRestClient = await _kepwareApiClient.Project.IotGateway.GetOrCreateIotItemAsync("Channel1.Device1.Tag1", parentRestClientAgent);
            var resultRestServer = await _kepwareApiClient.Project.IotGateway.GetOrCreateIotItemAsync("Channel1.Device1.Tag1", parentRestServerAgent);

            // Assert
            resultRestClient.ShouldNotBeNull();
            resultRestClient.Name.ShouldBe("Channel1_Device1_Tag1");
            resultRestClient.ServerTag.ShouldBe("Channel1.Device1.Tag1");

            resultRestServer.ShouldNotBeNull();
            resultRestServer.Name.ShouldBe("Channel1_Device1_Tag1");
            resultRestServer.ServerTag.ShouldBe("Channel1.Device1.Tag1");
        }

        
        // Clean up
        await DeleteAllIoTAgentsAsync();
        await DeleteAllChannelsAsync();
    }

    [Fact]
    public async Task GetOrCreateIotItem_WhenExists_ShouldReturnExistingItem()
    {
        // Arrange
        var channel = await AddTestChannel("Channel1");
        var device = await AddTestDevice(channel, "Device1");
        var tag = await AddSimulatorTestTag(device, "Tag1");
        
        var parentMqttAgent = await AddTestMqttClientAgent("ParentMqttAgent", new Dictionary<string, JsonElement> { { "iot_gateway.AGENTTYPES_ENABLED", JsonDocument.Parse("false").RootElement }});
        
        // TODO: add items via client base method
        var itemMqttClient = await _kepwareApiClient.Project.IotGateway.GetOrCreateIotItemAsync("Channel1.Device1.Tag1", parentMqttAgent);
        
        // Act
        var resultMqttClient = await _kepwareApiClient.Project.IotGateway.GetOrCreateIotItemAsync("Channel1.Device1.Tag1", parentMqttAgent);
        
        
        // Act - create a system tag item to test the leading underscore logic
        var systemTagItem = await _kepwareApiClient.Project.IotGateway.GetOrCreateIotItemAsync("_System._Time", parentMqttAgent);

        // Assert
        resultMqttClient.ShouldNotBeNull();
        resultMqttClient.Name.ShouldBe(itemMqttClient.Name);
        resultMqttClient.ServerTag.ShouldBe(itemMqttClient.ServerTag);

        systemTagItem.ShouldNotBeNull();
        systemTagItem.Name.ShouldBe(systemTagItem.Name);
        systemTagItem.ServerTag.ShouldBe(systemTagItem.ServerTag);

        // If Kepware Server, test that REST Client and REST Server agents also return items with the same name and server tag
        if (_productInfo.ProductId == "012")
        {
            // Arrange
            var parentRestClientAgent = await AddTestRestClientAgent("ParentRestClientAgent", new Dictionary<string, JsonElement> { { "iot_gateway.AGENTTYPES_ENABLED", JsonDocument.Parse("false").RootElement }});
            var parentRestServerAgent = await AddTestRestServerAgent("ParentRestServerAgent", new Dictionary<string, JsonElement> { { "iot_gateway.AGENTTYPES_ENABLED", JsonDocument.Parse("false").RootElement }});
            
            // TODO: add items via client base method
            var itemRestClient = await _kepwareApiClient.Project.IotGateway.GetOrCreateIotItemAsync("Channel1.Device1.Tag1", parentRestClientAgent);
            var itemRestServer = await _kepwareApiClient.Project.IotGateway.GetOrCreateIotItemAsync("Channel1.Device1.Tag1", parentRestServerAgent);

            // Act
            var resultRestClient = await _kepwareApiClient.Project.IotGateway.GetOrCreateIotItemAsync("Channel1.Device1.Tag1", parentRestClientAgent);
            var resultRestServer = await _kepwareApiClient.Project.IotGateway.GetOrCreateIotItemAsync("Channel1.Device1.Tag1", parentRestServerAgent);

            // Assert
            resultRestClient.ShouldNotBeNull();
            resultRestClient.Name.ShouldBe(itemRestClient.Name);
            resultRestClient.ServerTag.ShouldBe(itemRestClient.ServerTag);

            resultRestServer.ShouldNotBeNull();
            resultRestServer.Name.ShouldBe(itemRestServer.Name);
            resultRestServer.ServerTag.ShouldBe(itemRestServer.ServerTag);
        }

        // Clean up
        await DeleteAllIoTAgentsAsync();
        await DeleteAllChannelsAsync();
    }

    [Fact]
    public async Task GetOrCreateIotItem_WhenCreateFails_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var parentMqttAgent = await AddTestMqttClientAgent("ParentMqttAgent", new Dictionary<string, JsonElement> { { "iot_gateway.AGENTTYPES_ENABLED", JsonDocument.Parse("false").RootElement }});

        // Act
        await Should.ThrowAsync<InvalidOperationException>(async () =>
            await _kepwareApiClient.Project.IotGateway.GetOrCreateIotItemAsync("Channel1.Device1.Tag1", parentMqttAgent));

        if (_productInfo.ProductId == "012")
        {
            // Arrange
            var parentRestClientAgent = await AddTestRestClientAgent("ParentRestClientAgent", new Dictionary<string, JsonElement> { { "iot_gateway.AGENTTYPES_ENABLED", JsonDocument.Parse("false").RootElement }});
            var parentRestServerAgent = await AddTestRestServerAgent("ParentRestServerAgent", new Dictionary<string, JsonElement> { { "iot_gateway.AGENTTYPES_ENABLED", JsonDocument.Parse("false").RootElement }});

            // Act
            await Should.ThrowAsync<InvalidOperationException>(async () =>
                await _kepwareApiClient.Project.IotGateway.GetOrCreateIotItemAsync("Channel1.Device1.Tag1", parentRestClientAgent));
            await Should.ThrowAsync<InvalidOperationException>(async () =>
                await _kepwareApiClient.Project.IotGateway.GetOrCreateIotItemAsync("Channel1.Device1.Tag1", parentRestServerAgent));
        }
        // Clean up
        await DeleteAllIoTAgentsAsync();

    }

    [Fact]
    public async Task CreateIotItem_ShouldDeriveNameFromServerTag()
    {
        // Arrange - server tag "Channel1.Device1.Tag1" should query with name "Channel1_Device1_Tag1"
        var channel = await AddTestChannel("Channel1");
        var device = await AddTestDevice(channel, "Device1");
        var tag = await AddSimulatorTestTag(device, "Tag1");
        
        var parentMqttAgent = await AddTestMqttClientAgent("ParentMqttAgent", new Dictionary<string, JsonElement> { { "iot_gateway.AGENTTYPES_ENABLED", JsonDocument.Parse("false").RootElement }});
        

        // Act
        var resultMqttClient = await _kepwareApiClient.Project.IotGateway.CreateIotItemAsync("Channel1.Device1.Tag1", parentMqttAgent);
        
        
        // Act - create a system tag item to test the leading underscore logic
        var systemTagItem = await _kepwareApiClient.Project.IotGateway.CreateIotItemAsync("_System._Time", parentMqttAgent);

        // Assert
        resultMqttClient.ShouldNotBeNull();
        resultMqttClient.Name.ShouldBe("Channel1_Device1_Tag1");
        resultMqttClient.ServerTag.ShouldBe("Channel1.Device1.Tag1");
        
        systemTagItem.ShouldNotBeNull();
        systemTagItem.Name.ShouldBe("System__Time");
        systemTagItem.ServerTag.ShouldBe("_System._Time");

        if (_productInfo.ProductId == "012")
        {
            // Arrange
            var parentRestClientAgent = await AddTestRestClientAgent("ParentRestClientAgent", new Dictionary<string, JsonElement> { { "iot_gateway.AGENTTYPES_ENABLED", JsonDocument.Parse("false").RootElement }});
            var parentRestServerAgent = await AddTestRestServerAgent("ParentRestServerAgent", new Dictionary<string, JsonElement> { { "iot_gateway.AGENTTYPES_ENABLED", JsonDocument.Parse("false").RootElement }});

            // Act
            var resultRestClient = await _kepwareApiClient.Project.IotGateway.CreateIotItemAsync("Channel1.Device1.Tag1", parentRestClientAgent);
            var resultRestServer = await _kepwareApiClient.Project.IotGateway.CreateIotItemAsync("Channel1.Device1.Tag1", parentRestServerAgent);

            // Assert
            resultRestClient.ShouldNotBeNull();
            resultRestClient.Name.ShouldBe("Channel1_Device1_Tag1");
            resultRestClient.ServerTag.ShouldBe("Channel1.Device1.Tag1");

            resultRestServer.ShouldNotBeNull();
            resultRestServer.Name.ShouldBe("Channel1_Device1_Tag1");
            resultRestServer.ServerTag.ShouldBe("Channel1.Device1.Tag1");

        }
        // Clean up
        await DeleteAllIoTAgentsAsync();
        await DeleteAllChannelsAsync();
    }

    [Fact]
    public async Task CreateIotItem_WithHttpError_AlreadyExists_ShouldReturnNull()
    {
        // Arrange
        var channel = await AddTestChannel("Channel1");
        var device = await AddTestDevice(channel, "Device1");
        var tag = await AddSimulatorTestTag(device, "Tag1");
        
        var parentMqttAgent = await AddTestMqttClientAgent("ParentMqttAgent", new Dictionary<string, JsonElement> { { "iot_gateway.AGENTTYPES_ENABLED", JsonDocument.Parse("false").RootElement }});

        // TODO: add items via client base method
        var itemMqttClient = await _kepwareApiClient.Project.IotGateway.GetOrCreateIotItemAsync("Channel1.Device1.Tag1", parentMqttAgent);
        
        // Act
        var resultMqttClient = await _kepwareApiClient.Project.IotGateway.CreateIotItemAsync("Channel1.Device1.Tag1", parentMqttAgent);

        // Assert
        resultMqttClient.ShouldBeNull();

        if (_productInfo.ProductId == "012")
        {
            // Arrange
            var parentRestClientAgent = await AddTestRestClientAgent("ParentRestClientAgent", new Dictionary<string, JsonElement> { { "iot_gateway.AGENTTYPES_ENABLED", JsonDocument.Parse("false").RootElement }});
            var parentRestServerAgent = await AddTestRestServerAgent("ParentRestServerAgent", new Dictionary<string, JsonElement> { { "iot_gateway.AGENTTYPES_ENABLED", JsonDocument.Parse("false").RootElement }});

            // TODO: add items via client base method
            
            var itemRestClient = await _kepwareApiClient.Project.IotGateway.GetOrCreateIotItemAsync("Channel1.Device1.Tag1", parentRestClientAgent);
            var itemRestServer = await _kepwareApiClient.Project.IotGateway.GetOrCreateIotItemAsync("Channel1.Device1.Tag1", parentRestServerAgent);
            
            // Act
            
            var resultRestClient = await _kepwareApiClient.Project.IotGateway.CreateIotItemAsync("Channel1.Device1.Tag1", parentRestClientAgent);
            var resultRestServer = await _kepwareApiClient.Project.IotGateway.CreateIotItemAsync("Channel1.Device1.Tag1", parentRestServerAgent);

            resultRestClient.ShouldBeNull();
            resultRestServer.ShouldBeNull();
        }

        // Clean up
        await DeleteAllIoTAgentsAsync();
        await DeleteAllChannelsAsync();
    }


    [Fact]
    public async Task DeleteIotItem_ByEntity_WhenSuccessful_ShouldReturnTrue()
    {
        // Arrange
        var channel = await AddTestChannel("Channel1");
        var device = await AddTestDevice(channel, "Device1");
        var tag = await AddSimulatorTestTag(device, "Tag1");
        
        var parentMqttAgent = await AddTestMqttClientAgent("ParentMqttAgent", new Dictionary<string, JsonElement> { { "iot_gateway.AGENTTYPES_ENABLED", JsonDocument.Parse("false").RootElement }});
        
        // TODO: add items via client base method
        var itemMqttClient = await _kepwareApiClient.Project.IotGateway.GetOrCreateIotItemAsync("Channel1.Device1.Tag1", parentMqttAgent);
        
        // Act
        var resultMqttClient = await _kepwareApiClient.Project.IotGateway.DeleteIotItemAsync(itemMqttClient);
        
        // Assert
        resultMqttClient.ShouldBeTrue();

        if (_productInfo.ProductId == "012")
        {
            // Arrange
            var parentRestClientAgent = await AddTestRestClientAgent("ParentRestClientAgent", new Dictionary<string, JsonElement> { { "iot_gateway.AGENTTYPES_ENABLED", JsonDocument.Parse("false").RootElement }});
            var parentRestServerAgent = await AddTestRestServerAgent("ParentRestServerAgent", new Dictionary<string, JsonElement> { { "iot_gateway.AGENTTYPES_ENABLED", JsonDocument.Parse("false").RootElement }});

            // TODO: add items via client base method
           
            var itemRestClient = await _kepwareApiClient.Project.IotGateway.GetOrCreateIotItemAsync("Channel1.Device1.Tag1", parentRestClientAgent);
            var itemRestServer = await _kepwareApiClient.Project.IotGateway.GetOrCreateIotItemAsync("Channel1.Device1.Tag1", parentRestServerAgent);
            
            // Act
            
            var resultRestClient = await _kepwareApiClient.Project.IotGateway.DeleteIotItemAsync(itemRestClient);
            var resultRestServer = await _kepwareApiClient.Project.IotGateway.DeleteIotItemAsync(itemRestServer);

            // Assert
            resultRestClient.ShouldBeTrue();
            resultRestServer.ShouldBeTrue();
        }

        // Clean up
        await DeleteAllIoTAgentsAsync();
        await DeleteAllChannelsAsync();

    }

    [Fact]
    public async Task DeleteIotItem_ByServerTag_ShouldTranslateToItemName()
    {
        // Arrange
        var channel = await AddTestChannel("Channel1");
        var device = await AddTestDevice(channel, "Device1");
        var tag = await AddSimulatorTestTag(device, "Tag1");
        
        var parentMqttAgent = await AddTestMqttClientAgent("ParentMqttAgent", new Dictionary<string, JsonElement> { { "iot_gateway.AGENTTYPES_ENABLED", JsonDocument.Parse("false").RootElement }});
        
        // TODO: add items via client base method
        var itemMqttClient = await _kepwareApiClient.Project.IotGateway.GetOrCreateIotItemAsync("Channel1.Device1.Tag1", parentMqttAgent);
        
        // Act
        var resultMqttClient = await _kepwareApiClient.Project.IotGateway.DeleteIotItemAsync("Channel1.Device1.Tag1", parentMqttAgent);
        
        // Assert
        resultMqttClient.ShouldBeTrue();

        if (_productInfo.ProductId == "012")
        {
            
            var parentRestClientAgent = await AddTestRestClientAgent("ParentRestClientAgent", new Dictionary<string, JsonElement> { { "iot_gateway.AGENTTYPES_ENABLED", JsonDocument.Parse("false").RootElement }});
            var parentRestServerAgent = await AddTestRestServerAgent("ParentRestServerAgent", new Dictionary<string, JsonElement> { { "iot_gateway.AGENTTYPES_ENABLED", JsonDocument.Parse("false").RootElement }});

            // TODO: add items via client base method
            var itemRestClient = await _kepwareApiClient.Project.IotGateway.GetOrCreateIotItemAsync("Channel1.Device1.Tag1", parentRestClientAgent);
            var itemRestServer = await _kepwareApiClient.Project.IotGateway.GetOrCreateIotItemAsync("Channel1.Device1.Tag1", parentRestServerAgent);

            // Act
            var resultRestClient = await _kepwareApiClient.Project.IotGateway.DeleteIotItemAsync("Channel1.Device1.Tag1", parentRestClientAgent);
            var resultRestServer = await _kepwareApiClient.Project.IotGateway.DeleteIotItemAsync("Channel1.Device1.Tag1", parentRestServerAgent);

            // Assert
            resultRestClient.ShouldBeTrue();
            resultRestServer.ShouldBeTrue();
        }
        // Clean up
        await DeleteAllIoTAgentsAsync();
        await DeleteAllChannelsAsync();

    }

    [Fact]
    public async Task DeleteIotItem_WithHttpError_ShouldReturnFalse()
    {
        // Arrange
        var parentMqttAgent = await AddTestMqttClientAgent("ParentMqttAgent", new Dictionary<string, JsonElement> { { "iot_gateway.AGENTTYPES_ENABLED", JsonDocument.Parse("false").RootElement }});
        
        // Act
        var resultMqttClient = await _kepwareApiClient.Project.IotGateway.DeleteIotItemAsync("Channel1.Device1.Tag1", parentMqttAgent);
        
        // Assert
        resultMqttClient.ShouldBeFalse();

        if (_productInfo.ProductId == "012")
        {
            // Arrange
            var parentRestClientAgent = await AddTestRestClientAgent("ParentRestClientAgent", new Dictionary<string, JsonElement> { { "iot_gateway.AGENTTYPES_ENABLED", JsonDocument.Parse("false").RootElement }});
            var parentRestServerAgent = await AddTestRestServerAgent("ParentRestServerAgent", new Dictionary<string, JsonElement> { { "iot_gateway.AGENTTYPES_ENABLED", JsonDocument.Parse("false").RootElement }});

            // Act
            var resultRestClient = await _kepwareApiClient.Project.IotGateway.DeleteIotItemAsync("Channel1.Device1.Tag1", parentRestClientAgent);
            var resultRestServer = await _kepwareApiClient.Project.IotGateway.DeleteIotItemAsync("Channel1.Device1.Tag1", parentRestServerAgent);

            // Assert
            resultRestClient.ShouldBeFalse();
            resultRestServer.ShouldBeFalse();
        }

        // Clean up
        await DeleteAllIoTAgentsAsync();

    }

    #endregion

}
