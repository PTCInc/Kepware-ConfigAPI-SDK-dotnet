namespace Kepware.Api.Model
{
    /// <summary>
    /// Specifies the publish type for IoT Gateway agents.
    /// </summary>
    public enum IotPublishType
    {
        /// <summary>Publish at a set interval rate.</summary>
        Interval = 0,
        /// <summary>Publish on data change.</summary>
        OnDataChange = 1
    }

    /// <summary>
    /// Specifies the publish format for IoT Gateway agents.
    /// </summary>
    public enum IotPublishFormat
    {
        /// <summary>Output based on tags that have changed value or quality.</summary>
        NarrowFormat = 0,
        /// <summary>Output includes all enabled tags regardless of changes.</summary>
        WideFormat = 1
    }

    /// <summary>
    /// Specifies the message format for IoT Gateway agents.
    /// </summary>
    public enum IotMessageFormat
    {
        /// <summary>Standard template format.</summary>
        StandardTemplate = 0,
        /// <summary>Advanced template format.</summary>
        AdvancedTemplate = 1
    }

    /// <summary>
    /// Specifies the MQTT Quality of Service level.
    /// </summary>
    public enum MqttQos
    {
        /// <summary>At most once delivery (fire and forget).</summary>
        AtMostOnce = 0,
        /// <summary>At least once delivery (acknowledged delivery).</summary>
        AtLeastOnce = 1,
        /// <summary>Exactly once delivery (assured delivery).</summary>
        ExactlyOnce = 2
    }

    /// <summary>
    /// Specifies the TLS version for MQTT secure connections.
    /// </summary>
    public enum MqttTlsVersion
    {
        /// <summary>Default TLS version.</summary>
        Default = 0,
        /// <summary>TLS version 1.0.</summary>
        V1_0 = 1,
        /// <summary>TLS version 1.1.</summary>
        V1_1 = 2,
        /// <summary>TLS version 1.2.</summary>
        V1_2 = 3
    }

    /// <summary>
    /// Specifies the HTTP method for REST Client agents.
    /// </summary>
    public enum RestClientHttpMethod
    {
        /// <summary>HTTP POST method.</summary>
        Post = 0,
        /// <summary>HTTP PUT method.</summary>
        Put = 1
    }

    /// <summary>
    /// Specifies the content-type header for REST Client agent publish.
    /// </summary>
    public enum RestClientMediaType
    {
        /// <summary>application/json</summary>
        ApplicationJson = 0,
        /// <summary>application/xml</summary>
        ApplicationXml = 1,
        /// <summary>application/xhtml+xml</summary>
        ApplicationXhtmlXml = 2,
        /// <summary>text/plain</summary>
        TextPlain = 3,
        /// <summary>text/html</summary>
        TextHtml = 4
    }

    /// <summary>
    /// Specifies the data type for IoT Items.
    /// </summary>
    public enum IotItemDataType
    {
        /// <summary>Default / auto-detect.</summary>
        Default = -1,
        /// <summary>String data type.</summary>
        String = 0,
        /// <summary>Boolean data type.</summary>
        Boolean = 1,
        /// <summary>Char data type.</summary>
        Char = 2,
        /// <summary>Byte data type.</summary>
        Byte = 3,
        /// <summary>Short (16-bit signed integer) data type.</summary>
        Short = 4,
        /// <summary>Word (16-bit unsigned integer) data type.</summary>
        Word = 5,
        /// <summary>Long (32-bit signed integer) data type.</summary>
        Long = 6,
        /// <summary>DWord (32-bit unsigned integer) data type.</summary>
        DWord = 7,
        /// <summary>Float (32-bit floating point) data type.</summary>
        Float = 8,
        /// <summary>Double (64-bit floating point) data type.</summary>
        Double = 9,
        /// <summary>BCD (binary-coded decimal) data type.</summary>
        BCD = 10,
        /// <summary>LBCD (long binary-coded decimal) data type.</summary>
        LBCD = 11,
        /// <summary>Date data type.</summary>
        Date = 12,
        /// <summary>LLong (64-bit signed integer) data type.</summary>
        LLong = 13,
        /// <summary>QWord (64-bit unsigned integer) data type.</summary>
        QWord = 14,
        /// <summary>String array data type.</summary>
        StringArray = 20,
        /// <summary>Boolean array data type.</summary>
        BooleanArray = 21,
        /// <summary>Char array data type.</summary>
        CharArray = 22,
        /// <summary>Byte array data type.</summary>
        ByteArray = 23,
        /// <summary>Short array data type.</summary>
        ShortArray = 24,
        /// <summary>Word array data type.</summary>
        WordArray = 25,
        /// <summary>Long array data type.</summary>
        LongArray = 26,
        /// <summary>DWord array data type.</summary>
        DWordArray = 27,
        /// <summary>Float array data type.</summary>
        FloatArray = 28,
        /// <summary>Double array data type.</summary>
        DoubleArray = 29,
        /// <summary>BCD array data type.</summary>
        BCDArray = 30,
        /// <summary>LBCD array data type.</summary>
        LBCDArray = 31,
        /// <summary>Date array data type.</summary>
        DateArray = 32,
        /// <summary>LLong array data type.</summary>
        LLongArray = 33,
        /// <summary>QWord array data type.</summary>
        QWordArray = 34
    }
}
