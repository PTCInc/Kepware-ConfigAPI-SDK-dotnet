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
                    if (root.TryGetProperty("channels", out var channelsProp))
                    {
                        var jsonTypeInfo = (JsonTypeInfo<List<Channel>>)options.GetTypeInfo(typeof(List<Channel>));
                        var channels = JsonSerializer.Deserialize(channelsProp.GetRawText(), jsonTypeInfo);
                        if (channels != null)
                        {
                            project.Channels = new ChannelCollection();
                            foreach (var channel in channels)
                            {
                                project.Channels.Add(channel);
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
