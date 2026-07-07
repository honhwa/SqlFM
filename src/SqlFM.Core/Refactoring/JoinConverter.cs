using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace SqlFM.Core.Refactoring
{
    /// <summary>
    /// 隐式 JOIN 转换为标准 INNER JOIN。
    /// <para>
    /// 实现策略（基础版本，使用正则匹配简单模式）：
    /// 识别 <c>FROM t1, t2 WHERE t1.id = t2.id</c> 模式，
    /// 将逗号分隔的多表 FROM 与 WHERE 中的等值连接条件拆分为标准 INNER JOIN ... ON 语法。
    /// </para>
    /// <para>
    /// 限制：当前实现处理简单的单层 SELECT 语句；含子查询、CTE、UNION 等复杂场景建议
    /// 升级为 ScriptDom AST 解析方式。
    /// </para>
    /// </summary>
    public class JoinConverter
    {
        // 匹配 FROM 子句（含多个逗号分隔的表，可有可选别名），捕获表列表
        // 示例：FROM Orders o, Customers c, Products p
        private static readonly Regex _fromRegex = new Regex(
            @"\bFROM\s+(?<tables>(?:\[?\w+\]?(?:\s+(?:AS\s+)?\[?\w+\]?)?\s*,\s*)+\[?\w+\]?(?:\s+(?:AS\s+)?\[?\w+\]?)?)\s+WHERE\b",
            RegexOptions.IgnoreCase | RegexOptions.Singleline);

        // 匹配 WHERE 子句中的等值连接条件，形如：alias1.col = alias2.col
        private static readonly Regex _joinConditionRegex = new Regex(
            @"(?<left>\[?\w+\]?\.\[?\w+\]?)\s*=\s*(?<right>\[?\w+\]?\.\[?\w+\]?)",
            RegexOptions.IgnoreCase);

        /// <summary>
        /// 将 <c>FROM t1, t2 WHERE t1.id = t2.id</c> 模式转换为
        /// <c>FROM t1 INNER JOIN t2 ON t1.id = t2.id</c>。
        /// </summary>
        /// <param name="sql">原始 SQL 文本</param>
        /// <returns>转换后的 SQL；若未匹配到隐式 JOIN 模式则返回原文</returns>
        public string ConvertImplicitJoins(string sql)
        {
            if (string.IsNullOrWhiteSpace(sql)) return sql;

            // 查找 FROM ... WHERE 块
            var match = _fromRegex.Match(sql);
            if (!match.Success) return sql;

            var tablesPart = match.Groups["tables"].Value;
            // 解析表列表（表名/别名对）
            var tableEntries = ParseTableList(tablesPart);

            if (tableEntries.Count < 2) return sql; // 单表，无需处理

            // 获取 WHERE 之后的内容
            int fromStart  = match.Index;
            int fromLength = match.Length; // 包含 FROM 到 WHERE
            // match 结尾仍保留 "WHERE" 关键字
            // 我们需要获取 WHERE 后面的条件部分
            int afterWhere = match.Index + match.Length; // 此时光标在 WHERE 关键字后面紧随的字符

            // 原始 WHERE 位置（不含 FROM ... 表列表部分）
            // 实际上 _fromRegex 末尾 \b 后面还没吃掉 WHERE，我们需要在 sql 里找到 WHERE 的实际位置
            // match.Value 末尾是 "WHERE"，所以 afterWhere 指向 WHERE 后面一个字符
            var whereContent = sql.Substring(afterWhere);

            // 从 WHERE 条件中分离出 JOIN 条件和非 JOIN 条件
            var allConditions = SplitAndConditions(whereContent);
            var joinConditions   = new List<string>();
            var filterConditions = new List<string>();

            // 建立（别名/表名）集合，用于判断等值条件是否属于 JOIN
            var aliases = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var entry in tableEntries)
            {
                aliases.Add(entry.Alias ?? entry.TableName);
                aliases.Add(entry.TableName);
            }

            foreach (var cond in allConditions)
            {
                if (IsJoinCondition(cond.Trim(), aliases))
                    joinConditions.Add(cond.Trim());
                else
                    filterConditions.Add(cond.Trim());
            }

            if (joinConditions.Count == 0) return sql; // 没有可转换的 JOIN 条件

            // 构建新 FROM 子句：第一个表 INNER JOIN 第二个表 ON ... INNER JOIN ...
            var sb = new StringBuilder();
            sb.Append("FROM ");
            sb.Append(tableEntries[0].FullText);

            // 将 JOIN 条件按顺序分配给各表（简单策略：每对相邻表分配一个条件）
            // 更复杂的分配逻辑可通过 AST 实现，此处采用顺序分配
            int joinIdx = 0;
            for (int i = 1; i < tableEntries.Count; i++)
            {
                sb.Append("\r\n    INNER JOIN ");
                sb.Append(tableEntries[i].FullText);
                if (joinIdx < joinConditions.Count)
                {
                    sb.Append(" ON ");
                    sb.Append(joinConditions[joinIdx++]);
                }
            }

            // 追加剩余 JOIN 条件（超出表数量的）
            while (joinIdx < joinConditions.Count)
            {
                sb.Append("\r\n      AND ");
                sb.Append(joinConditions[joinIdx++]);
            }

            // 如果还有过滤条件，重新加上 WHERE
            if (filterConditions.Count > 0)
            {
                sb.Append("\r\nWHERE ");
                sb.Append(string.Join("\r\n  AND ", filterConditions));
            }

            // 替换原始的 FROM ... WHERE ... 片段
            var result = new StringBuilder(sql);
            // 替换范围：从 FROM 起始到 WHERE 后所有已处理条件的末尾
            // 确定原始已处理 WHERE 内容的结束位置
            int conditionsEnd = FindConditionsEnd(whereContent, allConditions);
            int replaceLength = (afterWhere + conditionsEnd) - fromStart;

            result.Remove(fromStart, replaceLength);
            result.Insert(fromStart, sb.ToString());

            return result.ToString();
        }

        // ── 私有辅助方法 ─────────────────────────────────────────────────

        /// <summary>解析逗号分隔的表列表，支持可选别名</summary>
        private static List<TableEntry> ParseTableList(string tablesPart)
        {
            var entries = new List<TableEntry>();
            var parts = tablesPart.Split(',');
            foreach (var part in parts)
            {
                var trimmed = part.Trim();
                if (string.IsNullOrEmpty(trimmed)) continue;

                // 处理 "TableName AS Alias" 或 "TableName Alias"
                var tokens = Regex.Split(trimmed, @"\s+");
                var entry = new TableEntry { FullText = trimmed };
                if (tokens.Length >= 1) entry.TableName = tokens[0];
                if (tokens.Length == 2) entry.Alias = tokens[1];
                if (tokens.Length == 3 && string.Equals(tokens[1], "AS",
                        StringComparison.OrdinalIgnoreCase))
                    entry.Alias = tokens[2];

                entries.Add(entry);
            }
            return entries;
        }

        /// <summary>按 AND 分割 WHERE 条件（简单分割，不处理括号嵌套）</summary>
        private static List<string> SplitAndConditions(string whereContent)
        {
            // 以 AND 分割，但要排除括号内的 AND
            var result = new List<string>();
            int depth = 0;
            int start = 0;
            for (int i = 0; i < whereContent.Length; i++)
            {
                char c = whereContent[i];
                if (c == '(') depth++;
                else if (c == ')') depth--;
                else if (depth == 0 && i + 3 < whereContent.Length)
                {
                    // 检查是否为 " AND "
                    if (string.Compare(whereContent, i, " AND ", 0, 5,
                            StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        result.Add(whereContent.Substring(start, i - start));
                        start = i + 5;
                        i += 4;
                    }
                }
            }
            // 最后一段
            var last = whereContent.Substring(start).TrimEnd(';', '\r', '\n', ' ');
            if (!string.IsNullOrWhiteSpace(last))
                result.Add(last);

            return result;
        }

        /// <summary>判断条件是否为跨表等值 JOIN 条件（两侧都含表别名前缀且属于已知别名）</summary>
        private static bool IsJoinCondition(string condition, HashSet<string> aliases)
        {
            var m = _joinConditionRegex.Match(condition);
            if (!m.Success) return false;

            var leftParts  = m.Groups["left"].Value.Split('.');
            var rightParts = m.Groups["right"].Value.Split('.');

            if (leftParts.Length < 2 || rightParts.Length < 2) return false;

            var leftAlias  = leftParts[0].Trim('[', ']');
            var rightAlias = rightParts[0].Trim('[', ']');

            return aliases.Contains(leftAlias) && aliases.Contains(rightAlias)
                   && !string.Equals(leftAlias, rightAlias, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>计算已处理条件在 whereContent 中到达的末尾字符位置</summary>
        private static int FindConditionsEnd(string whereContent, List<string> conditions)
        {
            if (conditions.Count == 0) return 0;
            // 找最后一个条件在 whereContent 中的末尾位置
            var lastCond = conditions[conditions.Count - 1];
            int idx = whereContent.LastIndexOf(lastCond, StringComparison.OrdinalIgnoreCase);
            if (idx < 0) return whereContent.Length;
            return idx + lastCond.Length;
        }

        private class TableEntry
        {
            public string TableName { get; set; } = string.Empty;
            public string? Alias { get; set; }
            /// <summary>原始文本（含别名）</summary>
            public string FullText { get; set; } = string.Empty;
        }
    }
}
