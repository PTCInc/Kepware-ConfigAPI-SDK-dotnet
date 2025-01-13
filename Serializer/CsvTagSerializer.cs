﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

        private static Dictionary<string, object?> CreateTagDictionary(DefaultEntity tag, IDataTypeEnumConverter dataTypeEnumConverter)
        {
            bool sclaing = tag.GetDynamicProperty<int>("servermain.TAG_SCALING_TYPE") != 0;
            return new Dictionary<string, object?>
            {
                { "Tag Name", tag.Name },
                { "Address", tag.GetDynamicProperty<string>("servermain.TAG_ADDRESS") },
                { "Data Type", dataTypeEnumConverter.ConvertToString(tag.GetDynamicProperty<int>("servermain.TAG_DATA_TYPE")) },
                { "Respect Data Type", "1" }, // Assuming this aligns
                { "Client Access", tag.GetDynamicProperty<int>("servermain.TAG_READ_WRITE_ACCESS") == 1 ? "R/W" : "RO" },
                { "Scan Rate", tag.GetDynamicProperty<int>("servermain.TAG_SCAN_RATE_MILLISECONDS") },
                { "Scaling", tag.GetDynamicProperty<int>("servermain.TAG_SCALING_TYPE") },
                { "Raw Low", sclaing ? tag.GetDynamicProperty<int>("servermain.TAG_SCALING_RAW_LOW") : null },
                { "Raw High",sclaing ? tag.GetDynamicProperty<int>("servermain.TAG_SCALING_RAW_HIGH") : null },
                { "Scaled Low",sclaing ? tag.GetDynamicProperty<int>("servermain.TAG_SCALING_SCALED_LOW") : null },
                { "Scaled High",sclaing ? tag.GetDynamicProperty<int>("servermain.TAG_SCALING_SCALED_HIGH") : null },
                { "Scaled Data Type", sclaing ? tag.GetDynamicProperty < int >("servermain.TAG_SCALING_SCALED_DATA_TYPE") : null },
                { "Clamp Low",sclaing ? tag.GetDynamicProperty<bool>("servermain.TAG_SCALING_CLAMP_LOW"): null },
                { "Clamp High", sclaing ? tag.GetDynamicProperty<bool>("servermain.TAG_SCALING_CLAMP_HIGH") :null },
                { "Eng Units", tag.GetDynamicProperty<string>("servermain.TAG_SCALING_UNITS") },
                { "Description", tag.Description },
                { "Negate Value",sclaing ? tag.GetDynamicProperty<bool>("servermain.TAG_SCALING_NEGATE_VALUE") : null }
            };
        }


        public Task ExportTagsAsync(string filePath, List<DefaultEntity> tags, IDataTypeEnumConverter dataTypeEnumConverter)
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

        public async Task<List<Dictionary<string, object?>>> ImportTagsAsync(string filePath)
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

            return tags;
        }
    }
}
