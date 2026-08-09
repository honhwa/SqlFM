using System;
using System.Globalization;
using System.Windows.Data;

namespace SqlFM.Localization
{
    /// <summary>
    /// 枚举值 → 本地化显示名转换器。
    /// 读取规则：Enum_{类型名}_{成员名}，如 Enum_IndentType_Spaces。
    /// 用于 ComboBox 的 ItemTemplate，使下拉显示本地化文本而 SelectedItem 仍是枚举值。
    /// </summary>
    public class EnumLocalizedConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return string.Empty;
            var key = $"Enum_{value.GetType().Name}_{value}";
            return Localizer.Get(key);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
