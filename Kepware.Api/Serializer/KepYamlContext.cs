using Kepware.Api.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.Serialization;

namespace Kepware.Api.Serializer
{
    [YamlStaticContext]
    [YamlSerializable(typeof(Channel))]
    [YamlSerializable(typeof(Device))]
    [YamlSerializable(typeof(DeviceTagGroup))]
    [YamlSerializable(typeof(DefaultEntity))]
    [YamlSerializable(typeof(LogGroup))]
    [YamlSerializable(typeof(LogItem))]
    [YamlSerializable(typeof(ColumnMapping))]
    [YamlSerializable(typeof(Trigger))]
    [YamlSerializable(typeof(LogItemGroup))]
    [YamlSerializable(typeof(ColumnMappingGroup))]
    [YamlSerializable(typeof(TriggerGroup))]
    [YamlSerializable(typeof(DataLoggerContainer))]
    public partial class KepYamlContext : StaticContext
    {

    }
}
