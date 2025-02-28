using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Kepware.Api.Model.Admin
{
    /// <summary>
    /// Represents a known project permission in Kepware.
    /// Project permissions define the actions that users in a group can perform on project objects.
    /// </summary>
    public readonly struct ProjectPermissionName : IEquatable<ProjectPermissionName>
    {
        private readonly string _value;

        private ProjectPermissionName(string value) => _value = value;

        /// <summary>
        /// Returns the string representation of the permission name.
        /// </summary>
        public override string ToString() => _value;

        /// <summary>
        /// Checks if two permission names are equal.
        /// </summary>
        public bool Equals(ProjectPermissionName other) => _value == other._value;

        public override bool Equals(object? obj) => obj is ProjectPermissionName other && Equals(other);

        public override int GetHashCode() => _value.GetHashCode();

        /// <summary>
        /// Implicit conversion to string.
        /// </summary>
        public static implicit operator string(ProjectPermissionName permission) => permission._value;

        /// <summary>
        /// Explicit conversion from string.
        /// </summary>
        public static explicit operator ProjectPermissionName(string value)
        {
            if (!Known.Contains(value))
                throw new ArgumentException($"Invalid project permission name: {value}", nameof(value));
            return new ProjectPermissionName(value);
        }

        // **Definierte bekannte Werte**

        /// <summary>
        /// Configure default 'Servermain Alias' access permissions for the selected user group.
        /// </summary>
        public static readonly ProjectPermissionName ServermainAlias = new("Servermain Alias");

        /// <summary>
        /// Configure default 'Servermain Channel' access permissions for the selected user group.
        /// </summary>
        public static readonly ProjectPermissionName ServermainChannel = new("Servermain Channel");

        /// <summary>
        /// Configure default 'Servermain Device' access permissions for the selected user group.
        /// </summary>
        public static readonly ProjectPermissionName ServermainDevice = new("Servermain Device");

        /// <summary>
        /// Configure default 'Servermain Meter Order' access permissions for the selected user group.
        /// Add and delete properties are disabled for this endpoint.
        /// </summary>
        public static readonly ProjectPermissionName ServermainMeterOrder = new("Servermain Meter Order");

        /// <summary>
        /// Configure default 'Servermain Phone Number' access permissions for the selected user group.
        /// </summary>
        public static readonly ProjectPermissionName ServermainPhoneNumber = new("Servermain Phone Number");

        /// <summary>
        /// Configure default 'Servermain Phone Priority' access permissions for the selected user group.
        /// Add and delete properties are disabled for this endpoint.
        /// </summary>
        public static readonly ProjectPermissionName ServermainPhonePriority = new("Servermain Phone Priority");

        /// <summary>
        /// Configure default 'Servermain Project' access permissions for the selected user group.
        /// Add and delete properties are disabled for this endpoint.
        /// </summary>
        public static readonly ProjectPermissionName ServermainProject = new("Servermain Project");

        /// <summary>
        /// Configure default 'Servermain Tag' access permissions for the selected user group.
        /// </summary>
        public static readonly ProjectPermissionName ServermainTag = new("Servermain Tag");

        /// <summary>
        /// Configure default 'Servermain Tag Group' access permissions for the selected user group.
        /// </summary>
        public static readonly ProjectPermissionName ServermainTagGroup = new("Servermain Tag Group");

        /// <summary>
        /// List of all known project permissions.
        /// </summary>
        public static readonly IReadOnlySet<string> Known = new HashSet<string>
        {
            ServermainAlias,
            ServermainChannel,
            ServermainDevice,
            ServermainMeterOrder,
            ServermainPhoneNumber,
            ServermainPhonePriority,
            ServermainProject,
            ServermainTag,
            ServermainTagGroup
        };

        /// <summary>
        /// Attempts to create a valid project permission name.
        /// </summary>
        public static bool TryParse(string value, [NotNullWhen(true)] out ProjectPermissionName? result)
        {
            if (Known.Contains(value))
            {
                result = new ProjectPermissionName(value);
                return true;
            }
            result = null;
            return false;
        }
        public static bool operator ==(ProjectPermissionName left, ProjectPermissionName right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ProjectPermissionName left, ProjectPermissionName right)
        {
            return !(left == right);
        }
    }
}
