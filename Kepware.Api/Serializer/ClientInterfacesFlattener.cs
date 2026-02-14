using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace Kepware.Api.Serializer
{
    internal static class ClientInterfacesFlattener
    {
        private static readonly HashSet<string> KnownPrefixes = new(StringComparer.OrdinalIgnoreCase)
        {
            "opcdaserver",
            "wwtoolkitinterface",
            "ddeserver",
            "uaserverinterface",
            "aeserverinterface",
            "hdaserver",
            "thingworxinterface"
        };

        public static bool IsInterfacePrefix(string prefix) => KnownPrefixes.Contains(prefix);

        /// <summary>
        /// Flatten a runtime-deserialized object (from YAML) representing the value of a
        /// top-level `client_interfaces` sequence into the provided dynamicProperties dictionary.
        /// Nested values overwrite existing top-level keys (nested wins).
        /// </summary>
        public static void FlattenFromObject(object? value, IDictionary<string, JsonElement> dynamicProperties)
        {
            if (value is not List<object?> list) return;

            foreach (var item in list)
            {
                if (item is not Dictionary<string, object?> dict) continue;

                // discover interface name if provided
                string? ifaceName = null;
                if (dict.TryGetValue("common.ALLTYPES_NAME", out var nameObj) && nameObj is string s)
                {
                    ifaceName = s;
                }

                // if name not present, try to infer from any prefixed keys
                if (string.IsNullOrEmpty(ifaceName))
                {
                    foreach (var k in dict.Keys)
                    {
                        var idx = k.IndexOf('.');
                        if (idx > 0)
                        {
                            var prefix = k.Substring(0, idx);
                            if (KnownPrefixes.Contains(prefix))
                            {
                                ifaceName = prefix;
                                break;
                            }
                        }
                    }
                }

                // inject all properties from the interface object into dynamicProperties
                foreach (var kv in dict)
                {
                    if (kv.Key == "common.ALLTYPES_NAME")
                        continue;

                    dynamicProperties[kv.Key] = KepJsonContext.WrapInJsonElement(kv.Value);
                }
            }
        }

        /// <summary>
        /// Group flattened dynamic properties into a client_interfaces JsonElement array.
        /// Returns null if no interface-prefixed keys are present.
        /// </summary>
        public static JsonElement? BuildClientInterfacesArrayFromDynamicProperties(IDictionary<string, JsonElement> dynamicProperties)
        {
            var groups = new Dictionary<string, Dictionary<string, JsonElement>>(StringComparer.OrdinalIgnoreCase);

            foreach (var kv in dynamicProperties)
            {
                var idx = kv.Key.IndexOf('.');
                if (idx <= 0) continue;
                var prefix = kv.Key.Substring(0, idx);
                if (!KnownPrefixes.Contains(prefix)) continue;

                if (!groups.TryGetValue(prefix, out var dict))
                {
                    dict = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
                    groups[prefix] = dict;
                }

                dict[kv.Key] = kv.Value;
            }

            if (groups.Count == 0) return null;

            var list = new List<Dictionary<string, object?>>();
            foreach (var g in groups)
            {
                var obj = new Dictionary<string, object?>();
                obj["common.ALLTYPES_NAME"] = g.Key;
                foreach (var kv in g.Value)
                {
                    obj[kv.Key] = KepJsonContext.Unwrap(kv.Value);
                }
                list.Add(obj);
            }

            // Build a JsonElement array by serializing each object using the AOT-friendly
            // wrapping helper and writing the elements into a Utf8JsonWriter. This avoids
            // calling JsonSerializer.Serialize on an open generic which would trigger
            // trimming/AOT issues (IL2026).
            var elements = new List<JsonElement>();
            foreach (var obj in list)
            {
                // Wrap the dictionary into a JsonElement using the generated context
                elements.Add(KepJsonContext.WrapInJsonElement(obj));
            }

            using var ms = new System.IO.MemoryStream();
            using (var writer = new Utf8JsonWriter(ms))
            {
                writer.WriteStartArray();
                foreach (var el in elements)
                {
                    el.WriteTo(writer);
                }
                writer.WriteEndArray();
                writer.Flush();
            }

            ms.Position = 0;
            using var doc = JsonDocument.Parse(ms);
            return doc.RootElement.Clone();
        }
    }
}
