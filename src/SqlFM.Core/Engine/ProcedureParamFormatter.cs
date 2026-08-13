using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.SqlServer.TransactSql.ScriptDom;
using SqlFM.Core.Configuration;

namespace SqlFM.Core.Engine
{
    /// <summary>
    /// 修复 Poor Man's T-SQL Formatter 在 ALTER/CREATE PROCEDURE 参数列表中将行内注释错位的缺陷。
    /// 机制：在 PoorMans 主格式化之后，用 ScriptDom 重新解析输出，提取每个参数声明及其后的行内注释，
    /// 将参数列表重排为“每行一个参数 + 行内注释归位到参数行尾”，再替换回原输出。
    /// 对无注释、无参数或 ScriptDom 解析失败的情况安全回退为原文本（不改变格式，不影响整体流程）。
    /// </summary>
    public static class ProcedureParamFormatter
    {
        private sealed class ParamInfo
        {
            public int FirstTokenIndex;
            public int LastTokenIndex;
        }

        private sealed class ProcInfo
        {
            public readonly List<ParamInfo> Params = new List<ParamInfo>();
        }

        private sealed class ProcFinder : TSqlFragmentVisitor
        {
            public readonly List<ProcInfo> Procs = new List<ProcInfo>();

            public override void Visit(AlterProcedureStatement node) => Collect(node.Parameters);
            public override void Visit(CreateProcedureStatement node) => Collect(node.Parameters);

            private static void Collect(IList<Parameter> parameters)
            {
                var info = new ProcInfo();
                foreach (var p in parameters)
                {
                    info.Params.Add(new ParamInfo
                    {
                        FirstTokenIndex = p.FirstTokenIndex,
                        LastTokenIndex = p.LastTokenIndex
                    });
                }
                Procs.Add(info);
            }
        }

        /// <summary>
        /// 修复 ALTER/CREATE PROCEDURE 参数列表中的行内注释错位。
        /// </summary>
        /// <param name="sql">PoorMans 主格式化后的 SQL 文本</param>
        /// <param name="style">当前格式化样式（用于决定缩进与逗号位置）</param>
        /// <returns>修复后的 SQL；无法安全修复时返回原文本</returns>
        public static string Fix(string sql, SqlFormatStyle style)
        {
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
                var finder = new ProcFinder();
                fragment.Accept(finder);
                if (finder.Procs.Count == 0)
                    return sql;

                string result = sql;
                bool changed = false;

                string indent = style.Global.IndentType == IndentType.Tabs
                    ? "\t"
                    : new string(' ', Math.Max(1, style.Global.IndentSize));
                bool leading = style.Dml.CommaPosition == CommaPosition.Before;

                foreach (var stmt in finder.Procs)
                {
                    if (stmt.Params.Count == 0)
                        continue;

                    int firstParamIdx = stmt.Params[0].FirstTokenIndex;

                    // 区域起点：第一个参数之前的空白 token（即过程名后的空格），避免重写后残留尾随空格
                    int regionStart = firstParamIdx;
                    while (regionStart - 1 >= 0 &&
                           tokens[regionStart - 1].TokenType == TSqlTokenType.WhiteSpace)
                        regionStart--;

                    // 区域终点：过程体 AS 关键字之前
                    int asIdx = -1;
                    for (int i = firstParamIdx; i < tokens.Count; i++)
                    {
                        if (tokens[i].TokenType == TSqlTokenType.As)
                        {
                            asIdx = i;
                            break;
                        }
                    }
                    if (asIdx < 0)
                        continue;

                    var region = new StringBuilder();
                    for (int i = regionStart; i < asIdx; i++)
                        region.Append(tokens[i].Text);
                    string regionText = region.ToString();
                    if (string.IsNullOrEmpty(regionText))
                        continue;

                    var nb = new StringBuilder();
                    nb.Append("\n");
                    for (int pi = 0; pi < stmt.Params.Count; pi++)
                    {
                        var p = stmt.Params[pi];

                        // 重建参数声明文本（压缩内部空白为单空格）
                        var pt = new StringBuilder();
                        for (int i = p.FirstTokenIndex; i <= p.LastTokenIndex; i++)
                        {
                            var t = tokens[i];
                            pt.Append(t.TokenType == TSqlTokenType.WhiteSpace ? " " : t.Text);
                        }
                        string paramText = Regex.Replace(pt.ToString(), @"\s+", " ").Trim();

                        // 提取该参数之后的行内注释（跳过空白，并容忍 poor man's 错位到逗号之后的注释）
                        string comment = null;
                        for (int i = p.LastTokenIndex + 1; i < tokens.Count; i++)
                        {
                            var t = tokens[i];
                            if (t.TokenType == TSqlTokenType.WhiteSpace ||
                                t.TokenType == TSqlTokenType.Comma)
                                continue;
                            if (t.TokenType == TSqlTokenType.SingleLineComment)
                                comment = t.Text.Trim();
                            break;
                        }

                        if (leading)
                        {
                            nb.Append(indent);
                            if (pi > 0)
                                nb.Append(", ");
                            nb.Append(paramText);
                            if (comment != null)
                                nb.Append("    " + comment);
                            nb.Append("\n");
                        }
                        else
                        {
                            nb.Append(indent).Append(paramText);
                            if (pi < stmt.Params.Count - 1)
                                nb.Append(",");
                            if (comment != null)
                                nb.Append("    " + comment);
                            nb.Append("\n");
                        }
                    }

                    if (result.IndexOf(regionText, StringComparison.Ordinal) >= 0)
                    {
                        result = result.Replace(regionText, nb.ToString());
                        changed = true;
                    }
                }

                return changed ? result : sql;
            }
            catch
            {
                // 任何异常都安全回退，不影响整体格式化
                return sql;
            }
        }
    }
}
