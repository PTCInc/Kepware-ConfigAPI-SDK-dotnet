﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Formats.Asn1;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CsvHelper;
using CsvHelper.Configuration;
using KepwareSync.Model;

namespace KepwareSync
{

    public class CsvTagSerializer
    {
        private readonly string[] _headers =
        {
            "Tag Name",
            "Address",
            "Data Type",
            "Respect Data Type",
            "Client Access",
            "Scan Rate",
            "Scaling",
            "Raw Low",
            "Raw High",
            "Scaled Low",
            "Scaled High",
            "Scaled Data Type",
            "Clamp Low",
            "Clamp High",
            "Eng Units",
            "Description",
            "Negate Value"
        };

        private readonly HashSet<string> quotedField = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Tag Name", "Address", "Description"
        };

        private static Dictionary<string, object?> CreateTagDictionary(Tag tag, IDataTypeEnumConverter dataTypeEnumConverter)
        {
            bool scaling = tag.GetDynamicProperty<int>(Properties.Tag.ScalingType) != 0;
            return new Dictionary<string, object?>
            {
                { "Tag Name", tag.Name },
                { "Address", tag.GetDynamicProperty<string>(Properties.Tag.Address) },
                { "Data Type", dataTypeEnumConverter.ConvertToString(tag.GetDynamicProperty<int>(Properties.Tag.DataType)) },
                { "Respect Data Type", "1" }, // Assuming this aligns
                { "Client Access", tag.GetDynamicProperty<int>(Properties.Tag.ReadWriteAccess) == 1 ? "R/W" : "RO" },
                { "Scan Rate", tag.GetDynamicProperty<int>(Properties.Tag.ScanRateMilliseconds) },
                { "Scaling", tag.GetDynamicProperty<int>(Properties.Tag.ScalingType) },
                { "Raw Low", scaling ? tag.GetDynamicProperty<int>(Properties.Tag.ScalingRawLow) : null },
                { "Raw High", scaling ? tag.GetDynamicProperty<int>(Properties.Tag.ScalingRawHigh) : null },
                { "Scaled Low", scaling ? tag.GetDynamicProperty<int>(Properties.Tag.ScalingScaledLow) : null },
                { "Scaled High", scaling ? tag.GetDynamicProperty<int>(Properties.Tag.ScalingScaledHigh) : null },
                { "Scaled Data Type", scaling ? tag.GetDynamicProperty<int>(Properties.Tag.ScalingScaledDataType) : null },
                { "Clamp Low", scaling ? tag.GetDynamicProperty<bool>(Properties.Tag.ScalingClampLow) : null },
                { "Clamp High", scaling ? tag.GetDynamicProperty<bool>(Properties.Tag.ScalingClampHigh) : null },
                { "Eng Units", tag.GetDynamicProperty<string>(Properties.Tag.ScalingUnits) },
                { "Description", tag.Description },
                { "Negate Value", scaling ? tag.GetDynamicProperty<bool>(Properties.Tag.ScalingNegateValue) : null }
            };
        }

        //[DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(CsvHelper.Configuration.DefaultClassMap<CsvRecord>))]
        //[DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(CsvHelper.Configuration.MemberMap<CsvRecord, string>))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(CsvHelper.Expressions.RecordManager))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(CsvHelper.Expressions.RecordCreatorFactory))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(CsvHelper.Expressions.RecordHydrator))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(CsvHelper.Expressions.ExpressionManager))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(CsvHelper.TypeConversion.StringConverter))]
        public CsvTagSerializer()
        {

        }

        public Task ExportTagsAsync(string filePath, List<Tag> tags, IDataTypeEnumConverter dataTypeEnumConverter)
            => ExportTagsAsync(filePath, tags
                .Where(tag => tag.GetDynamicProperty<bool>(Properties.NonSerialized.Autogenerated) != true)
                .Select(tag => CreateTagDictionary(tag, dataTypeEnumConverter)));

        public async Task ExportTagsAsync(string filePath, IEnumerable<Dictionary<string, object?>> tags)
        {
            using var writer = new StreamWriter(filePath);
            using var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                ShouldQuote = args => (args.Field != null && quotedField.Contains(args.Field)) || ConfigurationFunctions.ShouldQuote(args)
            });

            // Write header
            if (tags.Any())
            {
                foreach (var header in _headers)
                {
                    csv.WriteField(header);
                }
                await csv.NextRecordAsync();

                // Write rows
                foreach (var tag in tags)
                {
                    foreach (var header in _headers)
                    {
                        csv.WriteField(tag.ContainsKey(header) ? tag[header] : null);
                    }
                    await csv.NextRecordAsync();
                }
            }
        }

        public Task<List<Dictionary<string, object?>>> ImportTagsAsync(string filePath)
        {
            var tags = new List<Dictionary<string, object?>>();

            using var reader = new StreamReader(filePath);
            using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
            });

            var records = csv.GetRecords<dynamic>();
            foreach (var record in records)
            {
                var dict = ((IDictionary<string, object?>)record).ToDictionary(k => k.Key, v => v.Value);
                tags.Add(dict);
            }

            return Task.FromResult(tags);
        }
    }
}
