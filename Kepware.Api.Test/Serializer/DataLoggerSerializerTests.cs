using Kepware.Api.Model;
using Kepware.Api.Serializer;

namespace Kepware.Api.Test.Serializer
{
    public class DataLoggerSerializerTests
    {
        [Fact]
        public void KepJsonContext_GetJsonTypeInfo_LogGroup_ShouldNotThrow()
        {
            var typeInfo = KepJsonContext.GetJsonTypeInfo<LogGroup>();
            Assert.NotNull(typeInfo);
        }

        [Fact]
        public void KepJsonContext_GetJsonTypeInfo_LogItem_ShouldNotThrow()
        {
            var typeInfo = KepJsonContext.GetJsonTypeInfo<LogItem>();
            Assert.NotNull(typeInfo);
        }

        [Fact]
        public void KepJsonContext_GetJsonTypeInfo_ColumnMapping_ShouldNotThrow()
        {
            var typeInfo = KepJsonContext.GetJsonTypeInfo<ColumnMapping>();
            Assert.NotNull(typeInfo);
        }

        [Fact]
        public void KepJsonContext_GetJsonTypeInfo_Trigger_ShouldNotThrow()
        {
            var typeInfo = KepJsonContext.GetJsonTypeInfo<Trigger>();
            Assert.NotNull(typeInfo);
        }

        [Fact]
        public void KepJsonContext_GetJsonTypeInfo_LogItemGroup_ShouldNotThrow()
        {
            var typeInfo = KepJsonContext.GetJsonTypeInfo<LogItemGroup>();
            Assert.NotNull(typeInfo);
        }

        [Fact]
        public void KepJsonContext_GetJsonTypeInfo_ColumnMappingGroup_ShouldNotThrow()
        {
            var typeInfo = KepJsonContext.GetJsonTypeInfo<ColumnMappingGroup>();
            Assert.NotNull(typeInfo);
        }

        [Fact]
        public void KepJsonContext_GetJsonTypeInfo_TriggerGroup_ShouldNotThrow()
        {
            var typeInfo = KepJsonContext.GetJsonTypeInfo<TriggerGroup>();
            Assert.NotNull(typeInfo);
        }

        [Fact]
        public void KepJsonContext_GetJsonTypeInfo_DataLoggerContainer_ShouldNotThrow()
        {
            var typeInfo = KepJsonContext.Default.DataLoggerContainer;
            Assert.NotNull(typeInfo);
        }

        [Fact]
        public void KepJsonContext_GetJsonListTypeInfo_LogGroup_ShouldNotThrow()
        {
            var typeInfo = KepJsonContext.GetJsonListTypeInfo<LogGroup>();
            Assert.NotNull(typeInfo);
        }

        [Fact]
        public void KepJsonContext_GetJsonListTypeInfo_LogItem_ShouldNotThrow()
        {
            var typeInfo = KepJsonContext.GetJsonListTypeInfo<LogItem>();
            Assert.NotNull(typeInfo);
        }

        [Fact]
        public void KepJsonContext_GetJsonListTypeInfo_ColumnMapping_ShouldNotThrow()
        {
            var typeInfo = KepJsonContext.GetJsonListTypeInfo<ColumnMapping>();
            Assert.NotNull(typeInfo);
        }

        [Fact]
        public void KepJsonContext_GetJsonListTypeInfo_Trigger_ShouldNotThrow()
        {
            var typeInfo = KepJsonContext.GetJsonListTypeInfo<Trigger>();
            Assert.NotNull(typeInfo);
        }

        [Fact]
        public void KepJsonContext_GetJsonListTypeInfo_LogItemGroup_ShouldNotThrow()
        {
            var typeInfo = KepJsonContext.GetJsonListTypeInfo<LogItemGroup>();
            Assert.NotNull(typeInfo);
        }

        [Fact]
        public void KepJsonContext_GetJsonListTypeInfo_ColumnMappingGroup_ShouldNotThrow()
        {
            var typeInfo = KepJsonContext.GetJsonListTypeInfo<ColumnMappingGroup>();
            Assert.NotNull(typeInfo);
        }

        [Fact]
        public void KepJsonContext_GetJsonListTypeInfo_TriggerGroup_ShouldNotThrow()
        {
            var typeInfo = KepJsonContext.GetJsonListTypeInfo<TriggerGroup>();
            Assert.NotNull(typeInfo);
        }
    }
}
