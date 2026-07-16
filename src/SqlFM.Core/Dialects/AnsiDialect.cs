using System.Collections.Generic;
using SqlFM.Core.Dialects.KeywordSets;

namespace SqlFM.Core.Dialects
{
    /// <summary>
    /// ANSI SQL 基础方言，作为所有 SQL 方言的继承基础。
    /// 借鉴 sqlfluff 的 ansi dialect 设计：不严格遵循 ANSI/ISO SQL 标准，
    /// 而是作为通用基础方言，包含所有方言共用的语法元素和关键字集合。
    /// </summary>
    public class AnsiDialect : SqlDialect
    {
        /// <summary>ANSI 方言单例实例</summary>
        public static readonly AnsiDialect Instance = new AnsiDialect();

        /// <summary>构造 ANSI 方言，初始化关键字集合</summary>
        private AnsiDialect() : base(
            name: "ansi",
            formattedName: "ANSI SQL",
            inheritsFrom: null,
            docstring: "ANSI SQL base dialect. Contains common syntax elements shared by all SQL dialects."
        )
        {
            ReservedKeywords.UnionWith(AnsiKeywords.Reserved);
            UnreservedKeywords.UnionWith(AnsiKeywords.Unreserved);
            FutureReservedKeywords.UnionWith(new HashSet<string>
            {
                // SQL:2003/2008 新增的未来保留字
                "DISABLE", "ENABLE", "OVERRIDING", "PREORDER", "SUBMULTISET",
                "REGR_SXX", "REGR_SXY", "REGR_SYY", "REGR_AVGX",
                "REGR_AVGY", "REGR_COUNT", "REGR_INTERCEPT", "REGR_R2",
                "LATERAL", "WINDOW", "WITHIN"
            });
            BuiltInFunctions.UnionWith(AnsiKeywords.Functions);
            DataTypes.UnionWith(AnsiKeywords.DataTypes);
        }
    }
}
