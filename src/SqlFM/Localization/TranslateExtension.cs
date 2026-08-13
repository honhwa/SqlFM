using System;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;

namespace SqlFM.Localization
{
    /// <summary>
    /// XAML 本地化标记扩展：<c>{loc:Tr Key=BtnOk}</c>。
    /// 返回一个绑定到 Localizer 索引器的 OneWay Binding，语言切换时自动刷新。
    /// </summary>
    [MarkupExtensionReturnType(typeof(object))]
    public class TrExtension : MarkupExtension
    {
        /// <summary>字符串 key（对应 StringTable）</summary>
        public string Key { get; set; }

        /// <summary>构造（支持 <c>{loc:Tr Key=...}</c> 与 <c>{loc:Tr ...}</c> 两种写法）</summary>
        public TrExtension(string key) => Key = key;

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            var binding = new Binding("Item[]")
            {
                Source = Localizer.Instance,
                Mode = BindingMode.OneWay,
                Path = new PropertyPath($"Item[{Key}]")
            };
            return binding.ProvideValue(serviceProvider);
        }
    }
}
