using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Collections.Generic;
using System.Text.Json.Serialization.Metadata;
using Kepware.Api.Serializer;

namespace Kepware.Api.Model
{
    /// <summary>
    /// Custom converter to support both flat and nested (client_interfaces) project property formats.
    /// </summary>
    public class ProjectPropertiesJsonConverter : JsonConverter<Project>
    {
        public override Project? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.StartObject)
            {
                using var doc = JsonDocument.ParseValue(ref reader);
                var root = doc.RootElement;
                var project = new Project();

                // Detect if this is the nested format Project Properties from full project load (has "client_interfaces")  
                if (root.TryGetProperty("client_interfaces", out var clientInterfaces))
                {
                    foreach (var iface in clientInterfaces.EnumerateArray())
                    {
                        if (iface.TryGetProperty("common.ALLTYPES_NAME", out var nameProp))
                        {
                            var name = nameProp.GetString();
                            foreach (var prop in iface.EnumerateObject())
                            {
                                if (prop.Name != "common.ALLTYPES_NAME")
                                {
                                    // Map to ProjectProperties via dynamic property name  
                                    project.SetDynamicProperty($"{prop.Name}", prop.Value.Clone());
                                }
                            }
                        }
                    }
                }

                // Map other top-level properties (not client_interfaces)
                // This will include covering the flat format Project Properties (not full project load)

                foreach (var prop in root.EnumerateObject())
                {
                    if (prop.Name == "client_interfaces")
                    {
                        continue;
                    }

                    // Handle exposed properties supporting Project model
                    // This includes properties inherited from BaseEntity such as PROJECT_ID, DESCRIPTION, etc.
                    // TODO: Expand as needed for other known properties or consider common approach for BaseEntity properties
                    if (prop.Name == "channels")
                    {
                        var jsonTypeInfo = (JsonTypeInfo<List<Channel>>)options.GetTypeInfo(typeof(List<Channel>));
                        var channels = JsonSerializer.Deserialize(prop.Value.GetRawText(), jsonTypeInfo);
                        if (channels != null)
                        {
                            project.Channels = new ChannelCollection();
                            foreach (var channel in channels)
                            {
                                project.Channels.Add(channel);
                            }
                        }
                    }
                    else if (prop.Name == "_iot_gateway")
                    {
                        // _iot_gateway is an array containing a single wrapper object with
                        // "common.ALLTYPES_NAME": "_IoT_Gateway" and the agent collection arrays.
                        if (prop.Value.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var gwElement in prop.Value.EnumerateArray())
                            {
                                if (gwElement.ValueKind == JsonValueKind.Object)
                                {
                                    var container = new IotGatewayContainer();

                                    if (gwElement.TryGetProperty("mqtt_clients", out var mqttProp))
                                    {
                                        var mqttTypeInfo = (JsonTypeInfo<List<MqttClientAgent>>)options.GetTypeInfo(typeof(List<MqttClientAgent>));
                                        var agents = JsonSerializer.Deserialize(mqttProp.GetRawText(), mqttTypeInfo);
                                        if (agents != null)
                                        {
                                            container.MqttClientAgents = new MqttClientAgentCollection();
                                            foreach (var agent in agents) container.MqttClientAgents.Add(agent);
                                        }
                                    }

                                    if (gwElement.TryGetProperty("rest_clients", out var restClientProp))
                                    {
                                        var restClientTypeInfo = (JsonTypeInfo<List<RestClientAgent>>)options.GetTypeInfo(typeof(List<RestClientAgent>));
                                        var agents = JsonSerializer.Deserialize(restClientProp.GetRawText(), restClientTypeInfo);
                                        if (agents != null)
                                        {
                                            container.RestClientAgents = new RestClientAgentCollection();
                                            foreach (var agent in agents) container.RestClientAgents.Add(agent);
                                        }
                                    }

                                    if (gwElement.TryGetProperty("rest_servers", out var restServerProp))
                                    {
                                        var restServerTypeInfo = (JsonTypeInfo<List<RestServerAgent>>)options.GetTypeInfo(typeof(List<RestServerAgent>));
                                        var agents = JsonSerializer.Deserialize(restServerProp.GetRawText(), restServerTypeInfo);
                                        if (agents != null)
                                        {
                                            container.RestServerAgents = new RestServerAgentCollection();
                                            foreach (var agent in agents) container.RestServerAgents.Add(agent);
                                        }
                                    }

                                    project.IotGateway = container;
                                }
                            }
                        }
                    }
                    else if (prop.Name == "PROJECT_ID")
                    {
                        if (prop.Value.TryGetInt64(out var projectId))
                        {
                            project.ProjectId = projectId;
                        }
                        else
                        {
                            throw new JsonException($"Invalid value for PROJECT_ID: {prop.Value}");
                        }
                    }
                    else if (prop.Name == "common.ALLTYPES_DESCRIPTION")
                    {
                        project.Description = prop.Value.ToString();
                    }
                    else
                    {
                        project.SetDynamicProperty(prop.Name, prop.Value.Clone());
                    }
                }

                return project;
            }
            throw new JsonException("Expected start of object for Project");
        }

        public override void Write(Utf8JsonWriter writer, Project value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();

            // Emit channels specially (if present)
            if (value.Channels != null)
            {
                writer.WritePropertyName("channels");
                var jsonTypeInfo = (JsonTypeInfo<List<Channel>>)options.GetTypeInfo(typeof(List<Channel>));
                JsonSerializer.Serialize(writer, value.Channels.ToList(), jsonTypeInfo);
            }

            // Emit IoT Gateway container as array envelope (if present)
            if (value.IotGateway != null && !value.IotGateway.IsEmpty)
            {
                writer.WritePropertyName("_iot_gateway");
                writer.WriteStartArray();
                writer.WriteStartObject();

                writer.WriteString("common.ALLTYPES_NAME", "_IoT_Gateway");

                if (value.IotGateway.MqttClientAgents != null && value.IotGateway.MqttClientAgents.Count > 0)
                {
                    writer.WritePropertyName("mqtt_clients");
                    var mqttTypeInfo = (JsonTypeInfo<List<MqttClientAgent>>)options.GetTypeInfo(typeof(List<MqttClientAgent>));
                    JsonSerializer.Serialize(writer, value.IotGateway.MqttClientAgents.ToList(), mqttTypeInfo);
                }

                if (value.IotGateway.RestClientAgents != null && value.IotGateway.RestClientAgents.Count > 0)
                {
                    writer.WritePropertyName("rest_clients");
                    var restClientTypeInfo = (JsonTypeInfo<List<RestClientAgent>>)options.GetTypeInfo(typeof(List<RestClientAgent>));
                    JsonSerializer.Serialize(writer, value.IotGateway.RestClientAgents.ToList(), restClientTypeInfo);
                }

                if (value.IotGateway.RestServerAgents != null && value.IotGateway.RestServerAgents.Count > 0)
                {
                    writer.WritePropertyName("rest_servers");
                    var restServerTypeInfo = (JsonTypeInfo<List<RestServerAgent>>)options.GetTypeInfo(typeof(List<RestServerAgent>));
                    JsonSerializer.Serialize(writer, value.IotGateway.RestServerAgents.ToList(), restServerTypeInfo);
                }

                writer.WriteEndObject();
                writer.WriteEndArray();
            }

            // Build grouped client_interfaces element from flattened dynamic properties
            var clientInterfacesElement = ClientInterfacesFlattener.BuildClientInterfacesArrayFromDynamicProperties(value.DynamicProperties);
            if (clientInterfacesElement.HasValue)
            {
                writer.WritePropertyName("client_interfaces");
                clientInterfacesElement.Value.WriteTo(writer);
            }

            // Emit remaining dynamic properties (skip interface-prefixed keys)
            foreach (var kvp in value.DynamicProperties)
            {
                var idx = kvp.Key.IndexOf('.');
                if (idx > 0)
                {
                    var prefix = kvp.Key.Substring(0, idx);
                    if (ClientInterfacesFlattener.IsInterfacePrefix(prefix))
                    {
                        continue;
                    }
                }

                writer.WritePropertyName(kvp.Key);
                kvp.Value.WriteTo(writer);
            }

            writer.WriteEndObject();
        }
    }
}
