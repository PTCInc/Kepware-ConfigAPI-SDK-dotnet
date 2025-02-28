using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kepware.Api.Model
{
    public partial class Properties
    {
        public static class ServerUser
        {

            /// <summary>
            /// The name of the user group to which the user belongs.
            /// </summary>
            public const string UserGroupName = "libadminsettings.USERMANAGER_USER_GROUPNAME";

            /// <summary>
            /// Determines whether the user account is enabled or disabled.
            /// The user group’s enabled-state takes precedence over the user’s enabled-state.
            /// </summary>
            public const string Enabled = "libadminsettings.USERMANAGER_USER_ENABLED";

            /// <summary>
            /// The password for the user. This is case-sensitive and must meet security requirements.
            /// </summary>
            public const string Password = "libadminsettings.USERMANAGER_USER_PASSWORD";

        }
    }
}
