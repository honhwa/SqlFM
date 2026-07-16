using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace SqlFM.Core.Lint.Rules.Tsql
{
    /// <summary>
    /// TQ01 — tsql.sp_prefix 规则：检查存储过程是否使用 sp_ 前缀。
    /// 借鉴 sqlfluff 的 TQ01：sp_ 前缀是 SQL Server 系统存储过程的保留前缀，
    /// 用户自定义存储过程不应使用此前缀（会导致性能问题和命名冲突）。
    /// 不可自动修复（需手动重命名存储过程）。
    /// </summary>
    public class TQ01_SpPrefixRule : SqlRuleBase
    {
        /// <inheritdoc/>
        public override string RuleId => "TQ01";

        /// <inheritdoc/>
        public override string RuleName => "tsql.sp_prefix";

        /// <inheritdoc/>
        public override string Description => "存储过程不应使用 sp_ 前缀（系统保留前缀）";

        /// <inheritdoc/>
        public override RuleSeverity Severity => RuleSeverity.Warning;

        /// <inheritdoc/>
        public override bool IsFixCompatible => false;

        /// <inheritdoc/>
        public override string[] Groups => new[] { "all", "tsql" };

        /// <inheritdoc/>
        public override string[] SupportedDialects => new[] { "tsql" };

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

                var spMatch = Regex.Match(lines[i], @"\bsp_\w+", RegexOptions.IgnoreCase);
                if (spMatch.Success)
                {
                    // 排除系统存储过程引用（如 EXEC sp_help）
                    string prefix = spMatch.Value.ToUpperInvariant();
                    if (!prefix.StartsWith("SP_"))
                        continue;

                    // CREATE PROCEDURE sp_xxx 是最严重的
                    string lineUpper = lines[i].Trim().ToUpperInvariant();
                    bool isCreate = lineUpper.Contains("CREATE") && lineUpper.Contains("PROCEDURE") ||
                        lineUpper.Contains("CREATE") && lineUpper.Contains("PROC");

                    results.Add(LintResult.CreateManual(
                        lineNum, spMatch.Index + 1,
                        RuleId,
                        isCreate ? $"创建存储过程使用 sp_ 前缀 '{spMatch.Value}'（系统保留前缀，会导致命名冲突）"
                                 : $"引用 sp_ 前缀存储过程 '{spMatch.Value}'（建议检查是否为自定义过程误用系统前缀）",
                        isCreate ? RuleSeverity.Error : RuleSeverity.Warning
                    ));
                }
            }

            return results;
        }
    }

    /// <summary>
    /// TQ02 — tsql.procedure_begin_end 规则：检查存储过程是否包含 BEGIN...END。
    /// 借鉴 sqlfluff 的 TQ02：存储过程体应被 BEGIN...END 包裹。
    /// 不可自动修复（需手动添加 BEGIN...END）。
    /// </summary>
    public class TQ02_ProcedureBeginEndRule : SqlRuleBase
    {
        /// <inheritdoc/>
        public override string RuleId => "TQ02";

        /// <inheritdoc/>
        public override string RuleName => "tsql.procedure_begin_end";

        /// <inheritdoc/>
        public override string Description => "存储过程应包含 BEGIN...END";

        /// <inheritdoc/>
        public override RuleSeverity Severity => RuleSeverity.Warning;

        /// <inheritdoc/>
        public override bool IsFixCompatible => false;

        /// <inheritdoc/>
        public override string[] Groups => new[] { "all", "tsql" };

        /// <inheritdoc/>
        public override string[] SupportedDialects => new[] { "tsql" };

        /// <inheritdoc/>
        public override List<LintResult> Evaluate(RuleContext context)
        {
            var results = new List<LintResult>();
            string sql = context.Sql;

            // 检测 CREATE PROCEDURE 后是否有 BEGIN
            var procMatch = Regex.Match(sql,
                @"CREATE\s+PROC(EDURE)?\s+\w+\s*.*?AS\s+(?!BEGIN)",
                RegexOptions.IgnoreCase | RegexOptions.Multiline);

            if (procMatch.Success)
            {
                // 找到行号
                int pos = procMatch.Index;
                int lineNum = 1;
                for (int j = 0; j < pos && j < sql.Length; j++)
                {
                    if (sql[j] == '\n') lineNum++;
                }

                results.Add(LintResult.CreateManual(
                    lineNum, 1,
                    RuleId, "存储过程 AS 后缺少 BEGIN...END 包裹",
                    RuleSeverity.Warning
                ));
            }

            return results;
        }
    }
}
