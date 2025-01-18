﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using System.Collections;
using System.Text.Json;

namespace KepwareSync.Model
{
    [JsonSerializable(typeof(Project))]
    [JsonSerializable(typeof(List<Channel>))]
    [JsonSerializable(typeof(List<Device>))]
    [JsonSerializable(typeof(List<Tag>))]
    [JsonSerializable(typeof(List<DeviceTagGroup>))]
    [JsonSerializable(typeof(List<DefaultEntity>))]
    [JsonSerializable(typeof(List<object?>))]
    [JsonSerializable(typeof(int))]
    [JsonSerializable(typeof(long))]
    [JsonSerializable(typeof(float))]
    [JsonSerializable(typeof(double))]
    [JsonSerializable(typeof(bool))]
    [JsonSerializable(typeof(Dictionary<string, object?>))]
    public partial class KepJsonContext : JsonSerializerContext
    {
        public static JsonTypeInfo<T> GetJsonTypeInfo<T>()
          where T : BaseEntity
        {
            if (typeof(T) == typeof(Channel))
            {
                return (JsonTypeInfo<T>)(object)Default.Channel;
            }
            else if (typeof(T) == typeof(Project))
            {
                return (JsonTypeInfo<T>)(object)Default.Project;
            }
            else if (typeof(T) == typeof(Device))
            {
                return (JsonTypeInfo<T>)(object)Default.Device;
            }
            else if (typeof(T) == typeof(Tag))
            {
                return (JsonTypeInfo<T>)(object)Default.Tag;
            }
            else if (typeof(T) == typeof(DeviceTagGroup))
            {
                return (JsonTypeInfo<T>)(object)Default.DeviceTagGroup;
            }
            else if (typeof(T) == typeof(DefaultEntity))
            {
                return (JsonTypeInfo<T>)(object)Default.DefaultEntity;
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        public static JsonTypeInfo<List<T>> GetJsonListTypeInfo<T>()
            where T : BaseEntity
        {
            if (typeof(T) == typeof(Channel))
            {
                return (JsonTypeInfo<List<T>>)(object)Default.ListChannel;
            }
            else if (typeof(T) == typeof(Device))
            {
                return (JsonTypeInfo<List<T>>)(object)Default.ListDevice;
            }
            else if (typeof(T) == typeof(DeviceTagGroup))
            {
                return (JsonTypeInfo<List<T>>)(object)Default.ListDeviceTagGroup;
            }
            else if (typeof(T) == typeof(Tag))
            {
                return (JsonTypeInfo<List<T>>)(object)Default.ListTag;
            }
            else if (typeof(T) == typeof(DefaultEntity))
            {
                return (JsonTypeInfo<List<T>>)(object)Default.ListDefaultEntity;
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        public static IEnumerable<KeyValuePair<string, object?>> Unwrap(IEnumerable<KeyValuePair<string, JsonElement>> dic)
        {
            return dic.Select(kvp => new KeyValuePair<string, object?>(kvp.Key, Unwrap(kvp.Value)));
        }

        public static object? Unwrap(JsonElement element)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                    // Rekursive Entpackung für Objekte
                    return element.EnumerateObject()
                        .ToDictionary(prop => prop.Name, prop => Unwrap(prop.Value));

                case JsonValueKind.Array:
                    // Rekursive Entpackung für Arrays
                    return element.EnumerateArray()
                        .Select(Unwrap)
                        .ToList();

                case JsonValueKind.String:
                    return element.GetString();

                case JsonValueKind.Number:
                    if (element.TryGetInt64(out var longValue))
                    {
                        return longValue;
                    }
                    if (element.TryGetDouble(out var doubleValue))
                    {
                        return doubleValue;
                    }
                    return null; // Falls die Zahl nicht aufgelöst werden kann

                case JsonValueKind.True:
                case JsonValueKind.False:
                    return element.GetBoolean();

                case JsonValueKind.Null:
                    return null;

                default:
                    // Unbekannte oder unsupported ValueKind
                    return element.GetRawText();
            }
        }

        internal static JsonElement WrapInJsonElement(object? value)
        {
            if (value == null)
            {
                return JsonSerializer.SerializeToElement(null, Default.Object);
            }
            else if (value is bool blnValue)
            {
                return JsonSerializer.SerializeToElement(blnValue, Default.Boolean);
            }
            else if (value is int nValue)
            {
                return JsonSerializer.SerializeToElement(nValue, Default.Int32);
            }
            else if (value is long lValue)
            {
                return JsonSerializer.SerializeToElement(lValue, Default.Int64);
            }
            else if (value is float fValue)
            {
                return JsonSerializer.SerializeToElement(fValue, Default.Single);
            }
            else if (value is double dValue)
            {
                return JsonSerializer.SerializeToElement(dValue, Default.Double);
            }
            else if (value is string strValue)
            {
                return JsonSerializer.SerializeToElement(strValue, Default.String);
            }
            else if (value is Dictionary<string, object?> dict)
            {
                return JsonSerializer.SerializeToElement(dict, Default.DictionaryStringObject);
            }
            else if (value is List<object?> list)
            {
                return JsonSerializer.SerializeToElement(list, Default.ListObject);
            }
            else if (value is BaseEntity entity)
            {
                return JsonSerializer.SerializeToElement(entity, GetJsonTypeInfo<BaseEntity>());
            }
            else if (value is IEnumerable<BaseEntity> entities)
            {
                return JsonSerializer.SerializeToElement(entities, GetJsonListTypeInfo<BaseEntity>());
            }
            else
            {
                throw new NotSupportedException();
            }
        }
    }
}
