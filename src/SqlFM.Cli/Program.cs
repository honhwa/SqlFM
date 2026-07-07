using System;
using System.Collections.Generic;
using System.IO;
using SqlFM.Core.Configuration;
using SqlFM.Core.Engine;
using SqlFM.Core.PresetStyles;

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

            if (string.IsNullOrEmpty(options.FilePath))
            {
                Console.Error.WriteLine("Error: --file (-f) is required.");
                Console.Error.WriteLine();
                PrintHelp();
                return 3;
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
        /// 批量处理目录下所有 .sql 文件：扫描 → 逐文件格式化 → 输出汇总。
        /// 支持 --check 检查模式和 --recursive 递归子目录。
        /// </summary>
        /// <param name="pipeline">格式化管道实例</param>
        /// <param name="options">解析后的 CLI 参数</param>
        /// <returns>退出码（0=全部无需修改, 1=有文件被格式化/需格式化, 2=部分失败）</returns>
        static int ProcessDirectory(FormatterPipeline pipeline, CliOptions options)
        {
            var searchOption = options.Recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            string[] sqlFiles;

            try
            {
                sqlFiles = Directory.GetFiles(options.FilePath, "*.sql", searchOption);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Error: Failed to scan directory: " + ex.Message);
                return 4;
            }

            if (sqlFiles.Length == 0)
            {
                Console.WriteLine("No .sql files found in: " + options.FilePath);
                return 0;
            }

            var encoding = options.GetEncoding();
            int totalFiles = sqlFiles.Length;
            int formattedCount = 0;
            int unchangedCount = 0;
            int failedCount = 0;
            int needsFormatCount = 0;

            for (int i = 0; i < sqlFiles.Length; i++)
            {
                string filePath = sqlFiles[i];

                // 进度显示
                Console.Write("\r[{0}/{1}] Processing... ", i + 1, totalFiles);

                string originalSql;
                try
                {
                    originalSql = File.ReadAllText(filePath, encoding);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine();
                    Console.Error.WriteLine("Error: Failed to read file " + filePath + ": " + ex.Message);
                    failedCount++;
                    continue;
                }

                var result = pipeline.Format(originalSql);

                if (!result.Success)
                {
                    Console.Error.WriteLine();
                    Console.Error.WriteLine("Error: Failed to format " + filePath + ": " + result.ErrorMessage);
                    failedCount++;
                    continue;
                }

                bool changed = !string.Equals(originalSql, result.FormattedSql, StringComparison.Ordinal);

                if (options.CheckOnly)
                {
                    if (changed)
                    {
                        needsFormatCount++;
                        if (options.Verbose)
                        {
                            Console.WriteLine();
                            Console.WriteLine("needs formatting: " + filePath);
                        }
                    }
                    else
                    {
                        unchangedCount++;
                    }
                }
                else
                {
                    if (changed)
                    {
                        string outputPath = GetOutputPath(filePath!, options.OutputPath, options.FilePath);
                        try
                        {
                            EnsureDirectoryExists(outputPath);
                            File.WriteAllText(outputPath, result.FormattedSql, encoding);
                            formattedCount++;
                        }
                        catch (Exception ex)
                        {
                            Console.Error.WriteLine();
                            Console.Error.WriteLine("Error: Failed to write file " + filePath + ": " + ex.Message);
                            failedCount++;
                            continue;
                        }
                    }
                    else
                    {
                        unchangedCount++;
                    }
                }
            }

            // 清除进度行
            Console.Write("\r" + new string(' ', 60) + "\r");

            // 输出汇总
            Console.WriteLine();
            Console.WriteLine("Summary:");
            Console.WriteLine("  Total:       " + totalFiles);

            if (options.CheckOnly)
            {
                Console.WriteLine("  Needs format: " + needsFormatCount);
                Console.WriteLine("  OK:          " + unchangedCount);
                if (failedCount > 0)
                    Console.WriteLine("  Failed:      " + failedCount);

                if (failedCount > 0)
                    return 2;
                if (needsFormatCount > 0)
                    return 1;
                return 0;
            }
            else
            {
                Console.WriteLine("  Formatted:   " + formattedCount);
                Console.WriteLine("  Unchanged:   " + unchangedCount);
                if (failedCount > 0)
                    Console.WriteLine("  Failed:      " + failedCount);

                if (failedCount > 0)
                    return 2;
                if (formattedCount > 0)
                    return 1;
                return 0;
            }
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
            Console.WriteLine("      --verbose           Verbose output");
            Console.WriteLine("  -h, --help              Show this help message");
            Console.WriteLine("      --version           Show version number");
            Console.WriteLine();
            Console.WriteLine("Exit codes:");
            Console.WriteLine("  0  Success, no formatting changes needed");
            Console.WriteLine("  1  Success, files were formatted (or need formatting in --check mode)");
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
        }
    }
}
