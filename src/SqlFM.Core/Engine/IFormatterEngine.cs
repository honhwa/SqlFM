namespace SqlFM.Core.Engine
{
    /// <summary>
    /// 格式化引擎接口：定义 SQL 格式化与语法校验的统一契约。
    /// 当前实现：<see cref="PoorMansEngine"/>（基于 Poor Man's T-SQL Formatter）。
    /// </summary>
    public interface IFormatterEngine
    {
        /// <summary>
        /// 格式化 SQL 文本，返回格式化后的字符串。
        /// </summary>
        /// <param name="sql">待格式化的 SQL 文本</param>
        /// <returns>格式化后的 SQL 字符串</returns>
        string Format(string sql);

        /// <summary>
        /// 验证 SQL 语法是否合法。
        /// </summary>
        /// <param name="sql">待校验的 SQL 文本</param>
        /// <param name="errors">校验失败时输出的错误消息数组；成功时为空数组</param>
        /// <returns>语法合法返回 true，否则返回 false</returns>
        bool Validate(string sql, out string[] errors);
    }
}
