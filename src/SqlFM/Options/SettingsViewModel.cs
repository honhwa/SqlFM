using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using SqlFM.Core.Configuration;
using SqlFM.Core.Engine;
using SqlFM.Core.PresetStyles;
using SqlFM.Services;

namespace SqlFM.Options
{
    /// <summary>
    /// 配置窗口 ViewModel，绑定所有设置项并驱动实时 SQL 预览。
    /// </summary>
    public class SettingsViewModel : INotifyPropertyChanged
    {
        // ── 示例 SQL（用于实时预览格式化效果）──────────────────────────────
        // 修改此常量可调整配置窗口底部预览区域的输入 SQL
        private const string SampleSql =
            "select t1.id, t1.name, t2.email, t2.phone " +
            "from dbo.Users t1 " +
            "inner join dbo.Contacts t2 on t1.id = t2.user_id " +
            "left join dbo.Orders o on t1.id = o.user_id " +
            "where t1.is_active = 1 and t2.verified = 1 " +
            "group by t1.id, t1.name, t2.email, t2.phone " +
            "having count(*) > 1 " +
            "order by t1.name asc";

        // ── 私有字段 ──────────────────────────────────────────────────────
        private IList<SqlFormatStyle> _allStyles = new List<SqlFormatStyle>();
        private SqlFormatStyle _currentStyle = PresetStyleFactory.CreateDefault();
        private string _previewSql = string.Empty;
        private string _searchText = string.Empty;
        private string _selectedStyleName = "Default";
        private readonly FormatterPipeline _pipeline = new FormatterPipeline();

        // ── 构造 ──────────────────────────────────────────────────────────
        public SettingsViewModel()
        {
            _allStyles = StyleManager.LoadAllStyles();
            StyleNames = new ObservableCollection<string>(_allStyles.Select(s => s.Name));

            // 选中默认样式
            var defaultStyle = StyleManager.GetDefaultStyle();
            _selectedStyleName = defaultStyle.Name;
            LoadStyle(defaultStyle.Clone());

            // 初始化命令
            NewStyleCommand    = new RelayCommand(OnNewStyle);
            CopyStyleCommand   = new RelayCommand(OnCopyStyle);
            RenameStyleCommand = new RelayCommand(OnRenameStyle, () => !IsCurrentStyleSystemPreset);
            DeleteStyleCommand = new RelayCommand(OnDeleteStyle, () => !IsCurrentStyleSystemPreset);
            SetDefaultCommand  = new RelayCommand(OnSetDefault);
            ImportCommand      = new RelayCommand(OnImport);
            ExportCommand      = new RelayCommand(OnExport);
            ApplyCommand       = new RelayCommand(OnApply);
        }

        // ── 样式列表 ──────────────────────────────────────────────────────
        /// <summary>所有可用样式名称的集合（系统预设 + 用户自定义），供下拉框绑定</summary>
        public ObservableCollection<string> StyleNames { get; }

        /// <summary>当前选中的样式名称，切换时自动加载对应样式</summary>
        public string SelectedStyleName
        {
            get => _selectedStyleName;
            set
            {
                if (_selectedStyleName == value) return;
                _selectedStyleName = value;
                OnPropertyChanged();

                var style = _allStyles.FirstOrDefault(s => s.Name == value);
                if (style != null)
                    LoadStyle(style.Clone());
            }
        }

        /// <summary>当前样式是否为系统内置预设（不可删除/重命名）</summary>
        private bool IsCurrentStyleSystemPreset => _currentStyle.IsSystemPreset;

        // ── 搜索文本 ──────────────────────────────────────────────────────
        /// <summary>配置项搜索关键字，实时过滤左侧 Tab 页列表</summary>
        public string SearchText
        {
            get => _searchText;
            set { _searchText = value; OnPropertyChanged(); OnPropertyChanged(nameof(FilteredTabs)); }
        }

        // ── SQL 预览 ──────────────────────────────────────────────────────
        /// <summary>格式化预览结果文本（只读，由配置变更自动刷新）</summary>
        public string PreviewSql
        {
            get => _previewSql;
            private set { _previewSql = value; OnPropertyChanged(); }
        }

        // ── 当前样式（根节点）────────────────────────────────────────────
        /// <summary>当前正在编辑的格式化样式对象</summary>
        public SqlFormatStyle CurrentStyle => _currentStyle;

        // ── 8 大分组的代理属性（供 XAML 直接绑定）────────────────────────
        /// <summary>全局设置（缩进、空行、关键字大小写等）</summary>
        public GlobalSettings Global => _currentStyle.Global;
        /// <summary>DML 设置（SELECT/FROM/JOIN/WHERE 等）</summary>
        public DmlSettings Dml       => _currentStyle.Dml;
        /// <summary>CTE 设置（WITH 语句、递归 CTE 等）</summary>
        public CteSettings Cte       => _currentStyle.Cte;
        /// <summary>CASE 表达式设置（WHEN/THEN/ELSE/END）</summary>
        public CaseSettings Case     => _currentStyle.Case;
        /// <summary>流程控制设置（IF/BEGIN/END、TRY/CATCH 等）</summary>
        public FlowSettings Flow     => _currentStyle.Flow;
        /// <summary>DDL 设置（CREATE TABLE、存储过程等）</summary>
        public DdlSettings Ddl       => _currentStyle.Ddl;
        /// <summary>表达式设置（运算符间距、子查询缩进等）</summary>
        public ExpressionSettings Expression => _currentStyle.Expression;
        /// <summary>T-SQL 专属设置（dbo 架构、临时表等）</summary>
        public TsqlSettings Tsql     => _currentStyle.Tsql;

        // ── 枚举列表（供 ComboBox ItemsSource 绑定）───────────────────────
        /// <summary>缩进类型可选项</summary>
        public IEnumerable<IndentType>    IndentTypes    => Enum.GetValues(typeof(IndentType)).Cast<IndentType>();
        /// <summary>关键字大小写可选项</summary>
        public IEnumerable<KeywordCase>   KeywordCases   => Enum.GetValues(typeof(KeywordCase)).Cast<KeywordCase>();
        /// <summary>对象名大小写可选项</summary>
        public IEnumerable<ObjectNameCase> ObjectNameCases => Enum.GetValues(typeof(ObjectNameCase)).Cast<ObjectNameCase>();
        /// <summary>逗号位置可选项</summary>
        public IEnumerable<CommaPosition> CommaPositions => Enum.GetValues(typeof(CommaPosition)).Cast<CommaPosition>();
        /// <summary>方括号模式可选项</summary>
        public IEnumerable<BracketMode>   BracketModes   => Enum.GetValues(typeof(BracketMode)).Cast<BracketMode>();
        /// <summary>分号模式可选项</summary>
        public IEnumerable<SemicolonMode> SemicolonModes => Enum.GetValues(typeof(SemicolonMode)).Cast<SemicolonMode>();
        /// <summary>AS 关键字模式可选项</summary>
        public IEnumerable<AsKeywordMode> AsKeywordModes => Enum.GetValues(typeof(AsKeywordMode)).Cast<AsKeywordMode>();

        // ── 搜索过滤后的 Tab 名称（未使用时返回全部）─────────────────────
        public IEnumerable<string> FilteredTabs
        {
            get
            {
                var all = new[] { "全局通用", "DML语句", "CTE", "CASE WHEN", "流程控制", "DDL", "表达式", "特殊T-SQL" };
                if (string.IsNullOrWhiteSpace(_searchText))
                    return all;
                return all.Where(t => t.IndexOf(_searchText, StringComparison.OrdinalIgnoreCase) >= 0);
            }
        }

        // ── 命令 ──────────────────────────────────────────────────────────
        /// <summary>新建样式命令</summary>
        public ICommand NewStyleCommand    { get; }
        /// <summary>复制当前样式命令</summary>
        public ICommand CopyStyleCommand   { get; }
        /// <summary>重命名当前样式命令（系统预设不可用）</summary>
        public ICommand RenameStyleCommand { get; }
        /// <summary>删除当前样式命令（系统预设不可用）</summary>
        public ICommand DeleteStyleCommand { get; }
        /// <summary>设为默认样式命令</summary>
        public ICommand SetDefaultCommand  { get; }
        /// <summary>导入 .sqlstyle 文件命令</summary>
        public ICommand ImportCommand      { get; }
        /// <summary>导出当前样式为 .sqlstyle 文件命令</summary>
        public ICommand ExportCommand      { get; }
        /// <summary>应用当前样式到 FormatService 命令</summary>
        public ICommand ApplyCommand       { get; }

        // ── 通知配置变更后刷新预览（由 code-behind 调用）─────────────────
        /// <summary>通知配置项已变更，触发预览重新格式化。</summary>
        public void NotifySettingChanged()
        {
            UpdatePreview();
        }

        /// <summary>
        /// 将当前编辑中的样式应用到 FormatService（持久化在内存）。
        /// </summary>
        public void Apply()
        {
            OnApply();
        }

        // ── 私有方法 ──────────────────────────────────────────────────────

        /// <summary>加载指定样式到当前编辑状态，刷新所有绑定属性和预览。</summary>
        /// <param name="style">要加载的样式（通常是克隆副本）</param>
        private void LoadStyle(SqlFormatStyle style)
        {
            _currentStyle = style;
            OnPropertyChanged(nameof(Global));
            OnPropertyChanged(nameof(Dml));
            OnPropertyChanged(nameof(Cte));
            OnPropertyChanged(nameof(Case));
            OnPropertyChanged(nameof(Flow));
            OnPropertyChanged(nameof(Ddl));
            OnPropertyChanged(nameof(Expression));
            OnPropertyChanged(nameof(Tsql));
            OnPropertyChanged(nameof(CurrentStyle));
            OnPropertyChanged(nameof(IsCurrentStyleSystemPreset));
            UpdatePreview();
        }

        /// <summary>使用当前样式重新格式化示例 SQL，更新预览文本。</summary>
        private void UpdatePreview()
        {
            try
            {
                _pipeline.LoadStyle(_currentStyle);
                var result = _pipeline.Format(SampleSql);
                PreviewSql = result.Success ? result.FormattedSql : result.ErrorMessage ?? SampleSql;
            }
            catch (Exception ex)
            {
                PreviewSql = $"-- 预览失败: {ex.Message}";
            }
        }

        /// <summary>新建样式：弹出输入框，以 Default 为基础创建并保存。</summary>
        private void OnNewStyle()
        {
            var name = PromptInput("新增样式", "请输入新样式名称：", "MyStyle");
            if (string.IsNullOrWhiteSpace(name)) return;
            if (_allStyles.Any(s => s.Name == name))
            {
                MessageBox.Show($"样式名称 '{name}' 已存在。", "SqlFM", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var newStyle = PresetStyleFactory.CreateDefault();
            newStyle.Name = name!;
            newStyle.IsDefault = false;
            newStyle.IsSystemPreset = false;

            StyleManager.SaveStyle(newStyle);
            _allStyles.Add(newStyle);
            StyleNames.Add(name!);
            SelectedStyleName = name!;
        }

        /// <summary>复制当前样式：克隆后重命名并保存。</summary>
        private void OnCopyStyle()
        {
            var name = PromptInput("复制样式", "请输入新样式名称：", _selectedStyleName + "_Copy");
            if (string.IsNullOrWhiteSpace(name)) return;
            if (_allStyles.Any(s => s.Name == name))
            {
                MessageBox.Show($"样式名称 '{name}' 已存在。", "SqlFM", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var copy = _currentStyle.Clone();
            copy.Name = name!;
            copy.IsDefault = false;
            copy.IsSystemPreset = false;

            StyleManager.SaveStyle(copy);
            _allStyles.Add(copy);
            StyleNames.Add(name!);
            SelectedStyleName = name!;
        }

        /// <summary>重命名当前用户样式（系统预设不可重命名）。</summary>
        private void OnRenameStyle()
        {
            if (IsCurrentStyleSystemPreset)
            {
                MessageBox.Show("系统预设样式不可重命名。", "SqlFM", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var oldName = _currentStyle.Name;
            var name = PromptInput("重命名样式", "请输入新名称：", oldName);
            if (string.IsNullOrWhiteSpace(name) || name == oldName) return;
            if (_allStyles.Any(s => s.Name == name))
            {
                MessageBox.Show($"样式名称 '{name}' 已存在。", "SqlFM", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 删除旧文件
            StyleManager.DeleteStyle(oldName);

            // 保存新名称
            _currentStyle.Name = name!;
            StyleManager.SaveStyle(_currentStyle);

            var idx = StyleNames.IndexOf(oldName);
            if (idx >= 0) StyleNames[idx] = name!;

            var styleInList = _allStyles.FirstOrDefault(s => s.Name == oldName);
            if (styleInList != null) styleInList.Name = name!;

            _selectedStyleName = name!;
            OnPropertyChanged(nameof(SelectedStyleName));
        }

        /// <summary>删除当前用户样式（系统预设不可删除），删除后自动切换到首个可用样式。</summary>
        private void OnDeleteStyle()
        {
            if (IsCurrentStyleSystemPreset)
            {
                MessageBox.Show("系统预设样式不可删除。", "SqlFM", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var confirm = MessageBox.Show(
                $"确定要删除样式 '{_currentStyle.Name}' 吗？",
                "SqlFM",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);
            if (confirm != MessageBoxResult.Yes) return;

            var name = _currentStyle.Name;
            StyleManager.DeleteStyle(name);

            var toRemove = _allStyles.FirstOrDefault(s => s.Name == name);
            if (toRemove != null) _allStyles.Remove(toRemove);
            StyleNames.Remove(name);

            SelectedStyleName = StyleNames.FirstOrDefault() ?? "Default";
        }

        /// <summary>将当前样式设为默认样式，持久化到 settings.xml。</summary>
        private void OnSetDefault()
        {
            StyleManager.SetDefaultStyleName(_currentStyle.Name);
            MessageBox.Show($"已将 '{_currentStyle.Name}' 设置为默认样式。",
                "SqlFM", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>从 .sqlstyle 文件导入样式，重名时自动追加 _Imported 后缀。</summary>
        private void OnImport()
        {
            var dlg = new OpenFileDialog
            {
                Title = "导入样式文件",
                Filter = "SQL样式文件 (*.sqlstyle)|*.sqlstyle|所有文件 (*.*)|*.*",
                DefaultExt = ".sqlstyle"
            };
            if (dlg.ShowDialog() != true) return;

            try
            {
                var style = StyleSerializer.LoadFromFile(dlg.FileName);
                style.IsSystemPreset = false;

                // 如果名称已存在，追加后缀
                if (_allStyles.Any(s => s.Name == style.Name))
                    style.Name = style.Name + "_Imported";

                StyleManager.SaveStyle(style);
                _allStyles.Add(style);
                StyleNames.Add(style.Name);
                SelectedStyleName = style.Name;

                MessageBox.Show($"样式 '{style.Name}' 导入成功。",
                    "SqlFM", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"导入失败：{ex.Message}", "SqlFM",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>将当前样式导出为 .sqlstyle 文件。</summary>
        private void OnExport()
        {
            var dlg = new SaveFileDialog
            {
                Title = "导出样式文件",
                Filter = "SQL样式文件 (*.sqlstyle)|*.sqlstyle",
                DefaultExt = ".sqlstyle",
                FileName = _currentStyle.Name
            };
            if (dlg.ShowDialog() != true) return;

            try
            {
                StyleSerializer.SaveToFile(_currentStyle, dlg.FileName);
                MessageBox.Show($"样式已导出至：{dlg.FileName}",
                    "SqlFM", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"导出失败：{ex.Message}", "SqlFM",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>应用当前样式：保存用户自定义样式到磁盘，并同步到全局 FormatService。</summary>
        private void OnApply()
        {
            // 如果是用户自定义样式，保存修改
            if (!_currentStyle.IsSystemPreset)
                StyleManager.SaveStyle(_currentStyle);

            // 将当前样式应用到全局格式化服务
            FormatService.CurrentStyle = _currentStyle;
        }

        // ── 简单输入对话框（无 WPF InputBox，用 MessageBox + Clipboard 模拟）
        //    实际通过独立弱引用窗口实现
        private static string? PromptInput(string title, string prompt, string defaultValue)
        {
            return InputDialog.Show(title, prompt, defaultValue);
        }

        // ── INotifyPropertyChanged ────────────────────────────────────────
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

        /// <summary>
        /// 简单输入对话框（替代 VB InputBox）。
        /// 通过动态构建 WPF Window 实现文本输入交互。
        /// </summary>
        internal static class InputDialog
        {
            /// <summary>
            /// 显示模态输入对话框。
            /// </summary>
            /// <param name="title">窗口标题</param>
            /// <param name="prompt">输入提示文本</param>
            /// <param name="defaultValue">输入框默认值</param>
            /// <returns>用户输入的文本；点击取消时返回 null</returns>
            public static string? Show(string title, string prompt, string defaultValue = "")
        {
            var win = new Window
            {
                Title = title,
                Width = 400,
                Height = 160,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                ResizeMode = ResizeMode.NoResize,
                ShowInTaskbar = false
            };

            var sp = new System.Windows.Controls.StackPanel { Margin = new Thickness(12) };
            var lbl = new System.Windows.Controls.TextBlock { Text = prompt, Margin = new Thickness(0, 0, 0, 6) };
            var txt = new System.Windows.Controls.TextBox { Text = defaultValue, Margin = new Thickness(0, 0, 0, 10) };
            txt.SelectAll();

            var btnSp = new System.Windows.Controls.StackPanel
            {
                Orientation = System.Windows.Controls.Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right
            };
            var ok = new System.Windows.Controls.Button { Content = "确定", Width = 80, IsDefault = true, Margin = new Thickness(0, 0, 8, 0) };
            var cancel = new System.Windows.Controls.Button { Content = "取消", Width = 80, IsCancel = true };

            string? result = null;
            ok.Click += (_, __) => { result = txt.Text; win.Close(); };
            cancel.Click += (_, __) => win.Close();

            btnSp.Children.Add(ok);
            btnSp.Children.Add(cancel);
            sp.Children.Add(lbl);
            sp.Children.Add(txt);
            sp.Children.Add(btnSp);
            win.Content = sp;

            win.ShowDialog();
            return result;
        }
    }
}
