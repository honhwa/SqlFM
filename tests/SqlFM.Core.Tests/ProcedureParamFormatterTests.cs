using SqlFM.Core.Configuration;
using SqlFM.Core.Engine;
using SqlFM.Core.PresetStyles;
using Xunit;

namespace SqlFM.Core.Tests
{
    /// <summary>
    /// 验证 ProcedureParamFormatter（ALTER/CREATE PROCEDURE 参数列表注释错位修复）的正确性。
    /// </summary>
    public class ProcedureParamFormatterTests
    {
        [Fact]
        public void Pipeline_AlterProcedure_CommentAttachedToParamLine()
        {
            var pipeline = new FormatterPipeline();
            pipeline.LoadStyle(PresetStyleFactory.CreateDefault()); // CommaPosition.After

            var sql = "ALTER PROCEDURE dbo.usp_GetOrders " +
                      "@OrderId INT, -- 订单ID " +
                      "@StartDate DATETIME, -- 起始日期 " +
                      "@EndDate DATETIME -- 结束日期 " +
                      "AS BEGIN SELECT 1; END";

            var result = pipeline.Format(sql);
            Assert.True(result.Success);

            // 每个参数独占一行，且行内注释归位到对应参数行尾（而非错位到逗号之后的独立行）
            var lines = result.FormattedSql.Split('\n');
            bool orderIdOk = false;
            bool startDateOk = false;
            foreach (var line in lines)
            {
                if (line.Contains("@OrderId INT,") && line.Contains("-- 订单ID"))
                    orderIdOk = true;
                if (line.Contains("@StartDate DATETIME,") && line.Contains("-- 起始日期"))
                    startDateOk = true;
            }
            Assert.True(orderIdOk, "注释应归位到 @OrderId 所在行尾");
            Assert.True(startDateOk, "注释应归位到 @StartDate 所在行尾");

            // poor man's 错乱特征（“逗号独占一行且后接注释”）不应存在
            Assert.DoesNotContain(", -- 订单ID", result.FormattedSql);
            Assert.DoesNotContain(", -- 起始日期", result.FormattedSql);
        }

        [Fact]
        public void Pipeline_CreateProcedure_CommentAttachedToParamLine()
        {
            var pipeline = new FormatterPipeline();
            pipeline.LoadStyle(PresetStyleFactory.CreateDefault());

            var sql = "CREATE PROCEDURE dbo.usp_Ins " +
                      "@Name NVARCHAR(50), -- 名称 " +
                      "@Age INT -- 年龄 " +
                      "AS BEGIN SELECT 1; END";

            var result = pipeline.Format(sql);
            Assert.True(result.Success);

            var lines = result.FormattedSql.Split('\n');
            bool nameOk = false;
            foreach (var line in lines)
            {
                if (line.Contains("@Name NVARCHAR(50),") && line.Contains("-- 名称"))
                    nameOk = true;
            }
            Assert.True(nameOk, "CREATE PROCEDURE 注释应归位到 @Name 所在行尾");
            Assert.Contains("-- 年龄", result.FormattedSql);
        }

        [Fact]
        public void Pipeline_NoProcedure_Unchanged()
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
            // 无注释时仍应正常展开为每参数一行
            Assert.Contains("@A INT", result.FormattedSql);
            Assert.Contains("@B INT", result.FormattedSql);
        }
    }
}
