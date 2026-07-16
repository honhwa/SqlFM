namespace SqlFM.Core.Lint
{
    /// <summary>
    /// Lint 规则严重等级，借鉴 sqlfluff 的分类体系。
    /// Error 为必须修复的语法/约定违规，Warning 为建议性改进，Info 为信息提示。
    /// </summary>
    public enum RuleSeverity
    {
        /// <summary>错误级别：必须修复，如语法问题、安全风险</summary>
        Error,

        /// <summary>警告级别：建议改进，如风格不一致、潜在歧义</summary>
        Warning,

        /// <summary>信息级别：提示信息，如行长度超限</summary>
        Info
    }
}
