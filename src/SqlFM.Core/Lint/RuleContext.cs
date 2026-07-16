using System.Collections.Generic;
using SqlFM.Core.Configuration;
using SqlFM.Core.Dialects;

namespace SqlFM.Core.Lint
{
    /// <summary>
    /// 规则执行上下文，提供规则评估所需的全部环境信息。
    /// 借鉴 sqlfluff 的 RuleContext：包含源文本、AST、方言、配置、行号偏移等。
    /// </summary>
    public class RuleContext
    {
        /// <summary>待检查的 SQL 源文本</summary>
        public string Sql { get; set; } = string.Empty;

        /// <summary>SQL 源文本按行拆分（便于行号定位）</summary>
        public string[] Lines { get; set; } = new string[0];

        /// <summary>当前方言（决定关键字集合和规则适用性）</summary>
        public SqlDialect Dialect { get; set; } = AnsiDialect.Instance;

        /// <summary>当前格式化配置</summary>
        public SqlFormatStyle Style { get; set; } = new SqlFormatStyle();

        /// <summary>豁免区域列表（FORMAT OFF/ON、NOFORMAT 行）</summary>
        public List<ExemptionRegion> ExemptedRegions { get; set; } = new List<ExemptionRegion>();

        /// <summary>工作记忆：前一条规则传递的状态数据</summary>
        public object? Memory { get; set; }

        /// <summary>行号偏移（用于在部分文本上执行规则时映射到源文件位置）</summary>
        public int LineOffset { get; set; } = 0;
    }

    /// <summary>
    /// 豁免区域，描述源文本中被 FORMAT OFF/ON 或 NOFORMAT 标记的区域。
    /// Lint 规则跳过这些区域的检查。
    /// </summary>
    public class ExemptionRegion
    {
        /// <summary>豁免起始行号（从 1 开始）</summary>
        public int StartLine { get; set; }

        /// <summary>豁免结束行号</summary>
        public int EndLine { get; set; }

        /// <summary>豁免类型</summary>
        public string Type { get; set; } = string.Empty;
    }
}
