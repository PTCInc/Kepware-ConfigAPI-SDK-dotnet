using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using System.Collections;
using System.Text.Json;
using Kepware.Api.Serializer;
using Kepware.Api.Model;
using Kepware.Api.Model.Admin;
using Kepware.Api.Model.Services;

namespace Kepware.Api.Serializer
{
    [JsonSerializable(typeof(Project))]
    [JsonSerializable(typeof(JsonProjectRoot))]
    [JsonSerializable(typeof(ProductInfo))]
    [JsonSerializable(typeof(AdminSettings))]
    [JsonSerializable(typeof(UaEndpoint))]
    [JsonSerializable(typeof(ServerUserGroup))]
    [JsonSerializable(typeof(ServerUser))]
    [JsonSerializable(typeof(ProjectPermission))]
    [JsonSerializable(typeof(ApiResponseMessage))]
    [JsonSerializable(typeof(JobResponseMessage))]
    [JsonSerializable(typeof(JobStatusMessage))]
    [JsonSerializable(typeof(ServiceInvocationRequest))]
    
    [JsonSerializable(typeof(List<ApiStatus>))]
    [JsonSerializable(typeof(List<UaEndpoint>))]
    [JsonSerializable(typeof(List<ServerUserGroup>))]
    [JsonSerializable(typeof(List<ServerUser>))]
    [JsonSerializable(typeof(List<Channel>))]
    [JsonSerializable(typeof(List<Device>))]
    [JsonSerializable(typeof(List<Tag>))]
    [JsonSerializable(typeof(List<DeviceTagGroup>))]
    [JsonSerializable(typeof(List<DefaultEntity>))]
    [JsonSerializable(typeof(List<object?>))]
    [JsonSerializable(typeof(List<ApiResult>))]
    [JsonSerializable(typeof(List<ProjectPermission>))]
    [JsonSerializable(typeof(int))]
    [JsonSerializable(typeof(long))]
    [JsonSerializable(typeof(float))]
    [JsonSerializable(typeof(double))]
    [JsonSerializable(typeof(bool))]
    [JsonSerializable(typeof(Dictionary<string, object?>))]
    [JsonSourceGenerationOptions(WriteIndented = true)]
    internal partial class KepJsonContext : JsonSerializerContext
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
            else if (typeof(T) == typeof(AdminSettings))
            {
                return (JsonTypeInfo<T>)(object)Default.AdminSettings;
            }
            else if (typeof(T) == typeof(ServerUserGroup))
            {
                return (JsonTypeInfo<T>)(object)Default.ServerUserGroup;
            }
            else if (typeof(T) == typeof(ServerUser))
            {
                return (JsonTypeInfo<T>)(object)Default.ServerUser;
            }
            else if (typeof(T) == typeof(UaEndpoint))
            {
                return (JsonTypeInfo<T>)(object)Default.UaEndpoint;
            }
            else if (typeof(T) == typeof(ProjectPermission))
            {
                return (JsonTypeInfo<T>)(object)Default.ProjectPermission;
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
            else if (typeof(T) == typeof(UaEndpoint))
            {
                return (JsonTypeInfo<List<T>>)(object)Default.ListUaEndpoint;
            }
            else if (typeof(T) == typeof(ServerUserGroup))
            {
                return (JsonTypeInfo<List<T>>)(object)Default.ListServerUserGroup;
            }
            else if (typeof(T) == typeof(ServerUser))
            {
                return (JsonTypeInfo<List<T>>)(object)Default.ListServerUser;
            }
            else if (typeof(T) == typeof(ProjectPermission))
            {
                return (JsonTypeInfo<List<T>>)(object)Default.ListProjectPermission;
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

        public static bool JsonElementEquals(JsonElement? element, JsonElement? other)
        {
            if (other == null)
            {
                return element == null;
            }
            else if (element == null)
            {
                return false;
            }
            else if (element.Value.ValueKind != other.Value.ValueKind)
            {
                return false;
            }

            switch (element.Value.ValueKind)
            {
                case JsonValueKind.Object:
                    var elementProperties = element.Value.EnumerateObject().ToDictionary(prop => prop.Name, prop => prop.Value);
                    var otherProperties = other.Value.EnumerateObject().ToDictionary(prop => prop.Name, prop => prop.Value);

                    if (elementProperties.Count != otherProperties.Count)
                    {
                        return false;
                    }

                    foreach (var property in elementProperties)
                    {
                        if (!otherProperties.TryGetValue(property.Key, out var otherValue))
                        {
                            return false;
                        }

                        if (!JsonElementEquals(property.Value, otherValue))
                        {
                            return false;
                        }
                    }

                    return true;

                case JsonValueKind.Array:
                    var elementArray = element.Value.EnumerateArray().ToArray();
                    var otherArray = other.Value.EnumerateArray().ToArray();

                    if (elementArray.Length != otherArray.Length)
                    {
                        return false;
                    }

                    for (int i = 0; i < elementArray.Length; i++)
                    {
                        if (!JsonElementEquals(elementArray[i], otherArray[i]))
                        {
                            return false;
                        }
                    }

                    return true;

                case JsonValueKind.String:
                    return element.Value.GetString() == other.Value.GetString();

                case JsonValueKind.Number:
                    if (element.Value.TryGetInt64(out var elementLong) && other.Value.TryGetInt64(out var otherLong))
                    {
                        return elementLong == otherLong;
                    }

                    if (element.Value.TryGetDouble(out var elementDouble) && other.Value.TryGetDouble(out var otherDouble))
                    {
                        return Math.Abs(elementDouble - otherDouble) <= double.Epsilon;
                    }

                    return false;

                case JsonValueKind.True:
                case JsonValueKind.False:
                    return element.Value.GetBoolean() == other.Value.GetBoolean();

                case JsonValueKind.Null:
                    return true;

                default:
                    return element.Value.GetRawText() == other.Value.GetRawText();
            }
        }


        internal static JsonElement WrapInJsonElement(object? value)
        {
            if (value == null)
            {
                return JsonSerializer.SerializeToElement(null, Default.Object!);
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
            else if (value is Dictionary<string, object?> dict && dict != null)
            {
                return JsonSerializer.SerializeToElement<Dictionary<string, object?>>(dict, Default.DictionaryStringObject!);
            }
            else if (value is List<object?> list && list != null)
            {
                return JsonSerializer.SerializeToElement<List<object?>>(list, Default.ListObject!);
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
