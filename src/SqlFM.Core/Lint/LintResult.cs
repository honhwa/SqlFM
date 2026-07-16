using System.Collections.Generic;

namespace SqlFM.Core.Lint
{
    /// <summary>
    /// Lint 检测结果，封装规则评估的输出。
    /// 借鉴 sqlfluff 的 LintResult 设计：anchor 标识问题位置，fixes 描述修复操作，
    /// description 覆盖规则默认描述，memory 在段间传递工作状态。
    /// </summary>
    public class LintResult
    {
        /// <summary>违规位置行号（从 1 开始）</summary>
        public int Line { get; set; }

        /// <summary>违规位置列号（从 1 开始）</summary>
        public int Column { get; set; }

        /// <summary>违规位置的原始文本</summary>
        public string AnchorText { get; set; } = string.Empty;

        /// <summary>触发的规则 ID</summary>
        public string RuleId { get; set; } = string.Empty;

        /// <summary>违规描述（覆盖规则默认描述时使用）</summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>严重等级</summary>
        public RuleSeverity Severity { get; set; }

        /// <summary>修复操作列表（空列表表示需手动修复）</summary>
        public List<LintFix> Fixes { get; set; } = new List<LintFix>();

        /// <summary>工作记忆：在 Lint 遍历中从一段传递到下一段的状态数据。
        /// 借鉴 sqlfluff 的 memory 机制，用于跨段追踪（如 AL05 未使用别名检测）。</summary>
        public object? Memory { get; set; }

        /// <summary>是否有可自动修复的操作</summary>
        public bool HasFixes => Fixes.Count > 0;

        /// <summary>创建无修复的 LintResult（需手动修复）</summary>
        public static LintResult CreateManual(int line, int column, string ruleId, string description, RuleSeverity severity)
        {
            return new LintResult
            {
                Line = line,
                Column = column,
                RuleId = ruleId,
                Description = description,
                Severity = severity,
                AnchorText = string.Empty
            };
        }

        /// <summary>创建带自动修复的 LintResult</summary>
        public static LintResult CreateWithFix(int line, int column, string ruleId, string description, RuleSeverity severity, List<LintFix> fixes)
        {
            return new LintResult
            {
                Line = line,
                Column = column,
                RuleId = ruleId,
                Description = description,
                Severity = severity,
                Fixes = fixes
            };
        }

        /// <summary>格式化为标准 Lint 输出行（借鉴 sqlfluff 的 L:P:ID:Desc 格式）</summary>
        public string ToDisplayString()
        {
            return $"L:{Line} | P:{Column} | {RuleId} | {Description}";
        }
    }
}
