using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using SqlFM.Core.Dialects;

namespace SqlFM.Core.Lint.Rules.Convention
{
    /// <summary>
    /// CV01 — convention.not_equal 规则：检查不等于运算符的一致性。
    /// 借鉴 sqlfluff 的 CV01：!= 和 <> 应统一使用一种。
    /// 可自动修复：将 != 转为 <> 或反之（根据配置）。
    /// </summary>
    public class CV01_NotEqualRule : SqlRuleBase
    {
        /// <inheritdoc/>
        public override string RuleId => "CV01";

        /// <inheritdoc/>
        public override string RuleName => "convention.not_equal";

        /// <inheritdoc/>
        public override string Description => "不等于运算符 (!/<> 应统一使用一种";

        /// <inheritdoc/>
        public override string[] Groups => new[] { "all", "convention" };

        /// <inheritdoc/>
        public override string[] ConfigKeywords => new[] { "NotEqualOperator" };

        /// <inheritdoc/>
        public override List<LintResult> Evaluate(RuleContext context)
        {
            var results = new List<LintResult>();
            var lines = context.Lines;

            // 检测 != 运算符
            for (int i = 0; i < lines.Length; i++)
            {
                int lineNum = i + 1 + context.LineOffset;
                if (IsExempted(lineNum, context.ExemptedRegions))
                    continue;

                // 检测 !=
                var neMatch = Regex.Match(lines[i], @"!=");
                if (neMatch.Success)
                {
                    results.Add(LintResult.CreateWithFix(
                        lineNum, neMatch.Index + 1,
                        RuleId, $"使用了 != 运算符（建议统一使用 <>）",
                        RuleSeverity.Warning,
                        new List<LintFix> { LintFix.ReplaceAt(lineNum, neMatch.Index + 1, "!=", "<>") }
                    ));
                }
            }

            return results;
        }
    }

    /// <summary>
    /// CV05 — convention.is_null 规则：检查与 NULL 的比较应使用 IS/IS NOT。
    /// 借鉴 sqlfluff 的 CV05：= NULL 和 != NULL 应改为 IS NULL / IS NOT NULL。
    /// 可自动修复：将 = NULL 改为 IS NULL，!= NULL 或 <> NULL 改为 IS NOT NULL。
    /// </summary>
    public class CV05_IsNullRule : SqlRuleBase
    {
        /// <inheritdoc/>
        public override string RuleId => "CV05";

        /// <inheritdoc/>
        public override string RuleName => "convention.is_null";

        /// <inheritdoc/>
        public override string Description => "与 NULL 比较应使用 IS / IS NOT 而非 = / !=";

        /// <inheritdoc/>
        public override string[] Groups => new[] { "all", "core", "convention" };

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

                // = NULL
                var eqNullMatch = Regex.Match(lines[i], @"=\s+NULL", RegexOptions.IgnoreCase);
                if (eqNullMatch.Success)
                {
                    results.Add(LintResult.CreateWithFix(
                        lineNum, eqNullMatch.Index + 1,
                        RuleId, "= NULL 应改为 IS NULL",
                        RuleSeverity.Error,
                        new List<LintFix> { LintFix.ReplaceAt(lineNum, eqNullMatch.Index + 1, eqNullMatch.Value, "IS NULL") }
                    ));
                }

                // != NULL 或 <> NULL
                var neNullMatch = Regex.Match(lines[i], @"(!=|<>)\s+NULL", RegexOptions.IgnoreCase);
                if (neNullMatch.Success)
                {
                    string fixOp = "IS NOT NULL";
                    results.Add(LintResult.CreateWithFix(
                        lineNum, neNullMatch.Index + 1,
                        RuleId, $"{neNullMatch.Value} 应改为 IS NOT NULL",
                        RuleSeverity.Error,
                        new List<LintFix> { LintFix.ReplaceAt(lineNum, neNullMatch.Index + 1, neNullMatch.Value, fixOp) }
                    ));
                }
            }

            return results;
        }
    }

    /// <summary>
    /// CV06 — convention.terminator 规则：检查 SQL 语句是否以分号结尾。
    /// 借鉴 sqlfluff 的 CV06：语句必须以分号结束。
    /// 可自动修复：添加末尾分号。
    /// </summary>
    public class CV06_SemicolonRule : SqlRuleBase
    {
        /// <inheritdoc/>
        public override string RuleId => "CV06";

        /// <inheritdoc/>
        public override string RuleName => "convention.terminator";

        /// <inheritdoc/>
        public override string Description => "语句必须以分号结尾";

        /// <inheritdoc/>
        public override string[] Groups => new[] { "all", "convention" };

        /// <inheritdoc/>
        public override List<LintResult> Evaluate(RuleContext context)
        {
            var results = new List<LintResult>();
            var lines = context.Lines;

            // 查找不以分号结尾的语句行（非空行、非注释行、非 GO 行）
            for (int i = 0; i < lines.Length; i++)
            {
                int lineNum = i + 1 + context.LineOffset;
                if (IsExempted(lineNum, context.ExemptedRegions))
                    continue;

                string trimmed = lines[i].TrimEnd();
                if (string.IsNullOrWhiteSpace(trimmed))
                    continue;
                if (trimmed.StartsWith("--"))
                    continue;

                // 检测是语句结尾行（包含 SELECT/INSERT/UPDATE/DELETE 等关键字后的内容）
                // 但不以分号或 GO 结尾
                if (!trimmed.EndsWith(";") && !trimmed.ToUpperInvariant().EndsWith("GO"))
                {
                    // 仅对看起来是完整语句结尾的行报告（简化判断：非空且不是子句关键字开头）
                    string upper = trimmed.ToUpperInvariant();
                    bool looksLikeClauseStart = upper.StartsWith("FROM") || upper.StartsWith("WHERE") ||
                        upper.StartsWith("SET") || upper.StartsWith("ON") || upper.StartsWith("AND") ||
                        upper.StartsWith("OR") || upper.StartsWith("ORDER") || upper.StartsWith("GROUP") ||
                        upper.StartsWith("HAVING") || upper.StartsWith("VALUES") || upper.StartsWith("INTO") ||
                        upper.StartsWith("INNER") || upper.StartsWith("LEFT") || upper.StartsWith("RIGHT") ||
                        upper.StartsWith("CROSS") || upper.StartsWith("FULL") || upper.StartsWith("THEN") ||
                        upper.StartsWith("WHEN") || upper.StartsWith("ELSE") || upper.StartsWith("AS");

                    if (!looksLikeClauseStart)
                    {
                        results.Add(LintResult.CreateWithFix(
                            lineNum, trimmed.Length + 1,
                            RuleId, "语句缺少分号结尾",
                            RuleSeverity.Warning,
                            new List<LintFix> { LintFix.InsertAfterAt(lineNum, trimmed.Length + 1, ";") }
                        ));
                    }
                }
            }

            return results;
        }
    }

    /// <summary>
    /// CV12 — convention.join_condition 规则：检查是否使用 WHERE 作为 JOIN 条件（隐式连接）。
    /// 借鉴 sqlfluff 的 CV12 / AM08：应使用 JOIN ON 而非 WHERE 中的多表条件。
    /// 不可自动修复（需重构 SQL 结构）。
    /// </summary>
    public class CV12_JoinConditionRule : SqlRuleBase
    {
        /// <inheritdoc/>
        public override string RuleId => "CV12";

        /// <inheritdoc/>
        public override string RuleName => "convention.join_condition";

        /// <inheritdoc/>
        public override string Description => "应使用 JOIN ... ON 而非 WHERE 中的连接条件（隐式连接）";

        /// <inheritdoc/>
        public override RuleSeverity Severity => RuleSeverity.Warning;

        /// <inheritdoc/>
        public override bool IsFixCompatible => false;

        /// <inheritdoc/>
        public override string[] Groups => new[] { "all", "convention" };

        /// <inheritdoc/>
        public override List<LintResult> Evaluate(RuleContext context)
        {
            var results = new List<LintResult>();
            var lines = context.Lines;

            // 检测 FROM 子句中逗号分隔的表引用（如 FROM t1, t2）
            for (int i = 0; i < lines.Length; i++)
            {
                int lineNum = i + 1 + context.LineOffset;
                if (IsExempted(lineNum, context.ExemptedRegions))
                    continue;

                string upper = lines[i].Trim().ToUpperInvariant();
                if (upper.StartsWith("FROM"))
                {
                    // FROM 子句中出现逗号分隔的多个表引用
                    var commaMatch = Regex.Match(lines[i], @"FROM\s+\w+\s*,\s*\w+");
                    if (commaMatch.Success)
                    {
                        results.Add(LintResult.CreateManual(
                            lineNum, 1,
                            RuleId, "FROM 子句中存在逗号分隔的隐式连接，建议使用显式 JOIN ON",
                            RuleSeverity.Warning
                        ));
                    }
                }
            }

            return results;
        }
    }
}
