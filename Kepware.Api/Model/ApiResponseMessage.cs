using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Kepware.Api.Model
{
    public class ApiResponseMessage
    {
        [JsonPropertyName("code")]
        public int ResponseStatusCode { get; set; }
        [JsonIgnore]
        public ApiResponseCode ResponseStatus => (ApiResponseCode)ResponseStatusCode;

        [JsonPropertyName("message")]
        public string? Message { get; set; }
    }
}
