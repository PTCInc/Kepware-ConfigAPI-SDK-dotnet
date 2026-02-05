using Kepware.Api.Serializer;
using Kepware.Api.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using YamlDotNet.Serialization;

namespace Kepware.Api.Model
{
    /// <summary>
    /// Interface for entities that have an owner.
    /// </summary>
    public interface IHaveOwner
    {
        /// <summary>
        /// The owner of the entity.
        /// </summary>
        [JsonIgnore]
        [YamlIgnore]
        public NamedEntity? Owner { get; internal set; }
    }

    /// <summary>
    /// Interface for entities that have a name.
    /// </summary>
    public interface IHaveName
    {
        /// <summary>
        /// The name of the entity.
        /// </summary>
        public string Name { get; }
    }

    /// <summary>
    /// Abstract base class for all entities in the Kepware API.
    /// </summary>
    [DebuggerDisplay("{TypeName} - {Description}")]
    public abstract class BaseEntity : IEquatable<BaseEntity>
    {
        private bool _dynamicPropertiesNormalized = false;
        private ulong? _hash;

        /// <summary>
        /// Flag indicating if the entity includes nested dynamic properties that require normalization.
        /// </summary>
        /// <remarks>
        /// When set to true, accessing dynamic properties will trigger normalization via class overrides.
        /// For example, the <see cref="Project"/> class sets this to true to handle nested client_interfaces arrays.
        /// </remarks>
        public bool IncludesNestedDynamicProperties = false;

        /// <summary>
        /// Unique hash representing the current state of the entity.
        /// </summary>
        [JsonIgnore]
        [YamlIgnore]
        public ulong Hash => _hash ??= CalculateHash();

        /// <summary>
        /// The project ID the entity belongs to.
        /// </summary>
        [JsonPropertyName(Properties.ProjectId)]
        [YamlIgnore]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public long? ProjectId { get; set; } = null;

        /// <summary>
        /// The description of the entity.
        /// </summary>
        [JsonPropertyName(Properties.Description)]
        [YamlMember(Alias = Properties.Description)]
        public string? Description { get; set; } = string.Empty;

        /// <summary>
        /// The type name of the entity.
        /// </summary>
        [JsonIgnore]
        [YamlIgnore]
        public string TypeName => GetType().Name;

        /// <summary>
        /// Dynamic properties associated with the entity.
        /// </summary>
        [JsonExtensionData]
        public Dictionary<string, JsonElement> DynamicProperties { get; set; } = new();

        /// <summary>
        /// Compares the current entity with another for equality.
        /// </summary>
        /// <param name="other">The other entity to compare with.</param>
        /// <returns>True if the entities are equal, false otherwise.</returns>
        public virtual bool Equals(BaseEntity? other) => other?.Hash == Hash;

        /// <summary>
        /// Retrieves a dynamic property by key.
        /// </summary>
        /// <typeparam name="T">The type of the property.</typeparam>
        /// <param name="key">The key of the property.</param>
        /// <returns>The value of the property, or default if not found.</returns>
        public T? GetDynamicProperty<T>(string key)
        {
            if (IncludesNestedDynamicProperties) EnsureDynamicPropertiesNormalized();

            if (DynamicProperties.TryGetValue(key, out var value))
            {
                if (value is JsonElement jsonElement)
                {
                    if (typeof(T) == typeof(bool))
                        return (T?)(object)jsonElement.GetBoolean();
                    if (typeof(T) == typeof(byte))
                        return (T?)(object)jsonElement.GetByte();
                    if (typeof(T) == typeof(sbyte))
                        return (T?)(object)jsonElement.GetSByte();
                    if (typeof(T) == typeof(short))
                        return (T?)(object)jsonElement.GetInt16();
                    if (typeof(T) == typeof(ushort))
                        return (T?)(object)jsonElement.GetUInt16();
                    if (typeof(T) == typeof(int))
                        return (T?)(object)jsonElement.GetInt32();
                    if (typeof(T) == typeof(uint))
                        return (T?)(object)jsonElement.GetUInt32();
                    if (typeof(T) == typeof(long))
                        return (T?)(object)jsonElement.GetInt64();
                    if (typeof(T) == typeof(ulong))
                        return (T?)(object)jsonElement.GetUInt64();
                    if (typeof(T) == typeof(float))
                        return (T?)(object)jsonElement.GetSingle();
                    if (typeof(T) == typeof(double))
                        return (T?)(object)jsonElement.GetDouble();
                    if (typeof(T) == typeof(string))
                        return (T?)(object?)jsonElement.GetString();

                    throw new NotSupportedException($"Type '{typeof(T)}' is not supported.");
                }
                if (value is T castValue)
                {
                    return castValue;
                }
            }
            return default;
        }

        /// <summary>
        /// Sets a dynamic property for the entity.
        /// </summary>
        /// <typeparam name="T">The type of the property.</typeparam>
        /// <param name="key">The key of the property.</param>
        /// <param name="value">The value of the property.</param>
        /// <returns>The current instance for chaining.</returns>
        public BaseEntity SetDynamicProperty<T>(string key, T value)
        {
            if (IncludesNestedDynamicProperties) EnsureDynamicPropertiesNormalized();
            if (value is null)
            {
                if (DynamicProperties.ContainsKey(key))
                {
                    DynamicProperties.Remove(key);
                    _hash = null;
                }
            }
            else
            {
                DynamicProperties[key] = value is JsonElement jsonElement ? jsonElement : KepJsonContext.WrapInJsonElement(value);
                _hash = null;
            }
            
            return this;
        }

        /// <summary>
        /// Attempts to retrieve a dynamic property by key.
        /// </summary>
        /// <typeparam name="T">The type of the property.</typeparam>
        /// <param name="key">The key of the property.</param>
        /// <param name="value">The retrieved value, or null if not found.</param>
        /// <returns>True if the property exists, false otherwise.</returns>
        public bool TryGetDynamicProperty<T>(string key, [NotNullWhen(true)] out T? value)
        {
            if (IncludesNestedDynamicProperties) EnsureDynamicPropertiesNormalized();
            
            if (DynamicProperties.TryGetValue(key, out var jsonElement) &&
                Convert.ChangeType(KepJsonContext.Unwrap(jsonElement), typeof(T)) is T convertedValue)
            {
                value = convertedValue;
                return true;
            }
            value = default;
            return false;
        }

        /// <summary>
        /// Calculates the hash for the entity.
        /// </summary>
        /// <returns>The calculated hash.</returns>
        protected internal virtual ulong CalculateHash()
        {
            return CustomHashGenerator.ComputeHash(
                    KepJsonContext.Unwrap(DynamicProperties.Except(Properties.NonSerialized.AsHashSet, Properties.NonUpdatable.AsHashSet, ConditionalNonSerialized()))
                        .Concat(
                            AppendHashSources(
                                CustomHashGenerator.CreateHashSourceBuilder(nameof(Description), Description)
                            )
                        )
                );
        }

        /// <summary>
        /// Ensures that dynamic properties are normalized. This method uses the Template Method pattern,
        /// calling <see cref="NormalizeNestedProperties"/> to allow derived classes to customize normalization behavior.
        /// </summary>
        protected virtual void EnsureDynamicPropertiesNormalized()
        {
            if (_dynamicPropertiesNormalized) return;

            // Call the hook method to allow derived classes to normalize their nested properties
            NormalizeNestedProperties();

            _dynamicPropertiesNormalized = true;
        }

        /// <summary>
        /// Normalizes nested properties specific to this entity type. Override this method to implement
        /// custom normalization logic (e.g., flattening nested arrays into dynamic properties).
        /// </summary>
        /// <remarks>
        /// This method is called by <see cref="EnsureDynamicPropertiesNormalized"/> during property access.
        /// Implement this in derived classes to handle entity-specific normalization.
        /// For example, <see cref="Project"/> overrides this to flatten nested "client_interfaces" arrays.
        /// </remarks>
        protected virtual void NormalizeNestedProperties()
        {
            // Default implementation: no normalization. Derived classes override to add custom behavior.
        }

        /// <summary>
        /// Appends additional hash sources for derived classes.
        /// </summary>
        /// <param name="builder">The hash source builder.</param>
        /// <returns>The updated hash source builder.</returns>

        protected virtual CustomHashGenerator.HashSourceBuilder AppendHashSources(CustomHashGenerator.HashSourceBuilder builder)
        {
            return builder;
        }

        /// <summary>
        /// Retrieves the conditional non-serialized properties for the entity.
        /// </summary>
        protected virtual ISet<string>? ConditionalNonSerialized()
        {
            return null;
        }

        /// <summary>
        /// Cleans up the entity by removing unnecessary properties and standardizing its state.
        /// </summary>
        /// <param name="defaultValueProvider">Provider for default values.</param>
        /// <param name="blnRemoveProjectId">Whether to remove the project ID.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        public virtual async Task Cleanup(IKepwareDefaultValueProvider defaultValueProvider, bool blnRemoveProjectId = false, CancellationToken cancellationToken = default)
        {
            DynamicProperties = DynamicProperties.Except(Properties.NonSerialized.AsHashSet, ConditionalNonSerialized()).ToDictionary(x => x.Key, x => x.Value);
            if (DynamicProperties.TryGetValue(Properties.Description, out var descriptionElement) &&
                KepJsonContext.Unwrap(descriptionElement) is string description && string.IsNullOrEmpty(description))
            {
                DynamicProperties.Remove(Properties.Description);
            }

            if (TryGetDynamicProperty<string>(Properties.Channel.DeviceDriver, out var driver))
            {
                var defaultValues = await defaultValueProvider.GetDefaultValuesAsync(driver, TypeName, cancellationToken);
                foreach (var prop in DynamicProperties.ToList())
                {
                    if (defaultValues.TryGetValue(prop.Key, out var defaultValue) &&
                        KepJsonContext.JsonElementEquals(prop.Value, defaultValue!))
                    {
                        DynamicProperties.Remove(prop.Key);
                    }
                }
            }

            if (blnRemoveProjectId)
            {
                ProjectId = null;
            }
        }

        public virtual Dictionary<string, JsonElement> GetUpdateDiff(DefaultEntity other)
        {
            var diff = new Dictionary<string, JsonElement>();

            if (Description != other.Description)
            {
                diff[Properties.Description] = KepJsonContext.WrapInJsonElement(Description);
            }

            foreach (var kvp in DynamicProperties.Except(Properties.NonSerialized.AsHashSet, Properties.NonUpdatable.AsHashSet, ConditionalNonSerialized()))
            {
                if (!other.DynamicProperties.TryGetValue(kvp.Key, out var otherValue) ||
                    !KepJsonContext.JsonElementEquals(kvp.Value, otherValue))
                {
                    diff[kvp.Key] = kvp.Value;
                }
            }

            return diff;
        }
    }

    /// <summary>
    /// A default entity with an optional owner.
    /// </summary>
    public class DefaultEntity : BaseEntity, IHaveOwner
    {
        /// <summary>
        /// The owner of the entity.
        /// </summary>
        [YamlIgnore]
        [JsonIgnore]
        public NamedEntity? Owner { get; set; }
    }

    /// <summary>
    /// A named entity with a unique name and description.
    /// </summary>
    [DebuggerDisplay("{TypeName} - {Name} - {Description}")]
    public class NamedEntity : DefaultEntity, IHaveName
    {
        /// <summary>
        /// The name of the entity.
        /// </summary>
        [JsonPropertyName(Properties.Name)]
        [YamlIgnore]
        public string Name { get; set; } = string.Empty;

        public NamedEntity()
        {

        }

        public NamedEntity(string name)
        {
            Name = name;
        }

        /// <summary>
        /// Appends additional hash sources for named entities.
        /// </summary>
        protected override CustomHashGenerator.HashSourceBuilder AppendHashSources(CustomHashGenerator.HashSourceBuilder builder)
            => base.AppendHashSources(builder).Append(nameof(Name), Name);

        /// <summary>
        /// Retrieves the differences between this entity and another.
        /// </summary>
        /// <param name="other">The other entity to compare with.</param>
        /// <param name="blnAddProjectId">Whether to include the project ID in the differences.</param>
        /// <returns>A dictionary of differences.</returns>
        public virtual Dictionary<string, JsonElement> GetUpdateDiff(NamedEntity other, bool blnAddProjectId = true)
        {
            var diff = base.GetUpdateDiff(other);
            if (Name != other.Name)
            {
                diff[Properties.Name] = KepJsonContext.WrapInJsonElement(Name);
            }


            if (blnAddProjectId && ProjectId != 0)
            {
                diff[Properties.ProjectId] = KepJsonContext.WrapInJsonElement(ProjectId);
            }


         
            return diff;
        }
    }

    /// <summary>
    /// A named entity with a unique identifier.
    /// </summary>
    public abstract class NamedUidEntity : NamedEntity
    {
        /// <summary>
        /// The unique ID of the entity.
        /// </summary>
        [JsonIgnore]
        [YamlIgnore]
        public long UniqueId => GetDynamicProperty<long>(UniqueIdKey);

        /// <summary>
        /// The key used to retrieve the unique ID.
        /// </summary>
        protected abstract string UniqueIdKey { get; }

        /// <summary>
        /// Removes the unique ID from the entity.
        /// </summary>
        public void RemoveUniqueId() => DynamicProperties.Remove(UniqueIdKey);

        protected NamedUidEntity()
        {

        }

        protected NamedUidEntity(string name)
            : base(name)
        {

        }
    }
}
