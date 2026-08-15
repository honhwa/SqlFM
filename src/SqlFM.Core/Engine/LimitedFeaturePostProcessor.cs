using System;
using System.Collections.Generic;
using System.Text;
using SqlFM.Core.Configuration;

namespace SqlFM.Core.Engine
{
    /// <summary>
    /// 受限功能后处理器：补齐 Poor Man's T-SQL Formatter 不直接支持的格式化选项。
    /// 各项功能均由其对应的配置开关独立门控，且默认关闭，因此接入后不改变现有默认行为（无回归）。
    /// 当前实现两项确定性强、低风险的功能：
    /// 1) Dml.LogicOperatorBefore —— 将行尾的 AND / OR 逻辑运算符前置到下一非空行的行首；
    /// 2) Flow.IfElseBlankSplit —— 在 IF 块的 END 与 ELSE 之间插入一个空行。
    /// 对齐类受限项（OrderBySortAlign / ThenValueAlign / ConstraintAlign / OnConditionIndent / TriggerInsertedIndent 等）
    /// 因需跨行精确对齐、复杂度较高，留作后续迭代。
    /// </summary>
    public class LimitedFeaturePostProcessor
    {
        /// <summary>
        /// 应用受限功能后处理（按样式配置逐项门控）。
        /// </summary>
        /// <param name="sql">主格式化后的 SQL 文本</param>
        /// <param name="style">格式化样式</param>
        /// <returns>后处理后的 SQL 文本</returns>
        public string Process(string sql, SqlFormatStyle style)
        {
            if (style.Dml.LogicOperatorBefore)
                sql = MoveLogicOperatorBefore(sql);

            if (style.Flow.IfElseBlankSplit)
                sql = InsertIfElseBlankLine(sql);

            return sql;
        }

        // ── Dml.LogicOperatorBefore ───────────────────────────────

        private static string MoveLogicOperatorBefore(string sql)
        {
            var lines = sql.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            var result = new List<string>(lines.Length);

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                if (!TryFindTrailingLogicOperator(line, out string op, out int opStart))
                {
                    result.Add(line);
                    continue;
                }

                // 去掉行尾运算符，保留前面的内容
                string before = line.Substring(0, opStart).TrimEnd();
                if (string.IsNullOrEmpty(before))
                    before = string.Empty;

                // 找到下一个非空行
                int next = i + 1;
                while (next < lines.Length && string.IsNullOrWhiteSpace(lines[next]))
                    next++;

                if (next < lines.Length)
                {
                    string nextLine = lines[next];
                    int indent = LeadingWhitespace(nextLine);
                    string indentedOp = (indent > 0 ? nextLine.Substring(0, indent) : string.Empty) + op + " ";
                    lines[next] = indentedOp + nextLine.TrimStart();
                    result.Add(before);
                    i = next; // 下一行已处理，跳过
                }
                else
                {
                    result.Add(before);
                }
            }

            return string.Join("\n", result);
        }

        /// <summary>
        /// 检测行尾是否为独立的 AND / OR 关键字（位于字符串字面量之外）。
        /// </summary>
        private static bool TryFindTrailingLogicOperator(string line, out string op, out int opStart)
        {
            op = string.Empty;
            opStart = -1;

            // 从行尾向前定位运算符（跳过尾部空白）
            int end = line.Length - 1;
            while (end >= 0 && char.IsWhiteSpace(line[end]))
                end--;
            if (end < 2)
                return false;

            // 可能是 " AND" 或 " OR"
            if (end >= 3 && string.Equals(line.Substring(end - 3, 4), " AND", StringComparison.OrdinalIgnoreCase))
            {
                op = "AND";
                opStart = end - 3;
            }
            else if (end >= 2 && string.Equals(line.Substring(end - 2, 3), " OR", StringComparison.OrdinalIgnoreCase))
            {
                op = "OR";
                opStart = end - 2;
            }
            else
            {
                return false;
            }

            // 运算符前必须是空白（不是更长标识符的一部分）
            if (opStart > 0 && !char.IsWhiteSpace(line[opStart - 1]))
                return false;

            // 运算符必须位于字符串字面量之外
            if (IsInsideString(line, opStart))
                return false;

            return true;
        }

        // ── Flow.IfElseBlankSplit ─────────────────────────────────

        private static string InsertIfElseBlankLine(string sql)
        {
            var lines = sql.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            var result = new List<string>(lines.Length + 1);

            for (int i = 0; i < lines.Length; i++)
            {
                result.Add(lines[i]);

                string cur = lines[i].TrimEnd().TrimEnd(';').TrimEnd();
                if (!EndsWithKeywordOutsideString(cur, "END"))
                    continue;
                if (i + 1 >= lines.Length)
                    continue;

                string nextTrim = lines[i + 1].TrimStart();
                if (StartsWithKeywordOutsideString(nextTrim, "ELSE"))
                    result.Add(string.Empty);
            }

            return string.Join("\n", result);
        }

        // ── 字符串字面量感知辅助 ──────────────────────────────────

        /// <summary>判断位置 index 是否位于 ' 或 " 字符串字面量内（支持 '' / "" 转义）。</summary>
        private static bool IsInsideString(string line, int index)
        {
            bool inSingle = false;
            bool inDouble = false;

            for (int k = 0; k < index && k < line.Length; k++)
            {
                char c = line[k];
                if (inSingle)
                {
                    if (c == '\'' )
                    {
                        // 连续两个单引号是转义，否则结束
                        if (k + 1 < line.Length && line[k + 1] == '\'')
                        {
                            k++; // 跳过转义引号
                            continue;
                        }
                        inSingle = false;
                    }
                }
                else if (inDouble)
                {
                    if (c == '"')
                    {
                        if (k + 1 < line.Length && line[k + 1] == '"')
                        {
                            k++;
                            continue;
                        }
                        inDouble = false;
                    }
                }
                else
                {
                    if (c == '\'')
                        inSingle = true;
                    else if (c == '"')
                        inDouble = true;
                }
            }

            return inSingle || inDouble;
        }

        private static bool EndsWithKeywordOutsideString(string text, string keyword)
        {
            if (text.Length < keyword.Length)
                return false;
            string tail = text.Substring(text.Length - keyword.Length);
            if (!string.Equals(tail, keyword, StringComparison.OrdinalIgnoreCase))
                return false;
            if (text.Length == keyword.Length)
                return true;
            if (!char.IsWhiteSpace(text[text.Length - keyword.Length - 1]))
                return false;
            return !IsInsideString(text, text.Length - 1);
        }

        private static bool StartsWithKeywordOutsideString(string text, string keyword)
        {
            if (text.Length < keyword.Length)
                return false;
            string head = text.Substring(0, keyword.Length);
            if (!string.Equals(head, keyword, StringComparison.OrdinalIgnoreCase))
                return false;
            if (text.Length == keyword.Length)
                return true;
            return char.IsWhiteSpace(text[keyword.Length]);
        }

        private static int LeadingWhitespace(string line)
        {
            int n = 0;
            while (n < line.Length && (line[n] == ' ' || line[n] == '\t'))
                n++;
            return n;
        }
    }
}
