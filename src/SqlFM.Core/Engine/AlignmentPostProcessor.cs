using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using SqlFM.Core.Configuration;

namespace SqlFM.Core.Engine
{
    /// <summary>
    /// 对齐后处理器：在主格式化后对多行结构进行纵向对齐操作。
    /// 支持比较运算符对齐、VALUES 列对齐、SET 等号对齐、AS 关键字处理、
    /// 列别名对齐、行内注释对齐和块注释格式化。
    /// 所有方法均为幂等操作，对已对齐的文本不会产生变化。
    /// </summary>
    public class AlignmentPostProcessor
    {
        /// <summary>比较运算符列表（两字符优先，确保正确匹配）</summary>
        private static readonly string[] ComparisonOperators =
        {
            "<>", "!=", ">=", "<=", "=", ">", "<"
        };

        /// <summary>SQL 子句关键字（用于识别列块边界）</summary>
        private static readonly HashSet<string> ClauseKeywords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "FROM", "WHERE", "GROUP", "HAVING", "ORDER", "UNION",
            "EXCEPT", "INTERSECT", "JOIN", "INNER", "LEFT", "RIGHT",
            "FULL", "CROSS", "ON", "AND", "OR", "WHEN", "THEN", "ELSE", "END"
        };

        /// <summary>
        /// 根据配置执行所有启用的对齐操作。
        /// 操作顺序：运算符对齐 → VALUES 对齐 → SET 对齐 → AS 处理 → 别名对齐 → 注释对齐 → 块注释格式化。
        /// </summary>
        /// <param name="sql">主格式化后的 SQL 文本</param>
        /// <param name="style">格式化样式配置</param>
        /// <returns>对齐后的 SQL 文本</returns>
        public string Process(string sql, SqlFormatStyle style)
        {
            if (string.IsNullOrEmpty(sql))
                return sql;

            var dml = style.Dml;
            var expr = style.Expression;

            // P1-2: 比较运算符纵向对齐
            if (dml.AlignCompareOperator)
                sql = SafeAlign(() => AlignCompareOperators(sql), sql);

            // P1-3: VALUES 多行列对齐
            if (dml.ValuesRowAlign)
                sql = SafeAlign(() => AlignValuesRows(sql), sql);

            // P1-4: UPDATE SET 等号对齐
            if (dml.UpdateSetAlignEqual)
                sql = SafeAlign(() => AlignSetEquals(sql), sql);

            // P2-5: AS 关键字处理
            if (dml.AsKeywordMode == AsKeywordMode.Remove)
                sql = SafeAlign(() => RemoveAsKeywords(sql), sql);
            else if (dml.AsKeywordMode == AsKeywordMode.Align)
                sql = SafeAlign(() => AlignAsKeywords(sql), sql);

            // P2-6: 列别名纵向对齐
            if (dml.AlignColumnAlias)
                sql = SafeAlign(() => AlignColumnAliases(sql), sql);

            // P2-7: 行内注释纵向对齐
            if (dml.AlignColumnComments)
                sql = SafeAlign(() => AlignInlineComments(sql), sql);

            // P3-10: 块注释格式化
            if (expr.BlockCommentFormat)
                sql = SafeAlign(() => FormatBlockComments(sql), sql);

            return sql;
        }

        // ════════════════════════════════════════════════════════════════════════
        // P1-2: 比较运算符纵向对齐
        // ════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// 对 WHERE/AND/OR 条件块中的比较运算符进行纵向对齐。
        /// 示例：
        ///   WHERE  a = 1        WHERE  a    = 1
        ///     AND bcd &lt;&gt; 2   →     AND bcd &lt;&gt; 2
        ///     AND ef  &gt;= 3        AND ef  &gt;= 3
        /// </summary>
        /// <param name="sql">待处理的 SQL 文本</param>
        /// <returns>运算符对齐后的 SQL 文本</returns>
        private string AlignCompareOperators(string sql)
        {
            var lines = sql.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            var result = new List<string>(lines.Length);

            int i = 0;
            while (i < lines.Length)
            {
                // 查找条件块起始（WHERE/AND/OR 行包含比较运算符）
                if (IsConditionLine(lines[i]) && FindComparisonOperator(lines[i]) >= 0)
                {
                    var block = new List<int> { i };
                    int j = i + 1;

                    // 收集连续的条件行
                    while (j < lines.Length && IsConditionLine(lines[j]) &&
                           FindComparisonOperator(lines[j]) >= 0)
                    {
                        block.Add(j);
                        j++;
                    }

                    // 只有 2 行以上才需要对齐
                    if (block.Count >= 2)
                    {
                        AlignOperatorBlock(lines, block);
                    }

                    foreach (int idx in block)
                        result.Add(lines[idx]);
                    i = j;
                }
                else
                {
                    result.Add(lines[i]);
                    i++;
                }
            }

            return string.Join(Environment.NewLine, result);
        }

        /// <summary>
        /// 判断一行是否为 WHERE/AND/OR 条件行（行首去掉空白后以关键字开头）。
        /// </summary>
        /// <param name="line">待检查的行</param>
        /// <returns>是条件行返回 true</returns>
        private static bool IsConditionLine(string line)
        {
            var trimmed = line.TrimStart();
            if (trimmed.Length == 0) return false;
            return trimmed.StartsWith("WHERE ", StringComparison.OrdinalIgnoreCase) ||
                   trimmed.StartsWith("AND ", StringComparison.OrdinalIgnoreCase) ||
                   trimmed.StartsWith("OR ", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 在一行中查找第一个比较运算符的位置（排除字符串和括号内的运算符）。
        /// </summary>
        /// <param name="line">待搜索的行</param>
        /// <returns>运算符起始索引；未找到返回 -1</returns>
        private static int FindComparisonOperator(string line)
        {
            bool inString = false;
            int parenDepth = 0;

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                if (inString)
                {
                    if (c == '\'') inString = false;
                    continue;
                }

                if (c == '\'') { inString = true; continue; }
                if (c == '(') { parenDepth++; continue; }
                if (c == ')') { if (parenDepth > 0) parenDepth--; continue; }
                if (parenDepth > 0) continue;

                // 跳过 -- 注释后的内容
                if (c == '-' && i + 1 < line.Length && line[i + 1] == '-')
                    break;

                // 优先匹配两字符运算符
                if (i + 1 < line.Length)
                {
                    string two = line.Substring(i, 2);
                    if (two == "<>" || two == "!=" || two == ">=" || two == "<=")
                        return i;
                }

                // 单字符运算符
                if (c == '=' || c == '>' || c == '<')
                    return i;
            }

            return -1;
        }

        /// <summary>
        /// 对一个条件块中的运算符执行对齐。
        /// </summary>
        /// <param name="lines">所有行数组</param>
        /// <param name="blockIndices">条件块行索引列表</param>
        private static void AlignOperatorBlock(string[] lines, List<int> blockIndices)
        {
            // 计算每行 LHS 结束位置（运算符前最后一个非空字符索引）
            int maxLhsEnd = -1;
            var opInfos = new int[blockIndices.Count][];

            for (int k = 0; k < blockIndices.Count; k++)
            {
                int lineIdx = blockIndices[k];
                int opStart = FindComparisonOperator(lines[lineIdx]);
                if (opStart < 0)
                {
                    opInfos[k] = null!;
                    continue;
                }

                // 找运算符前最后一个非空字符
                int lhsEnd = opStart - 1;
                while (lhsEnd >= 0 && char.IsWhiteSpace(lines[lineIdx][lhsEnd]))
                    lhsEnd--;

                opInfos[k] = new[] { lhsEnd, opStart };
                if (lhsEnd > maxLhsEnd)
                    maxLhsEnd = lhsEnd;
            }

            if (maxLhsEnd < 0) return;

            // 对齐：每行运算符起始位置 = maxLhsEnd + 2（1 个空格间隔 + LHS 结束位置偏移）
            int targetOpStart = maxLhsEnd + 2;

            for (int k = 0; k < blockIndices.Count; k++)
            {
                if (opInfos[k] == null) continue;

                int lineIdx = blockIndices[k];
                int lhsEnd = opInfos[k][0];
                int opStart = opInfos[k][1];

                string lhs = lines[lineIdx].Substring(0, lhsEnd + 1);
                string opAndRest = lines[lineIdx].Substring(opStart);
                int padding = targetOpStart - (lhsEnd + 1);

                lines[lineIdx] = lhs + new string(' ', padding) + opAndRest;
            }
        }

        // ════════════════════════════════════════════════════════════════════════
        // P1-3: VALUES 多行列对齐
        // ════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// 对多行 VALUES 子句中的列进行纵向对齐。
        /// 示例：
        ///   VALUES (1, 'abc'),    VALUES (1,  'abc'),
        ///          (22, 'def') →         (22, 'def')
        /// </summary>
        /// <param name="sql">待处理的 SQL 文本</param>
        /// <returns>列对齐后的 SQL 文本</returns>
        private string AlignValuesRows(string sql)
        {
            var lines = sql.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            var result = new List<string>(lines.Length);

            int i = 0;
            while (i < lines.Length)
            {
                // 查找 VALUES 行
                if (Regex.IsMatch(lines[i].TrimStart(), @"^VALUES\s*\(", RegexOptions.IgnoreCase))
                {
                    // 收集 VALUES 行及后续连续的值行（以 ( 开头或包含 ( 的行）
                    var blockLines = new List<int>();
                    int j = i;

                    // VALUES 行本身
                    blockLines.Add(j);
                    j++;

                    // 后续值行（以 ( 开头的行，跳过空行）
                    while (j < lines.Length)
                    {
                        var trimmed = lines[j].TrimStart();
                        if (trimmed.Length == 0)
                        {
                            j++;
                            continue;
                        }
                        if (trimmed.StartsWith("("))
                        {
                            blockLines.Add(j);
                            j++;
                        }
                        else
                        {
                            break;
                        }
                    }

                    if (blockLines.Count >= 2)
                    {
                        AlignValuesBlock(lines, blockLines);
                    }

                    foreach (int idx in blockLines)
                        result.Add(lines[idx]);
                    // 添加跳过的空行
                    for (int k = blockLines[blockLines.Count - 1] + 1; k < j; k++)
                        result.Add(lines[k]);
                    i = j;
                }
                else
                {
                    result.Add(lines[i]);
                    i++;
                }
            }

            return string.Join(Environment.NewLine, result);
        }

        /// <summary>
        /// 对一个 VALUES 块中的多行值进行列对齐。
        /// </summary>
        /// <param name="lines">所有行数组</param>
        /// <param name="blockIndices">VALUES 块行索引列表</param>
        private static void AlignValuesBlock(string[] lines, List<int> blockIndices)
        {
            // 解析每行的列
            var rowsColumns = new List<List<string>>();
            var rowPrefixes = new List<string>();  // 行中 ( 前的部分
            var rowSuffixes = new List<string>();   // 最后 ) 后的部分

            foreach (int idx in blockIndices)
            {
                string line = lines[idx];
                int openParen = FindCharOutsideString(line, '(');
                if (openParen < 0)
                {
                    rowsColumns.Add(null!);
                    rowPrefixes.Add(line);
                    rowSuffixes.Add("");
                    continue;
                }

                int closeParen = FindMatchingCloseParen(line, openParen);
                if (closeParen < 0)
                {
                    rowsColumns.Add(null!);
                    rowPrefixes.Add(line);
                    rowSuffixes.Add("");
                    continue;
                }

                string prefix = line.Substring(0, openParen + 1);  // 含 (
                string content = line.Substring(openParen + 1, closeParen - openParen - 1);
                string suffix = line.Substring(closeParen);  // 含 ) 及后续

                rowPrefixes.Add(prefix);
                rowSuffixes.Add(suffix);
                rowsColumns.Add(SplitByTopLevelCommas(content));
            }

            // 找到最大列数
            int maxColumns = 0;
            foreach (var cols in rowsColumns)
            {
                if (cols != null && cols.Count > maxColumns)
                    maxColumns = cols.Count;
            }

            if (maxColumns == 0) return;

            // 计算每列最大宽度
            var colWidths = new int[maxColumns];
            for (int c = 0; c < maxColumns; c++)
            {
                for (int r = 0; r < rowsColumns.Count; r++)
                {
                    if (rowsColumns[r] != null && c < rowsColumns[r].Count)
                    {
                        int w = rowsColumns[r][c].Trim().Length;
                        if (w > colWidths[c])
                            colWidths[c] = w;
                    }
                }
            }

            // 重建每行
            for (int r = 0; r < blockIndices.Count; r++)
            {
                if (rowsColumns[r] == null) continue;

                var sb = new StringBuilder();
                sb.Append(rowPrefixes[r]);

                for (int c = 0; c < rowsColumns[r].Count; c++)
                {
                    string col = rowsColumns[r][c].Trim();
                    if (c > 0)
                        sb.Append(",");

                    // 右填充到列宽（左对齐）
                    sb.Append(" ").Append(col);
                    int pad = colWidths[c] - col.Length;
                    if (pad > 0)
                        sb.Append(new string(' ', pad));
                }

                sb.Append(rowSuffixes[r]);
                lines[blockIndices[r]] = sb.ToString();
            }
        }

        // ════════════════════════════════════════════════════════════════════════
        // P1-4: UPDATE SET 等号对齐
        // ════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// 对 UPDATE SET 子句中的赋值等号进行纵向对齐。
        /// 示例：
        ///   SET col1 = val1,     SET col1   = val1,
        ///       col22 = val2 →       col22  = val2
        /// </summary>
        /// <param name="sql">待处理的 SQL 文本</param>
        /// <returns>等号对齐后的 SQL 文本</returns>
        private string AlignSetEquals(string sql)
        {
            var lines = sql.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            var result = new List<string>(lines.Length);

            int i = 0;
            while (i < lines.Length)
            {
                // 查找 SET 行（UPDATE 语句中的 SET 子句）
                if (Regex.IsMatch(lines[i].TrimStart(), @"^SET\s+", RegexOptions.IgnoreCase))
                {
                    var blockIndices = new List<int>();

                    // SET 行本身可能包含赋值
                    if (ContainsAssignmentEquals(lines[i]))
                        blockIndices.Add(i);

                    // 收集后续连续的赋值行
                    int j = i + 1;
                    while (j < lines.Length)
                    {
                        var trimmed = lines[j].TrimStart();
                        if (trimmed.Length == 0) break;

                        // 以子句关键字结束
                        string firstWord = GetFirstWord(trimmed);
                        if (ClauseKeywords.Contains(firstWord))
                            break;

                        if (ContainsAssignmentEquals(lines[j]))
                            blockIndices.Add(j);
                        else
                            break;

                        j++;
                    }

                    if (blockIndices.Count >= 2)
                    {
                        AlignSetBlock(lines, blockIndices);
                    }

                    // 输出 SET 行到最后一个赋值行
                    for (int k = i; k < j; k++)
                        result.Add(lines[k]);
                    i = j;
                }
                else
                {
                    result.Add(lines[i]);
                    i++;
                }
            }

            return string.Join(Environment.NewLine, result);
        }

        /// <summary>
        /// 判断一行是否包含赋值等号（非比较运算符的 =）。
        /// 在 SET 上下文中，等号用于赋值，且左侧通常是标识符。
        /// </summary>
        /// <param name="line">待检查的行</param>
        /// <returns>包含赋值等号返回 true</returns>
        private static bool ContainsAssignmentEquals(string line)
        {
            // 在 SET 上下文中查找 = 号（排除字符串内和括号内）
            bool inString = false;
            int parenDepth = 0;

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                if (inString)
                {
                    if (c == '\'') inString = false;
                    continue;
                }
                if (c == '\'') { inString = true; continue; }
                if (c == '(') { parenDepth++; continue; }
                if (c == ')') { if (parenDepth > 0) parenDepth--; continue; }
                if (parenDepth > 0) continue;

                // 跳过 >= <= <> !=
                if (c == '=' && i > 0)
                {
                    char prev = line[i - 1];
                    if (prev == '>' || prev == '<' || prev == '!')
                        continue;
                    if (i + 1 < line.Length && line[i + 1] == '>')
                        continue;
                    return true;
                }
                if (c == '=')
                    return true;
            }

            return false;
        }

        /// <summary>
        /// 对 SET 块中的等号执行对齐。
        /// </summary>
        /// <param name="lines">所有行数组</param>
        /// <param name="blockIndices">SET 块行索引列表</param>
        private static void AlignSetBlock(string[] lines, List<int> blockIndices)
        {
            int maxLhsEnd = -1;
            var eqInfos = new int[blockIndices.Count][];

            for (int k = 0; k < blockIndices.Count; k++)
            {
                int lineIdx = blockIndices[k];
                int eqPos = FindAssignmentEquals(lines[lineIdx]);
                if (eqPos < 0)
                {
                    eqInfos[k] = null!;
                    continue;
                }

                int lhsEnd = eqPos - 1;
                while (lhsEnd >= 0 && char.IsWhiteSpace(lines[lineIdx][lhsEnd]))
                    lhsEnd--;

                eqInfos[k] = new[] { lhsEnd, eqPos };
                if (lhsEnd > maxLhsEnd)
                    maxLhsEnd = lhsEnd;
            }

            if (maxLhsEnd < 0) return;

            int targetEqStart = maxLhsEnd + 2;

            for (int k = 0; k < blockIndices.Count; k++)
            {
                if (eqInfos[k] == null) continue;

                int lineIdx = blockIndices[k];
                int lhsEnd = eqInfos[k][0];
                int eqPos = eqInfos[k][1];

                string lhs = lines[lineIdx].Substring(0, lhsEnd + 1);
                string eqAndRest = lines[lineIdx].Substring(eqPos);
                int padding = targetEqStart - (lhsEnd + 1);

                lines[lineIdx] = lhs + new string(' ', padding) + eqAndRest;
            }
        }

        /// <summary>
        /// 在一行中查找赋值等号的位置（排除比较运算符中的 =）。
        /// </summary>
        /// <param name="line">待搜索的行</param>
        /// <returns>等号索引；未找到返回 -1</returns>
        private static int FindAssignmentEquals(string line)
        {
            bool inString = false;
            int parenDepth = 0;

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                if (inString)
                {
                    if (c == '\'') inString = false;
                    continue;
                }
                if (c == '\'') { inString = true; continue; }
                if (c == '(') { parenDepth++; continue; }
                if (c == ')') { if (parenDepth > 0) parenDepth--; continue; }
                if (parenDepth > 0) continue;

                if (c == '-' && i + 1 < line.Length && line[i + 1] == '-')
                    break;

                if (c == '=')
                {
                    // 排除 >=, <=, <>, !=
                    if (i > 0)
                    {
                        char prev = line[i - 1];
                        if (prev == '>' || prev == '<' || prev == '!')
                            continue;
                    }
                    if (i + 1 < line.Length && line[i + 1] == '>')
                        continue;
                    return i;
                }
            }

            return -1;
        }

        // ════════════════════════════════════════════════════════════════════════
        // P2-5: AS 关键字处理
        // ════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// 移除 SELECT 列别名中的 AS 关键字。
        /// 示例：SELECT col1 AS alias1 → SELECT col1 alias1
        /// </summary>
        /// <param name="sql">待处理的 SQL 文本</param>
        /// <returns>移除 AS 后的 SQL 文本</returns>
        private string RemoveAsKeywords(string sql)
        {
            var lines = sql.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            var result = new List<string>(lines.Length);

            foreach (var line in lines)
            {
                // 在 SELECT 列行中移除 AS（行内非字符串部分的 AS 关键字）
                string processed = TransformOutsideStrings(line, text =>
                {
                    // 匹配 " AS " (前后有空格), 不区分大小写
                    return Regex.Replace(text, @"\s+AS\s+", " ",
                        RegexOptions.IgnoreCase);
                });
                result.Add(processed);
            }

            return string.Join(Environment.NewLine, result);
        }

        /// <summary>
        /// 对齐 SELECT 列表中的 AS 关键字。
        /// 示例：
        ///   SELECT col1 AS a,     SELECT col1   AS a,
        ///          col22 AS b →          col22  AS b
        /// </summary>
        /// <param name="sql">待处理的 SQL 文本</param>
        /// <returns>AS 对齐后的 SQL 文本</returns>
        private string AlignAsKeywords(string sql)
        {
            var lines = sql.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            var result = new List<string>(lines.Length);

            int i = 0;
            while (i < lines.Length)
            {
                // 查找 SELECT 列块
                if (Regex.IsMatch(lines[i].TrimStart(), @"^SELECT\s", RegexOptions.IgnoreCase))
                {
                    var blockIndices = new List<int>();
                    int j = i;

                    // SELECT 行本身（如果含 AS）
                    if (ContainsAsKeyword(lines[j]))
                        blockIndices.Add(j);
                    j++;

                    // 后续列行
                    while (j < lines.Length)
                    {
                        var trimmed = lines[j].TrimStart();
                        if (trimmed.Length == 0) break;

                        string firstWord = GetFirstWord(trimmed);
                        if (ClauseKeywords.Contains(firstWord))
                            break;

                        if (ContainsAsKeyword(lines[j]))
                            blockIndices.Add(j);
                        j++;
                    }

                    if (blockIndices.Count >= 2)
                    {
                        AlignAsBlock(lines, blockIndices);
                    }

                    for (int k = i; k < j; k++)
                        result.Add(lines[k]);
                    i = j;
                }
                else
                {
                    result.Add(lines[i]);
                    i++;
                }
            }

            return string.Join(Environment.NewLine, result);
        }

        /// <summary>
        /// 判断一行是否包含 AS 关键字（非字符串内）。
        /// </summary>
        /// <param name="line">待检查的行</param>
        /// <returns>包含 AS 返回 true</returns>
        private static bool ContainsAsKeyword(string line)
        {
            bool found = false;
            TransformOutsideStrings(line, text =>
            {
                if (Regex.IsMatch(text, @"\bAS\b", RegexOptions.IgnoreCase))
                    found = true;
                return text;
            });
            return found;
        }

        /// <summary>
        /// 对一个 SELECT 列块中的 AS 关键字执行对齐。
        /// </summary>
        /// <param name="lines">所有行数组</param>
        /// <param name="blockIndices">列块行索引列表</param>
        private static void AlignAsBlock(string[] lines, List<int> blockIndices)
        {
            int maxLhsEnd = -1;
            var asInfos = new int[blockIndices.Count][];

            for (int k = 0; k < blockIndices.Count; k++)
            {
                int lineIdx = blockIndices[k];
                int asPos = FindAsKeyword(lines[lineIdx]);
                if (asPos < 0)
                {
                    asInfos[k] = null!;
                    continue;
                }

                int lhsEnd = asPos - 1;
                while (lhsEnd >= 0 && char.IsWhiteSpace(lines[lineIdx][lhsEnd]))
                    lhsEnd--;

                asInfos[k] = new[] { lhsEnd, asPos };
                if (lhsEnd > maxLhsEnd)
                    maxLhsEnd = lhsEnd;
            }

            if (maxLhsEnd < 0) return;

            int targetAsStart = maxLhsEnd + 2;

            for (int k = 0; k < blockIndices.Count; k++)
            {
                if (asInfos[k] == null) continue;

                int lineIdx = blockIndices[k];
                int lhsEnd = asInfos[k][0];
                int asPos = asInfos[k][1];

                string lhs = lines[lineIdx].Substring(0, lhsEnd + 1);
                string asAndRest = lines[lineIdx].Substring(asPos);
                int padding = targetAsStart - (lhsEnd + 1);

                lines[lineIdx] = lhs + new string(' ', padding) + asAndRest;
            }
        }

        /// <summary>
        /// 在一行中查找 AS 关键字的起始位置（非字符串内）。
        /// </summary>
        /// <param name="line">待搜索的行</param>
        /// <returns>AS 起始索引；未找到返回 -1</returns>
        private static int FindAsKeyword(string line)
        {
            int pos = -1;
            TransformOutsideStrings(line, text =>
            {
                var m = Regex.Match(text, @"\bAS\b", RegexOptions.IgnoreCase);
                if (m.Success)
                    pos = m.Index;
                return text;
            });
            return pos;
        }

        // ════════════════════════════════════════════════════════════════════════
        // P2-6: 列别名纵向对齐
        // ════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// 对 SELECT 列表中的列别名进行纵向对齐（AS 后的别名部分）。
        /// 若 AS 已被移除，则对齐列名后的别名。与 AlignAsKeywords 互补使用。
        /// </summary>
        /// <param name="sql">待处理的 SQL 文本</param>
        /// <returns>别名对齐后的 SQL 文本</returns>
        private string AlignColumnAliases(string sql)
        {
            var lines = sql.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            var result = new List<string>(lines.Length);

            int i = 0;
            while (i < lines.Length)
            {
                if (Regex.IsMatch(lines[i].TrimStart(), @"^SELECT\s", RegexOptions.IgnoreCase))
                {
                    var blockIndices = new List<int>();
                    int j = i;

                    // 收集含别名标记的行（AS 关键字或列名后跟别名）
                    if (HasColumnAlias(lines[i]))
                        blockIndices.Add(i);
                    j++;

                    while (j < lines.Length)
                    {
                        var trimmed = lines[j].TrimStart();
                        if (trimmed.Length == 0) break;

                        string firstWord = GetFirstWord(trimmed);
                        if (ClauseKeywords.Contains(firstWord))
                            break;

                        if (HasColumnAlias(lines[j]))
                            blockIndices.Add(j);
                        j++;
                    }

                    if (blockIndices.Count >= 2)
                    {
                        AlignAliasBlock(lines, blockIndices);
                    }

                    for (int k = i; k < j; k++)
                        result.Add(lines[k]);
                    i = j;
                }
                else
                {
                    result.Add(lines[i]);
                    i++;
                }
            }

            return string.Join(Environment.NewLine, result);
        }

        /// <summary>
        /// 判断一行是否包含列别名（AS 关键字或逗号后跟标识符模式）。
        /// </summary>
        /// <param name="line">待检查的行</param>
        /// <returns>包含列别名返回 true</returns>
        private static bool HasColumnAlias(string line)
        {
            return FindAsKeyword(line) >= 0 ||
                   Regex.IsMatch(line.TrimStart(), @"^\w+\s+\w+", RegexOptions.IgnoreCase);
        }

        /// <summary>
        /// 对一个列块中的别名执行对齐。
        /// 对齐目标是 AS 后的别名起始位置（或无 AS 时的列名后别名起始位置）。
        /// </summary>
        /// <param name="lines">所有行数组</param>
        /// <param name="blockIndices">列块行索引列表</param>
        private static void AlignAliasBlock(string[] lines, List<int> blockIndices)
        {
            // 对于有 AS 的行，对齐 AS 后的别名；对齐逻辑同 AlignAsBlock
            // 如果所有行都有 AS，则 AS 对齐后别名自然对齐，无需额外处理
            // 如果部分行无 AS，则需要单独对齐别名起始位置
            bool allHaveAs = true;
            foreach (int idx in blockIndices)
            {
                if (FindAsKeyword(lines[idx]) < 0)
                {
                    allHaveAs = false;
                    break;
                }
            }

            // 如果所有行都有 AS 且 AS 已对齐，别名自然对齐，无需处理
            // 只有在 AS 未对齐或部分无 AS 时才需要处理
            if (allHaveAs)
            {
                AlignAsBlock(lines, blockIndices);
                return;
            }

            // 混合情况：对齐别名起始位置
            int maxPrefixEnd = -1;
            var aliasInfos = new int[blockIndices.Count][];

            for (int k = 0; k < blockIndices.Count; k++)
            {
                int lineIdx = blockIndices[k];
                int aliasStart = FindAliasStart(lines[lineIdx]);
                if (aliasStart < 0)
                {
                    aliasInfos[k] = null!;
                    continue;
                }

                int prefixEnd = aliasStart - 1;
                while (prefixEnd >= 0 && char.IsWhiteSpace(lines[lineIdx][prefixEnd]))
                    prefixEnd--;

                aliasInfos[k] = new[] { prefixEnd, aliasStart };
                if (prefixEnd > maxPrefixEnd)
                    maxPrefixEnd = prefixEnd;
            }

            if (maxPrefixEnd < 0) return;

            int targetStart = maxPrefixEnd + 2;

            for (int k = 0; k < blockIndices.Count; k++)
            {
                if (aliasInfos[k] == null) continue;

                int lineIdx = blockIndices[k];
                int prefixEnd = aliasInfos[k][0];
                int aliasStart = aliasInfos[k][1];

                string prefix = lines[lineIdx].Substring(0, prefixEnd + 1);
                string aliasAndRest = lines[lineIdx].Substring(aliasStart);
                int padding = targetStart - (prefixEnd + 1);

                lines[lineIdx] = prefix + new string(' ', padding) + aliasAndRest;
            }
        }

        /// <summary>
        /// 在一行中查找别名起始位置（AS 后的标识符，或无 AS 时列名后的标识符）。
        /// </summary>
        /// <param name="line">待搜索的行</param>
        /// <returns>别名起始索引；未找到返回 -1</returns>
        private static int FindAliasStart(string line)
        {
            int asPos = FindAsKeyword(line);
            if (asPos >= 0)
            {
                // AS 后的别名起始
                int afterAs = asPos + 2;
                while (afterAs < line.Length && char.IsWhiteSpace(line[afterAs]))
                    afterAs++;
                return afterAs < line.Length ? afterAs : -1;
            }

            // 无 AS：查找列名后的空格分隔的标识符
            var trimmed = line.TrimStart();
            if (trimmed.Length == 0) return -1;

            // 匹配 "identifier whitespace identifier" 模式
            var m = Regex.Match(trimmed, @"^(\w+)\s+(\w+)");
            if (m.Success)
            {
                return line.Length - trimmed.Length + m.Groups[2].Index;
            }

            return -1;
        }

        // ════════════════════════════════════════════════════════════════════════
        // P2-7: 行内注释纵向对齐
        // ════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// 对连续行的行内 -- 注释进行纵向对齐。
        /// 示例：
        ///   SELECT col1, -- comment1     SELECT col1,   -- comment1
        ///          col22 -- comment2 →          col22  -- comment2
        /// </summary>
        /// <param name="sql">待处理的 SQL 文本</param>
        /// <returns>注释对齐后的 SQL 文本</returns>
        private string AlignInlineComments(string sql)
        {
            var lines = sql.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            var result = new List<string>(lines.Length);

            int i = 0;
            while (i < lines.Length)
            {
                int commentPos = FindInlineComment(lines[i]);
                if (commentPos >= 0)
                {
                    var blockIndices = new List<int> { i };
                    int maxPos = commentPos;
                    int j = i + 1;

                    while (j < lines.Length)
                    {
                        int pos = FindInlineComment(lines[j]);
                        if (pos < 0) break;
                        blockIndices.Add(j);
                        if (pos > maxPos)
                            maxPos = pos;
                        j++;
                    }

                    if (blockIndices.Count >= 2)
                    {
                        foreach (int idx in blockIndices)
                        {
                            int pos = FindInlineComment(lines[idx]);
                            if (pos < maxPos)
                            {
                                // 插入空格使注释对齐
                                int padding = maxPos - pos;
                                lines[idx] = lines[idx].Substring(0, pos) +
                                    new string(' ', padding) +
                                    lines[idx].Substring(pos);
                            }
                        }
                    }

                    foreach (int idx in blockIndices)
                        result.Add(lines[idx]);
                    i = j;
                }
                else
                {
                    result.Add(lines[i]);
                    i++;
                }
            }

            return string.Join(Environment.NewLine, result);
        }

        /// <summary>
        /// 在一行中查找行内注释（-- 且不在字符串内）的起始位置。
        /// 返回 -- 前空格的起始位置（用于对齐插入点）。
        /// </summary>
        /// <param name="line">待搜索的行</param>
        /// <returns>注释起始位置；无行内注释返回 -1</returns>
        private static int FindInlineComment(string line)
        {
            bool inString = false;

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                if (inString)
                {
                    if (c == '\'') inString = false;
                    continue;
                }

                if (c == '\'') { inString = true; continue; }

                // 检测 -- 注释
                if (c == '-' && i + 1 < line.Length && line[i + 1] == '-')
                {
                    // 跳过行首注释
                    var before = line.Substring(0, i).TrimEnd();
                    if (before.Length == 0)
                        return -1;

                    // 返回 -- 前的空格起始位置
                    int spaceStart = i;
                    while (spaceStart > 0 && char.IsWhiteSpace(line[spaceStart - 1]))
                        spaceStart--;
                    return spaceStart;
                }
            }

            return -1;
        }

        // ════════════════════════════════════════════════════════════════════════
        // P3-10: 块注释格式化
        // ════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// 格式化块注释（/* */），在每行前添加对齐的星号前缀。
        /// 示例：
        ///   /* This is          /*
        ///    a multi-line    →  * This is
        ///    comment */          * a multi-line
        ///                        * comment
        ///                        */
        /// </summary>
        /// <param name="sql">待处理的 SQL 文本</param>
        /// <returns>块注释格式化后的 SQL 文本</returns>
        private string FormatBlockComments(string sql)
        {
            // 匹配 /* ... */ 块注释
            return Regex.Replace(sql, @"/\*[\s\S]*?\*/", match =>
            {
                string comment = match.Value;
                var lines = comment.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

                if (lines.Length <= 1)
                    return comment; // 单行注释不处理

                var sb = new StringBuilder(comment.Length + lines.Length * 3);
                sb.Append("/*");

                // 提取首行 /* 后的内容
                string firstContent = lines[0].Substring(2).TrimStart(' ', '\t', '*');
                if (firstContent.Length > 0)
                    sb.Append(" ").Append(firstContent);

                // 处理中间行
                for (int i = 1; i < lines.Length - 1; i++)
                {
                    string content = lines[i].TrimStart(' ', '\t', '*').TrimEnd();
                    sb.AppendLine();
                    if (content.Length > 0)
                        sb.Append(" * ").Append(content);
                    else
                        sb.Append(" *");
                }

                // 处理末行 */ 前的内容
                string lastContent = lines[lines.Length - 1];
                int closeIdx = lastContent.LastIndexOf("*/");
                if (closeIdx >= 0)
                    lastContent = lastContent.Substring(0, closeIdx).TrimStart(' ', '\t', '*').TrimEnd();

                sb.AppendLine();
                if (lastContent.Length > 0)
                    sb.Append(" * ").Append(lastContent);
                sb.AppendLine();
                sb.Append(" */");

                return sb.ToString();
            });
        }

        // ════════════════════════════════════════════════════════════════════════
        // 通用辅助方法
        // ════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// 安全执行对齐操作：捕获异常并返回回退值。
        /// </summary>
        /// <param name="action">对齐操作委托</param>
        /// <param name="fallback">异常时的回退值</param>
        /// <returns>对齐结果或回退值</returns>
        private static string SafeAlign(Func<string> action, string fallback)
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
        /// 在字符串字面量之外对文本执行转换。
        /// 遍历文本，将单引号字符串外的部分传入转换函数，字符串内原样保留。
        /// </summary>
        /// <param name="text">待处理的文本</param>
        /// <param name="transform">非字符串部分的转换函数</param>
        /// <returns>转换后的文本</returns>
        private static string TransformOutsideStrings(string text, Func<string, string> transform)
        {
            var result = new StringBuilder(text.Length);
            int i = 0;

            while (i < text.Length)
            {
                if (text[i] == '\'')
                {
                    // 复制整个字符串字面量（处理 '' 转义）
                    int start = i;
                    i++;
                    while (i < text.Length)
                    {
                        if (text[i] == '\'')
                        {
                            if (i + 1 < text.Length && text[i + 1] == '\'')
                            {
                                i += 2;
                                continue;
                            }
                            i++;
                            break;
                        }
                        i++;
                    }
                    result.Append(text.Substring(start, i - start));
                }
                else
                {
                    // 复制非字符串部分直到下一个单引号
                    int start = i;
                    while (i < text.Length && text[i] != '\'')
                        i++;
                    result.Append(transform(text.Substring(start, i - start)));
                }
            }

            return result.ToString();
        }

        /// <summary>
        /// 按顶层逗号（括号深度为 0 且不在字符串内的逗号）分割文本。
        /// </summary>
        /// <param name="text">待分割的文本</param>
        /// <returns>分割后的片段列表（含前后空格）</returns>
        private static List<string> SplitByTopLevelCommas(string text)
        {
            var result = new List<string>();
            bool inString = false;
            int parenDepth = 0;
            int start = 0;

            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];

                if (inString)
                {
                    if (c == '\'') inString = false;
                    continue;
                }
                if (c == '\'') { inString = true; continue; }
                if (c == '(') { parenDepth++; continue; }
                if (c == ')') { if (parenDepth > 0) parenDepth--; continue; }

                if (c == ',' && parenDepth == 0)
                {
                    result.Add(text.Substring(start, i - start));
                    start = i + 1;
                }
            }

            result.Add(text.Substring(start));
            return result;
        }

        /// <summary>
        /// 在字符串外查找指定字符的第一个出现位置。
        /// </summary>
        /// <param name="text">待搜索的文本</param>
        /// <param name="ch">目标字符</param>
        /// <returns>字符索引；未找到返回 -1</returns>
        private static int FindCharOutsideString(string text, char ch)
        {
            bool inString = false;
            for (int i = 0; i < text.Length; i++)
            {
                if (inString)
                {
                    if (text[i] == '\'') inString = false;
                    continue;
                }
                if (text[i] == '\'') { inString = true; continue; }
                if (text[i] == ch) return i;
            }
            return -1;
        }

        /// <summary>
        /// 从指定左括号位置查找匹配的右括号位置（考虑嵌套和字符串）。
        /// </summary>
        /// <param name="text">待搜索的文本</param>
        /// <param name="openPos">左括号位置</param>
        /// <returns>匹配的右括号位置；未找到返回 -1</returns>
        private static int FindMatchingCloseParen(string text, int openPos)
        {
            bool inString = false;
            int depth = 0;

            for (int i = openPos; i < text.Length; i++)
            {
                if (inString)
                {
                    if (text[i] == '\'') inString = false;
                    continue;
                }
                if (text[i] == '\'') { inString = true; continue; }
                if (text[i] == '(') depth++;
                if (text[i] == ')')
                {
                    depth--;
                    if (depth == 0) return i;
                }
            }

            return -1;
        }

        /// <summary>
        /// 获取行首第一个单词（大写形式），用于子句关键字判断。
        /// </summary>
        /// <param name="trimmedLine">已去除前导空白的行</param>
        /// <returns>第一个单词的大写形式；空行返回空字符串</returns>
        private static string GetFirstWord(string trimmedLine)
        {
            if (string.IsNullOrEmpty(trimmedLine))
                return "";

            int end = 0;
            while (end < trimmedLine.Length &&
                   (char.IsLetterOrDigit(trimmedLine[end]) || trimmedLine[end] == '_'))
                end++;

            return trimmedLine.Substring(0, end).ToUpperInvariant();
        }
    }
}
