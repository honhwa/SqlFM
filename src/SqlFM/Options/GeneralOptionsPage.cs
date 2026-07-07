using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;

namespace SqlFM.Options
{
    /// <summary>
    /// SqlFM 格式化选项页面。
    /// 路径：工具 → 选项 → SqlFM → General
    /// 所有配置项通过 DialogPage 自动持久化到注册表。
    /// 注意：此页面暂时保留作为简单选项入口，后续 Task #16 会用 WPF 窗口替代。
    /// </summary>
    [Guid("A1B2C3D4-E5F6-7890-ABCD-EF1234567890")]
    public class GeneralOptionsPage : DialogPage
    {
        /// <summary>
        /// 是否启用保存时自动格式化。
        /// 默认值为 false。
        /// </summary>
        [Category("格式化")]
        [DisplayName("保存时自动格式化")]
        [Description("保存文件时自动格式化 SQL 文本")]
        [DefaultValue(false)]
        public bool FormatOnSave { get; set; } = false;

        /// <summary>
        /// 每级缩进的空格数。支持 2、4 或 8。
        /// 默认值为 4。
        /// </summary>
        [Category("格式化")]
        [DisplayName("缩进宽度")]
        [Description("每级缩进的空格数（2、4 或 8）")]
        [DefaultValue(4)]
        public int IndentWidth { get; set; } = 4;

        /// <summary>
        /// 是否将 SQL 关键字转换为大写。
        /// 默认值为 true。
        /// </summary>
        [Category("格式化")]
        [DisplayName("关键字大写")]
        [Description("是否将 SQL 关键字转换为大写")]
        [DefaultValue(true)]
        public bool UppercaseKeywords { get; set; } = true;

        /// <summary>
        /// 逗号放置位置：Trailing（后置）或 Leading（前置）。
        /// 默认值为 Trailing。
        /// </summary>
        [Category("格式化")]
        [DisplayName("逗号位置")]
        [Description("逗号放置位置：Trailing（后置）或 Leading（前置）")]
        [DefaultValue(CommaPlacement.Trailing)]
        public CommaPlacement CommaPosition { get; set; } = CommaPlacement.Trailing;

        /// <summary>
        /// 是否在 SELECT、FROM、WHERE 等子句前强制换行。
        /// 默认值为 true。
        /// </summary>
        [Category("格式化")]
        [DisplayName("子句强制换行")]
        [Description("是否在 SELECT、FROM、WHERE 等子句前强制换行")]
        [DefaultValue(true)]
        public bool ForceClauseNewLine { get; set; } = true;

        /// <summary>
        /// 注释是否跟随对应语句的缩进级别。
        /// 默认值为 true。
        /// </summary>
        [Category("格式化")]
        [DisplayName("注释跟随缩进")]
        [Description("注释是否跟随对应语句的缩进级别")]
        [DefaultValue(true)]
        public bool IndentComments { get; set; } = true;
    }

    /// <summary>
    /// 逗号放置位置枚举。
    /// </summary>
    public enum CommaPlacement
    {
        /// <summary>
        /// 后置（默认）：col1, col2, col3
        /// 逗号放在字段名之后。
        /// </summary>
        Trailing,

        /// <summary>
        /// 前置：col1
        ///            , col2
        ///            , col3
        /// 逗号放在字段名之前（换行后）。
        /// </summary>
        Leading
    }
}
