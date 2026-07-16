using System;
using System.Collections.Generic;
using SqlFM.Core.Configuration;
using SqlFM.Core.Dialects;

namespace SqlFM.Core.Lint.Rules.Capitalisation
{
    /// <summary>
    /// CP01 — capitalisation.keywords 规则：检查 SQL 关键字大小写一致性。
    /// 借鉴 sqlfluff 的 CP01：关键字大小写应与配置一致（Upper/Lower/Consistent）。
    /// 可自动修复：将关键字转换为配置指定的大小写。
    /// </summary>
    public class CP01_KeywordCaseRule : SqlRuleBase
    {
        /// <inheritdoc/>
        public override string RuleId => "CP01";

        /// <inheritdoc/>
        public override string RuleName => "capitalisation.keywords";

        /// <inheritdoc/>
        public override string Description => "关键字大小写不一致";

        /// <inheritdoc/>
        public override string[] Groups => new[] { "all", "core", "capitalisation" };

        /// <inheritdoc/>
        public override string[] ConfigKeywords => new[] { "Global.KeywordCase" };

        /// <inheritdoc/>
        public override List<LintResult> Evaluate(RuleContext context)
        {
            var results = new List<LintResult>();
            var expectedCase = context.Style.Global.KeywordCase;
            var keywords = context.Dialect.AllKeywords;
            var lines = context.Lines;

            for (int i = 0; i < lines.Length; i++)
            {
                int lineNum = i + 1 + context.LineOffset;
                if (IsExempted(lineNum, context.ExemptedRegions))
                    continue;

                // 匹配 SQL 标识符（连续字母数字下划线）
                var tokens = SplitTokens(lines[i]);
                foreach (var token in tokens)
                {
                    string upper = token.Text.ToUpperInvariant();
                    if (!keywords.Contains(upper))
                        continue;

                    string expected = GetExpectedCase(token.Text, expectedCase);
                    if (token.Text != expected)
                    {
                        results.Add(LintResult.CreateWithFix(
                            lineNum, token.Column,
                            RuleId, $"关键字 '{token.Text}' 大小写不一致（应为 '{expected}'）",
                            RuleSeverity.Warning,
                            new List<LintFix> { LintFix.ReplaceAt(lineNum, token.Column, token.Text, expected) }
                        ));
                    }
                }
            }

            return results;
        }

        private static string GetExpectedCase(string word, KeywordCase caseStyle)
        {
            switch (caseStyle)
            {
                case KeywordCase.Upper:
                    return word.ToUpperInvariant();
                case KeywordCase.Lower:
                    return word.ToLowerInvariant();
                case KeywordCase.Pascal:
                    return word.Substring(0, 1).ToUpperInvariant() + word.Substring(1).ToLowerInvariant();
                default:
                    return word;
            }
        }

        /// <summary>拆分行中的 SQL 标识符 token</summary>
        private static List<TokenInfo> SplitTokens(string line)
        {
            var tokens = new List<TokenInfo>();
            int i = 0;

            while (i < line.Length)
            {
                if (char.IsLetterOrDigit(line[i]) || line[i] == '_')
                {
                    int start = i;
                    while (i < line.Length && (char.IsLetterOrDigit(line[i]) || line[i] == '_'))
                        i++;
                    tokens.Add(new TokenInfo { Text = line.Substring(start, i - start), Column = start + 1 });
                }
                else
                {
                    i++;
                }
            }

            return tokens;
        }

        private class TokenInfo
        {
            public string Text = string.Empty;
            public int Column;
        }
    }

    /// <summary>
    /// CP03 — capitalisation.functions 规则：检查 SQL 函数名大小写一致性。
    /// 借鉴 sqlfluff 的 CP03：函数名大小写应与配置一致。
    /// 可自动修复：将函数名转换为配置指定的大小写。
    /// </summary>
    public class CP03_FunctionCaseRule : SqlRuleBase
    {
        /// <inheritdoc/>
        public override string RuleId => "CP03";

        /// <inheritdoc/>
        public override string RuleName => "capitalisation.functions";

        /// <inheritdoc/>
        public override string Description => "函数名大小写不一致";

        /// <inheritdoc/>
        public override string[] Groups => new[] { "all", "core", "capitalisation" };

        /// <inheritdoc/>
        public override string[] ConfigKeywords => new[] { "Global.FunctionCase" };

        /// <inheritdoc/>
        public override List<LintResult> Evaluate(RuleContext context)
        {
            var results = new List<LintResult>();
            var expectedCase = context.Style.Global.FunctionCase;
            var functions = context.Dialect.BuiltInFunctions;
            var lines = context.Lines;

            for (int i = 0; i < lines.Length; i++)
            {
                int lineNum = i + 1 + context.LineOffset;
                if (IsExempted(lineNum, context.ExemptedRegions))
                    continue;

                // 匹配函数调用：标识符后面紧跟括号
                for (int j = 0; j < lines[i].Length; j++)
                {
                    if (!char.IsLetter(lines[i][j])) continue;

                    int nameStart = j;
                    while (j < lines[i].Length && (char.IsLetterOrDigit(lines[i][j]) || lines[i][j] == '_'))
                        j++;

                    string name = lines[i].Substring(nameStart, j - nameStart);
                    string upperName = name.ToUpperInvariant();

                    // 函数名后必须紧跟括号才判定为函数调用
                    if (j < lines[i].Length && lines[i][j] == '(' && functions.Contains(upperName))
                    {
                        string expected = GetExpectedCase(name, expectedCase);
                        if (name != expected)
                        {
                            results.Add(LintResult.CreateWithFix(
                                lineNum, nameStart + 1,
                                RuleId, $"函数名 '{name}' 大小写不一致（应为 '{expected}'）",
                                RuleSeverity.Warning,
                                new List<LintFix> { LintFix.ReplaceAt(lineNum, nameStart + 1, name, expected) }
                            ));
                        }
                    }
                }
            }

            return results;
        }

        private static string GetExpectedCase(string word, KeywordCase caseStyle)
        {
            switch (caseStyle)
            {
                case KeywordCase.Upper:
                    return word.ToUpperInvariant();
                case KeywordCase.Lower:
                    return word.ToLowerInvariant();
                case KeywordCase.Pascal:
                    return word.Substring(0, 1).ToUpperInvariant() + word.Substring(1).ToLowerInvariant();
                default:
                    return word;
            }
        }
    }

    /// <summary>
    /// CP05 — capitalisation.types 规则：检查数据类型大小写一致性。
    /// 借鉴 sqlfluff 的 CP05：数据类型大小写应与配置一致。
    /// 可自动修复。
    /// </summary>
    public class CP05_DataTypeCaseRule : SqlRuleBase
    {
        /// <inheritdoc/>
        public override string RuleId => "CP05";

        /// <inheritdoc/>
        public override string RuleName => "capitalisation.types";

        /// <inheritdoc/>
        public override string Description => "数据类型大小写不一致";

        /// <inheritdoc/>
        public override string[] Groups => new[] { "all", "core", "capitalisation" };

        /// <inheritdoc/>
        public override string[] ConfigKeywords => new[] { "Global.DataTypeCase" };

        /// <inheritdoc/>
        public override List<LintResult> Evaluate(RuleContext context)
        {
            var results = new List<LintResult>();
            var expectedCase = context.Style.Global.DataTypeCase;
            var dataTypes = context.Dialect.DataTypes;
            var lines = context.Lines;

            for (int i = 0; i < lines.Length; i++)
            {
                int lineNum = i + 1 + context.LineOffset;
                if (IsExempted(lineNum, context.ExemptedRegions))
                    continue;

                var tokens = SplitTokens(lines[i]);
                foreach (var token in tokens)
                {
                    string upper = token.Text.ToUpperInvariant();
                    if (!dataTypes.Contains(upper))
                        continue;

                    string expected = GetExpectedCase(token.Text, expectedCase);
                    if (token.Text != expected)
                    {
                        results.Add(LintResult.CreateWithFix(
                            lineNum, token.Column,
                            RuleId, $"数据类型 '{token.Text}' 大小写不一致（应为 '{expected}'）",
                            RuleSeverity.Warning,
                            new List<LintFix> { LintFix.ReplaceAt(lineNum, token.Column, token.Text, expected) }
                        ));
                    }
                }
            }

            return results;
        }

        private static string GetExpectedCase(string word, KeywordCase caseStyle)
        {
            switch (caseStyle)
            {
                case KeywordCase.Upper:
                    return word.ToUpperInvariant();
                case KeywordCase.Lower:
                    return word.ToLowerInvariant();
                case KeywordCase.Pascal:
                    return word.Substring(0, 1).ToUpperInvariant() + word.Substring(1).ToLowerInvariant();
                default:
                    return word;
            }
        }

        private static List<TokenInfo> SplitTokens(string line)
        {
            var tokens = new List<TokenInfo>();
            int i = 0;
            while (i < line.Length)
            {
                if (char.IsLetterOrDigit(line[i]) || line[i] == '_')
                {
                    int start = i;
                    while (i < line.Length && (char.IsLetterOrDigit(line[i]) || line[i] == '_'))
                        i++;
                    tokens.Add(new TokenInfo { Text = line.Substring(start, i - start), Column = start + 1 });
                }
                else i++;
            }
            return tokens;
        }

        private class TokenInfo { public string Text = string.Empty; public int Column; }
    }
}
