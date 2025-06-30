﻿using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Moq;
using Moq.Contrib.HttpClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Net;
using System.IO;
using Kepware.Api.Model;
using Kepware.Api.Serializer;
using Kepware.Api.ClientHandler;

namespace Kepware.Api.TestIntg.ApiClient
{
    public abstract class TestApiClientBase
    {
        protected string TEST_ENDPOINT = "http://localhost:57412";
        protected bool _testIntegration = false; // Set to true to run integration tests

        protected readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
        protected readonly Mock<ILogger<KepwareApiClient>> _loggerMock;
        protected readonly Mock<ILogger<AdminApiHandler>> _loggerMockAdmin;
        protected readonly Mock<ILogger<ProjectApiHandler>> _loggerMockProject;
        protected readonly Mock<ILogger<GenericApiHandler>> _loggerMockGeneric;
        protected readonly Mock<ILoggerFactory> _loggerFactoryMock;
        protected readonly KepwareApiClient _kepwareApiClient;
        protected readonly KepwareApiClient _badCredKepwareApiClient;
        protected readonly ProductInfo _productInfo;

        protected TestApiClientBase()
        {
            _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
            _loggerMock = new Mock<ILogger<KepwareApiClient>>();
            _loggerMockAdmin = new Mock<ILogger<AdminApiHandler>>();
            _loggerMockGeneric = new Mock<ILogger<GenericApiHandler>>();
            _loggerMockProject = new Mock<ILogger<ProjectApiHandler>>();
            _loggerFactoryMock = new Mock<ILoggerFactory>();

            //_loggerFactoryMock.Setup(factory => factory.CreateLogger(It.IsAny<string>())).Returns((string name) =>
            //{
            //    if (name == typeof(KepwareApiClient).FullName)
            //        return _loggerMock.Object;
            //    else if (name == typeof(AdminApiHandler).FullName)
            //        return _loggerMockAdmin.Object;
            //    else if (name == typeof(GenericApiHandler).FullName)
            //        return _loggerMockGeneric.Object;
            //    else if (name == typeof(ProjectApiHandler).FullName)
            //        return _loggerMockProject.Object;
            //    else
            //        return Mock.Of<ILogger>();
            //});
            //_kepwareApiClient = kepwareApiClient;

            // Load configuration from appsettings.json
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .Build();

            // Check if the test integration flag is set
            _testIntegration = bool.TryParse(configuration["TestSettings:IntegrationTest"], out var testIntegration) && testIntegration;

            TEST_ENDPOINT = $"{configuration["TestSettings:TestServer:Host"]}:{configuration["TestSettings:TestServer:Port"]}" ?? TEST_ENDPOINT;

            var testHost = Host.CreateDefaultBuilder()
                .ConfigureServices((context, services) =>
                {
                    // Add application services
                    services.AddKepwareApiClient(
                        name: "TestClient",
                        baseUrl: TEST_ENDPOINT,
                        apiUserName: $"{configuration["TestSettings:TestServer:UserName"]}",
                        apiPassword: $"{configuration["TestSettings:TestServer:Password"]}",
                        disableCertificateValidation: true
                        );
                    // Add application services
                    services.AddKepwareApiClient(
                        name: "BadCredClient",
                        baseUrl: TEST_ENDPOINT,
                        apiUserName: $"{configuration["TestSettings:TestServer:UserName"]}",
                        apiPassword: $"Test1234567890",
                        disableCertificateValidation: true
                        );
                })
                .ConfigureLogging(logging =>
                {
                    // Configure logging to use the console
                    logging.ClearProviders();
                    logging.AddConsole();
                    logging.SetMinimumLevel(LogLevel.Information);

                    logging.AddFilter("Microsoft", LogLevel.Warning);
                    logging.AddFilter("System", LogLevel.Warning);
                })
                .Build();

            // 2. Get the KepwareApiClient
            var listClients = testHost.Services.GetServices<KepwareApiClient>();
            _kepwareApiClient = listClients.First(client => client.ClientName == "TestClient");
            _badCredKepwareApiClient = listClients.First(client => client.ClientName == "BadCredClient");

            // 3. Get the ProductInfo

            // Update the assignment to properly await the asynchronous method and handle the nullable return type.
            var connected = _kepwareApiClient.TestConnectionAsync().GetAwaiter().GetResult();
            _productInfo = _kepwareApiClient.GetProductInfoAsync().GetAwaiter().GetResult() ?? new ProductInfo();


            //var httpClient = new HttpClient()
            //{
            //    BaseAddress = new Uri(TEST_ENDPOINT)
            //};


            //// Encode username and password as Base64
            ////var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{configuration["TestSettings:TestServer:UserName"]}:{configuration["TestSettings:TestServer:Password"]}"));
            ////httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", credentials);

            //_kepwareApiClient = new KepwareApiClient("TestClient", new KepwareApiClientOptions { HostUri = httpClient.BaseAddress, Username = configuration["TestSettings:TestServer:UserName"], Password = configuration["TestSettings:TestServer:Password"] }, _loggerFactoryMock.Object, httpClient);
        }

        protected static async Task<JsonProjectRoot> LoadJsonTestDataAsync()
        {
            var json = await File.ReadAllTextAsync("_data/simdemo_en-us.json");
            return JsonSerializer.Deserialize(json, KepJsonContext.Default.JsonProjectRoot)!;
        }

        protected async Task ConfigureToServeDrivers()
        {
            var jsonData = await File.ReadAllTextAsync("_data/doc_drivers.json");
            _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, $"{TEST_ENDPOINT}/config/v1/doc/drivers/")
                .ReturnsResponse(HttpStatusCode.OK, jsonData, "application/json");
        }

        protected void ConfigureConnectedClient(
            string productName = "KEPServerEX",
            string productId = "012",
            int majorVersion = 6,
            int minorVersion = 17,
            int buildVersion = 240,
            int patchVersion = 0)
        {
            _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, $"{TEST_ENDPOINT}/config/v1/about")
                .ReturnsResponse(HttpStatusCode.OK, $$"""
                    {
                        "product_name": "{{productName}}",
                        "product_id": "{{productId}}",
                        "product_version": "V{{majorVersion}}.{{minorVersion}}.{{buildVersion}}.{{patchVersion}}",
                        "product_version_major": {{majorVersion}},
                        "product_version_minor": {{minorVersion}},
                        "product_version_build": {{buildVersion}},
                        "product_version_patch": {{patchVersion}}
                    }
                    """, "application/json");

            //    // Mock for the status endpoint
            //    var statusResponse = "[{\"Name\": \"ConfigAPI REST Service\", \"Healthy\": true}]";
            //    _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, $"{TEST_ENDPOINT}/config/v1/status")
            //        .ReturnsResponse(HttpStatusCode.OK, statusResponse, "application/json");
        }

        protected Channel CreateTestChannel(string name = "TestChannel", string driver = "Advanced Simulator")
        {
            var channel = new Channel { Name = name };
            channel.SetDynamicProperty("servermain.MULTIPLE_TYPES_DEVICE_DRIVER", driver);
            return channel;
        }

        protected Device CreateTestDevice(Channel owner, string name = "TestDevice", string driver = "Advanced Simulator")
        {
            var device = new Device { Name = name, Owner = owner };
            device.SetDynamicProperty("servermain.MULTIPLE_TYPES_DEVICE_DRIVER", driver);
            return device;
        }

        protected List<Tag> CreateTestTags(int count = 2)
        {
            return Enumerable.Range(1, count)
                .Select(i => new Tag { Name = $"Tag{i}" })
                .ToList();
        }
    }
}
