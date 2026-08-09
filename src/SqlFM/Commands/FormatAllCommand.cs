using System;
using System.ComponentModel.Design;
using Microsoft.VisualStudio.Shell;
using SqlFM.Editor;
using SqlFM.Services;
using SqlFM.Localization;

namespace SqlFM.Commands
{
    /// <summary>
    /// 格式化全部 SQL 命令处理器。
    /// 获取全文 → 调用 FormatService → 替换全文。
    /// </summary>
    internal sealed class FormatAllCommand
    {
        /// <summary>
        /// 命令集 GUID，与 VSCT 中定义的 guidSqlFMCmdSet 一致。
        /// </summary>
        public static readonly Guid CommandSet = new Guid("E8F2A3D4-5B6C-7D8E-9F0A-1B2C3D4E5F6A");

        /// <summary>
        /// 命令 ID，与 VSCT 中 FormatAllCmdId 一致。
        /// </summary>
        public const int CommandId = 0x0101;

        /// <summary>
        /// 命令实例，确保单例。
        /// </summary>
        private static FormatAllCommand? _instance;

        /// <summary>
        /// 异步包引用，用于获取 DTE 等服务。
        /// </summary>
        private readonly AsyncPackage _package;

        /// <summary>
        /// 私有构造函数，防止外部实例化。
        /// </summary>
        private FormatAllCommand(AsyncPackage package)
        {
            _package = package ?? throw new ArgumentNullException(nameof(package));
        }

        /// <summary>
        /// 初始化命令并注册到 OleMenuCommandService。
        /// </summary>
        /// <param name="package">宿主 AsyncPackage 实例</param>
        public static async System.Threading.Tasks.Task InitializeAsync(AsyncPackage package)
        {
            // 切换到 UI 线程，因为菜单命令服务只能在 UI 线程上操作
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var commandService = await package.GetServiceAsync(typeof(IMenuCommandService))
                as OleMenuCommandService;

            if (commandService != null)
            {
                // 创建命令标识
                var menuCommandId = new CommandID(CommandSet, CommandId);

                // 创建菜单命令，绑定执行回调
                var menuItem = CommandLocalizer.Create(menuCommandId, Execute, "CmdFormatAll");

                // 注册命令
                commandService.AddCommand(menuItem);
            }

            // 保存单例实例
            _instance = new FormatAllCommand(package);
        }

        /// <summary>
        /// 公开静态方法，供右键菜单 Click 事件直接调用。
        /// </summary>
        public static void ExecuteFormat()
        {
            Execute(null, EventArgs.Empty);
        }

        /// <summary>
        /// 命令执行回调。
        /// 获取当前文档的全部 SQL 文本，通过 FormatService 调用 Core 格式化引擎，然后替换全文。
        /// </summary>
        private static void Execute(object? sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            try
            {
                // 获取 DTE2 实例（Execute 在 UI 线程，使用 JoinableTaskFactory.Run 避免死锁）
                var dte = (EnvDTE80.DTE2?)ThreadHelper.JoinableTaskFactory.Run(
                    () => _instance!._package.GetServiceAsync(typeof(EnvDTE.DTE)));

                if (dte == null || !EditorHelper.HasActiveTextDocument(dte))
                {
                    return;
                }

                // 获取全文
                string? allText = EditorHelper.GetAllText(dte);
                if (string.IsNullOrEmpty(allText))
                {
                    return;
                }

                // 调用 FormatService 格式化全文
                var result = FormatService.FormatAll(allText!);

                if (result.Success)
                {
                    // 替换全文为格式化后的结果
                    EditorHelper.ReplaceAllText(dte, result.FormattedSql);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"FormatAllCommand 格式化失败: {result.ErrorMessage}");
                }
            }
            catch (Exception ex)
            {
                // 记录异常，避免命令执行失败导致 SSMS 崩溃
                System.Diagnostics.Debug.WriteLine($"FormatAllCommand 执行异常: {ex.Message}");
            }
        }
    }
}
