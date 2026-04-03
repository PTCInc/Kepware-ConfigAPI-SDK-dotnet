namespace Kepware.Api.Model
{
    /// <summary>
    /// ODBC SQL data type codes used by DataLogger column mappings.
    /// Values correspond to the ODBC standard SQL type identifiers.
    /// </summary>
    public enum SqlDataType
    {
        /// <summary>Long Unicode character string (SQL_WLONGVARCHAR).</summary>
        WLongVarChar  = -10,
        /// <summary>Variable-length Unicode character string (SQL_WVARCHAR).</summary>
        WVarChar      = -9,
        /// <summary>Fixed-length Unicode character string (SQL_WCHAR).</summary>
        WChar         = -8,
        /// <summary>Single binary digit (SQL_BIT).</summary>
        Bit           = -7,
        /// <summary>8-bit integer (SQL_TINYINT).</summary>
        TinyInt       = -6,
        /// <summary>64-bit integer (SQL_BIGINT).</summary>
        BigInt        = -5,
        /// <summary>Long variable-length binary (SQL_LONGVARBINARY).</summary>
        LongVarBinary = -4,
        /// <summary>Variable-length binary (SQL_VARBINARY).</summary>
        VarBinary     = -3,
        /// <summary>Fixed-length binary (SQL_BINARY).</summary>
        Binary        = -2,
        /// <summary>Long variable-length character string (SQL_LONGVARCHAR).</summary>
        LongVarChar   = -1,
        /// <summary>Unknown or unspecified type (SQL_UNKNOWN_TYPE).</summary>
        Unknown       = 0,
        /// <summary>Fixed-length character string (SQL_CHAR).</summary>
        Char          = 1,
        /// <summary>Exact numeric with precision and scale (SQL_NUMERIC).</summary>
        Numeric       = 2,
        /// <summary>Exact numeric with precision and scale (SQL_DECIMAL).</summary>
        Decimal       = 3,
        /// <summary>32-bit integer (SQL_INTEGER).</summary>
        Integer       = 4,
        /// <summary>16-bit integer (SQL_SMALLINT).</summary>
        SmallInt      = 5,
        /// <summary>Approximate numeric floating-point (SQL_FLOAT).</summary>
        Float         = 6,
        /// <summary>Single-precision floating-point (SQL_REAL).</summary>
        Real          = 7,
        /// <summary>Double-precision floating-point (SQL_DOUBLE).</summary>
        Double        = 8,
        /// <summary>Date/time value (SQL_DATETIME).</summary>
        DateTime      = 9,
        /// <summary>Variable-length character string (SQL_VARCHAR).</summary>
        VarChar       = 12,
    }
}
