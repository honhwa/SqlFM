using System;
using System.Windows;

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
        }

        /// <summary>任意配置项变更时刷新预览。</summary>
        private void Setting_Changed(object sender, EventArgs e)
        {
            _vm.NotifySettingChanged();
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
    }
}
