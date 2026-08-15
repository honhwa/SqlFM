using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using SqlFM.Core.Batch;
using SqlFM.Core.Configuration;
using SqlFM.Core.Dialects;
using SqlFM.Core.Engine;
using SqlFM.Core.Exemption;
using SqlFM.Core.Lint;
using SqlFM.Core.PresetStyles;
using SqlFM.Core.Refactoring;

namespace SqlFM.Cli
{
    /// <summary>
    /// SQL 格式化命令行工具入口。
    /// 支持单文件/目录批量格式化、样式文件加载、编码指定、检查模式等。
    /// 退出码：0=成功无需修改, 1=有文件被修改, 2=部分失败, 3=参数错误, 4=致命错误。
    /// </summary>
    class Program
    {
        /// <summary>CLI 版本号</summary>
        private const string Version = "1.0.0";

        /// <summary>
        /// CLI 主入口：解析参数 → 加载样式 → 初始化管道 → 执行格式化。
        /// </summary>
        /// <param name="args">命令行参数数组</param>
        /// <returns>退出码（0-4，详见类注释）</returns>
        static int Main(string[] args)
        {
            CliOptions options;
            try
            {
                options = CliOptions.Parse(args);
            }
            catch (ArgumentException ex)
            {
                Console.Error.WriteLine("Error: " + ex.Message);
                Console.Error.WriteLine();
                PrintHelp();
                return 3;
            }

            if (options.ShowHelp)
            {
                PrintHelp();
                return 0;
            }

            if (options.ShowVersion)
            {
                Console.WriteLine("SqlFormatterCli v" + Version);
                return 0;
            }

            // 加载样式
            SqlFormatStyle style;
            try
            {
                if (!string.IsNullOrEmpty(options.StylePath))
                {
                    if (!File.Exists(options.StylePath))
                    {
                        Console.Error.WriteLine("Error: Style file not found: " + options.StylePath);
                        return 4;
                    }
                    style = StyleSerializer.LoadFromFile(options.StylePath!);
                    if (options.Verbose)
                    {
                        Console.WriteLine("Loaded style: " + style.Name + " from " + options.StylePath);
                    }
                }
                else
                {
                    style = PresetStyleFactory.CreateDefault();
                    if (options.Verbose)
                    {
                        Console.WriteLine("Using default style: " + style.Name);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Error: Failed to load style file: " + ex.Message);
                return 4;
            }

            // 初始化管道
            var pipeline = new FormatterPipeline();
            pipeline.LoadStyle(style);

            // ── 配置导出 ──
            if (!string.IsNullOrEmpty(options.ExportPath))
            {
                try
                {
                    StyleSerializer.SaveToFile(style, options.ExportPath!);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine("Error: export failed: " + ex.Message);
                    return 4;
                }
                Console.WriteLine("exported style '" + style.Name + "' to " + options.ExportPath);
                return 0;
            }

            // ── 配置导入 / 校验 ──
            if (!string.IsNullOrEmpty(options.ImportPath))
            {
                try
                {
                    var imported = StyleSerializer.LoadFromFile(options.ImportPath!);
                    Console.WriteLine("imported style '" + imported.Name + "' from " + options.ImportPath);
                    style = imported;
                    pipeline.LoadStyle(style);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine("Error: import failed: " + ex.Message);
                    return 4;
                }
                if (string.IsNullOrEmpty(options.FilePath))
                    return 0; // 仅校验导入的样式文件
            }

            // ── Lint 检查模式 ──
            if (options.Lint)
                return RunLint(pipeline, options, style);

            // ── SELECT * 展开 ──
            if (options.ExpandStar)
                return RunExpand(options, style);

            // ── 数据库对象批量检查（只读，不执行 ALTER）──
            if (!string.IsNullOrEmpty(options.DbConnection) && !options.ExpandStar)
                return RunDbCheck(options, pipeline);

            // 格式化模式需要目标文件或目录（Lint/Expand/导出等模式已在上方处理）
            if (string.IsNullOrEmpty(options.FilePath))
            {
                Console.Error.WriteLine("Error: --file (-f) is required.");
                Console.Error.WriteLine();
                PrintHelp();
                return 3;
            }

            // 判断是文件还是目录
            if (File.Exists(options.FilePath))
            {
                return ProcessSingleFile(pipeline, options);
            }
            else if (Directory.Exists(options.FilePath))
            {
                return ProcessDirectory(pipeline, options);
            }
            else
            {
                Console.Error.WriteLine("Error: Path not found: " + options.FilePath);
                return 4;
            }
        }

        /// <summary>
        /// 处理单个 SQL 文件：读取 → 格式化 → 写入（或检查）。
        /// </summary>
        /// <param name="pipeline">格式化管道实例</param>
        /// <param name="options">解析后的 CLI 参数</param>
        /// <returns>退出码（0=无修改, 1=已格式化, 2=失败, 4=读取/写入失败）</returns>
        static int ProcessSingleFile(FormatterPipeline pipeline, CliOptions options)
        {
            var encoding = options.GetEncoding();

            string originalSql;
            try
            {
                originalSql = File.ReadAllText(options.FilePath, encoding);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Error: Failed to read file: " + ex.Message);
                return 4;
            }

            var result = pipeline.Format(originalSql);

            if (!result.Success)
            {
                Console.Error.WriteLine("Error: Failed to format " + options.FilePath + ": " + result.ErrorMessage);
                return 2;
            }

            bool changed = !string.Equals(originalSql, result.FormattedSql, StringComparison.Ordinal);

            if (options.CheckOnly)
            {
                if (changed)
                {
                    Console.WriteLine("needs formatting: " + options.FilePath);
                    return 1;
                }
                else
                {
                    if (options.Verbose)
                    {
                        Console.WriteLine("already formatted: " + options.FilePath);
                    }
                    return 0;
                }
            }

            if (!changed)
            {
                if (options.Verbose)
                {
                    Console.WriteLine("no changes: " + options.FilePath);
                }
                return 0;
            }

            // 写入格式化结果
            string outputPath = GetOutputPath(options.FilePath!, options.OutputPath, options.FilePath);
            try
            {
                EnsureDirectoryExists(outputPath);
                File.WriteAllText(outputPath, result.FormattedSql, encoding);
                if (options.Verbose)
                {
                    Console.WriteLine("formatted: " + options.FilePath + " -> " + outputPath);
                }
                else
                {
                    Console.WriteLine("formatted: " + options.FilePath);
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Error: Failed to write file: " + ex.Message);
                return 2;
            }

            return 1;
        }

        /// <summary>
        /// 批量处理目录下所有 .sql 文件：委托统一的 <see cref="FileBatchProcessor"/> 执行扫描与格式化，
        /// 再将其结果映射为与旧实现一致的控制台汇总与退出码。
        /// 支持 --check 检查模式和 --recursive 递归子目录。
        /// </summary>
        /// <param name="pipeline">格式化管道实例</param>
        /// <param name="options">解析后的 CLI 参数</param>
        /// <returns>退出码（0=全部无需修改, 1=有文件被格式化/需格式化, 2=部分失败）</returns>
        static int ProcessDirectory(FormatterPipeline pipeline, CliOptions options)
        {
            var processor = new FileBatchProcessor(pipeline);

            BatchResult result;
            try
            {
                result = processor.ProcessDirectory(
                    options.FilePath!,
                    options.GetEncoding(),
                    options.OutputPath,
                    options.Recursive,
                    (i, n, file) => Console.Write("\r[{0}/{1}] Processing... ", i, n),
                    options.CheckOnly);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Error: Failed to scan directory: " + ex.Message);
                return 4;
            }

            // 清除进度行
            Console.Write("\r" + new string(' ', 60) + "\r");

            if (result.TotalFiles == 0)
            {
                Console.WriteLine("No .sql files found in: " + options.FilePath);
                return 0;
            }

            // 输出汇总（与旧实现格式保持一致）
            Console.WriteLine();
            Console.WriteLine("Summary:");
            Console.WriteLine("  Total:       " + result.TotalFiles);

            int unchanged = result.SuccessFiles - result.ModifiedFiles;

            if (options.CheckOnly)
            {
                Console.WriteLine("  Needs format: " + result.ModifiedFiles);
                Console.WriteLine("  OK:          " + unchanged);
                if (result.FailedFiles.Count > 0)
                    Console.WriteLine("  Failed:      " + result.FailedFiles.Count);

                if (result.FailedFiles.Count > 0)
                    return 2;
                if (result.ModifiedFiles > 0)
                    return 1;
                return 0;
            }
            else
            {
                Console.WriteLine("  Formatted:   " + result.ModifiedFiles);
                Console.WriteLine("  Unchanged:   " + unchanged);
                if (result.FailedFiles.Count > 0)
                    Console.WriteLine("  Failed:      " + result.FailedFiles.Count);

                if (result.FailedFiles.Count > 0)
                    return 2;
                if (result.ModifiedFiles > 0)
                    return 1;
                return 0;
            }
        }

        /// <summary>
        /// 执行 Lint 检查（单文件或目录），逐文件输出违规并汇总。
        /// 退出码：0=无违规，1=存在违规，3=参数错误，4=路径不存在。
        /// </summary>
        static int RunLint(FormatterPipeline pipeline, CliOptions options, SqlFormatStyle style)
        {
            if (string.IsNullOrEmpty(options.FilePath))
            {
                Console.Error.WriteLine("Error: --file (-f) is required for --lint.");
                return 3;
            }

            var engine = LintRuleCatalog.DefaultEngine;
            var exemption = new ExemptionProcessor();
            var dialect = TsqlDialect.Instance;
            var encoding = options.GetEncoding();
            var enable = SplitRules(options.EnableRules);
            var disable = SplitRules(options.DisableRules);
            int violationCount = 0;

            if (File.Exists(options.FilePath))
            {
                violationCount += LintFile(engine, exemption, dialect, style, options.FilePath!, encoding, enable, disable);
            }
            else if (Directory.Exists(options.FilePath))
            {
                var search = options.Recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
                foreach (var file in Directory.GetFiles(options.FilePath, "*.sql", search))
                {
                    violationCount += LintFile(engine, exemption, dialect, style, file, encoding, enable, disable);
                }
            }
            else
            {
                Console.Error.WriteLine("Error: Path not found: " + options.FilePath);
                return 4;
            }

            Console.WriteLine(violationCount == 0
                ? "lint: no issues found."
                : "lint: " + violationCount + " issue(s) found.");
            return violationCount == 0 ? 0 : 1;
        }

        /// <summary>
        /// 对单个 SQL 文件执行 Lint，输出违规明细并返回违规数量。
        /// </summary>
        static int LintFile(SqlRuleEngine engine, ExemptionProcessor exemption, SqlDialect dialect,
            SqlFormatStyle style, string path, Encoding encoding, string[] enable, string[] disable)
        {
            string sql;
            try
            {
                sql = File.ReadAllText(path, encoding);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Error: failed to read " + path + ": " + ex.Message);
                return 0;
            }

            // 提取豁免区域，使豁免段内的违规被过滤
            var (processed, regions) = exemption.PreProcess(sql);
            var lintRegions = LintRuleCatalog.ToLintRegions(regions, processed);
            var results = engine.Lint(processed, dialect, style, lintRegions, enable, disable);

            foreach (var r in results)
            {
                Console.WriteLine(path + " | " + r.ToDisplayString());
            }
            return results.Count;
        }

        /// <summary>
        /// 将 SELECT * 展开为完整字段列表。需通过 --metadata(JSON) 或 --db(连接串) 提供表-列映射。
        /// 展开结果输出到标准输出（可重定向到文件）。
        /// </summary>
        static int RunExpand(CliOptions options, SqlFormatStyle style)
        {
            IDictionary<string, IList<string>> tableColumns;
            if (!string.IsNullOrEmpty(options.MetadataPath))
            {
                try { tableColumns = MetadataProvider.FromJson(options.MetadataPath!); }
                catch (Exception ex) { Console.Error.WriteLine("Error: load metadata failed: " + ex.Message); return 4; }
            }
            else if (!string.IsNullOrEmpty(options.DbConnection))
            {
                try { tableColumns = MetadataProvider.FromDatabase(options.DbConnection!); }
                catch (Exception ex) { Console.Error.WriteLine("Error: load metadata from DB failed: " + ex.Message); return 4; }
            }
            else
            {
                Console.Error.WriteLine("Error: --expand requires --metadata <file> or --db <conn>.");
                return 3;
            }

            string sql;
            try { sql = ReadInput(options.FilePath); }
            catch (Exception ex) { Console.Error.WriteLine("Error: " + ex.Message); return 3; }

            var expander = new StarExpander(new ScriptDomEngine());
            string expanded = expander.ExpandStar(sql, tableColumns);
            Console.WriteLine(expanded);
            return 0;
        }

        /// <summary>
        /// 数据库对象批量格式化检查（只读）：读取所有用户可编程对象，格式化后统计会被改写的数量。
        /// 不执行任何 ALTER，避免破坏性操作；用于 CI 预览差异。
        /// </summary>
        static int RunDbCheck(CliOptions options, FormatterPipeline pipeline)
        {
            try
            {
                var batch = new DbMetadataBatch(pipeline);
                var objects = batch.GetProgrammableObjects(options.DbConnection!);
                int wouldChange = 0;
                foreach (var obj in objects)
                {
                    if (string.IsNullOrEmpty(obj.Definition))
                        continue;
                    var result = pipeline.Format(obj.Definition!);
                    if (result.Success && result.FormattedSql != obj.Definition)
                        wouldChange++;
                }
                Console.WriteLine("db-check: " + objects.Count + " object(s), " + wouldChange + " would be reformatted (read-only).");
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Error: db-check failed: " + ex.Message);
                return 4;
            }
        }

        /// <summary>将逗号/分号分隔的规则列表拆分为数组（去空白、去空项）。</summary>
        static string[] SplitRules(string? value)
        {
            if (value == null)
                return Array.Empty<string>();
            return value.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(x => x.Trim())
                        .Where(x => x.Length > 0)
                        .ToArray();
        }

        /// <summary>读取 SQL 输入：优先读取 --file 指定文件，否则从标准输入（管道）读取。</summary>
        static string ReadInput(string? filePath)
        {
            if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
                return File.ReadAllText(filePath);
            if (Console.IsInputRedirected)
                return Console.In.ReadToEnd();
            throw new InvalidOperationException("No input: provide --file <path> or pipe SQL via stdin.");
        }

        /// <summary>
        /// 计算输出文件路径。
        /// 如果指定了输出目录，则保持源文件的相对目录结构；
        /// 否则覆盖原文件。
        /// </summary>
        /// <param name="sourceFilePath">源 SQL 文件完整路径</param>
        /// <param name="outputDir">输出目录；为 null/空时覆盖原文件</param>
        /// <param name="baseDir">基准目录（用于计算相对路径，通常为输入目录）</param>
        /// <returns>最终输出文件路径</returns>
        static string GetOutputPath(string sourceFilePath, string? outputDir, string? baseDir)
        {
            if (string.IsNullOrEmpty(outputDir))
            {
                return sourceFilePath;
            }

            if (string.IsNullOrEmpty(baseDir))
            {
                return Path.Combine(outputDir, Path.GetFileName(sourceFilePath));
            }

            // 获取源文件相对于 baseDir 的相对路径
            string relativePath = GetRelativePath(baseDir!, Path.GetDirectoryName(sourceFilePath) ?? "");
            string fileName = Path.GetFileName(sourceFilePath);
            return Path.Combine(outputDir, relativePath, fileName);
        }

        /// <summary>
        /// 计算相对路径（兼容 net48，不使用 .NET Core 的 Path.GetRelativePath）。
        /// </summary>
        /// <param name="relativeTo">基准路径</param>
        /// <param name="path">目标路径</param>
        /// <returns>相对路径字符串；无法计算时返回原路径</returns>
        static string GetRelativePath(string relativeTo, string path)
        {
            if (string.IsNullOrEmpty(relativeTo))
                return path;

            // 标准化路径分隔符
            string from = relativeTo.Replace('/', '\\').TrimEnd('\\');
            string to = path.Replace('/', '\\').TrimEnd('\\');

            if (to.StartsWith(from, StringComparison.OrdinalIgnoreCase))
            {
                string rel = to.Substring(from.Length).TrimStart('\\');
                return rel;
            }

            return path;
        }

        /// <summary>确保目标文件所在目录存在，不存在则自动创建。</summary>
        /// <param name="filePath">目标文件完整路径</param>
        static void EnsureDirectoryExists(string filePath)
        {
            string? dir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
        }

        /// <summary>输出帮助信息到标准输出（包含用法、参数列表、退出码和示例）。</summary>
        static void PrintHelp()
        {
            Console.WriteLine("SqlFormatterCli - SQL formatting command-line tool");
            Console.WriteLine();
            Console.WriteLine("Usage: SqlFormatterCli [options]");
            Console.WriteLine();
            Console.WriteLine("Options:");
            Console.WriteLine("  -s, --style <path>      Style configuration file path (.sqlstyle XML)");
            Console.WriteLine("  -f, --file <path>       Target SQL file or directory path (required)");
            Console.WriteLine("  -o, --output <path>     Output directory (optional, overwrites in-place by default)");
            Console.WriteLine("  -e, --encoding <name>   File encoding (utf-8/gbk, default: utf-8)");
            Console.WriteLine("  -r, --recursive         Recurse into subdirectories (default: true)");
            Console.WriteLine("      --no-recursive      Do not recurse into subdirectories");
            Console.WriteLine("      --check             Check only, do not modify files");
            Console.WriteLine("      --lint              Run Lint checks (CI: exit 1 if any issue)");
            Console.WriteLine("      --export <path>     Export current style to a .sqlstyle file");
            Console.WriteLine("      --import <path>     Import & validate a .sqlstyle file");
            Console.WriteLine("      --expand            Expand SELECT * using --metadata or --db");
            Console.WriteLine("      --metadata <path>   Table-column metadata JSON for --expand");
            Console.WriteLine("      --db <connstr>      SQL Server connection (metadata or db-check)");
            Console.WriteLine("      --enable-rules <s>  Comma list of rules/groups to enable (--lint)");
            Console.WriteLine("      --disable-rules <s> Comma list of rules to disable (--lint)");
            Console.WriteLine("      --verbose           Verbose output");
            Console.WriteLine("  -h, --help              Show this help message");
            Console.WriteLine("      --version           Show version number");
            Console.WriteLine();
            Console.WriteLine("Exit codes:");
            Console.WriteLine("  0  Success, no formatting changes needed / no lint issues");
            Console.WriteLine("  1  Success, files were formatted (or need formatting in --check mode) / lint issues found");
            Console.WriteLine("  2  Some files failed to format");
            Console.WriteLine("  3  Argument error");
            Console.WriteLine("  4  Fatal error (file not found, invalid style file, etc.)");
            Console.WriteLine();
            Console.WriteLine("Examples:");
            Console.WriteLine("  SqlFormatterCli -f script.sql");
            Console.WriteLine("  SqlFormatterCli -f script.sql --check");
            Console.WriteLine("  SqlFormatterCli -f ./sql-folder --no-recursive");
            Console.WriteLine("  SqlFormatterCli -f ./sql-folder -o ./formatted -s custom.sqlstyle");
            Console.WriteLine("  SqlFormatterCli -f script.sql -e gbk --verbose");
            Console.WriteLine("  SqlFormatterCli -f script.sql --lint");
            Console.WriteLine("  SqlFormatterCli -f script.sql --export my.sqlstyle");
            Console.WriteLine("  SqlFormatterCli -f query.sql --expand --metadata meta.json");
            Console.WriteLine("  SqlFormatterCli -f query.sql --lint --disable-rules AM02,ST01");
        }
    }
}
