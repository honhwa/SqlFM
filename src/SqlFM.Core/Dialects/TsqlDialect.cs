using System.Collections.Generic;
using SqlFM.Core.Dialects.KeywordSets;

namespace SqlFM.Core.Dialects
{
    /// <summary>
    /// Transact-SQL (T-SQL) 方言，继承 ANSI 方言并添加 SQL Server 专有语法。
    /// 借鉴 sqlfluff 的 tsql dialect：在 ANSI 基础上扩展关键字、函数和数据类型。
    /// </summary>
    public class TsqlDialect : SqlDialect
    {
        /// <summary>T-SQL 方言单例实例</summary>
        public static readonly TsqlDialect Instance = new TsqlDialect();

        /// <summary>构造 T-SQL 方言，继承 ANSI 并扩展</summary>
        private TsqlDialect() : base(
            name: "tsql",
            formattedName: "Transact-SQL",
            inheritsFrom: "ansi",
            docstring: "Microsoft Transact-SQL dialect for SQL Server. Extends ANSI SQL with SQL Server-specific syntax."
        )
        {
            // 继承 ANSI 关键字
            ReservedKeywords.UnionWith(AnsiKeywords.Reserved);
            UnreservedKeywords.UnionWith(AnsiKeywords.Unreserved);
            BuiltInFunctions.UnionWith(AnsiKeywords.Functions);
            DataTypes.UnionWith(AnsiKeywords.DataTypes);

            // T-SQL 保留关键字扩展
            ReservedKeywords.UnionWith(TsqlKeywords.TsqlReserved);

            // T-SQL 非保留关键字扩展
            UnreservedKeywords.UnionWith(TsqlKeywords.TsqlUnreserved);

            // T-SQL 内置函数扩展
            BuiltInFunctions.UnionWith(TsqlKeywords.TsqlFunctions);

            // T-SQL 数据类型扩展
            DataTypes.UnionWith(TsqlKeywords.TsqlDataTypes);

            // T-SQL 方言专属规则
            DialectRules.AddRange(new[] { "TQ01", "TQ02", "TQ03" });
        }
    }
}
