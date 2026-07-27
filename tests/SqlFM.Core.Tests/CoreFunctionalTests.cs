using System;
using SqlFM.Core.Configuration;
using SqlFM.Core.Engine;
using SqlFM.Core.Exemption;
using SqlFM.Core.PresetStyles;
using Xunit;

namespace SqlFM.Core.Tests
{
    /// <summary>
    /// Core 库功能测试：验证 FormatterPipeline、ExemptionProcessor、CaseConverter、
    /// StyleSerializer 和 PresetStyleFactory 的核心功能正确性。
    /// </summary>
    public class CoreFunctionalTests
    {
        [Fact]
        public void Pipeline_FormatsBasicSelect()
        {
            var pipeline = new FormatterPipeline();
            pipeline.LoadStyle(PresetStyleFactory.CreateDefault());

            var result = pipeline.Format("select id,name from users where active=1");
            Assert.True(result.Success);
            Assert.Contains("SELECT", result.FormattedSql); // 关键字大写
        }

        [Fact]
        public void Pipeline_RespectsFormatOff()
        {
            var pipeline = new FormatterPipeline();
            pipeline.LoadStyle(PresetStyleFactory.CreateDefault());

            var sql = "SELECT col1\n/* FORMAT OFF */\nselect   a,b from t\n/* FORMAT ON */\nSELECT col2";
            var result = pipeline.Format(sql);
            Assert.True(result.Success);
            Assert.Contains("select   a,b from t", result.FormattedSql); // 豁免区保持原样
        }

        [Fact]
        public void StyleSerializer_RoundTrip()
        {
            var style = PresetStyleFactory.CreateCommasBefore();
            var xml = StyleSerializer.SerializeToString(style);
            var loaded = StyleSerializer.DeserializeFromString(xml);

            Assert.Equal(style.Name, loaded.Name);
            Assert.Equal(style.Dml.CommaPosition, loaded.Dml.CommaPosition);
        }

        [Fact]
        public void ExemptionProcessor_HandlesNoFormatLine()
        {
            var processor = new ExemptionProcessor();
            var sql = "SELECT a,b FROM t -- NOFORMAT\nSELECT c FROM t2";
            var (processed, regions) = processor.PreProcess(sql);

            Assert.Single(regions);
            Assert.Equal(ExemptionType.LineNoFormat, regions[0].Type);
        }

        [Fact]
        public void CaseConverter_UppercasesKeywords()
        {
            var converter = new CaseConverter();
            var result = converter.KeywordsToUpper("select id from users where active = 1");
            Assert.Contains("SELECT", result);
            Assert.Contains("FROM", result);
            Assert.Contains("WHERE", result);
            // 标识符不变
            Assert.Contains("users", result);
            Assert.Contains("active", result);
        }

        [Fact]
        public void PresetStyles_AllFiveExist()
        {
            var presets = PresetStyleFactory.GetAllPresets();
            Assert.Equal(5, presets.Count);
        }

        [Fact]
        public void Alignment_DeclareVariablesAligned()
        {
            var pipeline = new FormatterPipeline();
            pipeline.LoadStyle(PresetStyleFactory.CreateDefault());

            var sql = "DECLARE @timecount NUMERIC(9), @piececount NUMERIC(9), @actualsalemoney NUMERIC(9, 2)";
            var result = pipeline.Format(sql);

            Assert.True(result.Success);
            var lines = result.FormattedSql.Split('\n');
            // 找到包含 @timecount 和 @piececount 的行
            var firstAt = -1;
            var secondAt = -1;
            foreach (var line in lines)
            {
                if (line.Contains("@timecount"))
                    firstAt = line.IndexOf('@');
                if (line.Contains("@piececount"))
                    secondAt = line.IndexOf('@');
            }
            Assert.True(firstAt >= 0, "应包含 @timecount 行");
            Assert.True(secondAt >= 0, "应包含 @piececount 行");
            Assert.Equal(firstAt, secondAt); // 两个 @ 应在同一列
        }

        [Fact]
        public void Alignment_SelectListColumnsAligned()
        {
            var pipeline = new FormatterPipeline();
            pipeline.LoadStyle(PresetStyleFactory.CreateDefault());

            var sql = "SELECT receiverdate, receivercode, posno FROM t";
            var result = pipeline.Format(sql);

            Assert.True(result.Success);
            var lines = result.FormattedSql.Split('\n');
            int firstCol = -1, secondCol = -1;
            foreach (var line in lines)
            {
                if (line.Contains("receiverdate"))
                    firstCol = line.IndexOf("receiverdate", StringComparison.OrdinalIgnoreCase);
                if (line.Contains("receivercode"))
                    secondCol = line.IndexOf("receivercode", StringComparison.OrdinalIgnoreCase);
            }
            Assert.True(firstCol > 0, "应包含 receiverdate 行");
            Assert.True(secondCol > 0, "应包含 receivercode 行");
            Assert.Equal(firstCol, secondCol); // 两个列名起始列相同
        }

        [Fact]
        public void Alignment_BetweenAndCompacted()
        {
            var pipeline = new FormatterPipeline();
            pipeline.LoadStyle(PresetStyleFactory.CreateDefault());

            var sql = "SELECT a FROM t WHERE d BETWEEN @s AND @e";
            var result = pipeline.Format(sql);

            Assert.True(result.Success);
            Assert.Contains("BETWEEN @s AND @e", result.FormattedSql);
        }
    }
}
