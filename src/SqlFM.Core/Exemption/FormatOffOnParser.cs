using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace SqlFM.Core.Exemption
{
    /// <summary>
    /// 解析 /* FORMAT OFF */ ... /* FORMAT ON */ 块豁免标记
    /// </summary>
    public class FormatOffOnParser
    {
        // 支持多种写法：/* FORMAT OFF */, /* format off */, /*FORMAT OFF*/
        private static readonly Regex OffPattern = new Regex(
            @"/\*\s*FORMAT\s+OFF\s*\*/",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex OnPattern = new Regex(
            @"/\*\s*FORMAT\s+ON\s*\*/",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// <summary>
        /// 从 SQL 文本中解析所有 FORMAT OFF/ON 豁免区间。
        /// </summary>
        /// <param name="sql">原始 SQL 文本</param>
        /// <returns>FORMAT OFF/ON 豁免区域列表</returns>
        public IList<ExemptionRegion> Parse(string sql)
        {
            var regions = new List<ExemptionRegion>();
            var offMatches = OffPattern.Matches(sql);

            foreach (Match offMatch in offMatches)
            {
                int startIdx = offMatch.Index;
                // 从 OFF 标记之后搜索最近的 ON 标记
                var onMatch = OnPattern.Match(sql, offMatch.Index + offMatch.Length);
                int endIdx = onMatch.Success
                    ? onMatch.Index + onMatch.Length
                    : sql.Length; // 如果没有 ON，则豁免到文件末尾

                regions.Add(new ExemptionRegion
                {
                    StartIndex = startIdx,
                    EndIndex = endIdx,
                    OriginalText = sql.Substring(startIdx, endIdx - startIdx),
                    Type = ExemptionType.BlockFormatOff
                });
            }

            return regions;
        }
    }
}
