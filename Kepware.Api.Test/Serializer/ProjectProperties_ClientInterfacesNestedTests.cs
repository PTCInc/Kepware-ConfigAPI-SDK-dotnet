using System;
using System.Text.Json;
using Kepware.Api.Model;
using Xunit;

namespace Kepware.Api.Test.Serializer
{
    public class ProjectProperties_ClientInterfacesNestedTests
    {
        [Fact]
        public void DeserializeNestedClientInterfaces_ShouldPopulateProjectProperties()
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

            var project = JsonSerializer.Deserialize(json, Api.Serializer.KepJsonContext.Default.Project);
            Assert.NotNull(project);
            // After normalization, dynamic properties should contain flattened keys
            Assert.False(project.GetDynamicProperty<bool>("ddeserver.ENABLE"));
            Assert.Equal("ptcdde", project.GetDynamicProperty<string>("ddeserver.SERVICE_NAME"));
        }

        [Fact]
        public void Serialize_ProjectWithModifiedProperties_ShouldEmitClientInterfacesArray()
        {
            var project = new Project();
            project.SetDynamicProperty("ddeserver.ENABLE", true);
            project.SetDynamicProperty("ddeserver.SERVICE_NAME", "ptcdde");

            var json = JsonSerializer.Serialize(project, Api.Serializer.KepJsonContext.Default.Project);
            Assert.Contains("client_interfaces", json);
            Assert.Contains("ddeserver.SERVICE_NAME", json);
        }
    }
}
