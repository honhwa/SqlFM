using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using SqlFM.Localization;
using SqlFM.Services;

namespace SqlFM.Options
{
    /// <summary>
    /// WPF 配置窗口 code-behind。
    /// 所有业务逻辑委托给 <see cref="SettingsViewModel"/>。
    /// </summary>
    public partial class SettingsWindow : Window
    {
        private readonly SettingsViewModel _vm;

        public SettingsWindow()
        {
            InitializeComponent();
            _vm = new SettingsViewModel();
            DataContext = _vm;

            // 初始化语言下拉选中项（与当前生效语言一致）
            var current = Localizer.Instance.Language;
            foreach (ComboBoxItem item in LanguageCombo.Items)
            {
                if (Enum.TryParse<UiLanguage>(item.Tag?.ToString(), out var lang) && lang == current)
                {
                    LanguageCombo.SelectedItem = item;
                    break;
                }
            }
        }

        /// <summary>任意配置项变更时刷新预览。</summary>
        private void Setting_Changed(object sender, EventArgs e)
        {
            _vm.NotifySettingChanged();
        }

        /// <summary>语言切换：持久化偏好并刷新所有枚举下拉的本地化显示。</summary>
        private void LanguageCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (LanguageCombo.SelectedItem is not ComboBoxItem item) return;
            if (!Enum.TryParse<UiLanguage>(item.Tag?.ToString(), out var lang)) return;

            StyleManager.SaveInterfaceLanguage(lang);
            Localizer.Instance.Language = lang;

            // 刷新枚举下拉的本地化显示名（Tr 文本由绑定自动刷新，枚举项需手动刷新）
            foreach (var cb in FindVisualChildren<ComboBox>(this))
            {
                if (cb == LanguageCombo) continue;
                cb.Items.Refresh();
            }
        }

        /// <summary>确定：应用并关闭。</summary>
        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            _vm.Apply();
            DialogResult = true;
            Close();
        }

        /// <summary>取消：不保存，直接关闭。</summary>
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        /// <summary>应用：保存但不关闭窗口。</summary>
        private void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            _vm.Apply();
        }

        /// <summary>遍历视觉树收集指定类型的子元素。</summary>
        private static System.Collections.Generic.IEnumerable<T> FindVisualChildren<T>(DependencyObject root)
            where T : DependencyObject
        {
            if (root == null) yield break;
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
            {
                var child = VisualTreeHelper.GetChild(root, i);
                if (child is T t)
                    yield return t;
                foreach (var descendant in FindVisualChildren<T>(child))
                    yield return descendant;
            }
        }
    }
}
