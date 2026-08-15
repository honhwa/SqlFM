using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using Microsoft.VisualStudio.Shell;
using SqlFM.Core.Engine;
using SqlFM.Core.Refactoring;
using SqlFM.Editor;
using SqlFM.Localization;
using SqlFM.Services;

namespace SqlFM.Commands
{
    /// <summary>
    /// 展开 SELECT * 命令处理器（建议 #9）。
    /// 通过 OpenFileDialog 选择表-列元数据 JSON 文件，调用 StarExpander 将 SELECT *
    /// 展开为完整字段列表，并替换活动文档全文。无元数据或未匹配到表时给出提示，不做破坏性修改。
    /// </summary>
    internal sealed class ExpandStarCommand
    {
        /// <summary>
        /// 命令集 GUID，与 VSCT 中定义的 guidSqlFMCmdSet 一致。
        /// </summary>
        public static readonly Guid CommandSet = new Guid("E8F2A3D4-5B6C-7D8E-9F0A-1B2C3D4E5F6A");

        /// <summary>
        /// 命令 ID，与 VSCT 中 ExpandStarCmdId 一致。
        /// </summary>
        public const int CommandId = 0x0107;

        /// <summary>
        /// 命令实例，确保单例。
        /// </summary>
        private static ExpandStarCommand? _instance;

        /// <summary>
        /// 异步包引用，用于获取 DTE 等服务。
        /// </summary>
        private readonly AsyncPackage _package;

        /// <summary>
        /// 私有构造函数，防止外部实例化。
        /// </summary>
        private ExpandStarCommand(AsyncPackage package)
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
                var menuItem = CommandLocalizer.Create(menuCommandId, Execute, "CmdExpandStar");
                commandService.AddCommand(menuItem);
            }

            _instance = new ExpandStarCommand(package);
        }

        /// <summary>
        /// 公开静态方法，供右键菜单 Click 事件直接调用。
        /// </summary>
        public static void ExecuteExpand()
        {
            Execute(null, EventArgs.Empty);
        }

        /// <summary>
        /// 命令执行回调：选择元数据 → 展开 SELECT * → 替换全文。
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

                string? allText = EditorHelper.GetAllText(dte);
                if (string.IsNullOrEmpty(allText))
                {
                    return;
                }

                // 选择表-列元数据 JSON 文件
                var dlg = new Microsoft.Win32.OpenFileDialog
                {
                    Title = Localizer.Get("CmdExpandStar"),
                    Filter = "JSON 元数据 (*.json)|*.json|所有文件 (*.*)|*.*",
                    CheckFileExists = true
                };

                if (dlg.ShowDialog() != true)
                {
                    System.Windows.MessageBox.Show(
                        Localizer.Get("ExpandNoMeta"),
                        Localizer.Get("MsgTitle"));
                    return;
                }

                IDictionary<string, IList<string>> tableColumns;
                try
                {
                    tableColumns = MetadataProvider.FromJson(dlg.FileName);
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show(
                        string.Format(Localizer.Get("ExpandLoadFail"), ex.Message),
                        Localizer.Get("MsgTitle"));
                    return;
                }

                var expander = new StarExpander(new ScriptDomEngine());
                string expanded = expander.ExpandStar(allText!, tableColumns);

                if (expanded == allText)
                {
                    System.Windows.MessageBox.Show(
                        Localizer.Get("ExpandNothing"),
                        Localizer.Get("MsgTitle"));
                    return;
                }

                EditorHelper.ReplaceAllText(dte, expanded);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ExpandStarCommand 执行异常: {ex.Message}");
            }
        }
    }
}
