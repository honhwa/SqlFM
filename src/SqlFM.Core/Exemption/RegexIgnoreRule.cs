using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace SqlFM.Core.Exemption
{
    /// <summary>
    /// 基于正则表达式的代码忽略规则引擎
    /// </summary>
    public class RegexIgnoreRule
    {
        private readonly List<Regex> _rules = new List<Regex>();

        /// <summary>
        /// 添加正则忽略规则。
        /// </summary>
        /// <param name="pattern">正则表达式模式字符串</param>
        public void AddRule(string pattern)
        {
            _rules.Add(new Regex(pattern, RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Multiline));
        }

        /// <summary>
        /// 从规则列表批量加载（先清空再逐条添加，跳过无效正则）。
        /// </summary>
        /// <param name="patterns">正则表达式模式字符串集合</param>
        public void LoadRules(IEnumerable<string> patterns)
        {
            _rules.Clear();
            foreach (var pattern in patterns)
            {
                if (!string.IsNullOrWhiteSpace(pattern))
                {
                    try { AddRule(pattern); } catch { /* 跳过无效正则 */ }
                }
            }
        }

        /// <summary>
        /// 查找所有匹配忽略规则的区域。
        /// </summary>
        /// <param name="sql">原始 SQL 文本</param>
        /// <returns>正则匹配的豁免区域列表</returns>
        public IList<ExemptionRegion> FindMatches(string sql)
        {
            var regions = new List<ExemptionRegion>();
            foreach (var rule in _rules)
            {
                var matches = rule.Matches(sql);
                foreach (Match match in matches)
                {
                    regions.Add(new ExemptionRegion
                    {
                        StartIndex = match.Index,
                        EndIndex = match.Index + match.Length,
                        OriginalText = match.Value,
                        Type = ExemptionType.RegexMatch
                    });
                }
            }
            return regions;
        }
    }
}
