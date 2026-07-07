using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using SqlFM.Core.Configuration;
using SqlFM.Core.Exemption;
using SqlFM.Core.Refactoring;

namespace SqlFM.Core.Engine
{
    /// <summary>
    /// 格式化管道：预处理(豁免) → 预重构(JOIN转换) → 格式化(PoorMans) → 后处理(清理/大小写/重构/T-SQL/对齐) → 恢复豁免 → 最终清理。
    /// 协调 PoorMansEngine、ScriptDomEngine、ExemptionProcessor、BracketNormalizer、SchemaPrefix、JoinConverter、
    /// CasePostProcessor、AlignmentPostProcessor、TsqlNameFormatter，是 Core 库对外暴露的统一格式化入口。
    /// </summary>
    public class FormatterPipeline
    {
        // 主格式化引擎（Poor Man's T-SQL Formatter 封装）
        private readonly PoorMansEngine _mainEngine;
        // ScriptDom 辅助引擎（语法校验、AST 解析）
        private readonly ScriptDomEngine _scriptDom;
        // 豁免处理器（提取/恢复豁免区域）
        private readonly ExemptionProcessor _exemption;
        // 方括号标准化工具（基于 ScriptDom AST）
        private readonly BracketNormalizer _bracketNormalizer;
        // dbo 架构前缀工具（基于 ScriptDom AST）
        private readonly SchemaPrefix _schemaPrefix;
        // 隐式 JOIN 转换工具（基于正则匹配）
        private readonly JoinConverter _joinConverter;
        // 函数名/数据类型大小写后处理器（基于 ScriptDom token 流）
        private readonly CasePostProcessor _casePostProcessor;
        // 对齐后处理器（运算符/VALUES/SET/AS/别名/注释/块注释）
        private readonly AlignmentPostProcessor _alignmentProcessor;
        // T-SQL 标识符名称格式化器（临时表/表变量）
        private readonly TsqlNameFormatter _nameFormatter;
        // 当前加载的格式化样式
        private SqlFormatStyle _style;

        /// <summary>
        /// 初始化格式化管道，创建子引擎实例并加载默认样式。
        /// </summary>
        public FormatterPipeline()
        {
            _mainEngine = new PoorMansEngine();
            _scriptDom = new ScriptDomEngine();
            _exemption = new ExemptionProcessor();
            _bracketNormalizer = new BracketNormalizer();
            _schemaPrefix = new SchemaPrefix();
            _joinConverter = new JoinConverter();
            _casePostProcessor = new CasePostProcessor();
            _alignmentProcessor = new AlignmentPostProcessor();
            _nameFormatter = new TsqlNameFormatter();
            _style = new SqlFormatStyle();
        }

        /// <summary>
        /// 加载格式化样式，同步配置到主引擎和豁免处理器。
        /// </summary>
        /// <param name="style">格式化样式定义；为 null 时使用默认样式</param>
        public void LoadStyle(SqlFormatStyle style)
        {
            _style = style ?? new SqlFormatStyle();
            _mainEngine.Configure(_style);
            _exemption.LoadRegexRules(_style.IgnoreConfig.RegexIgnoreRules);
        }

        /// <summary>
        /// 执行完整格式化管道（豁免提取 → 预重构 → 主格式化 → 后处理(清理/大小写/重构/T-SQL/对齐) → 豁免恢复 → 最终清理）。
        /// </summary>
        /// <param name="sql">待格式化的 SQL 文本</param>
        /// <returns>包含格式化结果、原始文本、成功标志和错误信息的 <see cref="FormatResult"/></returns>
        public FormatResult Format(string sql)
        {
            if (string.IsNullOrWhiteSpace(sql))
                return new FormatResult { FormattedSql = sql ?? string.Empty, Success = true };

            try
            {
                // Step 1: 预处理 — 提取豁免区域
                var (processedSql, regions) = _exemption.PreProcess(sql);

                // Step 2: 预重构 — 隐式 JOIN 转显式（在格式化前改变 SQL 结构）
                string preProcessed = ApplyPreRefactoring(processedSql);

                // Step 3: 主格式化引擎
                string formatted = _mainEngine.Format(preProcessed);

                // Step 4: 后处理 — 应用 Poor Man's 不支持的高级规则
                formatted = ApplyPostProcessing(formatted);

                // Step 5: 恢复豁免区域
                formatted = _exemption.PostProcess(formatted, regions);

                // Step 6: 最终清理
                formatted = FinalCleanup(formatted);

                return new FormatResult
                {
                    FormattedSql = formatted,
                    Success = true,
                    OriginalSql = sql
                };
            }
            catch (Exception ex)
            {
                // 容错：格式化失败时返回原文 + 错误信息
                return new FormatResult
                {
                    FormattedSql = sql,
                    Success = false,
                    ErrorMessage = ex.Message,
                    OriginalSql = sql
                };
            }
        }

        /// <summary>
        /// 仅验证 SQL 语法，不执行格式化。
        /// </summary>
        /// <param name="sql">待校验的 SQL 文本</param>
        /// <param name="errors">校验失败时输出的错误消息列表</param>
        /// <returns>语法合法返回 true，否则返回 false</returns>
        public bool ValidateSyntax(string sql, out IList<string> errors)
        {
            var parseErrors = new List<string>();
            var result = _scriptDom.Validate(sql, out var scriptDomErrors);
            foreach (var e in scriptDomErrors)
            {
                parseErrors.Add($"Line {e.Line}, Col {e.Column}: {e.Message}");
            }
            errors = parseErrors;
            return result;
        }

        /// <summary>
        /// 获取当前样式
        /// </summary>
        public SqlFormatStyle CurrentStyle => _style;

        /// <summary>
        /// 预重构：在主格式化之前执行结构级 SQL 变换（隐式 JOIN 转显式）。
        /// </summary>
        /// <param name="sql">豁免预处理后的 SQL 文本</param>
        /// <returns>重构后的 SQL 文本</returns>
        private string ApplyPreRefactoring(string sql)
        {
            var d = _style.Dml;

            // 隐式 JOIN 转显式 INNER JOIN
            if (d.ConvertImplicitJoin)
            {
                try
                {
                    sql = _joinConverter.ConvertImplicitJoins(sql);
                }
                catch
                {
                    // 转换失败时保持原文，不影响后续格式化
                }
            }

            return sql;
        }

        /// <summary>
        /// 后处理：应用 Poor Man's 不直接支持的规则
        /// （行尾空格清理、连续空格合并、多余空行移除、函数名/数据类型大小写、方括号标准化、
        /// 架构前缀、全局变量大写、临时表/表变量名称格式化、运算符/VALUES/SET/AS/别名/注释对齐、块注释格式化）。
        /// </summary>
        /// <param name="sql">主格式化后的 SQL 文本</param>
        /// <returns>后处理完成的 SQL 文本</returns>
        private string ApplyPostProcessing(string sql)
        {
            var g = _style.Global;
            var t = _style.Tsql;

            // ── 基础清理 ──

            // 去除行尾空格
            if (g.TrimTrailingSpaces)
            {
                sql = TrimTrailingWhitespace(sql);
            }

            // 合并连续空格
            if (g.MergeMultipleSpaces)
            {
                sql = MergeConsecutiveSpaces(sql);
            }

            // 清理多余空行
            if (g.RemoveExtraBlankLines)
            {
                sql = RemoveExcessBlankLines(sql);
            }

            // ── 大小写转换 ──

            // 函数名/数据类型大小写（基于 ScriptDom token 流，Upper 是 PoorMans 默认无需处理）
            if (g.FunctionCase != KeywordCase.Upper || g.DataTypeCase != KeywordCase.Upper)
            {
                sql = SafeRefactor(() => _casePostProcessor.Process(sql, g.FunctionCase, g.DataTypeCase), sql);
            }

            // ── 重构操作 ──

            // 方括号标准化（基于 ScriptDom AST，解析失败时返回原文）
            if (g.SquareBracketMode == BracketMode.AutoAdd)
            {
                sql = SafeRefactor(() => _bracketNormalizer.AddBrackets(sql), sql);
            }
            else if (g.SquareBracketMode == BracketMode.AutoRemove)
            {
                sql = SafeRefactor(() => _bracketNormalizer.RemoveBrackets(sql), sql);
            }

            // dbo 架构前缀处理（基于 ScriptDom AST，解析失败时返回原文）
            if (t.AutoAddDboSchema)
            {
                sql = SafeRefactor(() => _schemaPrefix.AddDboPrefix(sql), sql);
            }
            else if (t.AutoRemoveDboSchema)
            {
                sql = SafeRefactor(() => _schemaPrefix.RemoveDboPrefix(sql), sql);
            }

            // ── T-SQL 专属规则 ──

            // 全局变量大写（@@SPID, @@ROWCOUNT 等）
            if (t.GlobalVariableFormat)
            {
                sql = UppercaseGlobalVariables(sql);
            }

            // 临时表名称格式化（#temp → #Temp）
            if (t.TempTableFormat)
            {
                sql = SafeRefactor(() => _nameFormatter.FormatTempTableNames(sql), sql);
            }

            // 表变量名称格式化（@tablevar → @Tablevar）
            if (t.TableVariableFormat)
            {
                sql = SafeRefactor(() => _nameFormatter.FormatTableVariableNames(sql), sql);
            }

            // ── 纵向对齐 ──

            // 对齐后处理器统一执行所有对齐操作（运算符/VALUES/SET/AS/别名/注释/块注释）
            sql = SafeRefactor(() => _alignmentProcessor.Process(sql, _style), sql);

            return sql;
        }

        /// <summary>
        /// 安全执行重构操作：捕获异常并返回回退值，确保格式化管道不会因子工具失败而中断。
        /// </summary>
        /// <param name="action">重构操作委托</param>
        /// <param name="fallback">异常时的回退值</param>
        /// <returns>重构结果或回退值</returns>
        private static string SafeRefactor(Func<string> action, string fallback)
        {
            try
            {
                return action();
            }
            catch
            {
                return fallback;
            }
        }

        /// <summary>
        /// 将 SQL 中的全局变量（@@开头的系统变量）名称转换为大写。
        /// 仅处理 @@ 后紧跟字母的标识符，不影响字符串字面量和注释内的内容。
        /// </summary>
        /// <param name="sql">待处理的 SQL 文本</param>
        /// <returns>全局变量大写后的 SQL 文本</returns>
        private static string UppercaseGlobalVariables(string sql)
        {
            // 匹配 @@ 后紧跟字母/数字/下划线的标识符
            // 不匹配 @（单@为局部变量）
            return Regex.Replace(sql, @"@@([a-zA-Z_]\w*)",
                m => "@@" + m.Groups[1].Value.ToUpperInvariant());
        }

        /// <summary>确保文件末尾有且仅有一个换行符。</summary>
        /// <param name="sql">待清理的 SQL 文本</param>
        /// <returns>末尾包含换行符的 SQL 文本</returns>
        private string FinalCleanup(string sql)
        {
            // 确保文件末尾有换行
            if (!sql.EndsWith(Environment.NewLine) && !sql.EndsWith("\n"))
                sql += Environment.NewLine;
            return sql;
        }

        /// <summary>移除每行行尾的空白字符（空格和制表符）。</summary>
        /// <param name="sql">待处理的 SQL 文本</param>
        /// <returns>行尾无空白的 SQL 文本</returns>
        private static string TrimTrailingWhitespace(string sql)
        {
            var lines = sql.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            for (int i = 0; i < lines.Length; i++)
                lines[i] = lines[i].TrimEnd();
            return string.Join(Environment.NewLine, lines);
        }

        /// <summary>合并非缩进位置的连续空格为单个空格（保留行首缩进）。</summary>
        /// <param name="sql">待处理的 SQL 文本</param>
        /// <returns>合并后的 SQL 文本</returns>
        private static string MergeConsecutiveSpaces(string sql)
        {
            // 仅合并非缩进位置的连续空格（保留行首缩进）
            var lines = sql.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                int indent = 0;
                while (indent < line.Length && (line[indent] == ' ' || line[indent] == '\t'))
                    indent++;

                if (indent < line.Length)
                {
                    var content = Regex.Replace(line.Substring(indent), @"  +", " ");
                    lines[i] = line.Substring(0, indent) + content;
                }
            }
            return string.Join(Environment.NewLine, lines);
        }

        /// <summary>将连续 3 行及以上的空行压缩为最多 1 个空行。</summary>
        /// <param name="sql">待处理的 SQL 文本</param>
        /// <returns>压缩空行后的 SQL 文本</returns>
        private static string RemoveExcessBlankLines(string sql)
        {
            return Regex.Replace(sql, @"(\r?\n){3,}", Environment.NewLine + Environment.NewLine);
        }
    }

    /// <summary>
    /// 格式化结果：包含格式化后的 SQL、原始 SQL、执行状态和错误信息。
    /// </summary>
    public class FormatResult
    {
        /// <summary>格式化后的 SQL 文本</summary>
        public string FormattedSql { get; set; } = string.Empty;

        /// <summary>原始 SQL 文本（用于对比是否发生变化）</summary>
        public string OriginalSql { get; set; } = string.Empty;

        /// <summary>格式化是否成功</summary>
        public bool Success { get; set; }

        /// <summary>失败时的错误信息；成功时为 null</summary>
        public string? ErrorMessage { get; set; }
    }
}
