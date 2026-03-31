using Kepware.Api.Model;
using Kepware.Api.Serializer;
using System.Text.Json;

namespace Kepware.Api.Test.Model.IotGateway
{
    public class IotGatewayModelTests
    {
        #region MqttClientAgent Tests

        [Fact]
        public void MqttClientAgent_Deserialize_ShouldPopulateProperties()
        {
            var json = """
            {
                "common.ALLTYPES_NAME": "TestMqttAgent",
                "common.ALLTYPES_DESCRIPTION": "A test MQTT client agent",
                "iot_gateway.AGENTTYPES_TYPE": "MQTT Client",
                "iot_gateway.AGENTTYPES_ENABLED": true,
                "iot_gateway.IGNORE_QUALITY_CHANGES": false,
                "iot_gateway.MQTT_CLIENT_URL": "tcp://localhost:1883",
                "iot_gateway.MQTT_CLIENT_TOPIC": "iotgateway",
                "iot_gateway.MQTT_CLIENT_QOS": 1,
                "iot_gateway.AGENTTYPES_PUBLISH_TYPE": 0,
                "iot_gateway.AGENTTYPES_RATE_MS": 10000,
                "iot_gateway.AGENTTYPES_PUBLISH_FORMAT": 0,
                "iot_gateway.AGENTTYPES_MAX_EVENTS": 1000,
                "iot_gateway.AGENTTYPES_TIMEOUT_S": 5,
                "iot_gateway.AGENTTYPES_MESSAGE_FORMAT": 0,
                "iot_gateway.AGENTTYPES_SEND_INITIAL_UPDATE": true,
                "iot_gateway.MQTT_CLIENT_ENABLE_LAST_WILL": false,
                "iot_gateway.MQTT_CLIENT_ENABLE_WRITE_TOPIC": false,
                "iot_gateway.MQTT_TLS_VERSION": 0,
                "iot_gateway.MQTT_CLIENT_CERTIFICATE": false
            }
            """;

            var agent = JsonSerializer.Deserialize<MqttClientAgent>(json, KepJsonContext.Default.MqttClientAgent);

            Assert.NotNull(agent);
            Assert.Equal("TestMqttAgent", agent.Name);
            Assert.Equal("A test MQTT client agent", agent.Description);
            Assert.Equal("MQTT Client", agent.AgentType);
            Assert.True(agent.Enabled);
            Assert.False(agent.IgnoreQualityChanges);
            Assert.Equal("tcp://localhost:1883", agent.Url);
            Assert.Equal("iotgateway", agent.Topic);
            Assert.Equal(MqttQos.AtLeastOnce, agent.Qos);
            Assert.Equal(IotPublishType.Interval, agent.PublishType);
            Assert.Equal(10000, agent.RateMs);
            Assert.Equal(IotPublishFormat.NarrowFormat, agent.PublishFormat);
            Assert.Equal(1000, agent.MaxEventsPerPublish);
            Assert.Equal(5, agent.TransactionTimeoutS);
            Assert.Equal(IotMessageFormat.StandardTemplate, agent.MessageFormat);
            Assert.True(agent.SendInitialUpdate);
            Assert.False(agent.EnableLastWill);
            Assert.False(agent.EnableWriteTopic);
            Assert.Equal(MqttTlsVersion.Default, agent.TlsVersion);

            Assert.False(agent.ClientCertificate);
        }

        [Fact]
        public void MqttClientAgent_SetProperties_ShouldUpdateDynamicProperties()
        {
            var agent = new MqttClientAgent("TestAgent");

            agent.Url = "tcp://broker:1883";
            agent.Topic = "test/topic";
            agent.Qos = MqttQos.ExactlyOnce;
            agent.Enabled = true;
            agent.PublishType = IotPublishType.OnDataChange;
            agent.RateMs = 5000;
            agent.TlsVersion = MqttTlsVersion.V1_2;
            agent.EnableLastWill = true;
            agent.LastWillTopic = "lwt/topic";
            agent.LastWillMessage = "offline";
            agent.EnableWriteTopic = true;
            agent.WriteTopic = "write/topic";

            Assert.Equal("tcp://broker:1883", agent.Url);
            Assert.Equal("test/topic", agent.Topic);
            Assert.Equal(MqttQos.ExactlyOnce, agent.Qos);
            Assert.True(agent.Enabled);
            Assert.Equal(IotPublishType.OnDataChange, agent.PublishType);
            Assert.Equal(5000, agent.RateMs);
            Assert.Equal(MqttTlsVersion.V1_2, agent.TlsVersion);
            Assert.True(agent.EnableLastWill);
            Assert.Equal("lwt/topic", agent.LastWillTopic);
            Assert.Equal("offline", agent.LastWillMessage);
            Assert.True(agent.EnableWriteTopic);
            Assert.Equal("write/topic", agent.WriteTopic);
        }

        [Fact]
        public void MqttClientAgent_RoundTrip_ShouldPreserveProperties()
        {
            var agent = new MqttClientAgent("RoundTripAgent");
            agent.Enabled = true;
            agent.Url = "tcp://localhost:1883";
            agent.Topic = "test";
            agent.Qos = MqttQos.AtLeastOnce;
            agent.PublishType = IotPublishType.Interval;
            agent.RateMs = 10000;

            var json = JsonSerializer.Serialize(agent, KepJsonContext.Default.MqttClientAgent);
            var deserialized = JsonSerializer.Deserialize<MqttClientAgent>(json, KepJsonContext.Default.MqttClientAgent);

            Assert.NotNull(deserialized);
            Assert.Equal("RoundTripAgent", deserialized.Name);
            Assert.True(deserialized.Enabled);
            Assert.Equal("tcp://localhost:1883", deserialized.Url);
            Assert.Equal("test", deserialized.Topic);
            Assert.Equal(MqttQos.AtLeastOnce, deserialized.Qos);
            Assert.Equal(IotPublishType.Interval, deserialized.PublishType);
            Assert.Equal(10000, deserialized.RateMs);
        }

        [Fact]
        public void MqttClientAgent_WithIotItems_ShouldDeserializeChildren()
        {
            var json = """
            {
                "common.ALLTYPES_NAME": "AgentWithItems",
                "iot_gateway.AGENTTYPES_ENABLED": true,
                "iot_items": [
                    {
                        "common.ALLTYPES_NAME": "Item1",
                        "iot_gateway.IOT_ITEM_SERVER_TAG": "Channel1.Device1.Tag1",
                        "iot_gateway.IOT_ITEM_ENABLED": true,
                        "iot_gateway.IOT_ITEM_SCAN_RATE_MS": 1000
                    }
                ]
            }
            """;

            var agent = JsonSerializer.Deserialize<MqttClientAgent>(json, KepJsonContext.Default.MqttClientAgent);

            Assert.NotNull(agent);
            Assert.NotNull(agent.IotItems);
            Assert.Single(agent.IotItems);
            Assert.Equal("Item1", agent.IotItems[0].Name);
            Assert.Equal("Channel1.Device1.Tag1", agent.IotItems[0].ServerTag);
            Assert.True(agent.IotItems[0].Enabled);
            Assert.Equal(1000, agent.IotItems[0].ScanRateMs);
        }

        #endregion

        #region RestClientAgent Tests

        [Fact]
        public void RestClientAgent_Deserialize_ShouldPopulateProperties()
        {
            var json = """
            {
                "common.ALLTYPES_NAME": "TestRestClient",
                "iot_gateway.AGENTTYPES_TYPE": "REST Client",
                "iot_gateway.AGENTTYPES_ENABLED": true,
                "iot_gateway.REST_CLIENT_URL": "http://127.0.0.1:3000",
                "iot_gateway.REST_CLIENT_METHOD": 0,
                "iot_gateway.AGENTTYPES_PUBLISH_TYPE": 1,
                "iot_gateway.AGENTTYPES_RATE_MS": 5000,
                "iot_gateway.AGENTTYPES_PUBLISH_FORMAT": 1,
                "iot_gateway.AGENTTYPES_MESSAGE_FORMAT": 1,
                "iot_gateway.REST_CLIENT_PUBLISH_MEDIA_TYPE": 0,
                "iot_gateway.BUFFER_ON_FAILED_PUBLISH": true,
                "iot_gateway.AGENTTYPES_SEND_INITIAL_UPDATE": true
            }
            """;

            var agent = JsonSerializer.Deserialize<RestClientAgent>(json, KepJsonContext.Default.RestClientAgent);

            Assert.NotNull(agent);
            Assert.Equal("TestRestClient", agent.Name);
            Assert.Equal("REST Client", agent.AgentType);
            Assert.True(agent.Enabled);
            Assert.Equal("http://127.0.0.1:3000", agent.Url);
            Assert.Equal(RestClientHttpMethod.Post, agent.HttpMethod);
            Assert.Equal(IotPublishType.OnDataChange, agent.PublishType);
            Assert.Equal(5000, agent.RateMs);
            Assert.Equal(IotPublishFormat.WideFormat, agent.PublishFormat);
            Assert.Equal(IotMessageFormat.AdvancedTemplate, agent.MessageFormat);
            Assert.Equal(RestClientMediaType.ApplicationJson, agent.PublishMediaType);
            Assert.True(agent.BufferOnFailedPublish);
            Assert.True(agent.SendInitialUpdate);
        }

        [Fact]
        public void RestClientAgent_SetProperties_ShouldUpdateDynamicProperties()
        {
            var agent = new RestClientAgent("TestAgent");

            agent.Url = "https://api.example.com";
            agent.HttpMethod = RestClientHttpMethod.Put;
            agent.HttpHeader = "Authorization: Bearer token123";
            agent.PublishMediaType = RestClientMediaType.TextPlain;
            agent.Username = "user";
            agent.Password = "pass";
            agent.BufferOnFailedPublish = false;

            Assert.Equal("https://api.example.com", agent.Url);
            Assert.Equal(RestClientHttpMethod.Put, agent.HttpMethod);
            Assert.Equal("Authorization: Bearer token123", agent.HttpHeader);
            Assert.Equal(RestClientMediaType.TextPlain, agent.PublishMediaType);
            Assert.Equal("user", agent.Username);
            Assert.Equal("pass", agent.Password);
            Assert.False(agent.BufferOnFailedPublish);
        }

        [Fact]
        public void RestClientAgent_RoundTrip_ShouldPreserveProperties()
        {
            var agent = new RestClientAgent("RoundTripRestClient");
            agent.Enabled = true;
            agent.Url = "https://api.example.com";
            agent.HttpMethod = RestClientHttpMethod.Put;
            agent.PublishType = IotPublishType.OnDataChange;
            agent.RateMs = 5000;
            agent.PublishFormat = IotPublishFormat.WideFormat;
            agent.BufferOnFailedPublish = true;

            var json = JsonSerializer.Serialize(agent, KepJsonContext.Default.RestClientAgent);
            var deserialized = JsonSerializer.Deserialize<RestClientAgent>(json, KepJsonContext.Default.RestClientAgent);

            Assert.NotNull(deserialized);
            Assert.Equal("RoundTripRestClient", deserialized.Name);
            Assert.True(deserialized.Enabled);
            Assert.Equal("https://api.example.com", deserialized.Url);
            Assert.Equal(RestClientHttpMethod.Put, deserialized.HttpMethod);
            Assert.Equal(IotPublishType.OnDataChange, deserialized.PublishType);
            Assert.Equal(5000, deserialized.RateMs);
            Assert.Equal(IotPublishFormat.WideFormat, deserialized.PublishFormat);
            Assert.True(deserialized.BufferOnFailedPublish);
        }

        #endregion

        #region RestServerAgent Tests

        [Fact]
        public void RestServerAgent_Deserialize_ShouldPopulateProperties()
        {
            var json = """
            {
                "common.ALLTYPES_NAME": "TestRestServer",
                "iot_gateway.AGENTTYPES_TYPE": "REST Server",
                "iot_gateway.AGENTTYPES_ENABLED": true,
                "iot_gateway.IGNORE_QUALITY_CHANGES": false,
                "iot_gateway.REST_SERVER_PORT_NUMBER": 39320,
                "iot_gateway.REST_SERVER_USE_HTTPS": true,
                "iot_gateway.REST_SERVER_ENABLE_WRITE_ENDPOINT": false,
                "iot_gateway.REST_SERVER_ALLOW_ANONYMOUS_LOGIN": false
            }
            """;

            var agent = JsonSerializer.Deserialize<RestServerAgent>(json, KepJsonContext.Default.RestServerAgent);

            Assert.NotNull(agent);
            Assert.Equal("TestRestServer", agent.Name);
            Assert.Equal("REST Server", agent.AgentType);
            Assert.True(agent.Enabled);
            Assert.False(agent.IgnoreQualityChanges);
            Assert.Equal(39320, agent.PortNumber);
            Assert.True(agent.UseHttps);
            Assert.False(agent.EnableWriteEndpoint);
            Assert.False(agent.AllowAnonymousLogin);
        }

        [Fact]
        public void RestServerAgent_ShouldNotHavePublishProperties()
        {
            // Verify that RestServerAgent inherits from IotAgent directly, not PublishingIotAgent
            Assert.False(typeof(RestServerAgent).IsSubclassOf(typeof(PublishingIotAgent)));
            Assert.True(typeof(RestServerAgent).IsSubclassOf(typeof(IotAgent)));
        }

        [Fact]
        public void RestServerAgent_SetProperties_ShouldUpdateDynamicProperties()
        {
            var agent = new RestServerAgent("TestServer");

            agent.PortNumber = 8080;
            agent.UseHttps = false;
            agent.EnableWriteEndpoint = true;
            agent.AllowAnonymousLogin = true;
            agent.CorsAllowedOrigins = "http://localhost:3000,http://example.com";
            agent.Enabled = true;

            Assert.Equal(8080, agent.PortNumber);
            Assert.False(agent.UseHttps);
            Assert.True(agent.EnableWriteEndpoint);
            Assert.True(agent.AllowAnonymousLogin);
            Assert.Equal("http://localhost:3000,http://example.com", agent.CorsAllowedOrigins);
            Assert.True(agent.Enabled);
        }

        [Fact]
        public void RestServerAgent_RoundTrip_ShouldPreserveProperties()
        {
            var agent = new RestServerAgent("RoundTripRestServer");
            agent.Enabled = true;
            agent.PortNumber = 8080;
            agent.UseHttps = false;
            agent.EnableWriteEndpoint = true;
            agent.AllowAnonymousLogin = true;
            agent.CorsAllowedOrigins = "http://localhost:3000";

            var json = JsonSerializer.Serialize(agent, KepJsonContext.Default.RestServerAgent);
            var deserialized = JsonSerializer.Deserialize<RestServerAgent>(json, KepJsonContext.Default.RestServerAgent);

            Assert.NotNull(deserialized);
            Assert.Equal("RoundTripRestServer", deserialized.Name);
            Assert.True(deserialized.Enabled);
            Assert.Equal(8080, deserialized.PortNumber);
            Assert.False(deserialized.UseHttps);
            Assert.True(deserialized.EnableWriteEndpoint);
            Assert.True(deserialized.AllowAnonymousLogin);
            Assert.Equal("http://localhost:3000", deserialized.CorsAllowedOrigins);
        }

        #endregion

        #region IotItem Tests

        [Fact]
        public void IotItem_Deserialize_ShouldPopulateProperties()
        {
            var json = """
            {
                "common.ALLTYPES_NAME": "TestItem",
                "common.ALLTYPES_DESCRIPTION": "A test IoT item",
                "iot_gateway.IOT_ITEM_SERVER_TAG": "Channel1.Device1.Tag1",
                "iot_gateway.IOT_ITEM_USE_SCAN_RATE": true,
                "iot_gateway.IOT_ITEM_SCAN_RATE_MS": 500,
                "iot_gateway.IOT_ITEM_SEND_EVERY_SCAN": false,
                "iot_gateway.IOT_ITEM_DEADBAND_PERCENT": 2.5,
                "iot_gateway.IOT_ITEM_ENABLED": true,
                "iot_gateway.IOT_ITEM_DATA_TYPE": -1
            }
            """;

            var item = JsonSerializer.Deserialize<IotItem>(json, KepJsonContext.Default.IotItem);

            Assert.NotNull(item);
            Assert.Equal("TestItem", item.Name);
            Assert.Equal("A test IoT item", item.Description);
            Assert.Equal("Channel1.Device1.Tag1", item.ServerTag);
            Assert.True(item.UseScanRate);
            Assert.Equal(500, item.ScanRateMs);
            Assert.False(item.PublishEveryScan);
            Assert.Equal(2.5, item.DeadbandPercent);
            Assert.True(item.Enabled);
            Assert.Equal(IotItemDataType.Default, item.DataType);
        }

        [Fact]
        public void IotItem_SetProperties_ShouldUpdateDynamicProperties()
        {
            var item = new IotItem("Item1");

            item.ServerTag = "Channel1.Device1.Tag2";
            item.UseScanRate = false;
            item.ScanRateMs = 2000;
            item.PublishEveryScan = true;
            item.DeadbandPercent = 5.0;
            item.Enabled = false;
            item.DataType = IotItemDataType.Float;

            Assert.Equal("Channel1.Device1.Tag2", item.ServerTag);
            Assert.False(item.UseScanRate);
            Assert.Equal(2000, item.ScanRateMs);
            Assert.True(item.PublishEveryScan);
            Assert.Equal(5.0, item.DeadbandPercent);
            Assert.False(item.Enabled);
            Assert.Equal(IotItemDataType.Float, item.DataType);
        }

        [Fact]
        public void IotItem_RoundTrip_ShouldPreserveProperties()
        {
            var item = new IotItem("RoundTripItem");
            item.ServerTag = "Ch1.Dev1.Tag1";
            item.Enabled = true;
            item.ScanRateMs = 1000;
            item.DataType = IotItemDataType.Double;

            var json = JsonSerializer.Serialize(item, KepJsonContext.Default.IotItem);
            var deserialized = JsonSerializer.Deserialize<IotItem>(json, KepJsonContext.Default.IotItem);

            Assert.NotNull(deserialized);
            Assert.Equal("RoundTripItem", deserialized.Name);
            Assert.Equal("Ch1.Dev1.Tag1", deserialized.ServerTag);
            Assert.True(deserialized.Enabled);
            Assert.Equal(1000, deserialized.ScanRateMs);
            Assert.Equal(IotItemDataType.Double, deserialized.DataType);
        }

        #endregion

        #region Inheritance Tests

        [Fact]
        public void MqttClientAgent_ShouldInheritFromPublishingIotAgent()
        {
            Assert.True(typeof(MqttClientAgent).IsSubclassOf(typeof(PublishingIotAgent)));
            Assert.True(typeof(MqttClientAgent).IsSubclassOf(typeof(IotAgent)));
            Assert.True(typeof(MqttClientAgent).IsSubclassOf(typeof(NamedEntity)));
        }

        [Fact]
        public void RestClientAgent_ShouldInheritFromPublishingIotAgent()
        {
            Assert.True(typeof(RestClientAgent).IsSubclassOf(typeof(PublishingIotAgent)));
            Assert.True(typeof(RestClientAgent).IsSubclassOf(typeof(IotAgent)));
            Assert.True(typeof(RestClientAgent).IsSubclassOf(typeof(NamedEntity)));
        }

        [Fact]
        public void RestServerAgent_ShouldInheritFromIotAgentDirectly()
        {
            Assert.True(typeof(RestServerAgent).IsSubclassOf(typeof(IotAgent)));
            Assert.True(typeof(RestServerAgent).IsSubclassOf(typeof(NamedEntity)));
            Assert.False(typeof(RestServerAgent).IsSubclassOf(typeof(PublishingIotAgent)));
        }

        [Fact]
        public void IotItem_ShouldInheritFromNamedEntity()
        {
            Assert.True(typeof(IotItem).IsSubclassOf(typeof(NamedEntity)));
            Assert.False(typeof(IotItem).IsSubclassOf(typeof(IotAgent)));
        }

        #endregion

        #region Collection Deserialization Tests

        [Fact]
        public void MqttClientAgentCollection_Deserialize_ShouldWork()
        {
            var json = """
            [
                {
                    "common.ALLTYPES_NAME": "Agent1",
                    "iot_gateway.AGENTTYPES_ENABLED": true,
                    "iot_gateway.MQTT_CLIENT_URL": "tcp://localhost:1883"
                },
                {
                    "common.ALLTYPES_NAME": "Agent2",
                    "iot_gateway.AGENTTYPES_ENABLED": false,
                    "iot_gateway.MQTT_CLIENT_URL": "tcp://broker:1883"
                }
            ]
            """;

            var agents = JsonSerializer.Deserialize<List<MqttClientAgent>>(json, KepJsonContext.Default.ListMqttClientAgent);

            Assert.NotNull(agents);
            Assert.Equal(2, agents.Count);
            Assert.Equal("Agent1", agents[0].Name);
            Assert.Equal("Agent2", agents[1].Name);
            Assert.True(agents[0].Enabled);
            Assert.False(agents[1].Enabled);
        }

        [Fact]
        public void IotItemCollection_Deserialize_ShouldWork()
        {
            var json = """
            [
                {
                    "common.ALLTYPES_NAME": "Item1",
                    "iot_gateway.IOT_ITEM_SERVER_TAG": "Ch1.Dev1.Tag1",
                    "iot_gateway.IOT_ITEM_ENABLED": true
                },
                {
                    "common.ALLTYPES_NAME": "Item2",
                    "iot_gateway.IOT_ITEM_SERVER_TAG": "Ch1.Dev1.Tag2",
                    "iot_gateway.IOT_ITEM_ENABLED": false
                }
            ]
            """;

            var items = JsonSerializer.Deserialize<List<IotItem>>(json, KepJsonContext.Default.ListIotItem);

            Assert.NotNull(items);
            Assert.Equal(2, items.Count);
            Assert.Equal("Item1", items[0].Name);
            Assert.Equal("Item2", items[1].Name);
            Assert.True(items[0].Enabled);
            Assert.False(items[1].Enabled);
        }

        #endregion
    }
}
