using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using SqlFM.Core.Dialects;

namespace SqlFM.Core.Lint.Rules.Structure
{
    /// <summary>
    /// ST01 — structure.else_null 规则：检查 CASE 语句中 ELSE NULL 是否冗余。
    /// 借鉴 sqlfluff 的 ST01：CASE 中的 ELSE NULL 是多余的（CASE 默认返回 NULL）。
    /// 可自动修复：移除 ELSE NULL 子句。
    /// </summary>
    public class ST01_ElseNullRule : SqlRuleBase
    {
        /// <inheritdoc/>
        public override string RuleId => "ST01";

        /// <inheritdoc/>
        public override string RuleName => "structure.else_null";

        /// <inheritdoc/>
        public override string Description => "CASE 语句中的 ELSE NULL 是冗余的";

        /// <inheritdoc/>
        public override string[] Groups => new[] { "all", "structure" };

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

                var elseNull = Regex.Match(lines[i], @"ELSE\s+NULL\b", RegexOptions.IgnoreCase);
                if (elseNull.Success)
                {
                    results.Add(LintResult.CreateWithFix(
                        lineNum, elseNull.Index + 1,
                        RuleId, "ELSE NULL 是冗余的（CASE 默认返回 NULL）",
                        RuleSeverity.Warning,
                        new List<LintFix> { LintFix.DeleteAt(lineNum, elseNull.Index + 1, elseNull.Value) }
                    ));
                }
            }

            return results;
        }
    }

    /// <summary>
    /// AM04 — ambiguous.column_count 规则：检测 SELECT * 使用。
    /// 借鉴 sqlfluff 的 AM04：查询不应产生未知数量的结果列（避免 SELECT *）。
    /// 不可自动修复（需手动指定列名）。
    /// </summary>
    public class AM04_SelectStarRule : SqlRuleBase
    {
        /// <inheritdoc/>
        public override string RuleId => "AM04";

        /// <inheritdoc/>
        public override string RuleName => "ambiguous.column_count";

        /// <inheritdoc/>
        public override string Description => "SELECT * 会产生未知数量的结果列";

        /// <inheritdoc/>
        public override RuleSeverity Severity => RuleSeverity.Warning;

        /// <inheritdoc/>
        public override bool IsFixCompatible => false;

        /// <inheritdoc/>
        public override string[] Groups => new[] { "all", "ambiguous" };

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

                var starMatch = Regex.Match(lines[i], @"SELECT\s+\*", RegexOptions.IgnoreCase);
                if (starMatch.Success)
                {
                    results.Add(LintResult.CreateManual(
                        lineNum, starMatch.Index + 1,
                        RuleId, "SELECT * 产生未知数量的结果列，建议显式指定列名",
                        RuleSeverity.Warning
                    ));
                }
            }

            return results;
        }
    }

    /// <summary>
    /// AM02 — ambiguous.union 规则：检查 UNION 是否明确指定 DISTINCT 或 ALL。
    /// 借鉴 sqlfluff 的 AM02：UNION 应明确为 UNION DISTINCT 或 UNION ALL。
    /// 可自动修复：将 UNION 转为 UNION ALL（默认行为，更明确）。
    /// </summary>
    public class AM02_UnionRule : SqlRuleBase
    {
        /// <inheritdoc/>
        public override string RuleId => "AM02";

        /// <inheritdoc/>
        public override string RuleName => "ambiguous.union";

        /// <inheritdoc/>
        public override string Description => "UNION 应明确为 UNION DISTINCT 或 UNION ALL";

        /// <inheritdoc/>
        public override string[] Groups => new[] { "all", "core", "ambiguous" };

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

                // 匹配单独的 UNION（后面不是 DISTINCT 或 ALL）
                var unionMatch = Regex.Match(lines[i], @"UNION(?!\s+(DISTINCT|ALL))", RegexOptions.IgnoreCase);
                if (unionMatch.Success)
                {
                    results.Add(LintResult.CreateWithFix(
                        lineNum, unionMatch.Index + 1,
                        RuleId, "UNION 未明确指定 DISTINCT 或 ALL",
                        RuleSeverity.Warning,
                        new List<LintFix> { LintFix.ReplaceAt(lineNum, unionMatch.Index + 1, "UNION", "UNION ALL") }
                    ));
                }
            }

            return results;
        }
    }
}
