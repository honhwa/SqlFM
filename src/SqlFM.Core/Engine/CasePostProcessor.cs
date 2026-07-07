using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.SqlServer.TransactSql.ScriptDom;
using SqlFM.Core.Configuration;

namespace SqlFM.Core.Engine
{
    /// <summary>
    /// 函数名和数据类型大小写后处理器。
    /// 基于 ScriptDom token 流精确识别函数调用（Identifier 后紧跟左括号）和已知数据类型，
    /// 根据 <see cref="GlobalSettings.FunctionCase"/> / <see cref="GlobalSettings.DataTypeCase"/> 配置应用大小写转换。
    /// 不修改关键字、字符串字面量、注释和普通标识符。
    /// </summary>
    public class CasePostProcessor
    {
        private readonly TSql160Parser _parser;

        /// <summary>
        /// 已知 T-SQL 内置数据类型集合（不区分大小写匹配）。
        /// </summary>
        private static readonly HashSet<string> DataTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            // ── 精确数值 ──
            "bigint", "int", "smallint", "tinyint", "bit", "decimal", "numeric", "money", "smallmoney",
            // ── 近似数值 ──
            "float", "real",
            // ── 日期时间 ──
            "date", "datetime", "datetime2", "datetimeoffset", "smalldatetime", "time",
            // ── 字符串 ──
            "char", "varchar", "text", "nchar", "nvarchar", "ntext",
            // ── 二进制 ──
            "binary", "varbinary", "image",
            // ── 其他 ──
            "cursor", "hierarchyid", "sql_variant", "table", "timestamp", "uniqueidentifier", "xml",
            "geometry", "geography"
        };

        /// <summary>初始化 CasePostProcessor，创建 TSql160 解析器实例。</summary>
        public CasePostProcessor()
        {
            _parser = new TSql160Parser(initialQuotedIdentifiers: false);
        }

        /// <summary>
        /// 根据配置转换函数名和数据类型大小写。
        /// 仅在配置非 Upper 时执行转换（Upper 是 PoorMans 默认行为，无需后处理）。
        /// </summary>
        /// <param name="sql">主格式化后的 SQL 文本</param>
        /// <param name="functionCase">内置函数大小写风格</param>
        /// <param name="dataTypeCase">数据类型大小写风格</param>
        /// <returns>函数名和数据类型大小写转换后的 SQL 文本</returns>
        public string Process(string sql, KeywordCase functionCase, KeywordCase dataTypeCase)
        {
            if (string.IsNullOrEmpty(sql))
                return sql;

            // Upper 是 PoorMans 默认行为，无需后处理
            if (functionCase == KeywordCase.Upper && dataTypeCase == KeywordCase.Upper)
                return sql;

            IList<TSqlParserToken> tokens;
            try
            {
                using (var reader = new StringReader(sql))
                {
                    tokens = _parser.GetTokenStream(reader, out _);
                }
            }
            catch
            {
                // Tokenization 失败时返回原文，不中断管道
                return sql;
            }

            var sb = new StringBuilder(sql.Length);
            for (int i = 0; i < tokens.Count; i++)
            {
                var token = tokens[i];
                bool converted = false;

                // ── 函数名转换：Identifier 后紧跟 LeftParenthesis ──
                if (functionCase != KeywordCase.Upper &&
                    (token.TokenType == TSqlTokenType.Identifier ||
                     token.TokenType == TSqlTokenType.QuotedIdentifier))
                {
                    int nextIdx = FindNextNonWhitespace(tokens, i + 1);
                    if (nextIdx >= 0 && tokens[nextIdx].TokenType == TSqlTokenType.LeftParenthesis)
                    {
                        sb.Append(ApplyCase(token.Text, functionCase));
                        converted = true;
                    }
                }

                // ── 数据类型转换：匹配已知数据类型名 ──
                if (!converted && dataTypeCase != KeywordCase.Upper &&
                    !IsStringOrCommentToken(token.TokenType) &&
                    DataTypes.Contains(token.Text))
                {
                    sb.Append(ApplyCase(token.Text, dataTypeCase));
                    converted = true;
                }

                if (!converted)
                    sb.Append(token.Text);
            }

            return sb.ToString();
        }

        /// <summary>
        /// 从指定索引开始查找下一个非空白 token 的索引。
        /// </summary>
        /// <param name="tokens">token 列表</param>
        /// <param name="start">起始索引</param>
        /// <returns>下一个非空白 token 的索引；不存在时返回 -1</returns>
        private static int FindNextNonWhitespace(IList<TSqlParserToken> tokens, int start)
        {
            for (int i = start; i < tokens.Count; i++)
            {
                if (tokens[i].TokenType != TSqlTokenType.WhiteSpace)
                    return i;
            }
            return -1;
        }

        /// <summary>
        /// 判断 token 类型是否为字符串字面量或注释。
        /// </summary>
        /// <param name="tokenType">ScriptDom token 类型</param>
        /// <returns>是字符串或注释返回 true</returns>
        private static bool IsStringOrCommentToken(TSqlTokenType tokenType)
        {
            return tokenType == TSqlTokenType.AsciiStringLiteral
                || tokenType == TSqlTokenType.UnicodeStringLiteral
                || tokenType == TSqlTokenType.SingleLineComment
                || tokenType == TSqlTokenType.MultilineComment;
        }

        /// <summary>
        /// 根据大小写风格枚举值应用转换。
        /// </summary>
        /// <param name="text">待转换的文本</param>
        /// <param name="caseStyle">目标大小写风格</param>
        /// <returns>转换后的文本</returns>
        private static string ApplyCase(string text, KeywordCase caseStyle)
        {
            switch (caseStyle)
            {
                case KeywordCase.Upper:
                    return text.ToUpperInvariant();
                case KeywordCase.Lower:
                    return text.ToLowerInvariant();
                case KeywordCase.Pascal:
                    if (string.IsNullOrEmpty(text)) return text;
                    return char.ToUpperInvariant(text[0]) + text.Substring(1).ToLowerInvariant();
                default:
                    return text;
            }
        }
    }
}
