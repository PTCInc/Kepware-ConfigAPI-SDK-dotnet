using System;
using System.Collections.Generic;
using System.Text.Json;
using Kepware.Api.Serializer;
using Xunit;

namespace Kepware.Api.Test.Serializer
{
    public class ClientInterfacesFlattenerTests
    {
        [Fact]
        public void FlattenFromObject_ShouldFlattenKnownInterfaceEntries()
        {
            var dynamicProps = new Dictionary<string, JsonElement>();

            var ddeiface = new Dictionary<string, object?>
            {
                ["common.ALLTYPES_NAME"] = "ddeserver",
                ["ddeserver.ENABLE"] = false,
                ["ddeserver.SERVICE_NAME"] = "ptcdde"
            };
            
            var opcdaiface = new Dictionary<string, object?>
            {
                ["common.ALLTYPES_NAME"] = "opcdaserver",
                ["opcdaserver.ENABLE"] = true
            };

            var list = new List<object?> { ddeiface, opcdaiface };

            ClientInterfacesFlattener.FlattenFromObject(list, dynamicProps);

            Assert.True(dynamicProps.ContainsKey("ddeserver.ENABLE"));
            Assert.True(dynamicProps.ContainsKey("ddeserver.SERVICE_NAME"));
            Assert.False(dynamicProps["ddeserver.ENABLE"].GetBoolean());
            Assert.Equal("ptcdde", dynamicProps["ddeserver.SERVICE_NAME"].GetString());
            Assert.True(dynamicProps.ContainsKey("opcdaserver.ENABLE"));
            Assert.True(dynamicProps["opcdaserver.ENABLE"].GetBoolean());
        }

        [Fact]
        public void BuildClientInterfacesArrayFromDynamicProperties_ShouldGroupInterfaceKeys()
        {
            var dynamicProps = new Dictionary<string, JsonElement>
            {
                ["ddeserver.ENABLE"] = Kepware.Api.Serializer.KepJsonContext.WrapInJsonElement(false),
                ["ddeserver.SERVICE_NAME"] = Kepware.Api.Serializer.KepJsonContext.WrapInJsonElement("ptcdde"),
                ["opcdaserver.ENABLE"] = Kepware.Api.Serializer.KepJsonContext.WrapInJsonElement(true),
                ["uaserverinterface.ENABLE"] = Kepware.Api.Serializer.KepJsonContext.WrapInJsonElement(true),
                ["servermain.PROJECT_TITLE"] = Kepware.Api.Serializer.KepJsonContext.WrapInJsonElement("MyProject")
            };

            var el = ClientInterfacesFlattener.BuildClientInterfacesArrayFromDynamicProperties(dynamicProps);

            Assert.NotNull(el);
            Assert.Equal(JsonValueKind.Array, el.Value.ValueKind);
            var arr = el.Value.EnumerateArray();
            foreach (var obj in arr)
            {
                if (obj.TryGetProperty("common.ALLTYPES_NAME", out var name) && name.GetString() == "ddeserver")
                {
                    Assert.True(obj.TryGetProperty("ddeserver.SERVICE_NAME", out var svc));
                    Assert.Equal("ptcdde", svc.GetString());
                    Assert.True(obj.TryGetProperty("ddeserver.ENABLE", out var en));
                    Assert.False(en.GetBoolean());
                }
                else if (obj.TryGetProperty("common.ALLTYPES_NAME", out var name2) && name2.GetString() == "opcdaserver")
                {
                    Assert.True(obj.TryGetProperty("opcdaserver.ENABLE", out var en));
                    Assert.True(en.GetBoolean());
                }
                else if (obj.TryGetProperty("common.ALLTYPES_NAME", out var name3) && name3.GetString() == "uaserverinterface")
                {
                    Assert.True(obj.TryGetProperty("uaserverinterface.ENABLE", out var en));
                    Assert.True(en.GetBoolean());
                }
                else
                {
                    Assert.Fail("Unexpected interface name in client_interfaces array");
                }
            }
        }
    }
}
