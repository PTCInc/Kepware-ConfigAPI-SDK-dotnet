using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Kepware.Api.Model.Services
{
    public class JobResponseMessage : ApiResponseMessage
    {
        [JsonPropertyName("href")]
        public string? JobId { get; set; }
    }
}
