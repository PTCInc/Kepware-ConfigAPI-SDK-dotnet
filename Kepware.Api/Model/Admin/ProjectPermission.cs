using System;
using System.Text.Json.Serialization;
using YamlDotNet.Serialization;

namespace Kepware.Api.Model.Admin
{
    /// <summary>
    /// Represents a project permission in Kepware.
    /// Project permissions define the actions that users in a group can perform on project objects.
    /// </summary>
    [Endpoint("/config/v1/admin/server_usergroups/{groupName}/project_permissions/{permissionName}")]
    public class ProjectPermission : NamedEntity
    {
        /// <summary>
        /// Get or sets the name of the project permission set for which this permissions applies on the group.
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public new ProjectPermissionName Name
        {
            get => (ProjectPermissionName)base.Name;
            set => base.Name = value;
        }

        /// <summary>
        /// Allows or denies users belonging to the group to add this type of object.
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public bool AddObject
        {
            get => GetDynamicProperty<bool>(Properties.ProjectPermission.AddObject);
            set => SetDynamicProperty(Properties.ProjectPermission.AddObject, value);
        }

        /// <summary>
        /// Allows or denies users belonging to the group to edit this type of object.
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public bool EditObject
        {
            get => GetDynamicProperty<bool>(Properties.ProjectPermission.EditObject);
            set => SetDynamicProperty(Properties.ProjectPermission.EditObject, value);
        }

        /// <summary>
        /// Allows or denies users belonging to the group to delete this type of object.
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public bool DeleteObject
        {
            get => GetDynamicProperty<bool>(Properties.ProjectPermission.DeleteObject);
            set => SetDynamicProperty(Properties.ProjectPermission.DeleteObject, value);
        }
    }

    [Endpoint("/config/v1/admin/server_usergroups/{groupName}/project_permissions")]
    public class ProjectPermissionCollection : EntityCollection<ProjectPermission>
    {

    }
}
