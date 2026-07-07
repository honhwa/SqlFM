using System;
using System.Linq;
using Xunit;
using SqlFM.Core.Engine;
using SqlFM.Core.Configuration;
using SqlFM.Core.PresetStyles;

namespace SqlFM.Tests
{
    /// <summary>
    /// FormatterPipeline 端到端格式化的单元测试。
    /// 覆盖基础格式化、关键字大小写、缩进、JOIN/CTE/CASE、注释保留、字符串保留、
    /// 复杂嵌套场景、边界情况、幂等性、方括号标识符、变量、HAVING/GROUP BY/ORDER BY 等。
    /// </summary>
    public class FormatterTests
    {
        #region ── 辅助方法 ──────────────────────────────────────────────────

        /// <summary>使用默认样式格式化 SQL 文本</summary>
        private static string Format(string sql)
        {
            var pipeline = new FormatterPipeline();
            pipeline.LoadStyle(PresetStyleFactory.CreateDefault());
            var result = pipeline.Format(sql);
            return result.FormattedSql;
        }

        /// <summary>标准化换行，便于比较</summary>
        private static string Norm(string s) =>
            s.Replace("\r\n", "\n").TrimEnd('\n');

        /// <summary>断言格式化结果与期望一致（忽略末尾换行差异）</summary>
        private void AssertFormatted(string expected, string actual)
        {
            Assert.Equal(Norm(expected), Norm(actual));
        }

        #endregion

        #region ── 1. 基础格式化 ─────────────────────────────────────────────

        [Fact]
        public void 简单SELECT_关键字大写子句换行()
        {
            var input = "select a, b from t where x = 1";
            var result = Format(input);

            // 关键字应大写
            Assert.Contains("SELECT", result);
            Assert.Contains("FROM", result);
            Assert.Contains("WHERE", result);

            // 子句应各占一行
            var lines = Norm(result).Split('\n');
            Assert.True(lines.Length >= 3, "至少有 SELECT/FROM/WHERE 三行");
        }

        #endregion

        #region ── 2. 关键字大写 ─────────────────────────────────────────────

        [Fact]
        public void 默认样式关键字全部大写()
        {
            var result = Format("select a from t");
            Assert.Contains("SELECT", result);
            Assert.Contains("FROM", result);
        }

        #endregion

        #region ── 3. 缩进 ───────────────────────────────────────────────────

        [Fact]
        public void 默认缩进宽度4()
        {
            var result = Format("select a, b from t");
            var lines = Norm(result).Split('\n');
            // 列列表行应有缩进
            var indentedLines = lines.Where(l => l.StartsWith("    ") && !l.TrimStart().StartsWith("SELECT") && !l.TrimStart().StartsWith("FROM")).ToList();
            Assert.NotEmpty(indentedLines);
        }

        [Fact]
        public void 嵌套子查询缩进()
        {
            var result = Format("select * from (select 1 as id) sub");
            var lines = Norm(result).Split('\n');
            Assert.True(lines.Length >= 3, "嵌套查询应有多个缩进级别");
        }

        #endregion

        #region ── 4. JOIN 格式化 ────────────────────────────────────────────

        [Fact]
        public void INNER_JOIN换行对齐()
        {
            var result = Format("select a from t1 inner join t2 on t1.id = t2.id");
            Assert.Contains("INNER JOIN", result);
            Assert.Contains("ON", result);
            var lines = Norm(result).Split('\n');
            Assert.True(lines.Length >= 3, "JOIN 和 ON 应分行");
        }

        #endregion

        #region ── 5. CTE 格式化 ─────────────────────────────────────────────

        [Fact]
        public void WITH_AS正确格式化()
        {
            var result = Format("with cte as (select 1) select * from cte");
            Assert.Contains("WITH", result);
            Assert.Contains("AS", result);
            Assert.Contains("SELECT", result);
        }

        #endregion

        #region ── 6. CASE WHEN 格式化 ───────────────────────────────────────

        [Fact]
        public void CASE_WHEN缩进()
        {
            var result = Format("select case when x = 1 then 'a' else 'b' end from t");
            Assert.Contains("CASE", result);
            Assert.Contains("WHEN", result);
            Assert.Contains("THEN", result);
            Assert.Contains("ELSE", result);
            Assert.Contains("END", result);
        }

        #endregion

        #region ── 7. 注释保留 ───────────────────────────────────────────────

        [Fact]
        public void 单行注释保留()
        {
            var result = Format("select a -- comment\r\nfrom t");
            Assert.Contains("-- comment", result);
        }

        [Fact]
        public void 多行注释保留()
        {
            var result = Format("select a /* comment */ from t");
            Assert.Contains("/* comment */", result);
        }

        #endregion

        #region ── 8. 字符串保留 ─────────────────────────────────────────────

        [Fact]
        public void 字符串内容不被修改()
        {
            var result = Format("select 'hello world' from t");
            Assert.Contains("'hello world'", result);
        }

        [Fact]
        public void 转义字符串保留()
        {
            var result = Format("select 'it''s ok' from t");
            Assert.Contains("'it''s ok'", result);
        }

        #endregion

        #region ── 9. 复杂场景 ──────────────────────────────────────────────

        [Fact]
        public void 复杂嵌套查询_子查询CTECASEJOIN()
        {
            var sql = @"with cte as (select a from t1 inner join t2 on t1.id = t2.id) select case when x = 1 then 'a' else 'b' end from cte";
            var result = Format(sql);
            Assert.Contains("WITH", result);
            Assert.Contains("AS", result);
            Assert.Contains("CASE", result);
            Assert.Contains("WHEN", result);
            Assert.Contains("END", result);
            Assert.Contains("INNER JOIN", result);
        }

        #endregion

        #region ── 10. 边界情况 ──────────────────────────────────────────────

        [Fact]
        public void 空输入返回空字符串()
        {
            var result = Format("");
            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public void 格式化不丢失语义内容()
        {
            var input = "select col1, col2 from dbo.MyTable where id = 1 and username = 'test'";
            var result = Format(input);
            Assert.Contains("col1", result);
            Assert.Contains("col2", result);
            Assert.Contains("dbo", result);
            Assert.Contains("MyTable", result);
            Assert.Contains("id", result);
            Assert.Contains("username", result);
            Assert.Contains("'test'", result);
        }

        #endregion

        #region ── 11. 格式化幂等性 ──────────────────────────────────────────

        [Fact]
        public void 格式化两次结果一致()
        {
            var input = "select a, b from t where x = 1";
            var first = Format(input);
            var second = Format(first);
            Assert.Equal(Norm(first), Norm(second));
        }

        #endregion

        #region ── 12. 方括号标识符格式化 ───────────────────────────────────

        [Fact]
        public void 方括号标识符保留()
        {
            var result = Format("select [Column Name] from [dbo].[Table1]");
            Assert.Contains("[Column Name]", result);
            Assert.Contains("[dbo]", result);
            Assert.Contains("[Table1]", result);
        }

        #endregion

        #region ── 13. 变量格式化 ────────────────────────────────────────────

        [Fact]
        public void 变量名保留()
        {
            var result = Format("select @UserId, @UserName from t");
            Assert.Contains("@UserId", result);
            Assert.Contains("@UserName", result);
        }

        #endregion

        #region ── 14. HAVING 格式化 ─────────────────────────────────────────

        [Fact]
        public void HAVING格式化()
        {
            var result = Format("select a, count(*) from t group by a having count(*) > 1");
            Assert.Contains("HAVING", result);
        }

        #endregion

        #region ── 15. GROUP BY / ORDER BY 格式化 ───────────────────────────

        [Fact]
        public void GROUPBY格式化()
        {
            var result = Format("select a, count(*) from t group by a");
            Assert.Contains("GROUP BY", result);
        }

        [Fact]
        public void ORDERBY格式化()
        {
            var result = Format("select a from t order by a");
            Assert.Contains("ORDER BY", result);
        }

        #endregion

        #region ── 16. SELECT DISTINCT 格式化 ────────────────────────────────

        [Fact]
        public void SELECT_DISTINCT保持在同行()
        {
            var result = Format("select distinct a from t");
            Assert.Contains("SELECT DISTINCT", result);
        }

        #endregion

        #region ── 17. 子查询格式化详细验证 ──────────────────────────────────

        [Fact]
        public void FROM子查询格式化带括号()
        {
            var result = Format("select * from (select 1 as id) sub");
            Assert.Contains("(", result);
            Assert.Contains(")", result);
            Assert.Contains("sub", result);
        }

        #endregion
    }
}
