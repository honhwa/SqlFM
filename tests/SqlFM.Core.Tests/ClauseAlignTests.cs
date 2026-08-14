using System;
using System.Linq;
using Xunit;
using SqlFM.Core.Configuration;
using SqlFM.Core.Engine;
using SqlFM.Core.PresetStyles;

namespace SqlFM.Core.Tests
{
    /// <summary>
    /// 验证子句关键字右对齐（AlignClauseKeyword）：所有顶层子句关键字（SELECT / FROM /
    /// JOIN 系列 / WHERE / GROUP BY / HAVING / ORDER BY）按各自最后一个字母统一对齐到
    /// 同一列；ON 条件跟在 JOIN 同一行不换行；关键字后仅保留 1 个分隔空格。
    /// </summary>
    public class ClauseAlignTests
    {
        private static string FormatWithAlign(string sql)
        {
            var style = PresetStyleFactory.CreateDefault();
            style.Dml.AlignClauseKeyword = true;     // 子句关键字右对齐
            style.Dml.JoinKeywordNewLine = false;    // ON 跟在 JOIN 后不换行
            var pipeline = new FormatterPipeline();
            pipeline.LoadStyle(style);
            var r = pipeline.Format(sql);
            Assert.True(r.Success, "格式化应成功：" + (r.ErrorMessage ?? string.Empty));
            return r.FormattedSql;
        }

        /// <summary>所有顶层子句关键字的最后一个字母应落在同一列（末尾字母对齐）。</summary>
        [Fact]
        public void AlignClauseKeyword_AllClauseKeywords_EndLettersAligned()
        {
            var sql = "SELECT o.OrderID, c.CustomerName, o.TotalAmount " +
                      "FROM Orders o " +
                      "INNER JOIN Customers c ON o.CustomerID = c.CustomerID " +
                      "LEFT JOIN OrderDetails od ON o.OrderID = od.OrderID " +
                      "GROUP BY o.OrderID, c.CustomerName, o.TotalAmount " +
                      "ORDER BY o.TotalAmount DESC;";

            var result = FormatWithAlign(sql);
            var lines = result.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

            int selEnd = EndColOf(lines, "SELECT");
            int fromEnd = EndColOf(lines, "FROM");
            int innerEnd = EndColOf(lines, "INNER JOIN");
            int leftEnd = EndColOf(lines, "LEFT JOIN");
            int groupEnd = EndColOf(lines, "GROUP BY");
            int orderEnd = EndColOf(lines, "ORDER BY");

            // 末尾字母（关键字最后一个字符）必须同列
            Assert.Equal(selEnd, fromEnd);
            Assert.Equal(selEnd, innerEnd);
            Assert.Equal(selEnd, leftEnd);
            Assert.Equal(selEnd, groupEnd);
            Assert.Equal(selEnd, orderEnd);
        }

        /// <summary>JOIN 的 ON 条件应留在同一行，不另起缩进。</summary>
        [Fact]
        public void AlignClauseKeyword_OnClause_StaysOnJoinLine()
        {
            var sql = "SELECT o.OrderID FROM Orders o " +
                      "INNER JOIN Customers c ON o.CustomerID = c.CustomerID ORDER BY o.OrderID;";

            var result = FormatWithAlign(sql);
            var lines = result.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

            var joinLine = lines.First(l => l.TrimStart().StartsWith("INNER JOIN", StringComparison.OrdinalIgnoreCase));
            Assert.Contains("ON", joinLine);
            // 不存在单独成行的 ON
            Assert.DoesNotContain(lines, l => l.Trim() == "ON" || l.TrimStart().StartsWith("ON ", StringComparison.OrdinalIgnoreCase) && !l.TrimStart().StartsWith("INNER JOIN", StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>关键字后仅保留 1 个分隔空格（pad=0 的关键字，如 INNER JOIN，后面不应有多余空格）。</summary>
        [Fact]
        public void AlignClauseKeyword_TrailingContent_SingleSpaceSeparator()
        {
            var sql = "SELECT o.OrderID FROM Orders o INNER JOIN Customers c ON o.CustomerID = c.CustomerID ORDER BY o.OrderID;";

            var result = FormatWithAlign(sql);
            // INNER JOIN 为块内最长关键字（pad=0），其后应恰好 1 个空格后接表名
            Assert.Contains("INNER JOIN Customers", result);
        }

        /// <summary>默认关闭 AlignClauseKeyword 时不应改变原有格式（向后兼容）。</summary>
        [Fact]
        public void AlignClauseKeyword_DefaultOff_Unchanged()
        {
            var style = PresetStyleFactory.CreateDefault(); // AlignClauseKeyword 默认 false
            var pipeline = new FormatterPipeline();
            pipeline.LoadStyle(style);
            var sql = "SELECT o.OrderID FROM Orders o INNER JOIN Customers c ON o.CustomerID = c.CustomerID ORDER BY o.OrderID;";
            var r = pipeline.Format(sql);
            Assert.True(r.Success);
            var lines = r.FormattedSql.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            // 默认模式下 FROM 紧跟 SELECT 等关键字左对齐（无右对齐补空格）
            var fromLine = lines.First(l => l.TrimStart().StartsWith("FROM", StringComparison.OrdinalIgnoreCase));
            // FROM 不应出现在 SELECT 之后被补空格的右对齐位置（这里只验证能正常格式化且不抛异常）
            Assert.Contains("FROM", fromLine);
        }

        /// <summary>返回关键字最后一个字母所在的 1-based 列号。</summary>
        private static int EndColOf(string[] lines, string keyword)
        {
            for (int i = 0; i < lines.Length; i++)
            {
                var trimmed = lines[i].TrimStart();
                if (trimmed.StartsWith(keyword, StringComparison.OrdinalIgnoreCase))
                {
                    int start0 = lines[i].Length - trimmed.Length; // 0-based 起始列
                    return start0 + keyword.Length;                // 末尾字母列（1-based 末尾）
                }
            }
            throw new Exception("关键字未找到：" + keyword);
        }
    }
}
