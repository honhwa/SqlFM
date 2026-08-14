using System;
using SqlFM.Core.Configuration;
using SqlFM.Core.Engine;
using SqlFM.Core.PresetStyles;
using Xunit;

namespace SqlFM.Core.Tests
{
    /// <summary>
    /// 验证 PROCEDURE 参数列表注释前置保护（ProcedureCommentProtector + FormatterPipeline）：
    /// 注释应正确归位到各自参数，不被 PoorMans 错位，多存储过程之间不错乱。
    /// </summary>
    public class ProcedureCommentProtectorTests
    {
        [Fact]
        public void Pipeline_AlterProcedure_InlineCommentStaysWithParam()
        {
            var pipeline = new FormatterPipeline();
            pipeline.LoadStyle(PresetStyleFactory.CreateDefault());

            var sql = "ALTER PROCEDURE dbo.usp_GetOrders " +
                      "@OrderId INT, -- 订单ID " +
                      "@StartDate DATETIME, -- 起始日期 " +
                      "@EndDate DATETIME -- 结束日期 " +
                      "AS BEGIN SELECT 1; END";

            var result = pipeline.Format(sql);
            Assert.True(result.Success);

            var outSql = result.FormattedSql;
            // 每个注释紧跟其原参数（按出现顺序，不被错位到其它参数）
            int idxOrder = outSql.IndexOf("@OrderId", StringComparison.Ordinal);
            int idxStart = outSql.IndexOf("@StartDate", StringComparison.Ordinal);
            int idxEnd = outSql.IndexOf("@EndDate", StringComparison.Ordinal);
            int c1 = outSql.IndexOf("订单ID", StringComparison.Ordinal);
            int c2 = outSql.IndexOf("起始日期", StringComparison.Ordinal);
            int c3 = outSql.IndexOf("结束日期", StringComparison.Ordinal);

            Assert.True(idxOrder < c1 && c1 < idxStart,
                $"注释[订单ID]应跟在 @OrderId 之后、@StartDate 之前\n{outSql}");
            Assert.True(idxStart < c2 && c2 < idxEnd,
                $"注释[起始日期]应跟在 @StartDate 之后、@EndDate 之前\n{outSql}");
            Assert.True(idxEnd < c3,
                $"注释[结束日期]应跟在 @EndDate 之后\n{outSql}");
        }

        [Fact]
        public void Pipeline_MultipleProcedures_CommentsNotCrossed()
        {
            var pipeline = new FormatterPipeline();
            pipeline.LoadStyle(PresetStyleFactory.CreateDefault());

            // 两个 proc 同批（无 GO，避免 ScriptDom 解析失败导致保护回退）
            var sql =
                "CREATE PROCEDURE dbo.p1 @x INT, -- 一 @y INT -- 二 AS BEGIN SELECT 1; END\n" +
                "CREATE PROCEDURE dbo.p2 @x INT, -- 三 @y INT -- 四 AS BEGIN SELECT 1; END";

            var result = pipeline.Format(sql);
            Assert.True(result.Success);

            var outSql = result.FormattedSql;
            int p1x = IndexOfNth(outSql, "@x", 0);
            int p1y = IndexOfNth(outSql, "@y", 0);
            int p2x = IndexOfNth(outSql, "@x", 1);
            int p2y = IndexOfNth(outSql, "@y", 1);
            int c1 = outSql.IndexOf("一", StringComparison.Ordinal);
            int c2 = outSql.IndexOf("二", StringComparison.Ordinal);
            int c3 = outSql.IndexOf("三", StringComparison.Ordinal);
            int c4 = outSql.IndexOf("四", StringComparison.Ordinal);

            Assert.True(p1x < c1 && c1 < p1y, $"注释[一]应落在 p1 的 @x 与 @y 之间\n{outSql}");
            Assert.True(p1y < c2 && c2 < p2x, $"注释[二]应落在 p1 的 @y 之后、p2 之前\n{outSql}");
            Assert.True(p2x < c3 && c3 < p2y, $"注释[三]应落在 p2 的 @x 与 @y 之间\n{outSql}");
            Assert.True(p2y < c4, $"注释[四]应落在 p2 的 @y 之后\n{outSql}");
        }

        [Fact]
        public void Pipeline_NonProcedureComment_Preserved()
        {
            var pipeline = new FormatterPipeline();
            pipeline.LoadStyle(PresetStyleFactory.CreateDefault());

            var sql = "SELECT a, b FROM t -- 行内注释";
            var result = pipeline.Format(sql);
            Assert.True(result.Success);
            Assert.Contains("-- 行内注释", result.FormattedSql);
        }

        [Fact]
        public void Pipeline_ProcedureWithoutComment_NotBroken()
        {
            var pipeline = new FormatterPipeline();
            pipeline.LoadStyle(PresetStyleFactory.CreateDefault());

            var sql = "ALTER PROCEDURE dbo.usp_Simple @A INT, @B INT AS BEGIN SELECT 1; END";
            var result = pipeline.Format(sql);
            Assert.True(result.Success);
            Assert.Contains("@A INT", result.FormattedSql);
            Assert.Contains("@B INT", result.FormattedSql);
        }

        [Fact]
        public void Pipeline_BlockComment_InParams_Preserved()
        {
            var pipeline = new FormatterPipeline();
            pipeline.LoadStyle(PresetStyleFactory.CreateDefault());

            var sql = "CREATE PROCEDURE dbo.usp_B @x INT /* 块注释X */, @y INT /* 块注释Y */ AS BEGIN SELECT 1; END";
            var result = pipeline.Format(sql);
            Assert.True(result.Success);
            Assert.Contains("块注释X", result.FormattedSql);
            Assert.Contains("块注释Y", result.FormattedSql);
        }

        private static int IndexOfNth(string s, string value, int nth)
        {
            int idx = -1;
            for (int i = 0; i <= nth; i++)
            {
                idx = s.IndexOf(value, idx + 1, StringComparison.Ordinal);
                if (idx < 0) break;
            }
            return idx;
        }
    }
}
