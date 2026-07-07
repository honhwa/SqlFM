using System.Xml.Serialization;

namespace SqlFM.Core.Configuration
{
    /// <summary>
    /// 全局格式化设置（分组1）：缩进、空行、关键字大小写、括号、分号等全局配置。
    /// </summary>
    public class GlobalSettings
    {
        /// <summary>缩进类型，默认使用空格</summary>
        [XmlElement]
        public IndentType IndentType { get; set; } = IndentType.Spaces;

        /// <summary>空格缩进宽度，默认 4 个空格</summary>
        [XmlElement]
        public int IndentSize { get; set; } = 4;

        /// <summary>制表符宽度（显示宽度），默认 4</summary>
        [XmlElement]
        public int TabWidth { get; set; } = 4;

        /// <summary>最大行宽（字符数），超过时尝试换行，默认 120</summary>
        [XmlElement]
        public int MaxLineWidth { get; set; } = 120;

        /// <summary>顶级语句之间的空行数，默认 1</summary>
        [XmlElement]
        public int StatementBlankLines { get; set; } = 1;

        /// <summary>子句（如 SELECT/FROM/WHERE）之间的空行数，默认 0</summary>
        [XmlElement]
        public int ClauseBlankLines { get; set; } = 0;

        /// <summary>GO 指令前的空行数，默认 1</summary>
        [XmlElement]
        public int GoBeforeBlankLines { get; set; } = 1;

        /// <summary>GO 指令后的空行数，默认 1</summary>
        [XmlElement]
        public int GoAfterBlankLines { get; set; } = 1;

        /// <summary>是否删除行尾空格，默认 true</summary>
        [XmlElement]
        public bool TrimTrailingSpaces { get; set; } = true;

        /// <summary>是否合并连续多个空格为一个，默认 true</summary>
        [XmlElement]
        public bool MergeMultipleSpaces { get; set; } = true;

        /// <summary>是否移除多余空行（超过 StatementBlankLines 的连续空行），默认 true</summary>
        [XmlElement]
        public bool RemoveExtraBlankLines { get; set; } = true;

        /// <summary>SQL 关键字大小写风格，默认大写</summary>
        [XmlElement]
        public KeywordCase KeywordCase { get; set; } = KeywordCase.Upper;

        /// <summary>内置函数大小写风格，默认大写</summary>
        [XmlElement]
        public KeywordCase FunctionCase { get; set; } = KeywordCase.Upper;

        /// <summary>数据类型大小写风格，默认大写</summary>
        [XmlElement]
        public KeywordCase DataTypeCase { get; set; } = KeywordCase.Upper;

        /// <summary>字面常量（数字/字符串）大小写风格，默认保持不变（Keep 映射为 Upper 枚举中处理）</summary>
        [XmlElement]
        public KeywordCase ConstantCase { get; set; } = KeywordCase.Upper;

        /// <summary>对象名称（表名/列名）大小写处理，默认保持</summary>
        [XmlElement]
        public ObjectNameCase ObjectNameCase { get; set; } = ObjectNameCase.Keep;

        /// <summary>变量和参数名称大小写处理，默认保持</summary>
        [XmlElement]
        public ObjectNameCase VariableParamCase { get; set; } = ObjectNameCase.Keep;

        /// <summary>方括号处理模式，默认保持</summary>
        [XmlElement]
        public BracketMode SquareBracketMode { get; set; } = BracketMode.Keep;

        /// <summary>左括号是否与表达式同行，默认 true</summary>
        [XmlElement]
        public bool ParenthesisOpenOnSameLine { get; set; } = true;

        /// <summary>右括号是否与对应左括号对齐，默认 true</summary>
        [XmlElement]
        public bool ParenthesisCloseAlign { get; set; } = true;

        /// <summary>短表达式是否压缩为单行，默认 false</summary>
        [XmlElement]
        public bool ShortExpressionSingleLine { get; set; } = false;

        /// <summary>是否标准化单引号（如将双引号字符串转换为单引号），默认 false</summary>
        [XmlElement]
        public bool SingleQuoteStandardize { get; set; } = false;

        /// <summary>分号处理模式，默认保持</summary>
        [XmlElement]
        public SemicolonMode SemicolonMode { get; set; } = SemicolonMode.Keep;
    }
}
