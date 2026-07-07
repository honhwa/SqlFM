using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using SqlFM.Core.Engine;

namespace SqlFM.Core.Batch
{
    /// <summary>
    /// 本地文件夹批量格式化处理器
    /// </summary>
    public class FileBatchProcessor
    {
        private readonly FormatterPipeline _pipeline;

        /// <summary>
        /// 初始化批量处理器
        /// </summary>
        /// <param name="pipeline">格式化管道实例</param>
        public FileBatchProcessor(FormatterPipeline pipeline)
        {
            _pipeline = pipeline;
        }

        /// <summary>
        /// 批量格式化目录下所有 .sql 文件
        /// </summary>
        /// <param name="directoryPath">目标目录</param>
        /// <param name="encoding">文件编码（UTF8/GBK等）</param>
        /// <param name="outputDirectory">输出目录（null 则覆盖原文件）</param>
        /// <param name="recursive">是否递归子目录</param>
        /// <param name="progress">进度回调(当前文件索引, 总数, 文件路径)</param>
        /// <returns>处理结果摘要</returns>
        public BatchResult ProcessDirectory(
            string directoryPath,
            Encoding encoding,
            string? outputDirectory = null,
            bool recursive = true,
            Action<int, int, string>? progress = null)
        {
            var result = new BatchResult();
            var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            var files = Directory.GetFiles(directoryPath, "*.sql", searchOption);
            result.TotalFiles = files.Length;

            for (int i = 0; i < files.Length; i++)
            {
                var filePath = files[i];
                progress?.Invoke(i + 1, files.Length, filePath);

                try
                {
                    var content = File.ReadAllText(filePath, encoding);
                    var formatResult = _pipeline.Format(content);

                    if (formatResult.Success)
                    {
                        var outputPath = outputDirectory != null
                            ? Path.Combine(outputDirectory, GetRelativePath(directoryPath, filePath))
                            : filePath;

                        if (outputDirectory != null)
                        {
                            var dir = Path.GetDirectoryName(outputPath);
                            if (dir != null && !Directory.Exists(dir))
                                Directory.CreateDirectory(dir);
                        }

                        File.WriteAllText(outputPath, formatResult.FormattedSql, encoding);
                        result.SuccessFiles++;

                        if (content != formatResult.FormattedSql)
                            result.ModifiedFiles++;
                    }
                    else
                    {
                        result.FailedFiles.Add(new BatchFailure
                        {
                            FilePath = filePath,
                            Error = formatResult.ErrorMessage ?? "Unknown error"
                        });
                    }
                }
                catch (Exception ex)
                {
                    result.FailedFiles.Add(new BatchFailure
                    {
                        FilePath = filePath,
                        Error = ex.Message
                    });
                }
            }

            return result;
        }

        /// <summary>
        /// 获取 <paramref name="filePath"/> 相对于 <paramref name="basePath"/> 的相对路径。
        /// 兼容 .NET Framework 4.8（使用 Uri 方式替代 Path.GetRelativePath）。
        /// </summary>
        /// <param name="basePath">基准目录</param>
        /// <param name="filePath">目标文件完整路径</param>
        /// <returns>相对路径字符串</returns>
        private static string GetRelativePath(string basePath, string filePath)
        {
            // 确保 basePath 以目录分隔符结尾，Uri 才能正确计算相对路径
            if (!basePath.EndsWith(Path.DirectorySeparatorChar.ToString())
                && !basePath.EndsWith(Path.AltDirectorySeparatorChar.ToString()))
            {
                basePath += Path.DirectorySeparatorChar;
            }

            var baseUri = new Uri(basePath);
            var fileUri = new Uri(filePath);

            if (baseUri.Scheme != fileUri.Scheme)
                return filePath; // 跨驱动器时直接返回绝对路径

            var relativeUri = baseUri.MakeRelativeUri(fileUri);
            // Uri 使用正斜杠，需转换为平台路径分隔符
            return Uri.UnescapeDataString(relativeUri.ToString())
                .Replace('/', Path.DirectorySeparatorChar);
        }
    }

    /// <summary>
    /// 批量处理结果摘要
    /// </summary>
    public class BatchResult
    {
        /// <summary>扫描到的文件总数</summary>
        public int TotalFiles { get; set; }

        /// <summary>处理成功的文件数</summary>
        public int SuccessFiles { get; set; }

        /// <summary>内容实际被修改的文件数</summary>
        public int ModifiedFiles { get; set; }

        /// <summary>处理失败的文件列表</summary>
        public List<BatchFailure> FailedFiles { get; set; } = new List<BatchFailure>();
    }

    /// <summary>
    /// 单个文件处理失败的详情
    /// </summary>
    public class BatchFailure
    {
        /// <summary>文件路径或对象名</summary>
        public string FilePath { get; set; } = string.Empty;

        /// <summary>失败原因</summary>
        public string Error { get; set; } = string.Empty;
    }
}
