using System.Text.Json;
using Kepware.Api.Model;
using Kepware.Api.Serializer;
using Xunit;

namespace Kepware.Api.Test.Model.IotGateway
{
    public class IotGatewayProjectIntegrationTests
    {
        #region Project Deserialization

        [Fact]
        public void ProjectWithIotGateway_ShouldDeserializeFromJson()
        {
            var json = @"{
                ""PROJECT_ID"": 42,
                ""common.ALLTYPES_DESCRIPTION"": ""Test project"",
                ""_iot_gateway"": [
                    {
                        ""common.ALLTYPES_NAME"": ""_IoT_Gateway"",
                        ""mqtt_clients"": [
                            {
                                ""common.ALLTYPES_NAME"": ""MqttAgent1"",
                                ""common.ALLTYPES_DESCRIPTION"": ""Test MQTT agent"",
                                ""iot_gateway.AGENTTYPES_ENABLED"": true,
                                ""iot_gateway.MQTT_CLIENT_URL"": ""tcp://broker:1883"",
                                ""iot_items"": [
                                    {
                                        ""common.ALLTYPES_NAME"": ""Item1"",
                                        ""iot_gateway.IOT_ITEM_SERVER_TAG"": ""Channel1.Device1.Tag1"",
                                        ""iot_gateway.IOT_ITEM_ENABLED"": true
                                    },
                                    {
                                        ""common.ALLTYPES_NAME"": ""_MqttItem2"",
                                        ""iot_gateway.IOT_ITEM_SERVER_TAG"": ""Channel1.Device1.Tag5"",
                                        ""iot_gateway.IOT_ITEM_ENABLED"": false
                                    }
                                ]
                            }
                        ],
                        ""rest_clients"": [
                            {
                                ""common.ALLTYPES_NAME"": ""RestClient1"",
                                ""iot_gateway.AGENTTYPES_ENABLED"": true,
                                ""iot_gateway.REST_CLIENT_URL"": ""https://api.example.com"",
                                ""iot_items"": [
                                    {
                                        ""common.ALLTYPES_NAME"": ""_RestClientItem1"",
                                        ""iot_gateway.IOT_ITEM_SERVER_TAG"": ""Channel1.Device1.Tag2"",
                                        ""iot_gateway.IOT_ITEM_ENABLED"": true
                                    },
                                    {
                                        ""common.ALLTYPES_NAME"": ""RestClientItem2"",
                                        ""iot_gateway.IOT_ITEM_SERVER_TAG"": ""Channel1.Device1.Tag3"",
                                        ""iot_gateway.IOT_ITEM_ENABLED"": false
                                    }
                                ]
                            }
                        ],
                        ""rest_servers"": [
                            {
                                ""common.ALLTYPES_NAME"": ""RestServer1"",
                                ""iot_gateway.AGENTTYPES_ENABLED"": false,
                                ""iot_gateway.REST_SERVER_PORT_NUMBER"": 39320,
                                ""iot_items"": [
                                    {
                                        ""common.ALLTYPES_NAME"": ""RestServerItem1"",
                                        ""iot_gateway.IOT_ITEM_SERVER_TAG"": ""Channel1.Device1.Tag4"",
                                        ""iot_gateway.IOT_ITEM_ENABLED"": true
                                    },
                                    {
                                        ""common.ALLTYPES_NAME"": ""_RestServerItem2"",
                                        ""iot_gateway.IOT_ITEM_SERVER_TAG"": ""Channel2.Device2.Tag1"",
                                        ""iot_gateway.IOT_ITEM_ENABLED"": false
                                    }
                                ]
                            }
                        ]
                    }
                ]
            }";

            var project = JsonSerializer.Deserialize(json, KepJsonContext.Default.Project);

            Assert.NotNull(project);
            Assert.Equal(42, project.ProjectId);
            Assert.Equal("Test project", project.Description);

            Assert.NotNull(project.IotGateway);
            Assert.False(project.IotGateway.IsEmpty);

            // MQTT Client
            Assert.NotNull(project.IotGateway.MqttClientAgents);
            Assert.Single(project.IotGateway.MqttClientAgents);
            var mqtt = project.IotGateway.MqttClientAgents[0];
            Assert.Equal("MqttAgent1", mqtt.Name);
            Assert.True(mqtt.Enabled);
            Assert.Equal("tcp://broker:1883", mqtt.Url);

            // MQTT Client IoT Items
            Assert.NotNull(mqtt.IotItems);
            Assert.Equal(2, mqtt.IotItems.Count);
            Assert.Equal("Item1", mqtt.IotItems[0].Name);
            Assert.Equal("Channel1.Device1.Tag1", mqtt.IotItems[0].ServerTag);
            Assert.True(mqtt.IotItems[0].Enabled);
            Assert.Equal("_MqttItem2", mqtt.IotItems[1].Name);
            Assert.Equal("Channel1.Device1.Tag5", mqtt.IotItems[1].ServerTag);
            Assert.False(mqtt.IotItems[1].Enabled);

            // REST Client
            Assert.NotNull(project.IotGateway.RestClientAgents);
            Assert.Single(project.IotGateway.RestClientAgents);
            var restClient = project.IotGateway.RestClientAgents[0];
            Assert.Equal("RestClient1", restClient.Name);
            Assert.True(restClient.Enabled);

            // REST Client IoT Items
            Assert.NotNull(restClient.IotItems);
            Assert.Equal(2, restClient.IotItems.Count);
            Assert.Equal("_RestClientItem1", restClient.IotItems[0].Name);
            Assert.Equal("Channel1.Device1.Tag2", restClient.IotItems[0].ServerTag);
            Assert.True(restClient.IotItems[0].Enabled);
            Assert.Equal("RestClientItem2", restClient.IotItems[1].Name);
            Assert.Equal("Channel1.Device1.Tag3", restClient.IotItems[1].ServerTag);
            Assert.False(restClient.IotItems[1].Enabled);

            // REST Server
            Assert.NotNull(project.IotGateway.RestServerAgents);
            Assert.Single(project.IotGateway.RestServerAgents);
            var restServer = project.IotGateway.RestServerAgents[0];
            Assert.Equal("RestServer1", restServer.Name);
            Assert.False(restServer.Enabled);
            Assert.Equal(39320, restServer.PortNumber);

            // REST Server IoT Items
            Assert.NotNull(restServer.IotItems);
            Assert.Equal(2, restServer.IotItems.Count);
            Assert.Equal("RestServerItem1", restServer.IotItems[0].Name);
            Assert.Equal("Channel1.Device1.Tag4", restServer.IotItems[0].ServerTag);
            Assert.True(restServer.IotItems[0].Enabled);
            Assert.Equal("_RestServerItem2", restServer.IotItems[1].Name);
            Assert.Equal("Channel2.Device2.Tag1", restServer.IotItems[1].ServerTag);
            Assert.False(restServer.IotItems[1].Enabled);
        }

        [Fact]
        public void ProjectWithoutIotGateway_ShouldDeserializeCorrectly()
        {
            var json = @"{
                ""PROJECT_ID"": 1,
                ""common.ALLTYPES_DESCRIPTION"": ""No IoT""
            }";

            var project = JsonSerializer.Deserialize(json, KepJsonContext.Default.Project);

            Assert.NotNull(project);
            Assert.Null(project.IotGateway);
        }

        [Fact]
        public void ProjectWithEmptyIotGateway_ShouldNotSerializeIotGateway()
        {
            var project = new Project
            {
                IotGateway = new IotGatewayContainer()
            };

            var json = JsonSerializer.Serialize(project, KepJsonContext.Default.Project);

            Assert.DoesNotContain("_iot_gateway", json);
        }

        #endregion

        #region Round-trip Serialization

        [Fact]
        public void ProjectWithIotGateway_ShouldRoundTripSerialize()
        {
            var project = new Project();
            project.IotGateway = new IotGatewayContainer
            {
                MqttClientAgents = new MqttClientAgentCollection
                {
                    new MqttClientAgent
                    {
                        Name = "MqttAgent1",
                        Enabled = true,
                        Url = "tcp://broker:1883",
                        Topic = "test/topic",
                        IotItems = new IotItemCollection
                        {
                            new IotItem
                            {
                                Name = "Item1",
                                ServerTag = "Channel1.Device1.Tag1",
                                Enabled = true
                            },
                            new IotItem
                            {
                                Name = "_MqttItem2",
                                ServerTag = "Channel1.Device1.Tag5",
                                Enabled = false
                            }
                        }
                    }
                },
                RestClientAgents = new RestClientAgentCollection
                {
                    new RestClientAgent
                    {
                        Name = "RestClient1",
                        Enabled = true,
                        Url = "https://api.example.com",
                        IotItems = new IotItemCollection
                        {
                            new IotItem
                            {
                                Name = "_RestClientItem1",
                                ServerTag = "Channel1.Device1.Tag2",
                                Enabled = true
                            },
                            new IotItem
                            {
                                Name = "RestClientItem2",
                                ServerTag = "Channel1.Device1.Tag3",
                                Enabled = false
                            }
                        }
                    }
                },
                RestServerAgents = new RestServerAgentCollection
                {
                    new RestServerAgent
                    {
                        Name = "RestServer1",
                        Enabled = false,
                        PortNumber = 39320,
                        IotItems = new IotItemCollection
                        {
                            new IotItem
                            {
                                Name = "RestServerItem1",
                                ServerTag = "Channel1.Device1.Tag4",
                                Enabled = true
                            },
                            new IotItem
                            {
                                Name = "_RestServerItem2",
                                ServerTag = "Channel2.Device2.Tag1",
                                Enabled = false
                            }
                        }
                    }
                }
            };

            var json = JsonSerializer.Serialize(project, KepJsonContext.Default.Project);
            var deserialized = JsonSerializer.Deserialize(json, KepJsonContext.Default.Project);

            Assert.NotNull(deserialized);
            Assert.NotNull(deserialized.IotGateway);
            Assert.False(deserialized.IotGateway.IsEmpty);

            // MQTT
            Assert.NotNull(deserialized.IotGateway.MqttClientAgents);
            Assert.Single(deserialized.IotGateway.MqttClientAgents);
            Assert.Equal("MqttAgent1", deserialized.IotGateway.MqttClientAgents[0].Name);
            Assert.Equal("tcp://broker:1883", deserialized.IotGateway.MqttClientAgents[0].Url);
            Assert.Equal("test/topic", deserialized.IotGateway.MqttClientAgents[0].Topic);

            // MQTT Items
            Assert.NotNull(deserialized.IotGateway.MqttClientAgents[0].IotItems);
            Assert.Equal(2, deserialized.IotGateway.MqttClientAgents[0].IotItems!.Count);
            Assert.Equal("Item1", deserialized.IotGateway.MqttClientAgents[0].IotItems![0].Name);
            Assert.Equal("_MqttItem2", deserialized.IotGateway.MqttClientAgents[0].IotItems![1].Name);
            Assert.False(deserialized.IotGateway.MqttClientAgents[0].IotItems![1].Enabled);

            // REST Client
            Assert.NotNull(deserialized.IotGateway.RestClientAgents);
            Assert.Single(deserialized.IotGateway.RestClientAgents);
            Assert.Equal("RestClient1", deserialized.IotGateway.RestClientAgents[0].Name);

            // REST Client Items
            Assert.NotNull(deserialized.IotGateway.RestClientAgents[0].IotItems);
            Assert.Equal(2, deserialized.IotGateway.RestClientAgents[0].IotItems!.Count);
            Assert.Equal("_RestClientItem1", deserialized.IotGateway.RestClientAgents[0].IotItems![0].Name);
            Assert.Equal("Channel1.Device1.Tag2", deserialized.IotGateway.RestClientAgents[0].IotItems![0].ServerTag);
            Assert.Equal("RestClientItem2", deserialized.IotGateway.RestClientAgents[0].IotItems![1].Name);
            Assert.Equal("Channel1.Device1.Tag3", deserialized.IotGateway.RestClientAgents[0].IotItems![1].ServerTag);

            // REST Server
            Assert.NotNull(deserialized.IotGateway.RestServerAgents);
            Assert.Single(deserialized.IotGateway.RestServerAgents);
            Assert.Equal("RestServer1", deserialized.IotGateway.RestServerAgents[0].Name);
            Assert.Equal(39320, deserialized.IotGateway.RestServerAgents[0].PortNumber);

            // REST Server Items
            Assert.NotNull(deserialized.IotGateway.RestServerAgents[0].IotItems);
            Assert.Equal(2, deserialized.IotGateway.RestServerAgents[0].IotItems!.Count);
            Assert.Equal("RestServerItem1", deserialized.IotGateway.RestServerAgents[0].IotItems![0].Name);
            Assert.Equal("Channel1.Device1.Tag4", deserialized.IotGateway.RestServerAgents[0].IotItems![0].ServerTag);
            Assert.Equal("_RestServerItem2", deserialized.IotGateway.RestServerAgents[0].IotItems![1].Name);
            Assert.Equal("Channel2.Device2.Tag1", deserialized.IotGateway.RestServerAgents[0].IotItems![1].ServerTag);
        }

        #endregion

        #region Project.IsEmpty

        [Fact]
        public void Project_IsEmpty_WithNullIotGateway_ShouldBeTrue()
        {
            var project = new Project();
            Assert.True(project.IsEmpty);
        }

        [Fact]
        public void Project_IsEmpty_WithEmptyIotGateway_ShouldBeTrue()
        {
            var project = new Project
            {
                IotGateway = new IotGatewayContainer()
            };
            Assert.True(project.IsEmpty);
        }

        [Fact]
        public void Project_IsEmpty_WithIotGatewayAgents_ShouldBeFalse()
        {
            var project = new Project
            {
                IotGateway = new IotGatewayContainer
                {
                    MqttClientAgents = new MqttClientAgentCollection
                    {
                        new MqttClientAgent { Name = "Agent1" }
                    }
                }
            };
            Assert.False(project.IsEmpty);
        }

        #endregion

        #region JsonProjectRoot

        [Fact]
        public void FullProjectJson_ShouldDeserializeWithIotGatewayInProjectRoot()
        {
            var json = @"{
                ""project"": {
                    ""PROJECT_ID"": 100,
                    ""common.ALLTYPES_DESCRIPTION"": ""Full project"",
                    ""_iot_gateway"": [
                        {
                            ""common.ALLTYPES_NAME"": ""_IoT_Gateway"",
                            ""mqtt_clients"": [
                                {
                                    ""common.ALLTYPES_NAME"": ""Mqtt1"",
                                    ""iot_gateway.AGENTTYPES_ENABLED"": true,
                                    ""iot_items"": [
                                        {
                                            ""common.ALLTYPES_NAME"": ""_MqttRootItem1"",
                                            ""iot_gateway.IOT_ITEM_SERVER_TAG"": ""Channel1.Device1.Tag1"",
                                            ""iot_gateway.IOT_ITEM_ENABLED"": true
                                        }
                                    ]
                                }
                            ],
                            ""rest_servers"": [
                                {
                                    ""common.ALLTYPES_NAME"": ""RestSrv1"",
                                    ""iot_gateway.REST_SERVER_PORT_NUMBER"": 8080,
                                    ""iot_items"": [
                                        {
                                            ""common.ALLTYPES_NAME"": ""_RestSrvItem1"",
                                            ""iot_gateway.IOT_ITEM_SERVER_TAG"": ""Channel1.Device1.Tag2"",
                                            ""iot_gateway.IOT_ITEM_ENABLED"": false
                                        }
                                    ]
                                }
                            ]
                        }
                    ],
                    ""channels"": [
                        {
                            ""common.ALLTYPES_NAME"": ""Channel1"",
                            ""servermain.MULTIPLE_TYPES_DEVICE_DRIVER"": ""Simulator""
                        }
                    ]
                }
            }";

            var root = JsonSerializer.Deserialize(json, KepJsonContext.Default.JsonProjectRoot);

            Assert.NotNull(root);
            Assert.NotNull(root.Project);
            Assert.Equal(100, root.Project.ProjectId);

            // Channels
            Assert.NotNull(root.Project.Channels);
            Assert.Single(root.Project.Channels);
            Assert.Equal("Channel1", root.Project.Channels[0].Name);

            // IoT Gateway
            Assert.NotNull(root.Project.IotGateway);
            Assert.NotNull(root.Project.IotGateway.MqttClientAgents);
            Assert.Single(root.Project.IotGateway.MqttClientAgents);
            Assert.Equal("Mqtt1", root.Project.IotGateway.MqttClientAgents[0].Name);
            Assert.True(root.Project.IotGateway.MqttClientAgents[0].Enabled);
            Assert.NotNull(root.Project.IotGateway.MqttClientAgents[0].IotItems);
            Assert.Single(root.Project.IotGateway.MqttClientAgents[0].IotItems!);
            Assert.Equal("_MqttRootItem1", root.Project.IotGateway.MqttClientAgents[0].IotItems![0].Name);
            Assert.Equal("Channel1.Device1.Tag1", root.Project.IotGateway.MqttClientAgents[0].IotItems![0].ServerTag);

            Assert.NotNull(root.Project.IotGateway.RestServerAgents);
            Assert.Single(root.Project.IotGateway.RestServerAgents);
            Assert.Equal("RestSrv1", root.Project.IotGateway.RestServerAgents[0].Name);
            Assert.Equal(8080, root.Project.IotGateway.RestServerAgents[0].PortNumber);
            Assert.NotNull(root.Project.IotGateway.RestServerAgents[0].IotItems);
            Assert.Single(root.Project.IotGateway.RestServerAgents[0].IotItems!);
            Assert.Equal("_RestSrvItem1", root.Project.IotGateway.RestServerAgents[0].IotItems![0].Name);
            Assert.False(root.Project.IotGateway.RestServerAgents[0].IotItems![0].Enabled);
        }

        #endregion

        #region IotGatewayContainer.IsEmpty

        [Fact]
        public void IotGatewayContainer_IsEmpty_WithNullCollections_ShouldBeTrue()
        {
            var container = new IotGatewayContainer();
            Assert.True(container.IsEmpty);
        }

        [Fact]
        public void IotGatewayContainer_IsEmpty_WithEmptyCollections_ShouldBeTrue()
        {
            var container = new IotGatewayContainer
            {
                MqttClientAgents = new MqttClientAgentCollection(),
                RestClientAgents = new RestClientAgentCollection(),
                RestServerAgents = new RestServerAgentCollection()
            };
            Assert.True(container.IsEmpty);
        }

        [Fact]
        public void IotGatewayContainer_IsEmpty_WithAgents_ShouldBeFalse()
        {
            var container = new IotGatewayContainer
            {
                RestClientAgents = new RestClientAgentCollection
                {
                    new RestClientAgent { Name = "Agent1" }
                }
            };
            Assert.False(container.IsEmpty);
        }

        #endregion
    }
}
