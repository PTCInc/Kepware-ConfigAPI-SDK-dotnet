using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kepware.Api.Model
{
    public static class EntityFactory
    {
        private static readonly Dictionary<Type, Func<BaseEntity>> Factories = new();

        static EntityFactory()
        {
            // Registrierung aller Typen, die instanziiert werden können
            Factories[typeof(Project)] = () => new Project();
            Factories[typeof(Channel)] = () => new Channel();
            Factories[typeof(Device)] = () => new Device();
            Factories[typeof(Tag)] = () => new Tag();
            Factories[typeof(DefaultEntity)] = () => new DefaultEntity();
            Factories[typeof(NamedEntity)] = () => new NamedEntity();
            Factories[typeof(MqttClientAgent)] = () => new MqttClientAgent();
            Factories[typeof(RestClientAgent)] = () => new RestClientAgent();
            Factories[typeof(RestServerAgent)] = () => new RestServerAgent();
            Factories[typeof(IotItem)] = () => new IotItem();
        }

        public static BaseEntity CreateInstance(Type type)
        {
            if (!Factories.ContainsKey(type))
            {
                throw new InvalidOperationException($"No factory registered for type {type}");
            }

            return Factories[type]();
        }
    }
}
