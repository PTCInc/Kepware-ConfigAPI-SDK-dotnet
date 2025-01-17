﻿using KepwareSync.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.Serialization.NamingConventions;
using YamlDotNet.Serialization;

namespace KepwareSync.Serializer
{
    public class YamlSerializer
    {
        private readonly ISerializer _serializer;
        private readonly IDeserializer _deserializer;

        public YamlSerializer()
        {
            var context = new KepYamlContext();

            var converter = new BaseEntityYamlTypeConverter(Properties.NonSerialized.AsHashSet);


            _serializer = new StaticSerializerBuilder(context)
                .WithTypeConverter(converter)
                .WithNamingConvention(CamelCaseNamingConvention.Instance) // Optional
                .Build();

            _deserializer = new StaticDeserializerBuilder(context)
                .WithTypeConverter(new BaseEntityYamlTypeConverter())
                .WithNamingConvention(CamelCaseNamingConvention.Instance) // Optional
                .Build();
        }

        public async Task<T> LoadFromYaml<T>(string filePath)
            where T : BaseEntity, new()
        {
            FileInfo file = new FileInfo(filePath);
            if (!file.Exists)
            {
                return default!;
            }
            var yaml = await System.IO.File.ReadAllTextAsync(filePath);
            var entity = _deserializer.Deserialize<T>(yaml);

            if (entity is NamedEntity namedEntity)
            {
                namedEntity.Name = file.DirectoryName!.Split('\\').Last();
            }
            
            return entity;
        }

        public async Task SaveAsYaml(string filePath, object entity)
        {
            var yaml = _serializer.Serialize(entity);
            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!); // Erstelle Verzeichnis, falls es nicht existiert

            if (yaml.Trim().Equals("{}"))
            {
                //don't write empty files
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
            }
            else
            {
                if (!File.Exists(filePath) ||  await File.ReadAllTextAsync(filePath) != yaml)
                {
                    await File.WriteAllTextAsync(filePath, yaml);
                }
                else
                {
                    // Content is the same -> dont rewrite to keep the change date & time
                }
            }
        }
    }

}
