using System.Xml.Serialization;

namespace SqlFM.Core.Configuration
{
    /// <summary>
    /// CASE 表达式格式化设置（分组4）：WHEN/THEN/ELSE/END 结构格式控制。
    /// </summary>
    public class CaseSettings
    {
        /// <summary>每个 WHEN 分支是否另起新行，默认 true</summary>
        [XmlElement]
        public bool CaseEachWhenNewLine { get; set; } = true;

        /// <summary>WHEN 条件相对 CASE 的缩进层数，默认 1</summary>
        [XmlElement]
        public int WhenConditionIndent { get; set; } = 1;

        /// <summary>THEN 后的值是否与其他分支对齐，默认 false</summary>
        [XmlElement]
        public bool ThenValueAlign { get; set; } = false;

        /// <summary>ELSE 分支是否另起新行，默认 true</summary>
        [XmlElement]
        public bool ElseNewLine { get; set; } = true;

        /// <summary>END 是否与 CASE 关键字对齐（同列），默认 true</summary>
        [XmlElement]
        public bool EndAlignCase { get; set; } = true;

        /// <summary>只有一个 WHEN 的简单 CASE 是否压缩为单行，默认 false</summary>
        [XmlElement]
        public bool ShortCaseSingleLine { get; set; } = false;
    }
}
