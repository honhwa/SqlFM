using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using SqlFM.Core.Dialects;

namespace SqlFM.Core.Lint.Rules.Aliasing
{
    /// <summary>
    /// AL02 — aliasing.column 规则：检查列别名是否使用显式 AS。
    /// 借鉴 sqlfluff 的 AL02：列别名应使用显式 AS 关键字而非隐式别名（空格分隔）。
    /// 可自动修复：在列别名前插入 AS。
    /// </summary>
    public class AL02_ColumnAliasRule : SqlRuleBase
    {
        /// <inheritdoc/>
        public override string RuleId => "AL02";

        /// <inheritdoc/>
        public override string RuleName => "aliasing.column";

        /// <inheritdoc/>
        public override string Description => "列别名应使用显式 AS 关键字";

        /// <inheritdoc/>
        public override string[] Groups => new[] { "all", "core", "aliasing" };

        /// <inheritdoc/>
        public override List<LintResult> Evaluate(RuleContext context)
        {
            var results = new List<LintResult>();
            var lines = context.Lines;

            // 在 SELECT 子句中检测隐式别名（列名后空格直接跟标识符，无 AS）
            for (int i = 0; i < lines.Length; i++)
            {
                int lineNum = i + 1 + context.LineOffset;
                if (IsExempted(lineNum, context.ExemptedRegions))
                    continue;

                string upper = lines[i].Trim().ToUpperInvariant();

                // 检测 SELECT 列行中：表达式/列名 + 空格 + 别名标识符（无 AS）
                // 例：SELECT col alias → 应为 SELECT col AS alias
                var implicitAlias = Regex.Match(lines[i],
                    @"(\w+)\s+(\w+)(?!\s*=|\s*\(|\s*,|\s*FROM|\s*WHERE|\s*ORDER|\s*GROUP|\s*HAVING|\s*ON|\s*AND|\s*OR)",
                    RegexOptions.IgnoreCase);

                if (implicitAlias.Success && !upper.Contains("AS"))
                {
                    string expr = implicitAlias.Groups[1].Value;
                    string alias = implicitAlias.Groups[2].Value;

                    // 排除关键字作为"别名"的情况
                    if (!context.Dialect.IsAnyKeyword(alias))
                    {
                        results.Add(LintResult.CreateWithFix(
                            lineNum, implicitAlias.Groups[2].Index + 1,
                            RuleId, $"列别名 '{alias}' 缺少 AS 关键字",
                            RuleSeverity.Warning,
                            new List<LintFix> { LintFix.InsertBeforeAt(lineNum, implicitAlias.Groups[2].Index + 1, "AS ") }
                        ));
                    }
                }
            }

            return results;
        }
    }

    /// <summary>
    /// AL09 — aliasing.self_alias.column 规则：检查列是否自别名。
    /// 借鉴 sqlfluff 的 AL09：col AS col 是冗余的，应移除。
    /// 可自动修复：移除冗余 AS alias。
    /// </summary>
    public class AL09_SelfAliasRule : SqlRuleBase
    {
        /// <inheritdoc/>
        public override string RuleId => "AL09";

        /// <inheritdoc/>
        public override string RuleName => "aliasing.self_alias.column";

        /// <inheritdoc/>
        public override string Description => "列不应自别名（如 col AS col 是冗余的）";

        /// <inheritdoc/>
        public override string[] Groups => new[] { "all", "core", "aliasing" };

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

                // 匹配 col AS col（大小写无关）
                var selfAlias = Regex.Match(lines[i],
                    @"(\w+)\s+AS\s+(\1)\b",
                    RegexOptions.IgnoreCase);

                if (selfAlias.Success)
                {
                    string fullMatch = selfAlias.Value;
                    results.Add(LintResult.CreateWithFix(
                        lineNum, selfAlias.Index + 1,
                        RuleId, $"自别名 '{fullMatch}' 是冗余的",
                        RuleSeverity.Warning,
                        new List<LintFix> { LintFix.ReplaceAt(lineNum, selfAlias.Index + 1, fullMatch, selfAlias.Groups[1].Value) }
                    ));
                }
            }

            return results;
        }
    }
}
