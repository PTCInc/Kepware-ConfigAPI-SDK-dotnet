using Kepware.SyncService.ProjectStorage;
using Polly;
using Polly.Extensions.Http;
using System.CommandLine;
using Kepware.SyncService.Configuration;
using Serilog;
using ILogger = Microsoft.Extensions.Logging.ILogger;
using Kepware.Api.Serializer;
using Kepware.Api;

namespace Kepware.SyncService
{
    internal class Program
    {
        static Task Main(string[] args)
        {
            var builder = Host.CreateApplicationBuilder();

            var cfgBuilder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables();
            
            var configuration = cfgBuilder.Build();
            builder.Configuration.AddConfiguration(configuration);
            Console.WriteLine("Dumb stuff");    
            var app = new AppRunner(builder);

            // Binder
            var kepApiBinder = new KepApiOptionsBinder(configuration);
            var kepStorageBinder = new KepStorageOptionsBinder(configuration);
            var kepSyncBinder = new KepSyncOptionsBinder(configuration);

            // Root Command
            var rootCommand = new RootCommand("KepwareSync CLI Tool");
            kepApiBinder.BindTo(rootCommand);
            kepStorageBinder.BindTo(rootCommand);
            kepSyncBinder.BindTo(rootCommand);

            rootCommand.SetHandler(app.RunRootCommand, kepApiBinder, kepStorageBinder, kepSyncBinder);

            // SyncToDisk Command
            var syncToDiskCommand = new Command("SyncToDisk", "Synchronize data to disk");
            kepApiBinder.BindTo(syncToDiskCommand);
            kepStorageBinder.BindTo(syncToDiskCommand);

            syncToDiskCommand.SetHandler(app.SyncToDisk, kepApiBinder, kepStorageBinder);

            // SyncFromDisk Command
            var syncFromDiskCommand = new Command("SyncFromDisk", "Synchronize data from disk");
            kepApiBinder.BindTo(syncFromDiskCommand);
            kepStorageBinder.BindTo(syncFromDiskCommand);

            syncFromDiskCommand.SetHandler(app.SyncFromDisk, kepApiBinder, kepStorageBinder);

            // Commands hinzufügen
            rootCommand.AddCommand(syncToDiskCommand);
            rootCommand.AddCommand(syncFromDiskCommand);

            return rootCommand.InvokeAsync(args);
        }
        private sealed class AppRunner(HostApplicationBuilder builder)
        {

            public async Task RunRootCommand(KepApiOptions apiOptions, KepStorageOptions kepStorageOptions, KepSyncOptions syncOption)
            {
                ConfigureHost(apiOptions, kepStorageOptions, syncOption);
                builder.Services.AddHostedService<SyncService>();
                var host = builder.Build();
                await host.RunAsync();
            }

            private SyncService CreateSyncService(KepApiOptions apiOptions, KepStorageOptions kepStorageOptions, KepSyncOptions? syncOptions = null)
            {
                ConfigureHost(apiOptions, kepStorageOptions, syncOptions);
                builder.Services.AddSingleton<SyncService>();
                var host = builder.Build();
                return host.Services.GetRequiredService<SyncService>();
            }

            public async Task SyncToDisk(KepApiOptions apiOptions, KepStorageOptions kepStorageOptions)
            {
                var syncService = CreateSyncService(apiOptions, kepStorageOptions);
                await syncService.SyncFromPrimaryKepServerAsync();

                Console.WriteLine("Sync to disk completed.");
            }

            public async Task SyncFromDisk(KepApiOptions apiOptions, KepStorageOptions kepStorageOptions)
            {
                var syncService = CreateSyncService(apiOptions, kepStorageOptions);
                await syncService.SyncFromLocalFileAsync();

                Console.WriteLine("Sync from disk completed.");
            }

            #region BuildHost
            private void ConfigureHost(KepApiOptions apiOptions, KepStorageOptions kepStorageOptions, KepSyncOptions? syncOptions = null)
            {
                builder.Services.AddLogging();
                builder.Services.AddSerilog((services, loggerConfiguration) =>
                {
                    loggerConfiguration
                        .Enrich.FromLogContext()
                        .MinimumLevel.Information()
                        .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
                        .MinimumLevel.Override("System", Serilog.Events.LogEventLevel.Warning)
                        .WriteTo.Console()
                        .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day);
                });

                builder.Services.AddSingleton(syncOptions ?? new KepSyncOptions());
                builder.Services.AddSingleton(apiOptions);
                builder.Services.AddSingleton(kepStorageOptions);
                builder.Services.AddSingleton<YamlSerializer>();
                builder.Services.AddSingleton<CsvTagSerializer>();
                builder.Services.AddSingleton<IProjectStorage, KepFolderStorage>();

                if (string.IsNullOrEmpty(apiOptions.Primary.Host))
                    throw new ArgumentException("Primary Host is required");

                builder.Services.AddKepwareApiClient(nameof(apiOptions.Primary),
                    new KepwareApiClientOptions
                    {
                        HostUri = new Uri(apiOptions.Primary.Host),
                        Username = apiOptions.Primary.UserName,
                        Password = apiOptions.Primary.Password,
                        Timeout = TimeSpan.FromSeconds(apiOptions.TimeoutInSeconds),
                        DisableCertifcateValidation = apiOptions.DisableCertificateValidation,
                        ConfigureClientBuilder = ConfigureRetryPolicy,
                        Tag = apiOptions.Primary
                    });

                Dictionary<string, KepwareApiClientOptions> secondaryClients = [];
                for (int i = 0; i < apiOptions.Secondary.Count; i++)
                {
                    var secondaryClient = apiOptions.Secondary[i];

                    if (string.IsNullOrEmpty(secondaryClient.Host))
                        throw new ArgumentException($"Secondary Host {i + 1} is required");

                    secondaryClients.Add($"{nameof(apiOptions.Secondary)}-{i + 1:00}",
                        new KepwareApiClientOptions
                        {
                            HostUri = new Uri(secondaryClient.Host),
                            Username = secondaryClient.UserName,
                            Password = secondaryClient.Password,
                            Timeout = TimeSpan.FromSeconds(apiOptions.TimeoutInSeconds),
                            DisableCertifcateValidation = apiOptions.DisableCertificateValidation,
                            ConfigureClientBuilder = ConfigureRetryPolicy,
                            Tag = secondaryClient
                        });
                }

                builder.Services.AddKepwareApiClients(secondaryClients);
            }
            #endregion

            #region retryPolicy

            private static void ConfigureRetryPolicy(IHttpClientBuilder httpClientBuilder)
                => httpClientBuilder.AddPolicyHandler(GetRetryPolicy());

            private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
            {
                return HttpPolicyExtensions
                    .HandleTransientHttpError()
                    .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                        onRetry: (outcome, timespan, retryAttempt, context) =>
                        {
                            if (context.TryGetValue("Logger", out var objLogger) && objLogger is ILogger logger)
                                logger.LogWarning("Delaying for {DelayTime} seconds, then making retry {RetryAttempt}", timespan.TotalSeconds, retryAttempt);
                        });
            }
            #endregion

        }
    }
}
