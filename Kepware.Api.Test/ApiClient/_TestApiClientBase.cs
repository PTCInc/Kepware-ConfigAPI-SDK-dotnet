using Microsoft.Extensions.Logging;
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

namespace Kepware.Api.Test.ApiClient
{
    public abstract class TestApiClientBase
    {
        protected const string TEST_ENDPOINT = "http://localhost:57412";

        protected readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
        protected readonly Mock<ILogger<KepwareApiClient>> _loggerMock;
        protected readonly Mock<ILogger<AdminApiHandler>> _loggerMockAdmin;
        protected readonly Mock<ILogger<ProjectApiHandler>> _loggerMockProject;
        protected readonly Mock<ILogger<GenericApiHandler>> _loggerMockGeneric;
        protected readonly Mock<ILoggerFactory> _loggerFactoryMock;
        protected readonly KepwareApiClient _kepwareApiClient;

        protected TestApiClientBase()
        {
            _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
            _loggerMock = new Mock<ILogger<KepwareApiClient>>();
            _loggerMockAdmin = new Mock<ILogger<AdminApiHandler>>();
            _loggerMockGeneric = new Mock<ILogger<GenericApiHandler>>();
            _loggerMockProject = new Mock<ILogger<ProjectApiHandler>>();
            _loggerFactoryMock = new Mock<ILoggerFactory>();

            _loggerFactoryMock.Setup(factory => factory.CreateLogger(It.IsAny<string>())).Returns((string name) =>
            {
                if (name == typeof(KepwareApiClient).FullName)
                    return _loggerMock.Object;
                else if (name == typeof(AdminApiHandler).FullName)
                    return _loggerMockAdmin.Object;
                else if (name == typeof(GenericApiHandler).FullName)
                    return _loggerMockGeneric.Object;
                else if (name == typeof(ProjectApiHandler).FullName)
                    return _loggerMockProject.Object;
                else
                    return Mock.Of<ILogger>();
            });

            var httpClient = new HttpClient(_httpMessageHandlerMock.Object)
            {
                BaseAddress = new Uri(TEST_ENDPOINT)
            };

            _kepwareApiClient = new KepwareApiClient("TestClient", new KepwareApiClientOptions { HostUri = httpClient.BaseAddress }, _loggerFactoryMock.Object, httpClient);
        }

        protected static async Task<JsonProjectRoot> LoadJsonTestDataAsync(string filePath = "_data/simdemo_en-us.json")
        {
            var json = await File.ReadAllTextAsync(filePath);
            return JsonSerializer.Deserialize<JsonProjectRoot>(json, KepJsonContext.Default.JsonProjectRoot)!;
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

            // Mock for the status endpoint
            var statusResponse = "[{\"Name\": \"ConfigAPI REST Service\", \"Healthy\": true}]";
            _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, $"{TEST_ENDPOINT}/config/v1/status")
                .ReturnsResponse(HttpStatusCode.OK, statusResponse, "application/json");

            _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, $"{TEST_ENDPOINT}/config/v1/doc")
                .ReturnsResponse(HttpStatusCode.OK, statusResponse, "application/json");

            _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, $"{TEST_ENDPOINT}/config/v1/project")
                .ReturnsResponse(HttpStatusCode.OK, "[]", "application/json");
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

        private const string ENDPONT_FULL_PROJECT = "/config/v1/project?content=serialize";
        protected async Task ConfigureToServeFullProject(string filePath = "_data/simdemo_en-us.json")
        {
            var jsonData = await File.ReadAllTextAsync(filePath);
            _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, TEST_ENDPOINT + ENDPONT_FULL_PROJECT)
                                   .ReturnsResponse(jsonData, "application/json");
        }
    }
}
