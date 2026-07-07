using System;
using System.Windows.Input;

namespace SqlFM.Options
{
    /// <summary>
    /// 通用 ICommand 实现，用于 MVVM 命令绑定。
    /// 将委托包装为 <see cref="ICommand"/> 接口，供 WPF View 通过 Command 绑定触发。
    /// </summary>
    internal sealed class RelayCommand : ICommand
    {
        // 命令执行委托（带 object? 参数）
        private readonly Action<object?> _execute;
        // 可执行判断委托（null 表示始终可执行）
        private readonly Func<object?, bool>? _canExecute;

        /// <summary>
        /// 初始化 RelayCommand（带参数版本）。
        /// </summary>
        /// <param name="execute">命令执行委托，不可为 null</param>
        /// <param name="canExecute">可执行判断委托；为 null 时命令始终可执行</param>
        public RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        /// <summary>
        /// 初始化 RelayCommand（无参数便捷版本）。
        /// </summary>
        /// <param name="execute">无参数命令执行委托</param>
        /// <param name="canExecute">无参数可执行判断委托；为 null 时命令始终可执行</param>
        public RelayCommand(Action execute, Func<bool>? canExecute = null)
            : this(_ => execute(), canExecute == null ? null : _ => canExecute())
        {
        }

        /// <summary>
        /// CanExecuteChanged 事件：订阅 WPF <see cref="CommandManager.RequerySuggested"/>，
        /// 在 UI 焦点变化等时机自动重新评估 CanExecute。
        /// </summary>
        public event EventHandler? CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        /// <summary>
        /// 判断命令是否可执行。
        /// </summary>
        /// <param name="parameter">命令参数（可为 null）</param>
        /// <returns>无 canExecute 委托时返回 true，否则返回委托执行结果</returns>
        public bool CanExecute(object? parameter) => _canExecute == null || _canExecute(parameter);

        /// <summary>
        /// 执行命令。
        /// </summary>
        /// <param name="parameter">命令参数（可为 null）</param>
        public void Execute(object? parameter) => _execute(parameter);

        /// <summary>手动通知 CanExecute 状态变更。</summary>
        public void RaiseCanExecuteChanged() => CommandManager.InvalidateRequerySuggested();
    }
}
