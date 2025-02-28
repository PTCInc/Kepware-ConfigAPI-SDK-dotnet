using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kepware.Api
{
    public class KepwareApiClientOptions
    {
        public required Uri HostUri { get; init; }
        public string? Password { get; init; }
        public string? Username { get; init; }

        public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(60);

        public bool DisableCertifcateValidation { get; init; } = false;

        public Action<HttpClient>? ConfigureClient { get; init; }
        public Action<IHttpClientBuilder>? ConfigureClientBuilder { get; init; }

        public bool EnableIpv6 { get; init; } = false;

        public object? Tag { get; init; }
    }
}
