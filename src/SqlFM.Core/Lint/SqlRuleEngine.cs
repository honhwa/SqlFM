using System;
using System.Collections.Generic;
using System.Linq;
using SqlFM.Core.Configuration;
using SqlFM.Core.Dialects;

namespace SqlFM.Core.Lint
{
    /// <summary>
    /// SQL 规则引擎，借鉴 sqlfluff 的 RuleSet/RulePack 架构。
    /// 负责注册、过滤、实例化规则，并批量执行 Lint 检查。
    /// 支持规则组过滤（core/layout/convention 等）和方言适配。
    /// </summary>
    public class SqlRuleEngine
    {
        /// <summary>已注册的全部规则实例</summary>
        private readonly List<ISqlRule> _rules = new List<ISqlRule>();

        /// <summary>规则引用映射：code → rule, name → rule, group → rules</summary>
        private readonly Dictionary<string, ISqlRule> _codeMap = new Dictionary<string, ISqlRule>();
        private readonly Dictionary<string, ISqlRule> _nameMap = new Dictionary<string, ISqlRule>();
        private readonly Dictionary<string, List<ISqlRule>> _groupMap = new Dictionary<string, List<ISqlRule>>();

        /// <summary>已注册规则总数</summary>
        public int RuleCount => _rules.Count;

        /// <summary>注册一条规则</summary>
        public void Register(ISqlRule rule)
        {
            _rules.Add(rule);
            _codeMap[rule.RuleId] = rule;
            _nameMap[rule.RuleName] = rule;

            foreach (var group in rule.Groups)
            {
                if (!_groupMap.ContainsKey(group))
                    _groupMap[group] = new List<ISqlRule>();
                _groupMap[group].Add(rule);
            }
        }

        /// <summary>批量注册规则</summary>
        public void RegisterAll(IEnumerable<ISqlRule> rules)
        {
            foreach (var rule in rules)
                Register(rule);
        }

        /// <summary>获取规则 by code</summary>
        public ISqlRule? GetByCode(string code)
        {
            return _codeMap.TryGetValue(code, out var rule) ? rule : null;
        }

        /// <summary>获取规则 by name</summary>
        public ISqlRule? GetByName(string name)
        {
            return _nameMap.TryGetValue(name, out var rule) ? rule : null;
        }

        /// <summary>获取规则组中的所有规则</summary>
        public List<ISqlRule> GetByGroup(string group)
        {
            return _groupMap.TryGetValue(group, out var rules) ? new List<ISqlRule>(rules) : new List<ISqlRule>();
        }

        /// <summary>获取核心规则组</summary>
        public List<ISqlRule> GetCoreRules()
        {
            return GetByGroup("core");
        }

        /// <summary>获取适用于指定方言的规则</summary>
        public List<ISqlRule> GetRulesForDialect(SqlDialect dialect)
        {
            return _rules.Where(r =>
            {
                if (r.SupportedDialects.Length == 0) return true;
                return r.SupportedDialects.Contains(dialect.Name);
            }).ToList();
        }

        /// <summary>根据配置过滤规则（允许启用/禁用特定规则）</summary>
        public List<ISqlRule> FilterRules(List<ISqlRule> rules, string[]? enableOnly = null, string[]? disable = null)
        {
            var result = new List<ISqlRule>(rules);

            if (enableOnly != null && enableOnly.Length > 0)
            {
                result = result.Where(r =>
                    enableOnly.Contains(r.RuleId) ||
                    enableOnly.Contains(r.RuleName) ||
                    r.Groups.Any(g => enableOnly.Contains(g))
                ).ToList();
            }

            if (disable != null && disable.Length > 0)
            {
                result = result.Where(r =>
                    !disable.Contains(r.RuleId) &&
                    !disable.Contains(r.RuleName)
                ).ToList();
            }

            return result;
        }

        /// <summary>
        /// 对 SQL 文本执行 Lint 检查，返回所有违规结果。
        /// 借鉴 sqlfluff 的 crawl 机制：遍历所有适用规则，每条规则独立评估。
        /// </summary>
        /// <param name="sql">待检查的 SQL 文本</param>
        /// <param name="dialect">当前方言</param>
        /// <param name="style">格式化配置（部分规则读取配置）</param>
        /// <param name="exemptedRegions">豁免区域（这些区域内的违规将被过滤）</param>
        /// <param name="enableOnly">仅启用指定规则/组</param>
        /// <param name="disable">禁用指定规则</param>
        /// <returns>LintResult 列表</returns>
        public List<LintResult> Lint(
            string sql,
            SqlDialect dialect,
            SqlFormatStyle style,
            List<ExemptionRegion>? exemptedRegions = null,
            string[]? enableOnly = null,
            string[]? disable = null)
        {
            var context = new RuleContext
            {
                Sql = sql,
                Lines = sql.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None),
                Dialect = dialect,
                Style = style,
                ExemptedRegions = exemptedRegions ?? new List<ExemptionRegion>()
            };

            // 1. 获取适用于当前方言的规则
            var applicableRules = GetRulesForDialect(dialect);

            // 2. 根据配置过滤
            applicableRules = FilterRules(applicableRules, enableOnly, disable);

            // 3. 执行每条规则
            var allResults = new List<LintResult>();
            foreach (var rule in applicableRules)
            {
                try
                {
                    var results = rule.Evaluate(context);
                    if (results != null && results.Count > 0)
                    {
                        // 过滤豁免区域内的违规
                        allResults.AddRange(results.Where(r => !IsInExemptedRegion(r, context.ExemptedRegions)));
                    }
                }
                catch (Exception)
                {
                    // 规则执行失败不影响其他规则，借鉴 sqlfluff 的容错机制
                }
            }

            // 4. 按行号排序
            allResults.Sort((a, b) => a.Line == b.Line ? a.Column - b.Column : a.Line - b.Line);
            return allResults;
        }

        /// <summary>
        /// 对 LintResult 中的可自动修复项批量应用修复，返回修复后的 SQL 文本。
        /// 借鉴 sqlfluff 的 fix 流程：仅应用 is_fix_compatible 规则的修复建议。
        /// </summary>
        public string AutoFix(string sql, List<LintResult> results)
        {
            string current = sql;
            var fixableResults = results.Where(r => r.HasFixes).ToList();

            // 按行号逆序应用修复（避免位置偏移），借鉴 sqlfluff 的修复排序策略
            fixableResults.Sort((a, b) => b.Line == a.Line ? b.Column - a.Column : b.Line - a.Line);

            foreach (var result in fixableResults)
            {
                foreach (var fix in result.Fixes)
                {
                    current = fix.Apply(current);
                }
            }

            return current;
        }

        /// <summary>判断 LintResult 是否在豁免区域内</summary>
        private static bool IsInExemptedRegion(LintResult result, List<ExemptionRegion> regions)
        {
            foreach (var region in regions)
            {
                if (result.Line >= region.StartLine && result.Line <= region.EndLine)
                    return true;
            }
            return false;
        }
    }
}
