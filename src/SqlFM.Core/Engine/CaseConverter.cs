using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace SqlFM.Core.Engine
{
    /// <summary>
    /// SQL 关键字/标识符大小写转换工具。
    /// 独立于格式化引擎，可单独调用（对应快捷键 Ctrl+B,Ctrl+U / Ctrl+B,Ctrl+L）。
    /// 使用 ScriptDom tokenizer 精确区分关键字与标识符/字符串/注释，避免误改。
    /// </summary>
    public class CaseConverter
    {
        // 不需要转换大小写的 token 类型（标识符、字符串、注释、空白等）
        private static readonly HashSet<TSqlTokenType> _skipTypes = new HashSet<TSqlTokenType>
        {
            TSqlTokenType.Identifier,
            TSqlTokenType.QuotedIdentifier,
            TSqlTokenType.AsciiStringLiteral,
            TSqlTokenType.UnicodeStringLiteral,
            TSqlTokenType.Variable,
            TSqlTokenType.SingleLineComment,
            TSqlTokenType.MultilineComment,
            TSqlTokenType.WhiteSpace,
            TSqlTokenType.EndOfFile,
            TSqlTokenType.None,
        };

        private readonly TSql160Parser _parser;

        /// <summary>初始化 CaseConverter，创建 SQL Server 2022 (TSql160) 解析器实例。</summary>
        public CaseConverter()
        {
            _parser = new TSql160Parser(initialQuotedIdentifiers: false);
        }

        /// <summary>
        /// 将 SQL 中所有关键字转换为大写（不改变标识符/字符串/注释）。
        /// </summary>
        /// <param name="sql">待处理的 SQL 文本</param>
        /// <returns>关键字全大写的 SQL 字符串</returns>
        public string KeywordsToUpper(string sql)
        {
            return ConvertKeywordCase(sql, s => s.ToUpperInvariant());
        }

        /// <summary>
        /// 将 SQL 中所有关键字转换为小写（不改变标识符/字符串/注释）。
        /// </summary>
        /// <param name="sql">待处理的 SQL 文本</param>
        /// <returns>关键字全小写的 SQL 字符串</returns>
        public string KeywordsToLower(string sql)
        {
            return ConvertKeywordCase(sql, s => s.ToLowerInvariant());
        }

        /// <summary>
        /// 将 SQL 中所有关键字转换为 Pascal 大小写（首字母大写，其余小写）。
        /// </summary>
        /// <param name="sql">待处理的 SQL 文本</param>
        /// <returns>关键字 Pascal 大小写的 SQL 字符串</returns>
        public string KeywordsToPascal(string sql)
        {
            return ConvertKeywordCase(sql, s =>
            {
                if (string.IsNullOrEmpty(s)) return s;
                return char.ToUpperInvariant(s[0]) + s.Substring(1).ToLowerInvariant();
            });
        }

        /// <summary>
        /// 核心转换逻辑：基于 ScriptDom token 流，仅对关键字 token 做大小写变换。
        /// </summary>
        /// <param name="sql">待处理的 SQL 文本</param>
        /// <param name="transform">大小写转换函数（ToUpper/ToLower/ToPascal）</param>
        /// <returns>转换后的 SQL 字符串</returns>
        private string ConvertKeywordCase(string sql, Func<string, string> transform)
        {
            if (string.IsNullOrEmpty(sql))
                return sql;

            IList<TSqlParserToken> tokens;
            using (var reader = new StringReader(sql))
            {
                tokens = _parser.GetTokenStream(reader, out _);
            }

            var sb = new StringBuilder(sql.Length);
            foreach (var token in tokens)
            {
                if (_skipTypes.Contains(token.TokenType))
                {
                    // 非关键字：原样保留
                    sb.Append(token.Text);
                }
                else
                {
                    // 关键字：按指定规则转换
                    sb.Append(transform(token.Text));
                }
            }

            return sb.ToString();
        }
    }
}
