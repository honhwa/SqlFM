using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace SqlFM.Core.Exemption
{
    /// <summary>
    /// 解析行尾 -- NOFORMAT 单行豁免标记
    /// </summary>
    public class NoFormatLineParser
    {
        // 匹配行尾 -- NOFORMAT（不区分大小写）
        private static readonly Regex NoFormatPattern = new Regex(
            @"^(.*--\s*NOFORMAT\s*)$",
            RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Compiled);

        /// <summary>
        /// 从 SQL 文本中解析所有带 NOFORMAT 标记的行。
        /// </summary>
        /// <param name="sql">原始 SQL 文本</param>
        /// <returns>NOFORMAT 行豁免区域列表</returns>
        public IList<ExemptionRegion> Parse(string sql)
        {
            var regions = new List<ExemptionRegion>();
            var matches = NoFormatPattern.Matches(sql);

            foreach (Match match in matches)
            {
                regions.Add(new ExemptionRegion
                {
                    StartIndex = match.Index,
                    EndIndex = match.Index + match.Length,
                    OriginalText = match.Value,
                    Type = ExemptionType.LineNoFormat
                });
            }

            return regions;
        }
    }
}
