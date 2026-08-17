using System;
using System.Windows.Markup;

namespace SqlFM.Localization
{
    /// <summary>
    /// XAML 本地化标记扩展：<c>{loc:Tr Key=BtnOk}</c>。
    /// 直接返回解析后的字符串（不走 Binding 引擎），避免 SSMS/VS 扩展上下文中
    /// WPF 绑定引擎 IServiceProvider 不完整导致的 NullReferenceException。
    /// <para>
    /// 设计取舍：配置窗口为模态对话框，关闭重开即可切换语言，
    /// 不需要运行时动态绑定刷新。如需动态刷新，可在窗口级监听 LanguageChanged 事件。
    /// </para>
    /// </summary>
    [MarkupExtensionReturnType(typeof(object))]
    public class TrExtension : MarkupExtension
    {
        /// <summary>字符串 key（对应 StringTable）</summary>
        public string Key { get; set; }

        /// <summary>构造（支持 <c>{loc:Tr Key=...}</c> 与 <c>{loc:Tr ...}</c> 两种写法）</summary>
        public TrExtension(string key) => Key = key;

        /// <summary>
        /// 直接返回本地化字符串，绕过 WPF 绑定引擎。
        /// 在 VS/SSMS 扩展宿主中，XAML 的 IServiceProvider 不提供完整的绑定解析服务，
        /// 走 Binding.ProvideValue 会抛 NullReferenceException；直接取值则无此问题。
        /// </summary>
        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return Localizer.Get(Key);
        }
    }
}
