using System.Text.Json;
using Kepware.Api.Model;
using Kepware.Api.Serializer;
using Kepware.Api.Util;

namespace Kepware.Api.Test.Model.DataLogger
{
    public class ColumnMappingModelTests
    {
        [Fact]
        public void ColumnMapping_Deserialize_ShouldPopulateProperties()
        {
            var json = """
            {
                "common.ALLTYPES_NAME": "defaultMapping",
                "datalogger.TABLE_ALIAS_LOG_ITEM_ID": "defaultMapping",
                "datalogger.TABLE_ALIAS_DATABASE_FIELD_NAME_NAME":      "Tag",
                "datalogger.TABLE_ALIAS_SQL_DATA_TYPE_NAME":            12,
                "datalogger.TABLE_ALIAS_SQL_LENGTH_NAME":               64,
                "datalogger.TABLE_ALIAS_DATABASE_FIELD_NAME_NUMERIC":   "ID",
                "datalogger.TABLE_ALIAS_SQL_DATA_TYPE_NUMERIC":         4,
                "datalogger.TABLE_ALIAS_SQL_LENGTH_NUMERIC":            0,
                "datalogger.TABLE_ALIAS_DATABASE_FIELD_NAME_QUALITY":   "Quality",
                "datalogger.TABLE_ALIAS_SQL_DATA_TYPE_QUALITY":         5,
                "datalogger.TABLE_ALIAS_SQL_LENGTH_QUALITY":            0,
                "datalogger.TABLE_ALIAS_DATABASE_FIELD_NAME_TIMESTAMP": "Timestamp",
                "datalogger.TABLE_ALIAS_SQL_DATA_TYPE_TIMESTAMP":       9,
                "datalogger.TABLE_ALIAS_SQL_LENGTH_TIMESTAMP":          0,
                "datalogger.TABLE_ALIAS_DATABASE_FIELD_NAME_VALUE":     "Value",
                "datalogger.TABLE_ALIAS_SQL_DATA_TYPE_VALUE":           12,
                "datalogger.TABLE_ALIAS_SQL_LENGTH_VALUE":              255,
                "datalogger.TABLE_ALIAS_DATABASE_FIELD_NAME_BATCHID":   "BatchID",
                "datalogger.TABLE_ALIAS_SQL_DATA_TYPE_BATCHID":         4,
                "datalogger.TABLE_ALIAS_SQL_LENGTH_BATCHID":            0
            }
            """;

            var mapping = JsonSerializer.Deserialize<ColumnMapping>(json, KepJsonContext.Default.ColumnMapping);

            Assert.NotNull(mapping);
            Assert.Equal("defaultMapping", mapping.Name);
            Assert.Equal("defaultMapping", mapping.LogItemServerItemName);
            Assert.Equal("Tag", mapping.FieldNameName);
            Assert.Equal(SqlDataType.VarChar, mapping.SqlDataTypeName);
            Assert.Equal(64, mapping.SqlLengthName);
            Assert.Equal("ID", mapping.FieldNameNumeric);
            Assert.Equal(SqlDataType.Integer, mapping.SqlDataTypeNumeric);
            Assert.Equal(0, mapping.SqlLengthNumeric);
            Assert.Equal("Quality", mapping.FieldNameQuality);
            Assert.Equal(SqlDataType.SmallInt, mapping.SqlDataTypeQuality);
            Assert.Equal("Timestamp", mapping.FieldNameTimestamp);
            Assert.Equal(SqlDataType.DateTime, mapping.SqlDataTypeTimestamp);
            Assert.Equal("Value", mapping.FieldNameValue);
            Assert.Equal(SqlDataType.VarChar, mapping.SqlDataTypeValue);
            Assert.Equal(255, mapping.SqlLengthValue);
            Assert.Equal("BatchID", mapping.FieldNameBatchId);
            Assert.Equal(SqlDataType.Integer, mapping.SqlDataTypeBatchId);
        }

        [Fact]
        public void ColumnMapping_SetProperties_ShouldUpdateDynamicProperties()
        {
            var mapping = new ColumnMapping("TestMapping");

            mapping.LogItemServerItemName = "MyItem";
            mapping.FieldNameValue = "val";
            mapping.SqlDataTypeValue = SqlDataType.Double;
            mapping.SqlLengthValue = 0;
            mapping.FieldNameTimestamp = "ts";
            mapping.SqlDataTypeTimestamp = SqlDataType.DateTime;

            Assert.Equal("TestMapping", mapping.Name);
            Assert.Equal("MyItem", mapping.LogItemServerItemName);
            Assert.Equal("val", mapping.FieldNameValue);
            Assert.Equal(SqlDataType.Double, mapping.SqlDataTypeValue);
            Assert.Equal(0, mapping.SqlLengthValue);
            Assert.Equal("ts", mapping.FieldNameTimestamp);
            Assert.Equal(SqlDataType.DateTime, mapping.SqlDataTypeTimestamp);
        }

        [Fact]
        public void ColumnMapping_RoundTrip_ShouldPreserveProperties()
        {
            var mapping = new ColumnMapping("RoundTripMapping");
            mapping.LogItemServerItemName = "SomeItem";
            mapping.FieldNameValue = "DataValue";
            mapping.SqlDataTypeValue = SqlDataType.Float;
            mapping.SqlLengthValue = 0;
            mapping.FieldNameName = "TagName";
            mapping.SqlDataTypeName = SqlDataType.VarChar;
            mapping.SqlLengthName = 128;

            var json = JsonSerializer.Serialize(mapping, KepJsonContext.Default.ColumnMapping);
            var deserialized = JsonSerializer.Deserialize<ColumnMapping>(json, KepJsonContext.Default.ColumnMapping);

            Assert.NotNull(deserialized);
            Assert.Equal("RoundTripMapping", deserialized.Name);
            Assert.Equal("SomeItem", deserialized.LogItemServerItemName);
            Assert.Equal("DataValue", deserialized.FieldNameValue);
            Assert.Equal(SqlDataType.Float, deserialized.SqlDataTypeValue);
            Assert.Equal(0, deserialized.SqlLengthValue);
            Assert.Equal("TagName", deserialized.FieldNameName);
            Assert.Equal(SqlDataType.VarChar, deserialized.SqlDataTypeName);
            Assert.Equal(128, deserialized.SqlLengthName);
        }

        [Fact]
        public void SqlDataType_Values_ShouldMatchApiIntegers()
        {
            Assert.Equal(-10, (int)SqlDataType.WLongVarChar);
            Assert.Equal(-9,  (int)SqlDataType.WVarChar);
            Assert.Equal(-8,  (int)SqlDataType.WChar);
            Assert.Equal(-7,  (int)SqlDataType.Bit);
            Assert.Equal(-6,  (int)SqlDataType.TinyInt);
            Assert.Equal(-5,  (int)SqlDataType.BigInt);
            Assert.Equal(-4,  (int)SqlDataType.LongVarBinary);
            Assert.Equal(-3,  (int)SqlDataType.VarBinary);
            Assert.Equal(-2,  (int)SqlDataType.Binary);
            Assert.Equal(-1,  (int)SqlDataType.LongVarChar);
            Assert.Equal(0,   (int)SqlDataType.Unknown);
            Assert.Equal(1,   (int)SqlDataType.Char);
            Assert.Equal(2,   (int)SqlDataType.Numeric);
            Assert.Equal(3,   (int)SqlDataType.Decimal);
            Assert.Equal(4,   (int)SqlDataType.Integer);
            Assert.Equal(5,   (int)SqlDataType.SmallInt);
            Assert.Equal(6,   (int)SqlDataType.Float);
            Assert.Equal(7,   (int)SqlDataType.Real);
            Assert.Equal(8,   (int)SqlDataType.Double);
            Assert.Equal(9,   (int)SqlDataType.DateTime);
            Assert.Equal(12,  (int)SqlDataType.VarChar);
        }

        [Fact]
        public void ColumnMapping_SqlType_ShouldRoundTripAsInt()
        {
            var mapping = new ColumnMapping("IntRoundTrip");
            mapping.SqlDataTypeValue = SqlDataType.Integer;

            var json = JsonSerializer.Serialize(mapping, KepJsonContext.Default.ColumnMapping);
            using var doc = JsonDocument.Parse(json);
            var raw = doc.RootElement.GetProperty("datalogger.TABLE_ALIAS_SQL_DATA_TYPE_VALUE");

            Assert.Equal(JsonValueKind.Number, raw.ValueKind);
            Assert.Equal(4, raw.GetInt32());

            var deserialized = JsonSerializer.Deserialize<ColumnMapping>(json, KepJsonContext.Default.ColumnMapping);
            Assert.Equal(SqlDataType.Integer, deserialized!.SqlDataTypeValue);
        }
    }
}
