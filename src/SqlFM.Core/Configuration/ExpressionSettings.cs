using System.Xml.Serialization;

namespace SqlFM.Core.Configuration
{
    /// <summary>
    /// 表达式格式化设置（分组7）：运算符间距、子查询、IN/EXISTS、注释格式等。
    /// </summary>
    public class ExpressionSettings
    {
        /// <summary>运算符两侧是否加空格（如 a = b → a = b），默认 true</summary>
        [XmlElement]
        public bool OperatorSpacePad { get; set; } = true;

        /// <summary>子查询相对外层的缩进层数，默认 1</summary>
        [XmlElement]
        public int SubQueryIndent { get; set; } = 1;

        /// <summary>IN/EXISTS 子查询是否换行展开，默认 true</summary>
        [XmlElement]
        public bool InExistsWrap { get; set; } = true;

        /// <summary>单行注释（--）是否与代码对齐缩进，默认 true</summary>
        [XmlElement]
        public bool SingleCommentIndent { get; set; } = true;

        /// <summary>块注释（/* */）是否格式化（对齐星号），默认 false</summary>
        [XmlElement]
        public bool BlockCommentFormat { get; set; } = false;
    }
}
