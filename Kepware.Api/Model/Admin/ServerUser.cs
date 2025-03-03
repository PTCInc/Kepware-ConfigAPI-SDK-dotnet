using System;
using System.Text.Json.Serialization;
using YamlDotNet.Serialization;

namespace Kepware.Api.Model.Admin
{
    /// <summary>
    /// Represents a user in the Kepware server.
    /// </summary>
    [Endpoint("/config/v1/admin/server_users/{name}")]
    public class ServerUser : NamedEntity
    {
        /// <summary>
        /// The user group to which the user belongs.
        /// The user inherits permissions from the assigned group.
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public string? UserGroupName
        {
            get => GetDynamicProperty<string>(Properties.ServerUser.UserGroupName);
            set => SetDynamicProperty(Properties.ServerUser.UserGroupName, value);
        }

        /// <summary>
        /// Specifies whether the user account is enabled or disabled.
        /// Note: If the user group is disabled, all users in that group are also disabled.
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public bool? Enabled
        {
            get => GetDynamicProperty<bool>(Properties.ServerUser.Enabled);
            set => SetDynamicProperty(Properties.ServerUser.Enabled, value);
        }

        /// <summary>
        /// The password for the user account.
        /// The password is case-sensitive and must meet security requirements:
        /// - Minimum 14 and maximum 512 characters.
        /// - Must include uppercase and lowercase letters, numbers, and special characters.
        /// - Should avoid common or easily guessed passwords.
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public string? Password
        {
            get => GetDynamicProperty<string>(Properties.ServerUser.Password);
            set => SetDynamicProperty(Properties.ServerUser.Password, value);
        }
    }

    [Endpoint("/config/v1/admin/server_users")]
    public class ServerUserCollection : EntityCollection<ServerUser>
    {

    }
}
