using System;
using System.ComponentModel.Design;
using Microsoft.VisualStudio.Shell;
using SqlFM.Editor;

namespace SqlFM.Commands
{
    /// <summary>
    /// 插入豁免标记命令处理器。
    /// 在光标位置插入 FORMAT OFF/ON 标记对（快捷键 Ctrl+D,Ctrl+I）。
    /// </summary>
    internal sealed class InsertExemptionCommand
    {
        /// <summary>
        /// 命令集 GUID，与 VSCT 中定义的 guidSqlFMCmdSet 一致。
        /// </summary>
        public static readonly Guid CommandSet = new Guid("E8F2A3D4-5B6C-7D8E-9F0A-1B2C3D4E5F6A");

        /// <summary>
        /// 命令 ID，与 VSCT 中 InsertExemptionCmdId 一致。
        /// </summary>
        public const int CommandId = 0x0105;

        /// <summary>
        /// 命令实例，确保单例。
        /// </summary>
        private static InsertExemptionCommand? _instance;

        /// <summary>
        /// 异步包引用，用于获取 DTE 等服务。
        /// </summary>
        private readonly AsyncPackage _package;

        /// <summary>
        /// 私有构造函数，防止外部实例化。
        /// </summary>
        private InsertExemptionCommand(AsyncPackage package)
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
                var menuItem = new MenuCommand(Execute, menuCommandId);
                commandService.AddCommand(menuItem);
            }

            _instance = new InsertExemptionCommand(package);
        }

        /// <summary>
        /// 公开静态方法，供右键菜单 Click 事件直接调用。
        /// </summary>
        public static void ExecuteInsertExemption()
        {
            Execute(null, EventArgs.Empty);
        }

        /// <summary>
        /// 命令执行回调。
        /// 在当前光标位置插入 FORMAT OFF/ON 标记对。
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

                // 插入 FORMAT OFF/ON 标记对
                string exemptionBlock = $"{Environment.NewLine}/* FORMAT OFF */{Environment.NewLine}{Environment.NewLine}/* FORMAT ON */{Environment.NewLine}";

                var textDoc = (EnvDTE.TextDocument)dte.ActiveDocument!.Object("TextDocument")!;
                var selection = textDoc.Selection;
                selection?.Insert(exemptionBlock);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"InsertExemptionCommand 执行异常: {ex.Message}");
            }
        }
    }
}
