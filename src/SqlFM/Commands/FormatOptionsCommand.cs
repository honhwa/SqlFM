using System;
using System.ComponentModel.Design;
using Microsoft.VisualStudio.Shell;
using SqlFM.Options;

namespace SqlFM.Commands
{
    /// <summary>
    /// 格式化选项命令处理器。
    /// 打开 SqlFM 选项页（工具 → 选项 → SqlFM）。
    /// </summary>
    internal sealed class FormatOptionsCommand
    {
        /// <summary>
        /// 命令集 GUID，与 VSCT 中定义的 guidSqlFMCmdSet 一致。
        /// </summary>
        public static readonly Guid CommandSet = new Guid("E8F2A3D4-5B6C-7D8E-9F0A-1B2C3D4E5F6A");

        /// <summary>
        /// 命令 ID，与 VSCT 中 FormatOptionsCmdId 一致。
        /// </summary>
        public const int CommandId = 0x0102;

        /// <summary>
        /// 命令实例，确保单例。
        /// </summary>
        private static FormatOptionsCommand? _instance;

        /// <summary>
        /// 异步包引用，用于显示选项页。
        /// </summary>
        private readonly AsyncPackage _package;

        /// <summary>
        /// 私有构造函数，防止外部实例化。
        /// </summary>
        private FormatOptionsCommand(AsyncPackage package)
        {
            _package = package ?? throw new ArgumentNullException(nameof(package));
        }

        /// <summary>
        /// 初始化命令并注册到 OleMenuCommandService。
        /// </summary>
        /// <param name="package">宿主 AsyncPackage 实例</param>
        public static async System.Threading.Tasks.Task InitializeAsync(AsyncPackage package)
        {
            // 切换到 UI 线程
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var commandService = await package.GetServiceAsync(typeof(IMenuCommandService))
                as OleMenuCommandService;

            if (commandService != null)
            {
                // 创建命令标识
                var menuCommandId = new CommandID(CommandSet, CommandId);

                // 创建菜单命令，绑定执行回调
                var menuItem = new MenuCommand(Execute, menuCommandId);

                // 注册命令
                commandService.AddCommand(menuItem);
            }

            // 保存单例实例
            _instance = new FormatOptionsCommand(package);
        }

        /// <summary>
        /// 命令执行回调。
        /// 打开 WPF 配置窗口。
        /// </summary>
        private static void Execute(object? sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            try
            {
                var window = new SettingsWindow();
                window.ShowDialog();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"FormatOptionsCommand 执行异常: {ex.Message}");
            }
        }
    }
}
