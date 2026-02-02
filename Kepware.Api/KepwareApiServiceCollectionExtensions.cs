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
    /// <summary>
    /// Provides extension methods for registering Kepware Configuration API clients with the dependency injection container.
    /// </summary>
    public static class KepwareApiServiceCollectionExtensions
    {
        /// <summary>
        /// Adds multiple Kepware Configuration API clients to the service collection.
        /// </summary>
        /// <param name="services">The service collection to add the clients to.</param>
        /// <param name="options">A collection of key-value pairs where the key is the client name and the value is the client options.</param>
        /// <returns>The updated service collection.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the services parameter is null.</exception>
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

        /// <summary>
        /// Adds a single Kepware Configuration API client to the service collection.
        /// </summary>
        /// <param name="services">The service collection to add the client to.</param>
        /// <param name="name">The name of the client.</param>
        /// <param name="options">The options for configuring the client.</param>
        /// <returns>The updated service collection.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the services or options parameter is null.</exception>
        /// <exception cref="ArgumentException">Thrown when the name parameter is null or empty.</exception>
        /// <exception cref="InvalidOperationException">Thrown when a client with the same name is already registered or the host URI is not absolute.</exception>
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

        /// <summary>
        /// Adds a single Kepware API client to the service collection with specified parameters.
        /// </summary>
        /// <param name="services">The service collection to add the client to.</param>
        /// <param name="name">The name of the client.</param>
        /// <param name="baseUrl">The base URL of the Kepware Configuration API in the format of https://{hostname}:{port} or http://{hostname}:{port}</param>
        /// <param name="apiUserName">The username for authentication.</param>
        /// <param name="apiPassword">The password for authentication.</param>
        /// <param name="timeoutInSeconds">The timeout period for the HTTP client in seconds. Default is 60 seconds.</param>
        /// <param name="disableCertificateValidation">Indicates whether to disable certificate validation. Default is false.</param>
        /// <param name="configureClient">An optional action to configure the <see cref="HttpClient"/>.</param>
        /// <param name="configureHttpClientBuilder">An optional action to configure the <see cref="IHttpClientBuilder"/>.</param>
        /// <returns>The updated service collection.</returns>
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

        /// <summary>
        /// Configures the HTTP client builder with the specified options.
        /// </summary>
        /// <param name="options">The options for configuring the client builder.</param>
        /// <returns>An action to configure the <see cref="IHttpClientBuilder"/>.</returns>
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

        /// <summary>
        /// Configures the HTTP client with the specified options.
        /// </summary>
        /// <param name="options">The options for configuring the client.</param>
        /// <returns>An action to configure the <see cref="HttpClient"/>.</returns>
        private static Action<HttpClient> ConfigureHttpClient(KepwareApiClientOptions options)
            => client =>
            {
                client.BaseAddress = options.HostUri;
                client.Timeout = options.Timeout;
                client.DefaultRequestHeaders.Add("Accept", "application/json");
                client.DefaultRequestHeaders.Add("User-Agent", "Kepware.Api Client");

                if (!string.IsNullOrEmpty(options.Username))
                {
                    string credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{options.Username}:{options.Password ?? string.Empty}"));
                    client.DefaultRequestHeaders.Add("Authorization", $"Basic {credentials}");
                }

                options.ConfigureClient?.Invoke(client);
            };
    }
}
