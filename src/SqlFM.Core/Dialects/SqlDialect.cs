using System;
using System.Collections.Generic;

namespace SqlFM.Core.Dialects
{
    /// <summary>
    /// SQL 方言基类，借鉴 sqlfluff 的 Dialect 继承体系。
    /// ANSI 为基础方言，其他方言通过继承 + 覆盖扩展。
    /// 每个方言定义自己的关键字集合、内置函数列表、数据类型列表和方言专属规则。
    /// </summary>
    public class SqlDialect
    {
        /// <summary>方言名称（如 "ansi", "tsql", "mysql"）</summary>
        public string Name { get; }

        /// <summary>格式化显示名称（如 "ANSI SQL", "Transact-SQL"）</summary>
        public string FormattedName { get; }

        /// <summary>父方言名称（继承来源）</summary>
        public string? InheritsFrom { get; }

        /// <summary>方言文档描述</summary>
        public string Docstring { get; }

        /// <summary>保留关键字集合（不可用作标识符）</summary>
        public HashSet<string> ReservedKeywords { get; } = new HashSet<string>();

        /// <summary>非保留关键字集合（可用作标识符，但有特殊含义）</summary>
        public HashSet<string> UnreservedKeywords { get; } = new HashSet<string>();

        /// <summary>未来保留关键字集合（未来版本可能升级为保留）</summary>
        public HashSet<string> FutureReservedKeywords { get; } = new HashSet<string>();

        /// <summary>内置函数列表</summary>
        public HashSet<string> BuiltInFunctions { get; } = new HashSet<string>();

        /// <summary>数据类型列表</summary>
        public HashSet<string> DataTypes { get; } = new HashSet<string>();

        /// <summary>方言专属规则 ID 列表</summary>
        public List<string> DialectRules { get; } = new List<string>();

        /// <summary>所有关键字集合（reserved + unreserved + future_reserved 的合并）</summary>
        public HashSet<string> AllKeywords
        {
            get
            {
                var all = new HashSet<string>(ReservedKeywords);
                all.UnionWith(UnreservedKeywords);
                all.UnionWith(FutureReservedKeywords);
                return all;
            }
        }

        /// <summary>构造方言</summary>
        protected SqlDialect(string name, string formattedName, string? inheritsFrom, string docstring)
        {
            Name = name;
            FormattedName = formattedName;
            InheritsFrom = inheritsFrom;
            Docstring = docstring;
        }

        /// <summary>判断指定标识符是否为保留关键字</summary>
        public bool IsReservedKeyword(string word)
        {
            return ReservedKeywords.Contains(word.ToUpperInvariant());
        }

        /// <summary>判断指定标识符是否为任何类型的关键字</summary>
        public bool IsAnyKeyword(string word)
        {
            return AllKeywords.Contains(word.ToUpperInvariant());
        }

        /// <summary>判断指定名称是否为内置函数</summary>
        public bool IsBuiltInFunction(string name)
        {
            return BuiltInFunctions.Contains(name.ToUpperInvariant());
        }

        /// <summary>判断指定名称是否为数据类型</summary>
        public bool IsDataType(string name)
        {
            return DataTypes.Contains(name.ToUpperInvariant());
        }
    }
}
