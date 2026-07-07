namespace SqlFM.Core.Exemption
{
    /// <summary>
    /// 表示一个豁免区域（不需要格式化的文本范围）
    /// </summary>
    public class ExemptionRegion
    {
        /// <summary>起始位置（字符索引）</summary>
        public int StartIndex { get; set; }

        /// <summary>结束位置（字符索引）</summary>
        public int EndIndex { get; set; }

        /// <summary>原始文本内容</summary>
        public string OriginalText { get; set; } = string.Empty;

        /// <summary>豁免类型</summary>
        public ExemptionType Type { get; set; }
    }

    /// <summary>
    /// 豁免类型枚举
    /// </summary>
    public enum ExemptionType
    {
        /// <summary>/* FORMAT OFF */ ... /* FORMAT ON */ 区间</summary>
        BlockFormatOff,
        /// <summary>行尾 -- NOFORMAT 标记</summary>
        LineNoFormat,
        /// <summary>正则规则匹配</summary>
        RegexMatch
    }
}
