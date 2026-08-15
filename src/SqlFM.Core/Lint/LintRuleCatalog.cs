using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SqlFM.Core.Configuration;
using SqlFM.Core.Dialects;
using SqlFM.Core.Exemption;

namespace SqlFM.Core.Lint
{
    /// <summary>
    /// Lint 规则目录：集中发现并注册所有 <see cref="ISqlRule"/> 实现，构建可直接使用的 <see cref="SqlRuleEngine"/>。
    /// 解决此前 SqlRuleEngine「已建成未接线」的问题——程序集中虽有多条规则，却从未被集中注册，
    /// 导致引擎实例化后 RuleCount 为 0、无法执行任何检查。
    /// 采用反射扫描 SqlFM.Core 程序集中所有具体（非抽象）、具备公共无参构造的 ISqlRule 类型，
    /// 对新增规则零配置自动接入，符合开闭原则。
    /// </summary>
    public static class LintRuleCatalog
    {
        private static readonly Lazy<SqlRuleEngine> _defaultEngine =
            new Lazy<SqlRuleEngine>(BuildEngine);

        /// <summary>获取内置全部规则的默认 Lint 引擎单例（线程安全，按需构建一次）。</summary>
        public static SqlRuleEngine DefaultEngine => _defaultEngine.Value;

        /// <summary>
        /// 构建并注册所有已发现规则的 Lint 引擎。
        /// </summary>
        /// <returns>已注册全部规则的 <see cref="SqlRuleEngine"/> 实例</returns>
        public static SqlRuleEngine BuildEngine()
        {
            var engine = new SqlRuleEngine();
            engine.RegisterAll(DiscoverRules());
            return engine;
        }

        /// <summary>
        /// 通过反射发现程序集中所有具体 ISqlRule 实现。
        /// 仅收集非抽象、可实现 ISqlRule、且拥有公共无参构造的类型；
        /// 单个规则实例化失败时会被跳过，不影响其他规则。
        /// </summary>
        /// <returns>规则实例列表</returns>
        public static IList<ISqlRule> DiscoverRules()
        {
            var rules = new List<ISqlRule>();
            var asm = typeof(LintRuleCatalog).Assembly;

            foreach (var type in asm.GetTypes())
            {
                if (type.IsAbstract || type.IsInterface)
                    continue;
                if (!typeof(ISqlRule).IsAssignableFrom(type))
                    continue;
                if (type.GetConstructor(Type.EmptyTypes) == null)
                    continue;

                try
                {
                    rules.Add((ISqlRule)Activator.CreateInstance(type)!);
                }
                catch
                {
                    // 容错：单条规则构建失败不影响整体
                }
            }

            // 按规则代码稳定排序，保证输出顺序可预期
            return rules.OrderBy(r => r.RuleId, StringComparer.Ordinal).ToList();
        }

        /// <summary>
        /// 使用默认引擎与 T-SQL 方言对 SQL 执行 Lint 检查。
        /// </summary>
        /// <param name="sql">待检查的 SQL 文本</param>
        /// <param name="style">格式化样式（部分规则读取配置）</param>
        /// <param name="exemptedRegions">豁免区域（区域内违规被过滤）</param>
        /// <param name="enableOnly">仅启用指定规则/组</param>
        /// <param name="disable">禁用指定规则</param>
        /// <returns>LintResult 列表</returns>
        public static List<LintResult> Lint(
            string sql,
            SqlFormatStyle style,
            List<ExemptionRegion>? exemptedRegions = null,
            string[]? enableOnly = null,
            string[]? disable = null)
        {
            return DefaultEngine.Lint(sql, TsqlDialect.Instance, style, exemptedRegions, enableOnly, disable);
        }

        /// <summary>
        /// 将豁免处理器的字符索引区域转换为 Lint 引擎所需的行号区域。
        /// 两者分属不同命名空间且语义不同（字符索引 vs 行号），此为既有设计错位；
        /// 通过统计目标索引前的换行符数量完成映射，使 FORMAT OFF/ON、NOFORMAT 等豁免段
        /// 在 Lint 中同样生效。
        /// </summary>
        /// <param name="regions">豁免处理器返回的区域列表（字符索引）</param>
        /// <param name="sql">对应 SQL 文本（用于计算行号）</param>
        /// <returns>Lint 引擎可用的行号区域列表</returns>
        public static List<ExemptionRegion> ToLintRegions(IList<SqlFM.Core.Exemption.ExemptionRegion> regions, string sql)
        {
            var result = new List<ExemptionRegion>();
            foreach (var r in regions)
            {
                result.Add(new ExemptionRegion
                {
                    StartLine = LineOfIndex(sql, r.StartIndex),
                    EndLine = LineOfIndex(sql, r.EndIndex),
                    Type = r.Type.ToString()
                });
            }
            return result;
        }

        /// <summary>计算字符索引在文本中对应的 1-based 行号。</summary>
        private static int LineOfIndex(string sql, int index)
        {
            if (index < 0) index = 0;
            if (index > sql.Length) index = sql.Length;
            int line = 1;
            for (int i = 0; i < index && i < sql.Length; i++)
            {
                if (sql[i] == '\n')
                    line++;
            }
            return line;
        }
    }
}
