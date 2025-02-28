using Kepware.Api.Serializer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using YamlDotNet.Serialization;

namespace Kepware.Api.Model
{
    /// <summary>
    /// Represents a project in the Kepware configuration
    /// </summary>
    [Endpoint("/config/v1/project")]
    public class Project : BaseEntity
    {
        /// <summary>
        /// If this is true the project was loaded by the JsonProjectLoad service (added to Kepware Server v6.17 / Kepware Edge v1.10)
        /// </summary>
        public bool IsLoadedByProjectLoadService { get; internal set; } = false;

        /// <summary>
        /// Gets or sets the channels in the project
        /// </summary>
        [YamlIgnore]
        [JsonPropertyName("channels")]
        [JsonPropertyOrder(100)]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public ChannelCollection? Channels { get; set; }

        /// <summary>
        /// Recursively cleans up the project and all its children
        /// </summary>
        /// <param name="defaultValueProvider"></param>
        /// <param name="blnRemoveProjectId"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public override async Task Cleanup(IKepwareDefaultValueProvider defaultValueProvider, bool blnRemoveProjectId = false, CancellationToken cancellationToken = default)
        {
            await base.Cleanup(defaultValueProvider, blnRemoveProjectId, cancellationToken).ConfigureAwait(false);


            if (Channels != null)
            {
                foreach (var channel in Channels)
                {
                    await channel.Cleanup(defaultValueProvider, blnRemoveProjectId, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        public async Task<Project> CloneAsync(CancellationToken cancellationToken = default)
        {
            using var stream = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream, this, KepJsonContext.Default.Project, cancellationToken).ConfigureAwait(false);
            stream.Position = 0;

            return await JsonSerializer.DeserializeAsync(stream, KepJsonContext.Default.Project, cancellationToken).ConfigureAwait(false) ??
                throw new InvalidOperationException("CloneAsync failed");
        }

    }


}
