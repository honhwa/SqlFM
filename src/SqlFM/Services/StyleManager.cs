using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;
using SqlFM.Core.Configuration;
using SqlFM.Core.PresetStyles;

namespace SqlFM.Services
{
    /// <summary>
    /// 样式管理器：负责样式的持久化读写、默认样式管理。
    /// 用户自定义样式保存在 %AppData%\SqlFM\Styles\*.sqlstyle。
    /// 当前默认样式名保存在 %AppData%\SqlFM\settings.xml。
    /// </summary>
    internal static class StyleManager
    {
        private static readonly string AppDataDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SqlFM");

        private static readonly string StylesDir = Path.Combine(AppDataDir, "Styles");
        private static readonly string SettingsFile = Path.Combine(AppDataDir, "settings.xml");

        /// <summary>
        /// 加载所有样式（系统预设 + 用户自定义）。
        /// 系统预设始终位于列表前面，IsSystemPreset = true。
        /// </summary>
        public static IList<SqlFormatStyle> LoadAllStyles()
        {
            var result = new List<SqlFormatStyle>();

            // 先加入系统预设
            result.AddRange(PresetStyleFactory.GetAllPresets());

            // 再加入用户自定义
            if (Directory.Exists(StylesDir))
            {
                foreach (var file in Directory.GetFiles(StylesDir, "*.sqlstyle")
                                              .OrderBy(f => f))
                {
                    try
                    {
                        var style = StyleSerializer.LoadFromFile(file);
                        // 避免与预设重名
                        if (result.All(s => s.Name != style.Name))
                        {
                            result.Add(style);
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine(
                            $"[StyleManager] 加载样式失败 ({file}): {ex.Message}");
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// 保存用户自定义样式到磁盘（系统预设不可保存，直接忽略）。
        /// </summary>
        /// <param name="style">要保存的样式对象</param>
        /// <exception cref="ArgumentNullException">style 为 null 时抛出</exception>
        public static void SaveStyle(SqlFormatStyle style)
        {
            if (style == null) throw new ArgumentNullException(nameof(style));
            if (style.IsSystemPreset)
                return; // 系统预设只读，不写磁盘

            EnsureDirectories();
            var filePath = GetStyleFilePath(style.Name);
            StyleSerializer.SaveToFile(style, filePath);
        }

        /// <summary>
        /// 删除指定名称的用户自定义样式文件（系统预设不可删除）。
        /// </summary>
        /// <param name="name">要删除的样式名称</param>
        public static void DeleteStyle(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return;

            var filePath = GetStyleFilePath(name);
            if (File.Exists(filePath))
                File.Delete(filePath);
        }

        /// <summary>
        /// 获取当前默认样式（按 settings.xml 中记录的名称）。
        /// 如果没有配置或对应样式不存在，则返回 Default 预设。
        /// </summary>
        public static SqlFormatStyle GetDefaultStyle()
        {
            var defaultName = LoadDefaultStyleName();
            var all = LoadAllStyles();
            return all.FirstOrDefault(s => s.Name == defaultName)
                   ?? PresetStyleFactory.CreateDefault();
        }

        /// <summary>
        /// 将指定名称设置为默认样式，并写入 settings.xml。
        /// </summary>
        /// <param name="name">要设为默认的样式名称</param>
        public static void SetDefaultStyleName(string name)
        {
            EnsureDirectories();
            var settings = new AppSettings { DefaultStyleName = name };
            var serializer = new XmlSerializer(typeof(AppSettings));
            var xmlSettings = new XmlWriterSettings { Indent = true };
            using (var writer = XmlWriter.Create(SettingsFile, xmlSettings))
            {
                serializer.Serialize(writer, settings);
            }
        }

        // ── 内部工具 ──────────────────────────────────────────────────────────

        private static string LoadDefaultStyleName()
        {
            if (!File.Exists(SettingsFile))
                return "Default";

            try
            {
                var serializer = new XmlSerializer(typeof(AppSettings));
                using (var reader = new StreamReader(SettingsFile))
                {
                    var settings = (AppSettings?)serializer.Deserialize(reader);
                    return settings?.DefaultStyleName ?? "Default";
                }
            }
            catch
            {
                return "Default";
            }
        }

        private static string GetStyleFilePath(string styleName)
        {
            // 将样式名中的非法字符替换为下划线
            var safe = string.Join("_",
                styleName.Split(Path.GetInvalidFileNameChars()));
            return Path.Combine(StylesDir, safe + ".sqlstyle");
        }

        private static void EnsureDirectories()
        {
            if (!Directory.Exists(StylesDir))
                Directory.CreateDirectory(StylesDir);
        }
    }

    /// <summary>应用程序持久化设置（最小集）。</summary>
    [XmlRoot("SqlFMSettings")]
    public class AppSettings
    {
        /// <summary>默认样式名称，序列化到 settings.xml，启动时读取</summary>
        [XmlElement]
        public string DefaultStyleName { get; set; } = "Default";
    }
}
