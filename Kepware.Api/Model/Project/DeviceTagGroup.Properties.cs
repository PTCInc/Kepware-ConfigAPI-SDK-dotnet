using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kepware.Api.Model
{
    public partial class Properties
    {
        public static class DeviceTagGroup
        {

            /// <summary>
            /// Represents the constant name used to identify the count of tags in the local tag group.
            /// </summary>
            public const string LocalTagCount = Properties.NonSerialized.TagGrpTagCount;

            /// <summary>
            /// Represents the constant name used to identify the total count of tags in the tag group, including all child tag groups.
            /// </summary>
            public const string TotalTagCount = Properties.NonSerialized.TagGrpTotalTagCount;
        }
    }
}
