using SqlFM.Core.Configuration;
using SqlFM.Core.Engine;
using SqlFM.Core.PresetStyles;

namespace SqlFM.Services
{
    /// <summary>
    /// VSIX 层格式化服务单例，管理 Pipeline 和 Style。
    /// 所有命令通过此服务调用 Core 库的格式化功能。
    /// </summary>
    internal static class FormatService
    {
        private static readonly FormatterPipeline _pipeline = new FormatterPipeline();
        private static SqlFormatStyle _currentStyle;
        private static readonly CaseConverter _caseConverter = new CaseConverter();

        static FormatService()
        {
            _currentStyle = PresetStyleFactory.CreateDefault();
            _pipeline.LoadStyle(_currentStyle);
        }

        /// <summary>
        /// 格式化管道实例
        /// </summary>
        public static FormatterPipeline Pipeline => _pipeline;

        /// <summary>
        /// 当前格式化样式。设置后自动重新加载到 Pipeline。
        /// </summary>
        public static SqlFormatStyle CurrentStyle
        {
            get => _currentStyle;
            set
            {
                _currentStyle = value ?? PresetStyleFactory.CreateDefault();
                _pipeline.LoadStyle(_currentStyle);
            }
        }

        /// <summary>
        /// 格式化全部 SQL 文本
        /// </summary>
        /// <param name="sql">待格式化的 SQL 文本</param>
        /// <returns>格式化结果</returns>
        public static FormatResult FormatAll(string sql) => _pipeline.Format(sql);

        /// <summary>
        /// 格式化选中的 SQL 文本
        /// </summary>
        /// <param name="sql">待格式化的 SQL 文本</param>
        /// <returns>格式化结果</returns>
        public static FormatResult FormatSelected(string sql) => _pipeline.Format(sql);

        /// <summary>
        /// 将 SQL 中所有关键字转换为大写
        /// </summary>
        /// <param name="sql">待处理的 SQL 文本</param>
        /// <returns>关键字大写后的 SQL 字符串</returns>
        public static string KeywordsToUpper(string sql) => _caseConverter.KeywordsToUpper(sql);

        /// <summary>
        /// 将 SQL 中所有关键字转换为小写
        /// </summary>
        /// <param name="sql">待处理的 SQL 文本</param>
        /// <returns>关键字小写后的 SQL 字符串</returns>
        public static string KeywordsToLower(string sql) => _caseConverter.KeywordsToLower(sql);
    }
}
