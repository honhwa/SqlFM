using System.Collections.Generic;

namespace SqlFM.Core.Dialects.KeywordSets
{
    /// <summary>
    /// Transact-SQL (T-SQL) 扩展关键字、函数和数据类型集合。
    /// 在 ANSI 基础上添加 SQL Server 2008-2022 支持的专有语法元素。
    /// </summary>
    public static class TsqlKeywords
    {
        /// <summary>T-SQL 保留关键字（ANSI 基础之上新增）</summary>
        public static readonly HashSet<string> TsqlReserved = new HashSet<string>
        {
            // T-SQL 流程控制
            "BEGIN", "TRY", "CATCH", "THROW", "GOTO", "BREAK", "CONTINUE", "RETURN",
            "WAITFOR", "EXIT", "EXEC", "EXECUTE", "PRINT", "RAISERROR",

            // T-SQL DDL 扩展
            "TRUNCATE", "MERGE", "TOP", "OUTPUT", "INSERTED", "DELETED",
            "PROCEDURE", "PROC", "FUNCTION", "TRIGGER", "CURSOR",

            // T-SQL 安全
            "DENY", "REVOKE", "GRANT",

            // T-SQL 特殊语法
            "APPLY", "CROSS_APPLY", "OUTER_APPLY",
            "PIVOT", "UNPIVOT",
            "OVER", "ROW_NUMBER", "DENSE_RANK", "NTILE",
            "TABLESAMPLE", "OFFSET", "FETCH",
            "CLUSTERED", "NONCLUSTERED",
            "FILLFACTOR", "PAD_INDEX",
            "COMPUTE", "FOR", "BROWSE", "OPTION", "HINTS",
            "CHECKPOINT", "DBCC", "KILL", "BACKUP", "RESTORE",
            "LOAD", "RECONFIGURE", "SHUTDOWN", "STATISTICS",
            "DISK", "DUMP", "IDENTITY_INSERT", "RESEED",
            "REPLICATE", "SUBSTRING", "CHARINDEX", "PATINDEX",
            "STUFF", "QUOTENAME", "REVERSE", "REPLACE",
            "SPACE", "DIFFERENCE", "SOUNDEX",
            "LEFT", "RIGHT", "LEN", "DATALENGTH",
            "UNICODE", "NCHAR", "ASCII", "CHAR",
            "STR", "FORMAT", "CONVERT", "TRY_CONVERT",
            "CAST", "TRY_CAST", "PARSE", "TRY_PARSE",
            "COALESCE", "ISNULL", "NULLIF", "IIF",
            "CHOOSE", "LAG", "LEAD", "FIRST_VALUE", "LAST_VALUE",
            "PERCENT_RANK", "CUME_DIST", "PERCENTILE_CONT", "PERCENTILE_DISC",
            "STRING_AGG", "CONCAT_WS", "CONCAT", "TRANSLATE",
            "TRIM", "COMPRESS", "DECOMPRESS",

            // T-SQL 事务扩展
            "TRAN", "TRANSACTION", "SAVE", "TRANSACTION_NAME",
            "XACT_ABORT", "XACT_STATE", "LOCK", "HOLDLOCK",

            // T-SQL XML
            "XML", "OPENXML", "XMLDATA", "XMLNAMESPACES",

            // T-SQL JSON
            "JSON", "OPENJSON", "JSON_VALUE", "JSON_QUERY", "JSON_MODIFY",
            "ISJSON", "FOR_JSON"
        };

        /// <summary>T-SQL 非保留关键字</summary>
        public static readonly HashSet<string> TsqlUnreserved = new HashSet<string>
        {
            // T-SQL 系统对象
            "SYS", "SYSOBJECTS", "SYSCOLUMNS", "SYSTYPES", "SYSDATABASES",
            "SYSINDEXES", "SYSFILES", "SYSLOGINS", "SYSPERFINFO",
            "INFORMATION_SCHEMA", "DBO",

            // T-SQL 配置/选项
            "ANSI_NULLS", "ANSI_PADDING", "ANSI_WARNINGS", "ARITHABORT",
            "ARITHIGNORE", "CONCAT_NULL_YIELDS_NULL",
            "CURSOR_CLOSE_ON_COMMIT", "DATEFIRST", "DATEFORMAT",
            "DEADLOCK_PRIORITY", "DELAY", "DENSITY",
            "FIPS_FLAGGER", "FORCEPLAN", "IMPLICIT_TRANSACTIONS",
            "LANGUAGE", "LOCK_TIMEOUT", "NOCOUNT", "NOEXEC",
            "NUMERIC_ROUNDABORT", "PARSEONLY", "QUERY_GOVERNOR_COST_LIMIT",
            "REMOTE_PROC_TRANSACTIONS", "ROWCOUNT", "SHOWPLAN",
            "SHOWPLAN_ALL", "SHOWPLAN_TEXT", "STATISTICS_IO",
            "STATISTICS_PROFILE", "STATISTICS_TIME", "TEXTSIZE",
            "TRANSACTION_ISOLATION_LEVEL", "XACT_ABORT",

            // T-SQL 特殊标识符
            "NTEXT", "IMAGE", "MONEY", "SMALLMONEY",
            "DATETIME", "DATETIME2", "DATETIMEOFFSET", "SMALLDATETIME",
            "HIERARCHYID", "SQL_VARIANT", "UNIQUEIDENTIFIER", "GEOMETRY", "GEOGRAPHY",
            "TABLE", "ROWVERSION", "TIMESTAMP",
            "NVARCHAR", "NCHAR", "VARBINARY", "VARCHAR", "BINARY",

            // T-SQL 元数据
            "COLUMNPROPERTY", "TYPEPROPERTY", "OBJECTPROPERTY",
            "OBJECTPROPERTYEX", "DATABASEPROPERTYEX", "FILEGROUPPROPERTY",
            "INDEXPROPERTY", "SERVERPROPERTY", "SESSIONPROPERTY",
            "COL_LENGTH", "COL_NAME", "COLUMN_NAME",
            "OBJECT_DEFINITION", "OBJECT_NAME", "OBJECT_SCHEMA_NAME",
            "OBJECT_ID", "OBJECT_TYPE", "TYPE_ID", "TYPE_NAME",
            "SCHEMA_ID", "SCHEMA_NAME", "DB_ID", "DB_NAME",
            "FILE_ID", "FILE_IDEX", "FILE_NAME", "FILEGROUP_ID",
            "FILEGROUP_NAME",

            // T-SQL 系统变量
            "SPID", "ROWCOUNT", "ERROR", "TRANCOUNT", "VERSION",
            "SERVERNAME", "SERVICENAME", "LANGUAGE", "CONNECTIONS",
            "MAX_CONNECTIONS", "CPU_BUSY", "IO_BUSY", "IDLE",
            "PACK_RECEIVED", "PACK_SENT", "PACK_ERRORS",
            "TOTAL_READ", "TOTAL_WRITE", "TOTAL_ERRORS",
            "TEXTSIZE", "NESTLEVEL", "PROCID"
        };

        /// <summary>T-SQL 内置函数（ANSI 基础之上新增）</summary>
        public static readonly HashSet<string> TsqlFunctions = new HashSet<string>
        {
            // 聚合
            "COUNT_BIG", "CHECKSUM_AGG", "GROUPING_ID", "STDEV", "STDEVP", "VAR", "VARP",
            "STRING_AGG", "APPROX_COUNT_DISTINCT",

            // 字符串
            "STUFF", "QUOTENAME", "REPLICATE", "REVERSE", "SPACE",
            "CHARINDEX", "PATINDEX", "DIFFERENCE", "SOUNDEX",
            "LEN", "DATALENGTH", "FORMAT", "CONCAT", "CONCAT_WS",
            "TRANSLATE", "TRIM", "LEFT", "RIGHT",
            "UNICODE", "NCHAR", "ASCII", "CHAR", "STR",

            // 数学
            "LOG", "EXP", "SQUARE", "SQRT", "SIGN", "ABS",
            "POWER", "ROUND", "CEILING", "FLOOR", "ATN2",
            "COS", "SIN", "TAN", "ACOS", "ASIN", "ATAN", "COT",
            "RADIANS", "DEGREES", "PI",

            // 日期/时间
            "GETDATE", "GETUTCDATE", "SYSDATETIME", "SYSUTCDATETIME",
            "SYSDATETIMEOFFSET", "CURRENT_TIMESTAMP",
            "DATEADD", "DATEDIFF", "DATEDIFF_BIG", "DATEFROMPARTS",
            "DATETIME2FROMPARTS", "DATETIMEFROMPARTS",
            "DATETIMEOFFSETFROMPARTS", "SMALLDATETIMEFROMPARTS",
            "TIMEFROMPARTS", "TODATETIMEOFFSET", "SWITCHOFFSET",
            "EOMONTH", "ISDATE", "DAY", "MONTH", "YEAR",
            "DATEDIFF", "DATEDIFF_BIG",

            // 类型转换
            "CAST", "CONVERT", "TRY_CAST", "TRY_CONVERT",
            "PARSE", "TRY_PARSE", "COALESCE", "ISNULL",
            "NULLIF", "IIF", "CHOOSE",

            // 系统/元数据
            "OBJECT_ID", "OBJECT_NAME", "OBJECT_SCHEMA_NAME",
            "OBJECT_DEFINITION", "OBJECTPROPERTY", "OBJECTPROPERTYEX",
            "COLUMNPROPERTY", "TYPEPROPERTY", "DATABASEPROPERTYEX",
            "SERVERPROPERTY", "SESSIONPROPERTY", "FILEGROUPPROPERTY",
            "INDEXPROPERTY", "SCHEMA_ID", "SCHEMA_NAME", "DB_ID", "DB_NAME",
            "COL_LENGTH", "COL_NAME", "TYPE_ID", "TYPE_NAME",
            "FILE_ID", "FILE_IDEX", "FILE_NAME", "FILEGROUP_ID", "FILEGROUP_NAME",
            "NEWID", "NEWSEQUENTIALID", "IDENT_CURRENT", "IDENT_INCR", "IDENT_SEED",
            "IDENTITY", "SCOPE_IDENTITY", "@@IDENTITY",
            "ROWCOUNT_BIG", "CHECKSUM", "BINARY_CHECKSUM", "HASHBYTES",
            "CONTEXT_INFO", "ERROR_LINE", "ERROR_MESSAGE", "ERROR_NUMBER",
            "ERROR_PROCEDURE", "ERROR_SEVERITY", "ERROR_STATE",
            "FORMATMESSAGE", "GET_FILESTREAM_TRANSACTION_CONTEXT",
            "ISNUMERIC", "ISJSON", "JSON_VALUE", "JSON_QUERY", "JSON_MODIFY",
            "OPENJSON", "MIN_ACTIVE_ROWVERSION", "MODIFY", "PATH",
            "COMPRESS", "DECOMPRESS", "PUBLISH", "VALIDATE",

            // 窗口函数（ANSI 基础之上）
            "ROW_NUMBER", "RANK", "DENSE_RANK", "NTILE",
            "LAG", "LEAD", "FIRST_VALUE", "LAST_VALUE",
            "PERCENT_RANK", "CUME_DIST", "PERCENTILE_CONT", "PERCENTILE_DISC",

            // 安全
            "SUSER_ID", "SUSER_NAME", "SUSER_SID", "SUSER_SNAME",
            "USER_ID", "USER_NAME", "IS_SRVROLEMEMBER", "IS_MEMBER",
            "HAS_DBACCESS", "HAS_PERMS_BY_NAME", "FN_GET_PERMISSIONS",
            "PERMISSIONS",

            // 加密
            "ENCRYPTBYKEY", "DECRYPTBYKEY", "ENCRYPTBYASYMKEY",
            "DECRYPTBYASYMKEY", "ENCRYPTBYCERT", "DECRYPTBYCERT",
            "ENCRYPTBYPASSPHRASE", "DECRYPTBYPASSPHRASE", "KEY_ID", "KEY_GUID",
            "ASYMKEY_ID", "CERT_ID", "SIGNBYASYMKEY", "SIGNBYCERT",
            "VERIFY_SIGNATURE_BY_ASYMKEY", "VERIFY_SIGNATURE_BY_CERT"
        };

        /// <summary>T-SQL 数据类型（ANSI 基础之上新增）</summary>
        public static readonly HashSet<string> TsqlDataTypes = new HashSet<string>
        {
            "INT", "BIGINT", "SMALLINT", "TINYINT", "BIT",
            "DECIMAL", "NUMERIC", "MONEY", "SMALLMONEY",
            "FLOAT", "REAL",
            "DATETIME", "DATETIME2", "DATETIMEOFFSET", "SMALLDATETIME",
            "DATE", "TIME",
            "CHAR", "VARCHAR", "TEXT", "NCHAR", "NVARCHAR", "NTEXT",
            "BINARY", "VARBINARY", "IMAGE",
            "UNIQUEIDENTIFIER", "SQL_VARIANT", "XML",
            "HIERARCHYID", "GEOMETRY", "GEOGRAPHY",
            "TABLE", "ROWVERSION", "TIMESTAMP",
            "CURSOR"
        };
    }
}
