using System.Text.Json.Serialization;

namespace Kepware.Api.Model.Services
{
    public class ServiceInvocationRequest
    {
        public const string ReinitializeRuntimeService = "ReinitializeRuntime";

        [JsonPropertyName(Properties.Name)]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public string? Name { get; set; }

        [JsonPropertyName(Properties.Job.TimeToLive)]
        public int TimeToLiveSeconds { get; set; } = 30;
    }
}
