using System;
using System.Collections.Generic;
using System.Text;

namespace SqlFM.Cli
{
    /// <summary>
    /// CLI 参数解析结果：封装所有命令行选项的解析与访问。
    /// 使用 <see cref="Parse(string[])"/> 从参数数组构建实例。
    /// </summary>
    internal class CliOptions
    {
        /// <summary>样式文件路径（.sqlstyle），为 null 时使用默认样式</summary>
        public string? StylePath { get; set; }
        /// <summary>目标 SQL 文件或目录路径（必填）</summary>
        public string? FilePath { get; set; }
        /// <summary>输出目录；为 null 时覆盖原文件</summary>
        public string? OutputPath { get; set; }
        /// <summary>文件编码名称（utf-8/gbk/gb2312），默认 utf-8</summary>
        public string Encoding { get; set; } = "utf-8";
        /// <summary>是否递归子目录，默认 true</summary>
        public bool Recursive { get; set; } = true;
        /// <summary>是否仅检查（不修改文件），默认 false</summary>
        public bool CheckOnly { get; set; } = false;
        /// <summary>是否执行 Lint 检查（替代格式化），默认 false</summary>
        public bool Lint { get; set; } = false;
        /// <summary>导出当前样式到指定 .sqlstyle 文件路径</summary>
        public string? ExportPath { get; set; }
        /// <summary>从指定 .sqlstyle 文件导入并校验样式</summary>
        public string? ImportPath { get; set; }
        /// <summary>是否将 SELECT * 展开为字段列表，默认 false</summary>
        public bool ExpandStar { get; set; } = false;
        /// <summary>SELECT * 展开所用的表-列元数据 JSON 文件路径</summary>
        public string? MetadataPath { get; set; }
        /// <summary>SQL Server 连接字符串（用于元数据或数据库对象批量格式化检查）</summary>
        public string? DbConnection { get; set; }
        /// <summary>仅启用的 Lint 规则/组（逗号分隔），用于 --lint</summary>
        public string? EnableRules { get; set; }
        /// <summary>禁用的 Lint 规则（逗号分隔），用于 --lint</summary>
        public string? DisableRules { get; set; }
        /// <summary>是否输出详细日志，默认 false</summary>
        public bool Verbose { get; set; } = false;
        /// <summary>是否显示帮助信息，默认 false</summary>
        public bool ShowHelp { get; set; } = false;
        /// <summary>是否显示版本号，默认 false</summary>
        public bool ShowVersion { get; set; } = false;

        /// <summary>
        /// 从命令行参数数组解析选项。
        /// </summary>
        /// <param name="args">命令行参数数组</param>
        /// <returns>填充好的 <see cref="CliOptions"/> 实例</returns>
        /// <exception cref="ArgumentException">参数缺失值或包含未知选项时抛出</exception>
        public static CliOptions Parse(string[] args)
        {
            var options = new CliOptions();

            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];

                switch (arg)
                {
                    case "-s":
                    case "--style":
                        if (i + 1 >= args.Length)
                            throw new ArgumentException($"Option '{arg}' requires a value.");
                        options.StylePath = args[++i];
                        break;

                    case "-f":
                    case "--file":
                        if (i + 1 >= args.Length)
                            throw new ArgumentException($"Option '{arg}' requires a value.");
                        options.FilePath = args[++i];
                        break;

                    case "-o":
                    case "--output":
                        if (i + 1 >= args.Length)
                            throw new ArgumentException($"Option '{arg}' requires a value.");
                        options.OutputPath = args[++i];
                        break;

                    case "-e":
                    case "--encoding":
                        if (i + 1 >= args.Length)
                            throw new ArgumentException($"Option '{arg}' requires a value.");
                        options.Encoding = args[++i];
                        break;

                    case "-r":
                    case "--recursive":
                        options.Recursive = true;
                        break;

                    case "--no-recursive":
                        options.Recursive = false;
                        break;

                    case "--check":
                        options.CheckOnly = true;
                        break;

                    case "--lint":
                        options.Lint = true;
                        break;

                    case "--export":
                        if (i + 1 >= args.Length)
                            throw new ArgumentException($"Option '{arg}' requires a value.");
                        options.ExportPath = args[++i];
                        break;

                    case "--import":
                        if (i + 1 >= args.Length)
                            throw new ArgumentException($"Option '{arg}' requires a value.");
                        options.ImportPath = args[++i];
                        break;

                    case "--expand":
                        options.ExpandStar = true;
                        break;

                    case "--metadata":
                        if (i + 1 >= args.Length)
                            throw new ArgumentException($"Option '{arg}' requires a value.");
                        options.MetadataPath = args[++i];
                        break;

                    case "--db":
                        if (i + 1 >= args.Length)
                            throw new ArgumentException($"Option '{arg}' requires a value.");
                        options.DbConnection = args[++i];
                        break;

                    case "--enable-rules":
                        if (i + 1 >= args.Length)
                            throw new ArgumentException($"Option '{arg}' requires a value.");
                        options.EnableRules = args[++i];
                        break;

                    case "--disable-rules":
                        if (i + 1 >= args.Length)
                            throw new ArgumentException($"Option '{arg}' requires a value.");
                        options.DisableRules = args[++i];
                        break;

                    case "--verbose":
                        options.Verbose = true;
                        break;

                    case "-h":
                    case "--help":
                        options.ShowHelp = true;
                        break;

                    case "--version":
                        options.ShowVersion = true;
                        break;

                    default:
                        // 未知参数：如果是 - 开头则报错，否则当作文件路径
                        if (arg.StartsWith("-"))
                        {
                            throw new ArgumentException($"Unknown option: '{arg}'");
                        }
                        // 未识别的非选项参数当作文件路径（兼容直接传文件路径的用法）
                        if (string.IsNullOrEmpty(options.FilePath))
                        {
                            options.FilePath = arg;
                        }
                        else
                        {
                            throw new ArgumentException($"Unexpected argument: '{arg}'");
                        }
                        break;
                }
            }

            return options;
        }

        /// <summary>
        /// 根据 Encoding 属性值获取对应的 <see cref="System.Text.Encoding"/> 实例。
        /// 支持 utf-8/utf8、gbk、gb2312；无法识别时默认返回 UTF-8。
        /// </summary>
        /// <returns>文件编码实例</returns>
        public System.Text.Encoding GetEncoding()
        {
            string lower = (Encoding ?? "utf-8").ToLowerInvariant();

            if (lower == "utf-8" || lower == "utf8")
            {
                return System.Text.Encoding.UTF8;
            }
            else if (lower == "gbk")
            {
                return System.Text.Encoding.GetEncoding("GBK");
            }
            else if (lower == "gb2312")
            {
                return System.Text.Encoding.GetEncoding("GB2312");
            }
            else
            {
                return System.Text.Encoding.UTF8;
            }
        }
    }
}
