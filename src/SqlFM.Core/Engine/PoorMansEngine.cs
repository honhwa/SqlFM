using System;
using PoorMansTSqlFormatterLib.Formatters;
using PoorMansTSqlFormatterLib.Parsers;
using PoorMansTSqlFormatterLib.Tokenizers;
using SqlFM.Core.Configuration;

namespace SqlFM.Core.Engine
{
    /// <summary>
    /// Poor Man's T-SQL Formatter 引擎封装。
    /// 将第三方库 PoorMansTSqlFormatterLib 的 tokenizer → parser → formatter 管道
    /// 适配为 <see cref="IFormatterEngine"/> 接口，并根据 <see cref="SqlFormatStyle"/> 配置 formatter 参数。
    /// </summary>
    public class PoorMansEngine : IFormatterEngine
    {
        // 当前格式化样式配置
        private SqlFormatStyle _style = new SqlFormatStyle();

        /// <summary>
        /// 加载格式化样式配置。
        /// </summary>
        /// <param name="style">格式化样式定义；为 null 时使用默认样式</param>
        public void Configure(SqlFormatStyle style)
        {
            _style = style ?? new SqlFormatStyle();
        }

        /// <summary>
        /// 格式化 SQL 文本：tokenizer → parser → formatter 管道处理。
        /// </summary>
        /// <param name="sql">待格式化的 SQL 文本</param>
        /// <returns>格式化后的 SQL 字符串</returns>
        public string Format(string sql)
        {
            var tokenizer = new TSqlStandardTokenizer();
            var parser = new TSqlStandardParser();
            var formatter = CreateFormatter();

            var tokenized = tokenizer.TokenizeSQL(sql);
            var parsed = parser.ParseSQL(tokenized);
            return formatter.FormatSQLTree(parsed);
        }

        /// <summary>
        /// 验证 SQL 语法：尝试 tokenize + parse，捕获异常即为语法错误。
        /// </summary>
        /// <param name="sql">待校验的 SQL 文本</param>
        /// <param name="errors">校验失败时输出的错误消息数组</param>
        /// <returns>语法合法返回 true，否则返回 false</returns>
        public bool Validate(string sql, out string[] errors)
        {
            errors = Array.Empty<string>();
            try
            {
                var tokenizer = new TSqlStandardTokenizer();
                var parser = new TSqlStandardParser();
                var tokenized = tokenizer.TokenizeSQL(sql);
                parser.ParseSQL(tokenized);
                return true;
            }
            catch (Exception ex)
            {
                errors = new[] { ex.Message };
                return false;
            }
        }

        /// <summary>
        /// 根据当前样式配置创建 TSqlStandardFormatter 实例
        /// TSqlStandardFormatter 通过构造函数参数配置，不支持属性赋值后生效
        /// </summary>
        private TSqlStandardFormatter CreateFormatter()
        {
            var g = _style.Global;
            var d = _style.Dml;

            // 缩进字符串
            string indentString = g.IndentType == IndentType.Tabs
                ? "\t"
                : new string(' ', Math.Max(1, g.IndentSize));

            // 逗号位置：TrailingCommas=true 表示逗号后置（行末）
            bool trailingCommas = d.CommaPosition == CommaPosition.After;

            // 关键字大小写：仅支持 Upper/Lower，Pascal 按 Upper 处理
            bool uppercaseKeywords = g.KeywordCase != KeywordCase.Lower;

            // JOIN ON 换行
            bool breakJoinOnSections = d.JoinKeywordNewLine;

            // CASE 展开
            bool expandCaseStatements = _style.Case.CaseEachWhenNewLine;

            // 构造函数签名：
            // (indentString, spacesPerTab, maxLineWidth, expandCommaLists,
            //  trailingCommas, spaceAfterExpandedComma, expandBooleanExpressions,
            //  expandCaseStatements, expandBetweenConditions, breakJoinOnSections,
            //  uppercaseKeywords, htmlColoring, keywordStandardization)
            return new TSqlStandardFormatter(
                indentString: indentString,
                spacesPerTab: g.TabWidth,
                maxLineWidth: g.MaxLineWidth,
                expandCommaLists: true,
                trailingCommas: trailingCommas,
                spaceAfterExpandedComma: true,
                expandBooleanExpressions: true,
                expandCaseStatements: expandCaseStatements,
                expandBetweenConditions: true,
                breakJoinOnSections: breakJoinOnSections,
                uppercaseKeywords: uppercaseKeywords,
                htmlColoring: false,
                keywordStandardization: true
            );
        }
    }
}
