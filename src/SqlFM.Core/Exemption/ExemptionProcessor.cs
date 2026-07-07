using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SqlFM.Core.Exemption
{
    /// <summary>
    /// 统一豁免处理器：在格式化前提取豁免区域，格式化后恢复原文
    /// </summary>
    public class ExemptionProcessor
    {
        private readonly FormatOffOnParser _blockParser = new FormatOffOnParser();
        private readonly NoFormatLineParser _lineParser = new NoFormatLineParser();
        private readonly RegexIgnoreRule _regexRule = new RegexIgnoreRule();

        /// <summary>
        /// 加载正则忽略规则。
        /// </summary>
        /// <param name="patterns">正则表达式模式字符串集合</param>
        public void LoadRegexRules(IEnumerable<string> patterns)
        {
            _regexRule.LoadRules(patterns);
        }

        /// <summary>
        /// 预处理：提取所有豁免区域，用占位符替换。
        /// 返回处理后的SQL和豁免区域列表。
        /// </summary>
        /// <param name="sql">原始 SQL 文本</param>
        /// <returns>(处理后SQL, 豁免区域列表) 元组</returns>
        public (string processedSql, IList<ExemptionRegion> regions) PreProcess(string sql)
        {
            // 收集所有豁免区域
            var allRegions = new List<ExemptionRegion>();
            allRegions.AddRange(_blockParser.Parse(sql));
            allRegions.AddRange(_lineParser.Parse(sql));
            allRegions.AddRange(_regexRule.FindMatches(sql));

            // 按起始位置排序，合并重叠区域（传入原始SQL用于重新截取文本）
            var merged = MergeOverlapping(allRegions.OrderBy(r => r.StartIndex).ToList(), sql);

            if (merged.Count == 0)
                return (sql, merged);

            // 用唯一占位符替换豁免区域（从后向前替换，避免索引偏移）
            var sb = new StringBuilder(sql);
            for (int i = merged.Count - 1; i >= 0; i--)
            {
                var region = merged[i];
                var placeholder = $"__EXEMPT_{i}__";
                sb.Remove(region.StartIndex, region.EndIndex - region.StartIndex);
                sb.Insert(region.StartIndex, placeholder);
            }

            return (sb.ToString(), merged);
        }

        /// <summary>
        /// 后处理：将占位符恢复为原始文本。
        /// </summary>
        /// <param name="formattedSql">格式化后的 SQL 文本（含占位符）</param>
        /// <param name="regions">预处理阶段提取的豁免区域列表</param>
        /// <returns>恢复豁免区域后的最终 SQL 文本</returns>
        public string PostProcess(string formattedSql, IList<ExemptionRegion> regions)
        {
            var result = formattedSql;
            for (int i = 0; i < regions.Count; i++)
            {
                var placeholder = $"__EXEMPT_{i}__";
                result = result.Replace(placeholder, regions[i].OriginalText);
            }
            return result;
        }

        /// <summary>
        /// 合并重叠的豁免区域
        /// </summary>
        /// <param name="sorted">按 StartIndex 排序的区域列表</param>
        /// <param name="originalSql">原始SQL文本，用于合并后重新截取 OriginalText</param>
        private IList<ExemptionRegion> MergeOverlapping(List<ExemptionRegion> sorted, string originalSql)
        {
            if (sorted.Count <= 1) return sorted;

            var merged = new List<ExemptionRegion> { sorted[0] };
            for (int i = 1; i < sorted.Count; i++)
            {
                var last = merged[merged.Count - 1];
                if (sorted[i].StartIndex <= last.EndIndex)
                {
                    // 合并重叠区域：扩展结束位置，并从原始SQL重新截取文本
                    int newEnd = System.Math.Max(last.EndIndex, sorted[i].EndIndex);
                    last.EndIndex = newEnd;
                    last.OriginalText = originalSql.Substring(last.StartIndex, newEnd - last.StartIndex);
                }
                else
                {
                    merged.Add(sorted[i]);
                }
            }
            return merged;
        }
    }
}
