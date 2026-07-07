using System;

namespace SqlFM.Core.Configuration
{
    /// <summary>缩进类型：空格或制表符</summary>
    public enum IndentType
    {
        /// <summary>使用空格缩进</summary>
        Spaces,
        /// <summary>使用制表符缩进</summary>
        Tabs
    }

    /// <summary>关键字大小写风格</summary>
    public enum KeywordCase
    {
        /// <summary>全大写，如 SELECT</summary>
        Upper,
        /// <summary>全小写，如 select</summary>
        Lower,
        /// <summary>Pascal 首字母大写，如 Select</summary>
        Pascal
    }

    /// <summary>对象名称大小写处理方式</summary>
    public enum ObjectNameCase
    {
        /// <summary>保持原样不变</summary>
        Keep,
        /// <summary>转换为大写</summary>
        Upper,
        /// <summary>转换为小写</summary>
        Lower
    }

    /// <summary>逗号位置：行末或行首</summary>
    public enum CommaPosition
    {
        /// <summary>逗号置于行末（后置）</summary>
        After,
        /// <summary>逗号置于行首（前置）</summary>
        Before
    }

    /// <summary>方括号处理模式</summary>
    public enum BracketMode
    {
        /// <summary>自动为保留字添加方括号</summary>
        AutoAdd,
        /// <summary>自动移除不必要的方括号</summary>
        AutoRemove,
        /// <summary>保持原样</summary>
        Keep
    }

    /// <summary>分号处理模式</summary>
    public enum SemicolonMode
    {
        /// <summary>自动在语句末尾添加分号</summary>
        AutoAdd,
        /// <summary>自动移除语句末尾分号</summary>
        AutoRemove,
        /// <summary>保持原样</summary>
        Keep
    }

    /// <summary>AS 关键字处理模式（用于别名）</summary>
    public enum AsKeywordMode
    {
        /// <summary>保持原样</summary>
        Keep,
        /// <summary>移除 AS 关键字</summary>
        Remove,
        /// <summary>对齐 AS 关键字列</summary>
        Align
    }
}
