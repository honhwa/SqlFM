using System;

namespace SqlFM.Core.Lint
{
    /// <summary>
    /// Lint 修复操作类型，借鉴 sqlfluff 的 LintFix 三种操作模式。
    /// Replace 替换现有文本，InsertBefore/InsertAfter 在指定位置前后插入，Delete 删除指定内容。
    /// </summary>
    public enum LintFixType
    {
        /// <summary>替换：将原文本替换为新文本</summary>
        Replace,

        /// <summary>在指定位置之前插入文本</summary>
        InsertBefore,

        /// <summary>在指定位置之后插入文本</summary>
        InsertAfter,

        /// <summary>删除指定范围的文本</summary>
        Delete
    }

    /// <summary>
    /// Lint 修复操作，描述对 SQL 文本的具体修改建议。
    /// 借鉴 sqlfluff 的 "parse-shaped replacements" 概念，修复基于 AST 结构而非纯文本替换。
    /// </summary>
    public class LintFix
    {
        /// <summary>修复操作类型</summary>
        public LintFixType Type { get; set; }

        /// <summary>修复目标位置行号（从 1 开始）</summary>
        public int Line { get; set; }

        /// <summary>修复目标位置列号（从 1 开始）</summary>
        public int Column { get; set; }

        /// <summary>被替换/删除的原始文本</summary>
        public string OriginalText { get; set; } = string.Empty;

        /// <summary>替换后的新文本（Replace 类型时使用）</summary>
        public string ReplacementText { get; set; } = string.Empty;

        /// <summary>插入的文本（InsertBefore/InsertAfter 类型时使用）</summary>
        public string InsertText { get; set; } = string.Empty;

        /// <summary>创建替换修复</summary>
        public static LintFix ReplaceAt(int line, int column, string original, string replacement)
        {
            return new LintFix
            {
                Type = LintFixType.Replace,
                Line = line,
                Column = column,
                OriginalText = original,
                ReplacementText = replacement
            };
        }

        /// <summary>创建前插入修复</summary>
        public static LintFix InsertBeforeAt(int line, int column, string text)
        {
            return new LintFix
            {
                Type = LintFixType.InsertBefore,
                Line = line,
                Column = column,
                InsertText = text
            };
        }

        /// <summary>创建后插入修复</summary>
        public static LintFix InsertAfterAt(int line, int column, string text)
        {
            return new LintFix
            {
                Type = LintFixType.InsertAfter,
                Line = line,
                Column = column,
                InsertText = text
            };
        }

        /// <summary>创建删除修复</summary>
        public static LintFix DeleteAt(int line, int column, string original)
        {
            return new LintFix
            {
                Type = LintFixType.Delete,
                Line = line,
                Column = column,
                OriginalText = original
            };
        }

        /// <summary>应用修复到源文本，返回修复后的完整文本</summary>
        public string Apply(string source)
        {
            var lines = source.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            if (Line < 1 || Line > lines.Length)
                return source;

            int lineIdx = Line - 1;
            string lineText = lines[lineIdx];

            switch (Type)
            {
                case LintFixType.Replace:
                    lines[lineIdx] = ReplaceAtColumn(lineText, Column, OriginalText, ReplacementText);
                    break;
                case LintFixType.InsertBefore:
                    lines[lineIdx] = InsertAtColumn(lineText, Column, InsertText);
                    break;
                case LintFixType.InsertAfter:
                    int afterPos = Column - 1 + OriginalText.Length;
                    lines[lineIdx] = InsertAtColumn(lineText, afterPos + 1, InsertText);
                    break;
                case LintFixType.Delete:
                    lines[lineIdx] = DeleteAtColumn(lineText, Column, OriginalText.Length);
                    break;
            }

            return string.Join(Environment.NewLine, lines);
        }

        private static string ReplaceAtColumn(string line, int col, string original, string replacement)
        {
            int start = col - 1;
            if (start < 0 || start + original.Length > line.Length)
                return line;
            return line.Substring(0, start) + replacement + line.Substring(start + original.Length);
        }

        private static string InsertAtColumn(string line, int col, string text)
        {
            int pos = col - 1;
            if (pos < 0) pos = 0;
            if (pos > line.Length) pos = line.Length;
            return line.Substring(0, pos) + text + line.Substring(pos);
        }

        private static string DeleteAtColumn(string line, int col, int length)
        {
            int start = col - 1;
            if (start < 0 || start + length > line.Length)
                return line;
            return line.Substring(0, start) + line.Substring(start + length);
        }
    }
}
