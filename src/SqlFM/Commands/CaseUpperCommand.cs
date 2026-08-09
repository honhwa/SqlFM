using System;
using System.ComponentModel.Design;
using Microsoft.VisualStudio.Shell;
using SqlFM.Editor;
using SqlFM.Services;
using SqlFM.Localization;

namespace SqlFM.Commands
{
    /// <summary>
    /// 关键字大写命令处理器。
    /// 将 SQL 中所有关键字转换为大写（快捷键 Ctrl+B,Ctrl+U）。
    /// </summary>
    internal sealed class CaseUpperCommand
    {
        /// <summary>
        /// 命令集 GUID，与 VSCT 中定义的 guidSqlFMCmdSet 一致。
        /// </summary>
        public static readonly Guid CommandSet = new Guid("E8F2A3D4-5B6C-7D8E-9F0A-1B2C3D4E5F6A");

        /// <summary>
        /// 命令 ID，与 VSCT 中 CaseUpperCmdId 一致。
        /// </summary>
        public const int CommandId = 0x0103;

        /// <summary>
        /// 命令实例，确保单例。
        /// </summary>
        private static CaseUpperCommand? _instance;

        /// <summary>
        /// 异步包引用，用于获取 DTE 等服务。
        /// </summary>
        private readonly AsyncPackage _package;

        /// <summary>
        /// 私有构造函数，防止外部实例化。
        /// </summary>
        private CaseUpperCommand(AsyncPackage package)
        {
            _package = package ?? throw new ArgumentNullException(nameof(package));
        }

        /// <summary>
        /// 初始化命令并注册到 OleMenuCommandService。
        /// </summary>
        /// <param name="package">宿主 AsyncPackage 实例</param>
        public static async System.Threading.Tasks.Task InitializeAsync(AsyncPackage package)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var commandService = await package.GetServiceAsync(typeof(IMenuCommandService))
                as OleMenuCommandService;

            if (commandService != null)
            {
                var menuCommandId = new CommandID(CommandSet, CommandId);
                var menuItem = CommandLocalizer.Create(menuCommandId, Execute, "CmdCaseUpper");
                commandService.AddCommand(menuItem);
            }

            _instance = new CaseUpperCommand(package);
        }

        /// <summary>
        /// 公开静态方法，供右键菜单 Click 事件直接调用。
        /// </summary>
        public static void ExecuteCaseUpper()
        {
            Execute(null, EventArgs.Empty);
        }

        /// <summary>
        /// 命令执行回调。
        /// 获取当前选中文本或全文，将关键字转为大写后替换。
        /// </summary>
        private static void Execute(object? sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            try
            {
                var dte = (EnvDTE80.DTE2?)ThreadHelper.JoinableTaskFactory.Run(
                    () => _instance!._package.GetServiceAsync(typeof(EnvDTE.DTE)));

                if (dte == null || !EditorHelper.HasActiveTextDocument(dte))
                {
                    return;
                }

                // 优先处理选中文本，若没有选中则处理全文
                string? selectedText = EditorHelper.GetSelectedText(dte);
                if (!string.IsNullOrEmpty(selectedText))
                {
                    string converted = FormatService.KeywordsToUpper(selectedText!);
                    EditorHelper.ReplaceSelectedText(dte, converted);
                }
                else
                {
                    string? allText = EditorHelper.GetAllText(dte);
                    if (!string.IsNullOrEmpty(allText))
                    {
                        string converted = FormatService.KeywordsToUpper(allText!);
                        EditorHelper.ReplaceAllText(dte, converted);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CaseUpperCommand 执行异常: {ex.Message}");
            }
        }
    }
}
