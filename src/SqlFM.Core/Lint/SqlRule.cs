using System;
using System.Collections.Generic;
using SqlFM.Core.Configuration;
using SqlFM.Core.Dialects;

namespace SqlFM.Core.Lint
{
    /// <summary>
    /// SQL Lint 规则接口，借鉴 sqlfluff 的 BaseRule 设计。
    /// 每条规则声明自己的 ID、名称、描述、严重等级、适用方言和配置关键字，
    /// 并通过 Evaluate 方法基于 RuleContext 返回 LintResult 列表。
    /// </summary>
    public interface ISqlRule
    {
        /// <summary>规则代码（如 "LT01", "CP01"），遵循 XX## 格式</summary>
        string RuleId { get; }

        /// <summary>规则名称（如 "layout.spacing"），遵循 bundle.rule_name 格式</summary>
        string RuleName { get; }

        /// <summary>规则描述</summary>
        string Description { get; }

        /// <summary>严重等级</summary>
        RuleSeverity Severity { get; }

        /// <summary>是否支持自动修复</summary>
        bool IsFixCompatible { get; }

        /// <summary>规则组（如 "core", "layout", "tsql"）</summary>
        string[] Groups { get; }

        /// <summary>适用的方言名称列表（空数组表示适用于所有方言）</summary>
        string[] SupportedDialects { get; }

        /// <summary>可配置参数名列表</summary>
        string[] ConfigKeywords { get; }

        /// <summary>基于上下文评估规则，返回 LintResult 列表</summary>
        List<LintResult> Evaluate(RuleContext context);
    }

    /// <summary>
    /// SQL Lint 规则基类，提供默认实现和辅助方法。
    /// 子类只需覆盖 Evaluate 方法和声明元数据属性。
    /// </summary>
    public abstract class SqlRuleBase : ISqlRule
    {
        /// <inheritdoc/>
        public abstract string RuleId { get; }

        /// <inheritdoc/>
        public abstract string RuleName { get; }

        /// <inheritdoc/>
        public abstract string Description { get; }

        /// <inheritdoc/>
        public virtual RuleSeverity Severity => RuleSeverity.Warning;

        /// <inheritdoc/>
        public virtual bool IsFixCompatible => true;

        /// <inheritdoc/>
        public virtual string[] Groups => new[] { "all" };

        /// <inheritdoc/>
        public virtual string[] SupportedDialects => Array.Empty<string>();

        /// <inheritdoc/>
        public virtual string[] ConfigKeywords => Array.Empty<string>();

        /// <inheritdoc/>
        public abstract List<LintResult> Evaluate(RuleContext context);

        /// <summary>判断指定行是否在豁免区域内</summary>
        protected bool IsExempted(int line, List<ExemptionRegion> regions)
        {
            foreach (var region in regions)
            {
                if (line >= region.StartLine && line <= region.EndLine)
                    return true;
            }
            return false;
        }

        /// <summary>判断当前方言是否适用此规则</summary>
        protected bool AppliesToDialect(SqlDialect dialect)
        {
            if (SupportedDialects.Length == 0)
                return true; // 空数组 = 所有方言

            foreach (var name in SupportedDialects)
            {
                if (dialect.Name == name)
                    return true;
            }
            return false;
        }

        /// <summary>将源文本按行拆分</summary>
        protected static string[] SplitLines(string sql)
        {
            return sql.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        }
    }
}
