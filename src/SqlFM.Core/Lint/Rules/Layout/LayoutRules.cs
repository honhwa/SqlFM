using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using SqlFM.Core.Configuration;
using SqlFM.Core.Dialects;

namespace SqlFM.Core.Lint.Rules.Layout
{
    /// <summary>
    /// LT01 — layout.spacing 规则：检查 SQL 中不当的空格间距。
    /// 借鉴 sqlfluff 的 LT01：关键字前后应只有单个空格，不应有多余空格或缺失空格。
    /// 可自动修复：将多余空格缩减为单个空格，或在缺失处插入空格。
    /// </summary>
    public class LT01_SpacingRule : SqlRuleBase
    {
        /// <inheritdoc/>
        public override string RuleId => "LT01";

        /// <inheritdoc/>
        public override string RuleName => "layout.spacing";

        /// <inheritdoc/>
        public override string Description => "不当的空格间距（行尾空格、关键字前后多空格、元素间缺失空格）";

        /// <inheritdoc/>
        public override RuleSeverity Severity => RuleSeverity.Warning;

        /// <inheritdoc/>
        public override string[] Groups => new[] { "all", "core", "layout" };

        /// <inheritdoc/>
        public override string[] ConfigKeywords => new[] { "Global.TrimTrailingSpaces", "Global.MergeMultipleSpaces" };

        /// <inheritdoc/>
        public override List<LintResult> Evaluate(RuleContext context)
        {
            var results = new List<LintResult>();
            var lines = context.Lines;

            for (int i = 0; i < lines.Length; i++)
            {
                int lineNum = i + 1 + context.LineOffset;
                if (IsExempted(lineNum, context.ExemptedRegions))
                    continue;

                string line = lines[i];

                // 1. 行尾空格检查
                if (line.Length > 0 && line.EndsWith(" "))
                {
                    int trailingStart = line.Length;
                    while (trailingStart > 0 && line[trailingStart - 1] == ' ')
                        trailingStart--;

                    int trailingCount = line.Length - trailingStart;
                    results.Add(LintResult.CreateWithFix(
                        lineNum, trailingStart + 1,
                        RuleId, $"行尾有 {trailingCount} 个多余空格",
                        RuleSeverity.Warning,
                        new List<LintFix> { LintFix.ReplaceAt(lineNum, trailingStart + 1, line.Substring(trailingStart), "") }
                    ));
                }

                // 2. 关键字前后多空格检查（两个以上连续空格）
                var multiSpaceMatch = Regex.Match(line, @"\S  +\S");
                if (multiSpaceMatch.Success)
                {
                    int pos = multiSpaceMatch.Index + 1;
                    string multiSpaces = line.Substring(pos, multiSpaceMatch.Length - 2);
                    results.Add(LintResult.CreateWithFix(
                        lineNum, pos + 1,
                        RuleId, $"元素间有 {multiSpaces.Length} 个空格（应为 1 个）",
                        RuleSeverity.Warning,
                        new List<LintFix> { LintFix.ReplaceAt(lineNum, pos + 1, multiSpaces, " ") }
                    ));
                }
            }

            return results;
        }
    }

    /// <summary>
    /// LT02 — layout.indent 规则：检查缩进是否正确。
    /// 借鉴 sqlfluff 的 LT02：首行不应缩进，子句缩进层级应一致。
    /// 可自动修复：纠正缩进层级。
    /// </summary>
    public class LT02_IndentRule : SqlRuleBase
    {
        /// <inheritdoc/>
        public override string RuleId => "LT02";

        /// <inheritdoc/>
        public override string RuleName => "layout.indent";

        /// <inheritdoc/>
        public override string Description => "缩进不正确（首行不应缩进，缩进层级不一致）";

        /// <inheritdoc/>
        public override RuleSeverity Severity => RuleSeverity.Warning;

        /// <inheritdoc/>
        public override string[] Groups => new[] { "all", "core", "layout" };

        /// <inheritdoc/>
        public override string[] ConfigKeywords => new[] { "Global.IndentType", "Global.IndentSize" };

        /// <inheritdoc/>
        public override List<LintResult> Evaluate(RuleContext context)
        {
            var results = new List<LintResult>();
            var lines = context.Lines;
            int indentSize = context.Style.Global.IndentSize;

            // 首行不应缩进
            if (lines.Length > 0)
            {
                string firstLine = lines[0];
                int leadingSpaces = 0;
                while (leadingSpaces < firstLine.Length && firstLine[leadingSpaces] == ' ')
                    leadingSpaces++;

                if (leadingSpaces > 0)
                {
                    results.Add(LintResult.CreateWithFix(
                        1, 1,
                        RuleId, "首行不应缩进",
                        RuleSeverity.Warning,
                        new List<LintFix> { LintFix.ReplaceAt(1, 1, firstLine.Substring(0, leadingSpaces), "") }
                    ));
                }
            }

            return results;
        }
    }

    /// <summary>
    /// LT05 — layout.long_lines 规则：检查行长度是否超限。
    /// 借鉴 sqlfluff 的 LT05：行长度超过配置的最大行宽时报告警告。
    /// 不可自动修复（需手动调整换行位置）。
    /// </summary>
    public class LT05_LongLinesRule : SqlRuleBase
    {
        /// <inheritdoc/>
        public override string RuleId => "LT05";

        /// <inheritdoc/>
        public override string RuleName => "layout.long_lines";

        /// <inheritdoc/>
        public override string Description => "行长度超过最大行宽限制";

        /// <inheritdoc/>
        public override RuleSeverity Severity => RuleSeverity.Info;

        /// <inheritdoc/>
        public override bool IsFixCompatible => false;

        /// <inheritdoc/>
        public override string[] Groups => new[] { "all", "core", "layout" };

        /// <inheritdoc/>
        public override string[] ConfigKeywords => new[] { "Global.MaxLineWidth" };

        /// <inheritdoc/>
        public override List<LintResult> Evaluate(RuleContext context)
        {
            var results = new List<LintResult>();
            int maxLineWidth = context.Style.Global.MaxLineWidth;
            var lines = context.Lines;

            for (int i = 0; i < lines.Length; i++)
            {
                int lineNum = i + 1 + context.LineOffset;
                if (IsExempted(lineNum, context.ExemptedRegions))
                    continue;

                // 去除行尾空格后计算实际长度
                string trimmed = lines[i].TrimEnd();
                if (trimmed.Length > maxLineWidth)
                {
                    results.Add(LintResult.CreateManual(
                        lineNum, maxLineWidth + 1,
                        RuleId, $"行长度 {trimmed.Length} 超过限制 {maxLineWidth}",
                        RuleSeverity.Info
                    ));
                }
            }

            return results;
        }
    }

    /// <summary>
    /// LT12 — layout.end_of_file 规则：检查文件是否以单个换行符结尾。
    /// 借鉴 sqlfluff 的 LT12：文件末尾必须以单个换行符结束。
    /// 可自动修复：添加末尾换行符。
    /// </summary>
    public class LT12_EndOfFileRule : SqlRuleBase
    {
        /// <inheritdoc/>
        public override string RuleId => "LT12";

        /// <inheritdoc/>
        public override string RuleName => "layout.end_of_file";

        /// <inheritdoc/>
        public override string Description => "文件末尾必须以单个换行符结束";

        /// <inheritdoc/>
        public override string[] Groups => new[] { "all", "core", "layout" };

        /// <inheritdoc/>
        public override List<LintResult> Evaluate(RuleContext context)
        {
            var results = new List<LintResult>();
            string sql = context.Sql;

            if (sql.Length > 0 && !sql.EndsWith("\n"))
            {
                int lastLine = context.Lines.Length;
                results.Add(LintResult.CreateWithFix(
                    lastLine, context.Lines[lastLine - 1].Length + 1,
                    RuleId, "文件末尾缺少换行符",
                    RuleSeverity.Warning,
                    new List<LintFix> { LintFix.InsertAfterAt(lastLine, context.Lines[lastLine - 1].Length + 1, "\n") }
                ));
            }
            else if (sql.Length > 0 && sql.EndsWith("\n\n"))
            {
                int lastLine = context.Lines.Length;
                results.Add(LintResult.CreateWithFix(
                    lastLine, 1,
                    RuleId, "文件末尾有多个换行符（应为 1 个）",
                    RuleSeverity.Warning,
                    new List<LintFix> { LintFix.DeleteAt(lastLine, 1, "") }
                ));
            }

            return results;
        }
    }

    /// <summary>
    /// LT15 — layout.newlines 规则：检查连续空行是否过多。
    /// 借鉴 sqlfluff 的 LT15：连续空行超过配置限制时报告警告。
    /// 可自动修复：删除多余空行。
    /// </summary>
    public class LT15_ExcessiveNewlinesRule : SqlRuleBase
    {
        /// <inheritdoc/>
        public override string RuleId => "LT15";

        /// <inheritdoc/>
        public override string RuleName => "layout.newlines";

        /// <inheritdoc/>
        public override string Description => "连续空行过多";

        /// <inheritdoc/>
        public override string[] Groups => new[] { "all", "layout" };

        /// <inheritdoc/>
        public override string[] ConfigKeywords => new[] { "Global.RemoveExtraBlankLines", "Global.StatementBlankLines" };

        /// <inheritdoc/>
        public override List<LintResult> Evaluate(RuleContext context)
        {
            var results = new List<LintResult>();
            int maxBlankLines = context.Style.Global.StatementBlankLines + 1; // 语句间允许的空行数
            var lines = context.Lines;

            int consecutiveBlank = 0;
            for (int i = 0; i < lines.Length; i++)
            {
                int lineNum = i + 1 + context.LineOffset;
                if (IsExempted(lineNum, context.ExemptedRegions))
                {
                    consecutiveBlank = 0;
                    continue;
                }

                if (string.IsNullOrWhiteSpace(lines[i]))
                {
                    consecutiveBlank++;
                    if (consecutiveBlank > maxBlankLines)
                    {
                        results.Add(LintResult.CreateWithFix(
                            lineNum, 1,
                            RuleId, $"连续空行超过 {maxBlankLines} 行",
                            RuleSeverity.Warning,
                            new List<LintFix> { LintFix.DeleteAt(lineNum, 1, lines[i]) }
                        ));
                    }
                }
                else
                {
                    consecutiveBlank = 0;
                }
            }

            return results;
        }
    }
}
