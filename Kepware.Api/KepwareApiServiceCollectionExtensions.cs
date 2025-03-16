using Kepware.Api.Util;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Kepware.Api
{
    public static class KepwareApiServiceCollectionExtensions
    {
        public static IServiceCollection AddKepwareApiClients(this IServiceCollection services, IEnumerable<KeyValuePair<string, KepwareApiClientOptions>> options)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));

            var clients = options.Select(
                kvp =>
                 (
                    name: kvp.Key,
                    options: kvp.Value,
                    builder: services.AddHttpClient(kvp.Key + "-httpClient", client => ConfigureHttpClient(kvp.Value))
                 ));

            foreach (var (_, option, builder) in clients)
            {
                builder.ConfigureHttpClient(client => ConfigureHttpClient(option)(client));
                ConfigureHttpClientBuilder(option)(builder);
            }

            services.AddSingleton(serviceProvider =>
            {
                return options.Select(kvp =>
                {
                    var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
                    var factory = serviceProvider.GetRequiredService<IHttpClientFactory>();
                    var httpClient = factory.CreateClient(kvp.Key + "-httpClient");

                    return new KepwareApiClient(kvp.Key, kvp.Value, loggerFactory, httpClient);
                }).ToList();
            });

            return services;
        }

        public static IServiceCollection AddKepwareApiClient(this IServiceCollection services, string name, KepwareApiClientOptions options)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name cannot be null or empty.", nameof(name));

            if (services.Any(service => service.ImplementationType == typeof(KepwareApiClient) && service.ServiceType.Name == name))
            {
                throw new InvalidOperationException($"KepwareApiClient with name '{name}' is already registered.");
            }

            if (options == null)
                throw new ArgumentNullException(nameof(options));
            if (!options.HostUri.IsAbsoluteUri)
                throw new InvalidOperationException(name + " host is not configured as absolute uri.");

            var builder = services.AddHttpClient(name + "-httpClient", client => ConfigureHttpClient(options).Invoke(client));
            ConfigureHttpClientBuilder(options).Invoke(builder);

            services.AddSingleton(serviceProvider =>
            {
                var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
                var factory = serviceProvider.GetRequiredService<IHttpClientFactory>();
                var httpClient = factory.CreateClient(name + "-httpClient");

                return new KepwareApiClient(name, options, loggerFactory, httpClient);
            });

            return services;
        }

        public static IServiceCollection AddKepwareApiClient(this IServiceCollection services,
            string name, string baseUrl, string apiUserName, string apiPassword,
            int timeoutInSeconds = 60, bool disableCertificateValidation = false,
            Action<HttpClient>? configureClient = null, Action<IHttpClientBuilder>? configureHttpClientBuilder = null)
        {
            return AddKepwareApiClient(services, name,
                new KepwareApiClientOptions
                {
                    HostUri = new Uri(baseUrl),
                    Username = apiUserName,
                    Password = apiPassword,
                    Timeout = TimeSpan.FromSeconds(timeoutInSeconds),
                    DisableCertifcateValidation = disableCertificateValidation,
                    ConfigureClient = configureClient,
                    ConfigureClientBuilder = configureHttpClientBuilder
                });
        }

        private static Action<IHttpClientBuilder> ConfigureHttpClientBuilder(KepwareApiClientOptions options)
            => clientBuilder =>
            {
                var handler = options.EnableIpv6 ? new HttpClientHandler() : new Ipv4OnlyHttpClientHandler();

                if (options.DisableCertifcateValidation)
                {
#pragma warning disable S4830 // Server certificates should be verified during SSL/TLS connections
                    handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;
#pragma warning restore S4830 // Server certificates should be verified during SSL/TLS connections
                }

                clientBuilder.ConfigurePrimaryHttpMessageHandler(() => { return handler; });
                options.ConfigureClientBuilder?.Invoke(clientBuilder);
            };

        private static Action<HttpClient> ConfigureHttpClient(KepwareApiClientOptions options)
            => client =>
            {
                client.BaseAddress = options.HostUri;
                client.Timeout = options.Timeout;
                client.DefaultRequestHeaders.Add("Accept", "application/json");
                client.DefaultRequestHeaders.Add("User-Agent", "KepwareSync");

                if (!string.IsNullOrEmpty(options.Username) && !string.IsNullOrEmpty(options.Password))
                {
                    string credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{options.Username}:{options.Password}"));
                    client.DefaultRequestHeaders.Add("Authorization", $"Basic {credentials}");
                }

                options.ConfigureClient?.Invoke(client);
            };
    }
}
