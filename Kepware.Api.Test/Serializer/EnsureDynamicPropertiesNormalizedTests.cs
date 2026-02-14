using System;
using System.Collections.Generic;
using System.Text.Json;
using Kepware.Api.Model;
using Kepware.Api.Serializer;
using Xunit;

namespace Kepware.Api.Test.Serializer
{
    /// <summary>
    /// Tests for the template method pattern in EnsureDynamicPropertiesNormalized and NormalizeNestedProperties.
    /// </summary>
    public class EnsureDynamicPropertiesNormalizedTests
    {
        /// <summary>
        /// Mock entity that demonstrates custom normalization via NormalizeNestedProperties override.
        /// </summary>
        private class MockEntityWithCustomNormalization : DefaultEntity
        {
            public bool NormalizeWasCalled { get; internal set; } = false;

            /// <summary>
            /// Demonstrates how a derived class implements custom normalization.
            /// </summary>
            protected override void NormalizeNestedProperties()
            {
                NormalizeWasCalled = true;

                // Flatten mock "custom_interfaces" array similar to how Project flattens "client_interfaces"
                if (DynamicProperties.TryGetValue("custom_interfaces", out var ciElement) &&
                    ciElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in ciElement.EnumerateArray())
                    {
                        if (item.ValueKind != JsonValueKind.Object) continue;
                        foreach (var prop in item.EnumerateObject())
                        {
                            if (prop.NameEquals("common.ALLTYPES_NAME")) continue;
                            DynamicProperties[prop.Name] = prop.Value.Clone();
                        }
                    }

                    DynamicProperties.Remove("custom_interfaces");
                }
            }
        }

        [Fact]
        public void EnsureDynamicPropertiesNormalized_CallsNormalizeNestedProperties()
        {
            var entity = new MockEntityWithCustomNormalization
            {
                IncludesNestedDynamicProperties = true
            };
            
            // Setting a property should trigger normalization
            entity.SetDynamicProperty("test.key", "test.value");
            
            // Verify that NormalizeNestedProperties was called
            Assert.True(entity.NormalizeWasCalled, "NormalizeNestedProperties should have been called");
        }

        [Fact]
        public void BaseEntity_DefaultNormalization_DoesNothing()
        {
            var entity = new DefaultEntity
            {
                IncludesNestedDynamicProperties = true
            };

            // Add a mock nested array that would be processed if default impl handled it
            var nestedArray = JsonSerializer.SerializeToElement(new[]
            {
                new Dictionary<string, object?> 
                { 
                    ["common.ALLTYPES_NAME"] = "interface1",
                    ["interface1.PROPERTY"] = "value"
                }
            });

            entity.DynamicProperties["custom_array"] = nestedArray;

            // Trigger normalization
            // Exception would be expected if base implementation tried to process the array
            Assert.Throws<NotSupportedException>(() => entity.GetDynamicProperty<Array?>("custom_array"));

            // Base implementation should NOT flatten custom arrays
            Assert.True(entity.DynamicProperties.ContainsKey("custom_array"),
                "Base implementation should not flatten custom arrays");
        }

        [Fact]
        public void DerivedClass_CustomNormalization_FlattensMockInterfaces()
        {
            var entity = new MockEntityWithCustomNormalization
            {
                IncludesNestedDynamicProperties = true
            };

            // Simulate nested properties from API response
            var nestedArray = JsonSerializer.SerializeToElement(new[]
            {
                new Dictionary<string, object?> 
                { 
                    ["common.ALLTYPES_NAME"] = "mock_interface",
                    ["mock.PROPERTY1"] = "value1",
                    ["mock.PROPERTY2"] = 42
                }
            });

            entity.DynamicProperties["custom_interfaces"] = nestedArray;

            // Trigger normalization
            entity.GetDynamicProperty<string?>("mock.PROPERTY1");

            // After normalization, nested array should be flattened
            Assert.False(entity.DynamicProperties.ContainsKey("custom_interfaces"), 
                "Nested custom_interfaces should be removed after normalization");
            Assert.True(entity.DynamicProperties.ContainsKey("mock.PROPERTY1"), 
                "Flattened properties should exist in DynamicProperties");
            Assert.Equal("value1", entity.GetDynamicProperty<string>("mock.PROPERTY1"));
        }

        [Fact]
        public void Project_NormalizeNestedProperties_FlattenClientInterfaces()
        {
            var json = """
            {
                "client_interfaces": [
                    {  
                        "common.ALLTYPES_NAME": "ddeserver",
                        "ddeserver.ENABLE": false,
                        "ddeserver.SERVICE_NAME": "ptcdde"
                    },
                    {  
                        "common.ALLTYPES_NAME": "opcdaserver",
                        "opcdaserver.ENABLE": true
                    }  
                ]
            }
            """;

            var project = JsonSerializer.Deserialize(json, KepJsonContext.Default.Project);
            Assert.NotNull(project);

            // Project's normalization should flatten client_interfaces
            Assert.False(project.GetDynamicProperty<bool>("ddeserver.ENABLE"));
            Assert.Equal("ptcdde", project.GetDynamicProperty<string>("ddeserver.SERVICE_NAME"));
            Assert.True(project.GetDynamicProperty<bool>("opcdaserver.ENABLE"));
        }

        [Fact]
        public void EnsureDynamicPropertiesNormalized_OnlyNormalizedOnce()
        {
            var entity = new MockEntityWithCustomNormalization
            {
                IncludesNestedDynamicProperties = true
            };

            // First call should trigger NormalizeNestedProperties
            entity.DynamicProperties["test.key"] = KepJsonContext.WrapInJsonElement("test.value");
            entity.GetDynamicProperty<string?>("test.key");
            var firstCallResult = entity.NormalizeWasCalled;

            // Reset the flag
            entity.NormalizeWasCalled = false;

            // Second call should NOT trigger NormalizeNestedProperties again
            entity.GetDynamicProperty<string?>("test.key");
            var secondCallResult = entity.NormalizeWasCalled;

            Assert.True(firstCallResult, "First call should trigger NormalizeNestedProperties");
            Assert.False(secondCallResult, "Second call should not trigger NormalizeNestedProperties");
        }

        [Fact]
        public void SetDynamicProperty_TriggerNormalization()
        {
            var entity = new MockEntityWithCustomNormalization
            {
                IncludesNestedDynamicProperties = true
            };

            // Setting a property should trigger normalization
            entity.SetDynamicProperty("test.key", "test.value");

            Assert.True(entity.NormalizeWasCalled, "NormalizeNestedProperties should be called on SetDynamicProperty");
        }

        [Fact]
        public void TryGetGetDynamicProperty_TriggerNormalization()
        {
            var entity = new MockEntityWithCustomNormalization
            {
                IncludesNestedDynamicProperties = true
            };

            entity.DynamicProperties["test.key"] = KepJsonContext.WrapInJsonElement("test.value");

            // Trying to get a property should trigger normalization
            entity.TryGetDynamicProperty<string>("test.key", out _);

            Assert.True(entity.NormalizeWasCalled, "NormalizeNestedProperties should be called on TryGetDynamicProperty");
        }

        [Fact]
        public void Serialize_ProjectWithModifiedProperties_ShouldEmitClientInterfacesArray()
        {
            var project = new Project();
            project.SetDynamicProperty("ddeserver.ENABLE", true);
            project.SetDynamicProperty("ddeserver.SERVICE_NAME", "ptcdde");

            var json = JsonSerializer.Serialize(project, KepJsonContext.Default.Project);
            Assert.Contains("client_interfaces", json);
            Assert.Contains("ddeserver.SERVICE_NAME", json);
        }
    }
}
