using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kepware.Api.Model
{
    public partial class Properties
    {
        public static class ServerUserGroup
        {
            /// <summary>
            /// Enables or disables the user group.
            /// </summary>
            public const string Enabled = "libadminsettings.USERMANAGER_GROUP_ENABLED";

            /// <summary>
            /// Allows or denies read access to I/O tags.
            /// </summary>
            public const string IoTagRead = "libadminsettings.USERMANAGER_IO_TAG_READ";

            /// <summary>
            /// Allows or denies write access to I/O tags.
            /// </summary>
            public const string IoTagWrite = "libadminsettings.USERMANAGER_IO_TAG_WRITE";

            /// <summary>
            /// Allows or denies dynamic addressing of I/O tags.
            /// </summary>
            public const string IoTagDynamicAddressing = "libadminsettings.USERMANAGER_IO_TAG_DYNAMIC_ADDRESSING";

            /// <summary>
            /// Allows or denies read access to system tags.
            /// </summary>
            public const string SystemTagRead = "libadminsettings.USERMANAGER_SYSTEM_TAG_READ";

            /// <summary>
            /// Allows or denies write access to system tags.
            /// </summary>
            public const string SystemTagWrite = "libadminsettings.USERMANAGER_SYSTEM_TAG_WRITE";

            /// <summary>
            /// Allows or denies read access to internal tags.
            /// </summary>
            public const string InternalTagRead = "libadminsettings.USERMANAGER_INTERNAL_TAG_READ";

            /// <summary>
            /// Allows or denies write access to internal tags.
            /// </summary>
            public const string InternalTagWrite = "libadminsettings.USERMANAGER_INTERNAL_TAG_WRITE";

            /// <summary>
            /// Allows or denies access to the licensing manager.
            /// </summary>
            public const string ManageLicenses = "libadminsettings.USERMANAGER_SERVER_MANAGE_LICENSES";

            /// <summary>
            /// Allows or denies modification of server settings.
            /// </summary>
            public const string ModifyServerSettings = "libadminsettings.USERMANAGER_SERVER_MODIFY_SERVER_SETTINGS";

            /// <summary>
            /// Allows or denies disconnecting clients from the server.
            /// </summary>
            public const string DisconnectClients = "libadminsettings.USERMANAGER_SERVER_DISCONNECT_CLIENTS";

            /// <summary>
            /// Allows or denies replacing the running project with a new one.
            /// </summary>
            public const string ReplaceRuntimeProject = "libadminsettings.USERMANAGER_SERVER_REPLACE_RUNTIME_PROJECT";

            /// <summary>
            /// Allows or denies resetting the event log.
            /// </summary>
            public const string ResetEventLog = "libadminsettings.USERMANAGER_SERVER_RESET_EVENT_LOG";

            /// <summary>
            /// Allows or denies access to OPC UA or XI configuration settings.
            /// </summary>
            public const string OpcUaConfiguration = "libadminsettings.USERMANAGER_SERVER_OPCUA_DOTNET_CONFIGURATION";

            /// <summary>
            /// Allows or denies browsing of the project namespace.
            /// </summary>
            public const string BrowseNamespace = "libadminsettings.USERMANAGER_BROWSE_BROWSENAMESPACE";

            /// <summary>
            /// Allows or denies adding objects to the project.
            /// </summary>
            public const string ProjectModificationAdd = "libadminsettings.USERMANAGER_PROJECTMOD_ADD";

            /// <summary>
            /// Allows or denies editing objects in the project.
            /// </summary>
            public const string ProjectModificationEdit = "libadminsettings.USERMANAGER_PROJECTMOD_EDIT";

            /// <summary>
            /// Allows or denies deleting objects in the project.
            /// </summary>
            public const string ProjectModificationDelete = "libadminsettings.USERMANAGER_PROJECTMOD_DELETE";

        }
    }
}
