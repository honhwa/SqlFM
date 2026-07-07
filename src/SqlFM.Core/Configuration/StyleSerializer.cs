using System;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace SqlFM.Core.Configuration
{
    /// <summary>
    /// SQL 格式化样式的 XML 序列化/反序列化工具类。
    /// 文件扩展名约定为 .sqlstyle，编码为 UTF-8 with BOM。
    /// </summary>
    public static class StyleSerializer
    {
        private static readonly XmlSerializer _serializer = new XmlSerializer(typeof(SqlFormatStyle));

        /// <summary>
        /// 将样式序列化并保存至文件（UTF-8 with BOM）。
        /// </summary>
        /// <param name="style">要保存的样式对象</param>
        /// <param name="filePath">目标文件路径，建议扩展名 .sqlstyle</param>
        /// <exception cref="ArgumentNullException">style 或 filePath 为 null 时抛出</exception>
        public static void SaveToFile(SqlFormatStyle style, string filePath)
        {
            if (style == null) throw new ArgumentNullException(nameof(style));
            if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentNullException(nameof(filePath));

            var dir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var settings = new XmlWriterSettings
            {
                Indent = true,
                IndentChars = "    ",
                Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true),
                NewLineChars = "\r\n",
                NewLineHandling = NewLineHandling.Replace
            };

            using (var writer = XmlWriter.Create(filePath, settings))
            {
                _serializer.Serialize(writer, style);
            }
        }

        /// <summary>
        /// 从文件加载样式（自动检测编码）。
        /// </summary>
        /// <param name="filePath">源文件路径</param>
        /// <returns>反序列化得到的 <see cref="SqlFormatStyle"/> 实例</returns>
        /// <exception cref="ArgumentNullException">filePath 为 null 时抛出</exception>
        /// <exception cref="FileNotFoundException">文件不存在时抛出</exception>
        public static SqlFormatStyle LoadFromFile(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentNullException(nameof(filePath));
            if (!File.Exists(filePath)) throw new FileNotFoundException("样式文件不存在。", filePath);

            using (var reader = new StreamReader(filePath, detectEncodingFromByteOrderMarks: true))
            {
                return (SqlFormatStyle)_serializer.Deserialize(reader);
            }
        }

        /// <summary>
        /// 将样式序列化为 XML 字符串。
        /// </summary>
        /// <param name="style">要序列化的样式对象</param>
        /// <returns>XML 格式字符串</returns>
        public static string SerializeToString(SqlFormatStyle style)
        {
            if (style == null) throw new ArgumentNullException(nameof(style));

            var settings = new XmlWriterSettings
            {
                Indent = true,
                IndentChars = "    ",
                Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
                OmitXmlDeclaration = false,
                NewLineChars = "\r\n",
                NewLineHandling = NewLineHandling.Replace
            };

            using (var ms = new MemoryStream())
            using (var writer = XmlWriter.Create(ms, settings))
            {
                _serializer.Serialize(writer, style);
                writer.Flush();
                return Encoding.UTF8.GetString(ms.ToArray());
            }
        }

        /// <summary>
        /// 从 XML 字符串反序列化为样式对象。
        /// </summary>
        /// <param name="xml">XML 格式字符串</param>
        /// <returns>反序列化得到的 <see cref="SqlFormatStyle"/> 实例</returns>
        public static SqlFormatStyle DeserializeFromString(string xml)
        {
            if (string.IsNullOrWhiteSpace(xml)) throw new ArgumentNullException(nameof(xml));

            using (var reader = new StringReader(xml))
            {
                return (SqlFormatStyle)_serializer.Deserialize(reader);
            }
        }
    }
}
