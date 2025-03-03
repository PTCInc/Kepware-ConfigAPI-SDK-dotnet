using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kepware.Api.Model
{
    public partial class Properties
    {
        public static class ProjectPermission
        {
            /// <summary>
            /// Allows or denies users belonging to the group to add this type of object.
            /// </summary>
            public const string AddObject = "libadminsettings.USERMANAGER_PROJECTMOD_ADD";

            /// <summary>
            /// Allows or denies users belonging to the group to edit this type of object.
            /// </summary>
            public const string EditObject = "libadminsettings.USERMANAGER_PROJECTMOD_EDIT";

            /// <summary>
            /// Allows or denies users belonging to the group to delete this type of object.
            /// </summary>
            public const string DeleteObject = "libadminsettings.USERMANAGER_PROJECTMOD_DELETE";
        }
    }
}
