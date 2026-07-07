using System.Xml.Serialization;

namespace SqlFM.Core.Configuration
{
    /// <summary>
    /// CTE（公用表表达式）格式化设置（分组3）：WITH 语句、递归 CTE 等格式控制。
    /// </summary>
    public class CteSettings
    {
        /// <summary>简单 CTE 是否压缩为单行，默认 false</summary>
        [XmlElement]
        public bool WithSingleLine { get; set; } = false;

        /// <summary>多 CTE 时，分隔逗号是否另起新行，默认 true</summary>
        [XmlElement]
        public bool CteCommaNewLine { get; set; } = true;

        /// <summary>CTE 查询体相对 WITH 的缩进层数，默认 1</summary>
        [XmlElement]
        public int CteQueryIndent { get; set; } = 1;

        /// <summary>多个 CTE 之间是否用空行分隔，默认 true</summary>
        [XmlElement]
        public bool CteBlankLineSplit { get; set; } = true;

        /// <summary>递归 CTE 中 UNION ALL 的缩进方式（相对 CTE 查询体），默认 false 表示同层</summary>
        [XmlElement]
        public bool RecursiveCteUnionIndent { get; set; } = false;
    }
}
