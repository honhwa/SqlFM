using System;
using System.IO;
using SqlFM.Core.Configuration;

namespace SqlFM.Options
{
    /// <summary>
    /// 配置导入/导出工具类。
    /// 支持将格式化样式序列化为 .sqlstyle 文件（XML 格式），或从文件导入样式。
    /// 使用 Core 库的 StyleSerializer 进行序列化/反序列化。
    /// </summary>
    public static class OptionsExporter
    {
        /// <summary>
        /// 导出当前样式到指定 .sqlstyle 文件（UTF-8 with BOM）。
        /// </summary>
        /// <param name="style">要导出的样式对象</param>
        /// <param name="filePath">目标文件路径，建议扩展名 .sqlstyle</param>
        /// <exception cref="ArgumentNullException">参数为 null 时抛出</exception>
        /// <exception cref="InvalidOperationException">序列化失败时抛出</exception>
        public static void ExportStyle(SqlFormatStyle style, string filePath)
        {
            if (style == null)
                throw new ArgumentNullException(nameof(style));

            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentNullException(nameof(filePath));

            try
            {
                StyleSerializer.SaveToFile(style, filePath);
            }
            catch (Exception ex) when (ex is not ArgumentNullException)
            {
                throw new InvalidOperationException($"导出样式到文件 '{filePath}' 失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 从指定 .sqlstyle 文件导入样式。
        /// </summary>
        /// <param name="filePath">源文件路径</param>
        /// <returns>反序列化得到的 SqlFormatStyle 实例</returns>
        /// <exception cref="ArgumentNullException">filePath 为 null 时抛出</exception>
        /// <exception cref="FileNotFoundException">文件不存在时抛出</exception>
        /// <exception cref="InvalidOperationException">反序列化失败时抛出</exception>
        public static SqlFormatStyle ImportStyle(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentNullException(nameof(filePath));

            if (!File.Exists(filePath))
                throw new FileNotFoundException($"样式文件未找到: {filePath}", filePath);

            try
            {
                return StyleSerializer.LoadFromFile(filePath);
            }
            catch (Exception ex) when (ex is not ArgumentNullException and not FileNotFoundException)
            {
                throw new InvalidOperationException($"从文件 '{filePath}' 导入样式失败: {ex.Message}", ex);
            }
        }
    }
}
