using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kepware.Api.Model
{
    public partial class Properties
    {
        public static class Job
        {
            public const string Completed = "servermain.JOB_COMPLETE";
            public const string Status = "servermain.JOB_STATUS";
            public const string Message = "servermain.JOB_STATUS_MSG";
            public const string TimeToLive = "servermain.JOB_TIME_TO_LIVE_SECONDS";
        }
    }
}
