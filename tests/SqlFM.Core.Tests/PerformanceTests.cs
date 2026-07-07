using System;
using System.Diagnostics;
using System.Text;
using SqlFM.Core.Configuration;
using SqlFM.Core.Engine;
using SqlFM.Core.PresetStyles;
using Xunit;

namespace SqlFM.Core.Tests
{
    /// <summary>
    /// 性能测试：验证格式化引擎在大规模 SQL 脚本下的处理速度。
    /// </summary>
    public class PerformanceTests
    {
        [Fact]
        public void Format_LargeScript_CompletesUnder5Seconds()
        {
            // 生成约 10000 行 SQL
            var sb = new StringBuilder();
            for (int i = 0; i < 500; i++)
            {
                sb.AppendLine($"SELECT t{i}.col1, t{i}.col2, t{i}.col3, t{i}.col4, t{i}.col5");
                sb.AppendLine($"FROM dbo.Table{i} t{i}");
                sb.AppendLine($"INNER JOIN dbo.Ref{i} r{i} ON t{i}.id = r{i}.table_id");
                sb.AppendLine($"WHERE t{i}.status = 1 AND t{i}.created > '2024-01-01'");
                sb.AppendLine($"ORDER BY t{i}.col1;");
                sb.AppendLine();
                sb.AppendLine("GO");
                sb.AppendLine();
            }

            var pipeline = new FormatterPipeline();
            pipeline.LoadStyle(PresetStyleFactory.CreateDefault());

            var sw = Stopwatch.StartNew();
            var result = pipeline.Format(sb.ToString());
            sw.Stop();

            Assert.True(result.Success, $"Formatting failed: {result.ErrorMessage}");
            Assert.True(sw.Elapsed.TotalSeconds < 5, $"Took {sw.Elapsed.TotalSeconds:F1}s, expected < 5s");
        }
    }
}
