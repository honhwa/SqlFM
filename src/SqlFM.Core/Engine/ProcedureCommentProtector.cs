using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace SqlFM.Core.Engine
{
    /// <summary>
    /// 保护 CREATE/ALTER PROCEDURE 参数列表中的注释，避免 Poor Man's T-SQL Formatter
    /// 在 leading-comma 模式下将行内注释错位（注释与参数的对应关系被破坏后，事后 ScriptDom 重排也无法恢复）。
    /// 机制（前置保护，而非事后修补）：
    ///   1. Protect：在 PoorMans 主格式化之前，用 ScriptDom 解析并定位每个 PROCEDURE 的
    ///      参数头区域（过程名 → AS 关键字之前），将该区域内的行内注释(-- ...)与块注释(/* ... */)
    ///      替换为唯一块注释占位符 /*SQLFMCOMMn*/，并记录 序号 → 原注释 的映射。
    ///      块注释形式的占位符不会被 PoorMans 错位，从而保留注释与参数的正确归属。
    ///   2. Restore：在全部后处理完成后，将占位符还原为原始注释。
    /// 解析失败或无可保护注释时安全回退，不改变文本、不影响整体流程。
    /// </summary>
    public static class ProcedureCommentProtector
    {
        private const string MarkerPrefix = "/*SQLFMCOMM";
        private const string MarkerSuffix = "*/";

        /// <summary>
        /// 前置保护：将 PROCEDURE 参数头区域内的注释替换为占位符。
        /// </summary>
        /// <param name="sql">原始 SQL 文本</param>
        /// <param name="map">输出：占位符序号 → 原始注释文本 的映射</param>
        /// <returns>保护后的 SQL；无法安全处理时返回原文本</returns>
        public static string Protect(string sql, out Dictionary<int, string> map)
        {
            map = new Dictionary<int, string>();
            if (string.IsNullOrWhiteSpace(sql))
                return sql;

            try
            {
                var parser = new TSql160Parser(initialQuotedIdentifiers: true);
                IList<ParseError> errors;
                var fragment = parser.Parse(new StringReader(sql), out errors);
                if (errors.Count > 0)
                    return sql;

                var tokens = parser.GetTokenStream(new StringReader(sql), out errors);

                // 收集每个 PROCEDURE 的参数头区域 [start, end)
                var ranges = new List<(int start, int end)>();
                var finder = new ProcRangeFinder();
                fragment.Accept(finder);
                foreach (var node in finder.AlterProcs)
                    ranges.Add(ComputeRange(node, node.Parameters, tokens));
                foreach (var node in finder.CreateProcs)
                    ranges.Add(ComputeRange(node, node.Parameters, tokens));

                if (ranges.Count == 0)
                    return sql;

                bool InRange(int idx)
                {
                    foreach (var r in ranges)
                        if (idx >= r.start && idx < r.end)
                            return true;
                    return false;
                }

                var sb = new StringBuilder();
                int counter = 0;
                for (int i = 0; i < tokens.Count; i++)
                {
                    var t = tokens[i];
                    if (InRange(i) &&
                        (t.TokenType == TSqlTokenType.SingleLineComment ||
                         t.TokenType == TSqlTokenType.MultiLineComment))
                    {
                        int key = counter++;
                        map[key] = t.Text;
                        sb.Append(MarkerPrefix).Append(key).Append(MarkerSuffix);
                    }
                    else
                    {
                        sb.Append(t.Text);
                    }
                }

                return sb.ToString();
            }
            catch
            {
                map = new Dictionary<int, string>();
                return sql;
            }
        }

        private static (int start, int end) ComputeRange(
            TSqlFragment node,
            IList<ProcedureParameter> parameters,
            IList<TSqlParserToken> tokens)
        {
            int start = node.FirstTokenIndex;
            int lastParamEnd = start;
            if (parameters != null && parameters.Count > 0)
            {
                start = parameters[0].FirstTokenIndex;
                lastParamEnd = parameters[parameters.Count - 1].LastTokenIndex;
            }

            // 从最后一个参数之后查找 AS 关键字，避免误判默认值内的 AS（如 CAST(... AS ...)）
            int asIdx = -1;
            for (int i = lastParamEnd + 1; i < tokens.Count; i++)
            {
                if (tokens[i].TokenType == TSqlTokenType.As)
                {
                    asIdx = i;
                    break;
                }
            }
            if (asIdx < 0)
                asIdx = node.LastTokenIndex + 1;

            return (start, asIdx);
        }

        /// <summary>
        /// 还原注释：将占位符替换回原始注释文本。
        /// </summary>
        /// <param name="sql">格式化后的 SQL（含占位符）</param>
        /// <param name="map">Protect 阶段输出的映射</param>
        /// <returns>还原后的 SQL；映射为空时原样返回</returns>
        public static string Restore(string sql, Dictionary<int, string> map)
        {
            if (map == null || map.Count == 0 || string.IsNullOrEmpty(sql))
                return sql;

            var result = sql;
            foreach (var kv in map)
            {
                var marker = MarkerPrefix + kv.Key + MarkerSuffix;
                result = result.Replace(marker, kv.Value);
            }
            return result;
        }

        private sealed class ProcRangeFinder : TSqlFragmentVisitor
        {
            public readonly List<AlterProcedureStatement> AlterProcs = new List<AlterProcedureStatement>();
            public readonly List<CreateProcedureStatement> CreateProcs = new List<CreateProcedureStatement>();

            public override void Visit(AlterProcedureStatement node) => AlterProcs.Add(node);
            public override void Visit(CreateProcedureStatement node) => CreateProcs.Add(node);
        }
    }
}
