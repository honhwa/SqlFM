using System;
using System.ComponentModel;
using System.Globalization;

namespace SqlFM.Localization
{
    /// <summary>
    /// 界面语言枚举。
    /// Auto 表示跟随系统区域（中文系统显示中文，其他显示英文）。
    /// </summary>
    public enum UiLanguage
    {
        /// <summary>简体中文</summary>
        ZhCn,
        /// <summary>英文</summary>
        En,
        /// <summary>跟随系统区域</summary>
        Auto
    }

    /// <summary>
    /// 轻量级本地化服务（不依赖 resx satellite，运行时自由切换中英文）。
    /// XAML 通过索引器绑定实现动态刷新；命令/代码通过静态 Get 方法取文本。
    /// </summary>
    public class Localizer : INotifyPropertyChanged
    {
        private static readonly Localizer _instance = new Localizer();
        /// <summary>全局单例</summary>
        public static Localizer Instance => _instance;

        private UiLanguage _language = UiLanguage.Auto;

        /// <summary>当前界面语言（包含 Auto 在内的原始选择）。</summary>
        public UiLanguage Language
        {
            get => _language;
            set
            {
                if (_language == value) return;
                _language = value;
                OnLanguageChanged();
            }
        }

        /// <summary>索引器：供 XAML 绑定 <c>{loc:Tr Key=...}</c> 动态取文本。</summary>
        /// <param name="key">字符串 key</param>
        public string this[string key] => Get(key);

        /// <summary>语言切换事件（命令文本等订阅以刷新）。</summary>
        public event EventHandler? LanguageChanged;

        /// <summary>索引器属性变更事件（供绑定刷新）。</summary>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// 静态取文本：按当前语言返回，缺失时回退英文，再缺失时返回 key 本身。
        /// </summary>
        /// <param name="key">字符串 key</param>
        public static string Get(string key)
        {
            if (string.IsNullOrEmpty(key)) return string.Empty;
            var effective = Instance.ResolveEffective();
            var dict = effective == UiLanguage.En ? StringTable.En : StringTable.ZhCn;
            if (dict.TryGetValue(key, out var v) && !string.IsNullOrEmpty(v))
                return v;
            // 回退到英文
            if (effective != UiLanguage.En && StringTable.En.TryGetValue(key, out var en) && !string.IsNullOrEmpty(en))
                return en;
            return key;
        }

        /// <summary>把设置字符串解析为 UiLanguage（无效值回退 Auto）。</summary>
        public static UiLanguage ParseSetting(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return UiLanguage.Auto;
            return value.Trim().ToLowerInvariant() switch
            {
                "zh-cn" or "zh_cn" or "zhcn" or "zh" => UiLanguage.ZhCn,
                "en" or "en-us" or "eng" => UiLanguage.En,
                _ => UiLanguage.Auto
            };
        }

        /// <summary>把 UiLanguage 转为设置字符串。</summary>
        public static string ToSettingString(UiLanguage lang) => lang switch
        {
            UiLanguage.ZhCn => "zh-Cn",
            UiLanguage.En => "en",
            _ => "auto"
        };

        private UiLanguage ResolveEffective()
        {
            if (_language != UiLanguage.Auto)
                return _language;
            var name = CultureInfo.CurrentUICulture.Name;
            return name.StartsWith("zh", StringComparison.OrdinalIgnoreCase)
                ? UiLanguage.ZhCn
                : UiLanguage.En;
        }

        private void OnLanguageChanged()
        {
            // 通知所有索引器绑定刷新
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
            LanguageChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
