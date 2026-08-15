using System;
using System.ComponentModel.Design;
using System.Text;
using Microsoft.VisualStudio.Shell;
using SqlFM.Core.Batch;
using SqlFM.Editor;
using SqlFM.Localization;
using SqlFM.Services;

namespace SqlFM.Commands
{
    /// <summary>
    /// 格式化文件夹命令处理器（建议 #10 的 VSIX 侧）。
    /// 通过 FolderBrowserDialog 选择目录，调用统一的 FileBatchProcessor 批量格式化，
    /// 以消息框报告统计结果。与 CLI 目录模式共用同一套批量引擎，保证行为一致。
    /// </summary>
    internal sealed class FormatFolderCommand
    {
        /// <summary>
        /// 命令集 GUID，与 VSCT 中定义的 guidSqlFMCmdSet 一致。
        /// </summary>
        public static readonly Guid CommandSet = new Guid("E8F2A3D4-5B6C-7D8E-9F0A-1B2C3D4E5F6A");

        /// <summary>
        /// 命令 ID，与 VSCT 中 FormatFolderCmdId 一致。
        /// </summary>
        public const int CommandId = 0x0108;

        /// <summary>
        /// 命令实例，确保单例。
        /// </summary>
        private static FormatFolderCommand? _instance;

        /// <summary>
        /// 异步包引用，用于获取 DTE 等服务。
        /// </summary>
        private readonly AsyncPackage _package;

        /// <summary>
        /// 私有构造函数，防止外部实例化。
        /// </summary>
        private FormatFolderCommand(AsyncPackage package)
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
                var menuItem = CommandLocalizer.Create(menuCommandId, Execute, "CmdFormatFolder");
                commandService.AddCommand(menuItem);
            }

            _instance = new FormatFolderCommand(package);
        }

        /// <summary>
        /// 公开静态方法，供右键菜单 Click 事件直接调用。
        /// </summary>
        public static void ExecuteFormatFolder()
        {
            Execute(null, EventArgs.Empty);
        }

        /// <summary>
        /// 命令执行回调：选择目录 → 批量格式化 → 弹窗报告结果。
        /// </summary>
        private static void Execute(object? sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            try
            {
                using var dlg = new System.Windows.Forms.FolderBrowserDialog
                {
                    Description = Localizer.Get("CmdFormatFolder"),
                    ShowNewFolderButton = false
                };

                if (dlg.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                {
                    return;
                }

                var processor = new FileBatchProcessor(FormatService.Pipeline);
                var result = processor.ProcessDirectory(
                    dlg.SelectedPath, Encoding.UTF8, null, true, null, false);

                var unchanged = result.SuccessFiles - result.ModifiedFiles;
                var msg = string.Format(
                    Localizer.Get("FolderSummary"),
                    result.TotalFiles, result.ModifiedFiles, unchanged, result.FailedFiles.Count);

                if (result.FailedFiles.Count > 0)
                {
                    var sb = new System.Text.StringBuilder(msg);
                    foreach (var f in result.FailedFiles)
                    {
                        sb.AppendLine("  ✗ " + f.FilePath + " : " + f.Error);
                    }
                    msg = sb.ToString();
                }

                System.Windows.MessageBox.Show(msg, Localizer.Get("MsgTitle"));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"FormatFolderCommand 执行异常: {ex.Message}");
            }
        }
    }
}
