using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;



namespace Kepware.Api.Model.Services
{
    public class JobStatusMessage : DefaultEntity
    {
        [JsonPropertyName(Properties.Name)]
        public string? Name { get; set; }

        [JsonPropertyName(Properties.Job.Completed)]
        public bool Completed { get; set; }

        [JsonPropertyName(Properties.Job.Status)]
        public int StatusCode { get; set; }

        [JsonPropertyName(Properties.Job.Message)]
        public string? Message { get; set; }
    }
}
