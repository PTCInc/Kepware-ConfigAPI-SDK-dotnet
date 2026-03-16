using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kepware.Api
{
    /// <summary>
    /// A class that provides the configuration options required to configure the <see cref="KepwareApiClient"/> class.
    /// </summary>
    public class KepwareApiClientOptions
    {
        /// <summary>
        /// Gets or sets the hostname URI of the Kepware server. Should be in the following format: 
        /// https://{hostname}:{port} or http://{hostname}:{port}</summary>
        public required Uri HostUri { get; init; }

        /// <summary>
        /// Gets or sets the password for authentication.
        /// </summary>
        public string? Password { get; init; }

        /// <summary>
        /// Gets or sets the username for authentication.
        /// </summary>
        public string? Username { get; init; }

        /// <summary>
        /// Gets or sets the timeout period for the HTTP client.
        /// Default is 60 seconds.
        /// </summary>
        public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(60);

        /// <summary>
        /// Gets or sets a value indicating whether to disable certificate validation.
        /// Default is false.
        /// </summary>
        public bool DisableCertifcateValidation { get; init; } = false;

        /// <summary>
        /// Gets or sets an action to configure the <see cref="HttpClient"/>.
        /// </summary>
        public Action<HttpClient>? ConfigureClient { get; init; }

        /// <summary>
        /// Gets or sets an action to configure the <see cref="IHttpClientBuilder"/>.
        /// </summary>
        public Action<IHttpClientBuilder>? ConfigureClientBuilder { get; init; }

        /// <summary>
        /// Gets or sets a value indicating whether to enable IPv6. Currently the Kepware Configuration
        /// API does not support IPV6.
        /// Default is false.
        /// </summary>
        public bool EnableIpv6 { get; init; } = false;

        /// <summary>
        /// Gets or sets an optional tag object for additional configuration metadata.
        /// </summary>
        public object? Tag { get; init; }

        /// <summary>
        /// Gets the maximum number of tags that can be loaded in a single action when using ProjectLoad methods.
        /// This limit is crucial for managing performance and resource utilization when working with large projects, 
        /// as it helps to prevent excessive memory usage and potential timeouts that may occur when attempting to 
        /// load an excessively large number of tags at once.
        /// </summary>
        /// <remarks>This property is initialized to a default value of 100,000. It is important to
        /// consider this limit when working with projects that may contain a large number of tags, as exceeding this
        /// limit may result in performance degradation or incomplete data loading.</remarks>
        public int ProjectLoadTagLimit { get; init; } = 100000;
    }
}
