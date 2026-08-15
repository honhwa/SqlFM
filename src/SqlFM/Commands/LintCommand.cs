using System;
using System.ComponentModel.Design;
using System.Text;
using Microsoft.VisualStudio.Shell;
using SqlFM.Core.Dialects;
using SqlFM.Core.Exemption;
using SqlFM.Core.Lint;
using SqlFM.Editor;
using SqlFM.Localization;
using SqlFM.Services;

namespace SqlFM.Commands
{
    /// <summary>
    /// Lint 检查命令处理器（建议 #7）。
    /// 获取全文 → 经豁免处理后调用 LintRuleCatalog → 结果写入 SSMS 输出窗口并弹窗汇总。
    /// 复用 FormatService 的当前样式，保证与格式化配置一致。
    /// </summary>
    internal sealed class LintCommand
    {
        /// <summary>
        /// 命令集 GUID，与 VSCT 中定义的 guidSqlFMCmdSet 一致。
        /// </summary>
        public static readonly Guid CommandSet = new Guid("E8F2A3D4-5B6C-7D8E-9F0A-1B2C3D4E5F6A");

        /// <summary>
        /// 命令 ID，与 VSCT 中 LintCmdId 一致。
        /// </summary>
        public const int CommandId = 0x0106;

        /// <summary>
        /// 命令实例，确保单例。
        /// </summary>
        private static LintCommand? _instance;

        /// <summary>
        /// 异步包引用，用于获取 DTE 等服务。
        /// </summary>
        private readonly AsyncPackage _package;

        /// <summary>
        /// 私有构造函数，防止外部实例化。
        /// </summary>
        private LintCommand(AsyncPackage package)
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
                var menuItem = CommandLocalizer.Create(menuCommandId, Execute, "CmdLint");
                commandService.AddCommand(menuItem);
            }

            _instance = new LintCommand(package);
        }

        /// <summary>
        /// 公开静态方法，供右键菜单 Click 事件直接调用。
        /// </summary>
        public static void ExecuteLint()
        {
            Execute(null, EventArgs.Empty);
        }

        /// <summary>
        /// 命令执行回调：对活动文档全文执行 Lint 检查并报告结果。
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

                var style = FormatService.CurrentStyle;
                var exemption = new ExemptionProcessor();
                var (processed, regions) = exemption.PreProcess(allText!);
                var lintRegions = LintRuleCatalog.ToLintRegions(regions, processed);
                var results = LintRuleCatalog.DefaultEngine.Lint(
                    processed, TsqlDialect.Instance, style, lintRegions);

                var sb = new StringBuilder();
                sb.AppendLine("=== SqlFM Lint ===");
                if (results.Count == 0)
                {
                    sb.AppendLine("未发现问题（0 issues）。");
                }
                else
                {
                    foreach (var r in results)
                    {
                        sb.AppendLine(r.ToDisplayString());
                    }
                    sb.AppendLine("共 " + results.Count + " 条问题。");
                }

                WriteToOutput(dte, sb.ToString());

                System.Windows.MessageBox.Show(
                    results.Count == 0
                        ? Localizer.Get("LintNone")
                        : string.Format(Localizer.Get("LintFound"), results.Count),
                    Localizer.Get("MsgTitle"));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LintCommand 执行异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 将文本写入 SSMS 的 SqlFM 输出窗格（不存在则创建）。
        /// </summary>
        private static void WriteToOutput(EnvDTE80.DTE2 dte, string text)
        {
            try
            {
                var outputWindow = dte.ToolWindows.OutputWindow;
                EnvDTE.OutputWindowPane pane;
                try
                {
                    pane = outputWindow.OutputWindowPanes.Item("SqlFM");
                }
                catch
                {
                    pane = outputWindow.OutputWindowPanes.Add("SqlFM");
                }
                pane.OutputString(text);
                pane.Activate();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LintCommand 写输出窗口失败: {ex.Message}");
            }
        }
    }
}
