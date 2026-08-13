using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using SqlFM.Commands;
using SqlFM.Localization;
using SqlFM.Services;
using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Task = System.Threading.Tasks.Task;

namespace SqlFM
{
    /// <summary>
    /// SqlFM 扩展主包类。
    /// 负责初始化扩展、注册命令和配置页。
    /// </summary>
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [Guid(PackageGuidString)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    // 自动加载：当解决方案存在时加载（确保菜单在 SSMS 启动时可见）
    [ProvideAutoLoad(UIContextGuids80.SolutionExists, PackageAutoLoadFlags.BackgroundLoad)]
    // 备用：无解决方案时也加载（SSMS 可能不使用 Solution 模型）
    [ProvideAutoLoad(UIContextGuids80.NoSolution, PackageAutoLoadFlags.BackgroundLoad)]
    public sealed class SqlFMPackage : AsyncPackage, IVsRunningDocTableEvents3
    {
        /// <summary>
        /// SqlFM 包的唯一标识符。
        /// </summary>
        public const string PackageGuidString = "B4AB3D7A-F5E7-485D-A68E-F9037042028C";

        /// <summary>
        /// 命令集 GUID，与 VSCT 中定义的 guidSqlFMCmdSet 一致。
        /// </summary>
        public const string CommandSetGuidString = "E8F2A3D4-5B6C-7D8E-9F0A-1B2C3D4E5F6A";

        /// <summary>
        /// 右键菜单按钮引用，必须保存以防止 GC 回收导致 Click 事件失效。
        /// </summary>
        private Microsoft.VisualStudio.CommandBars.CommandBarButton? _formatSelectedButton;
        private Microsoft.VisualStudio.CommandBars.CommandBarButton? _formatAllButton;
        private Microsoft.VisualStudio.CommandBars.CommandBarButton? _caseUpperButton;
        private Microsoft.VisualStudio.CommandBars.CommandBarButton? _caseLowerButton;
        private Microsoft.VisualStudio.CommandBars.CommandBarButton? _insertExemptionButton;

        /// <summary>
        /// Running Document Table 的 Cookie，用于取消订阅。
        /// </summary>
        private uint _rdtCookie;

        /// <summary>
        /// IVsRunningDocumentTable 服务引用。
        /// </summary>
        private IVsRunningDocumentTable? _rdt;

        /// <summary>
        /// 包异步初始化入口。
        /// 在此处注册所有命令处理器。
        /// </summary>
        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            // 切换到 UI 线程，命令注册必须在 UI 线程上执行
            await this.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            // 应用界面语言偏好（中文 / 英文 / 跟随系统），必须在注册菜单/命令前设置
            try
            {
                Localizer.Instance.Language = StyleManager.LoadInterfaceLanguage();
            }
            catch
            {
                // 读取失败时回退到默认（跟随系统）
            }

            // 注册格式化选中 SQL 命令
            await FormatSelectedCommand.InitializeAsync(this);

            // 注册格式化全部 SQL 命令
            await FormatAllCommand.InitializeAsync(this);

            // 注册格式化选项命令
            await FormatOptionsCommand.InitializeAsync(this);

            // 注册关键字大写命令
            await CaseUpperCommand.InitializeAsync(this);

            // 注册关键字小写命令
            await CaseLowerCommand.InitializeAsync(this);

            // 注册插入豁免标记命令
            await InsertExemptionCommand.InitializeAsync(this);

            // 从持久化存储加载默认样式到 FormatService
            try
            {
                var defaultStyle = Services.StyleManager.GetDefaultStyle();
                Services.FormatService.CurrentStyle = defaultStyle;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SqlFM] 加载默认样式失败: {ex.Message}");
            }

            // 注入右键上下文菜单（SSMS SQL 编辑器使用专有菜单，不走 VSCT 的 IDM_VS_CTXT_CODEWIN）
            try
            {
                var dte = (EnvDTE80.DTE2?)await GetServiceAsync(typeof(EnvDTE.DTE));
                if (dte != null)
                {
                    // 第一步：先诊断，输出所有 Popup 类型 CommandBar 名称
                    LogAllContextMenus(dte);

                    // 尝试注入到已知的可能菜单名称
                    AddToContextMenu(dte);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SqlFM] 右键菜单注入失败: {ex.Message}");
            }

            // 注册保存自动格式化（监听 RunningDocumentTable 的 SaveDocument 事件）
            try
            {
                _rdt = await GetServiceAsync(typeof(SVsRunningDocumentTable)) as IVsRunningDocumentTable;
                if (_rdt != null)
                {
                    _rdt.AdviseRunningDocTableEvents(this, out _rdtCookie);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SqlFM] 注册保存自动格式化失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 包销毁时取消 RDT 事件订阅。
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing && _rdt != null && _rdtCookie != 0)
            {
                _rdt.UnadviseRunningDocTableEvents(_rdtCookie);
                _rdtCookie = 0;
            }
            base.Dispose(disposing);
        }

        #region IVsRunningDocTableEvents3 — 保存时自动格式化

        /// <summary>
        /// 防止在自动格式化触发的保存中再次进入格式化逻辑（防御性重入保护）。
        /// </summary>
        private bool _formattingOnSave;

        /// <summary>
        /// 保存文档“前”自动格式化：在缓冲区写入磁盘之前改写内容，
        /// 因此格式化结果会随本次保存一并落盘，且不会引发保存死循环。
        /// 仅对 *.sql 文件、且为当前活动文档时生效（“保存全部”时跳过非活动文档）。
        /// 开关来自 settings.xml（StyleManager.FormatOnSave），与配置窗口保持一致。
        /// </summary>
        public int OnBeforeSave(uint docCookie)
        {
            if (_formattingOnSave)
                return Microsoft.VisualStudio.VSConstants.S_OK;

            try
            {
                // 开关关闭时直接跳过
                if (!StyleManager.LoadFormatOnSave())
                    return Microsoft.VisualStudio.VSConstants.S_OK;

                // 解析被保存文档的路径
                string docPath = string.Empty;
                _rdt?.GetDocumentInfo(docCookie, out _, out _, out _, out docPath, out _, out _, out _);
                if (string.IsNullOrEmpty(docPath))
                    return Microsoft.VisualStudio.VSConstants.S_OK;

                // 仅对 SQL 文件生效，避免误改其它类型文件
                if (!docPath.EndsWith(".sql", StringComparison.OrdinalIgnoreCase))
                    return Microsoft.VisualStudio.VSConstants.S_OK;

                ThreadHelper.JoinableTaskFactory.Run(async () =>
                {
                    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                    try
                    {
                        var dte = (EnvDTE80.DTE2?)await GetServiceAsync(typeof(EnvDTE.DTE));
                        if (dte == null || dte.ActiveDocument == null)
                            return;
                        // 仅处理真正被保存的活动文档（“保存全部”时不会误改其它文档）
                        if (!string.Equals(dte.ActiveDocument.FullName, docPath, StringComparison.OrdinalIgnoreCase))
                            return;
                        if (!Editor.EditorHelper.HasActiveTextDocument(dte))
                            return;

                        string? allText = Editor.EditorHelper.GetAllText(dte);
                        if (string.IsNullOrEmpty(allText))
                            return;

                        var result = FormatService.FormatAll(allText!);
                        if (result.Success && result.FormattedSql != allText)
                        {
                            _formattingOnSave = true;
                            try
                            {
                                Editor.EditorHelper.ReplaceAllText(dte, result.FormattedSql);
                            }
                            finally
                            {
                                _formattingOnSave = false;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[SqlFM] 保存前自动格式化失败: {ex.Message}");
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SqlFM] OnBeforeSave 异常: {ex.Message}");
            }

            return Microsoft.VisualStudio.VSConstants.S_OK;
        }

        /// <summary>
        /// 保存后无需额外处理（格式化已在 OnBeforeSave 中完成并随保存落盘）。
        /// </summary>
        public int OnAfterSave(uint docCookie) => Microsoft.VisualStudio.VSConstants.S_OK;

        // 以下 IVsRunningDocTableEvents3 成员为空实现（仅需 OnBeforeSave）
        public int OnAfterFirstDocumentLock(uint docCookie, uint dwRDTLockType, uint dwReadLocksRemaining, uint dwEditLocksRemaining) => Microsoft.VisualStudio.VSConstants.S_OK;
        public int OnBeforeLastDocumentUnlock(uint docCookie, uint dwRDTLockType, uint dwReadLocksRemaining, uint dwEditLocksRemaining) => Microsoft.VisualStudio.VSConstants.S_OK;
        public int OnAfterAttributeChange(uint docCookie, uint grfAttribs) => Microsoft.VisualStudio.VSConstants.S_OK;
        public int OnBeforeDocumentWindowShow(uint docCookie, int fFirstShow, IVsWindowFrame pFrame) => Microsoft.VisualStudio.VSConstants.S_OK;
        public int OnAfterDocumentWindowHide(uint docCookie, IVsWindowFrame pFrame) => Microsoft.VisualStudio.VSConstants.S_OK;
        public int OnAfterAttributeChangeEx(uint docCookie, uint grfAttribs, IVsHierarchy pHierOld, uint itemidOld, string pszMkDocumentOld, IVsHierarchy pHierNew, uint itemidNew, string pszMkDocumentNew) => Microsoft.VisualStudio.VSConstants.S_OK;

        #endregion

        /// <summary>
        /// 诊断方法：枚举所有 Popup 类型的 CommandBar，将名称写入 SSMS 输出窗口。
        /// 用于确定 SSMS SQL 编辑器右键菜单的确切名称。
        /// </summary>
        private void LogAllContextMenus(EnvDTE80.DTE2 dte)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            try
            {
                var commandBars = (Microsoft.VisualStudio.CommandBars.CommandBars)dte.CommandBars;
                var sb = new StringBuilder();
                sb.AppendLine("=== All CommandBars (Popup type) ===");

                foreach (Microsoft.VisualStudio.CommandBars.CommandBar bar in commandBars)
                {
                    try
                    {
                        if (bar.Type == Microsoft.VisualStudio.CommandBars.MsoBarType.msoBarTypePopup)
                        {
                            sb.AppendLine($"  Name='{bar.Name}', Controls={bar.Controls.Count}");
                        }
                    }
                    catch { }
                }

                // 写入 Output Window
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
                pane.OutputString(sb.ToString());
                pane.Activate();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SqlFM] LogAllContextMenus 失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 尝试将格式化命令注入到 SSMS SQL 编辑器的右键上下文菜单。
        /// 由于 SSMS 使用专有菜单（非 VS 标准的 Code Window），
        /// 需要通过 DTE CommandBars 动态添加菜单项。
        /// </summary>
        private void AddToContextMenu(EnvDTE80.DTE2 dte)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var commandBars = (Microsoft.VisualStudio.CommandBars.CommandBars)dte.CommandBars;

            // SSMS SQL 编辑器上下文菜单可能的名称
            string[] possibleMenuNames = new[]
            {
                "SQL Files Editor Context",  // SSMS 常用名
                "Code Window",               // VS 标准名
                "Script Context",            // 另一个可能的名称
                "SQLEditor Context",         // 可能的变体
            };

            Microsoft.VisualStudio.CommandBars.CommandBar? contextMenu = null;

            foreach (var menuName in possibleMenuNames)
            {
                try
                {
                    contextMenu = commandBars[menuName];
                    if (contextMenu != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"[SqlFM] 找到上下文菜单: '{menuName}'");
                        break;
                    }
                }
                catch { continue; }
            }

            if (contextMenu == null)
            {
                System.Diagnostics.Debug.WriteLine("[SqlFM] 未找到已知的上下文菜单名称，请查看输出窗口中的 CommandBar 列表");
                return;
            }

            // 查找插入位置：在"粘贴"之后、"插入片段"之前
            // Before 参数为 1-based 索引，表示在第 N 个控件之前插入
            int insertPosition = contextMenu.Controls.Count + 1; // 默认追加到末尾
            bool foundPaste = false;

            for (int i = 1; i <= contextMenu.Controls.Count; i++)
            {
                try
                {
                    var ctl = contextMenu.Controls[i];
                    string caption = ctl.Caption ?? "";

                    // 优先查找"插入片段"(Insert Snippet) 的位置，在它之前插入
                    if (caption.Contains("插入片段") || caption.Contains("Insert Sni"))
                    {
                        insertPosition = i;
                        foundPaste = true; // 标记已找到有效锚点
                        break;
                    }

                    // 备用：记录"粘贴"(Paste) 的位置，在其后一位插入
                    if (!foundPaste && (caption.Contains("粘贴") || caption.Contains("Paste")))
                    {
                        insertPosition = i + 1;
                        foundPaste = true;
                    }
                }
                catch { continue; }
            }

            System.Diagnostics.Debug.WriteLine($"[SqlFM] 右键菜单插入位置: {insertPosition} (共 {contextMenu.Controls.Count} 项)");

            // 添加 "格式化选中 SQL" — 在此按钮前显示分隔线
            _formatSelectedButton = (Microsoft.VisualStudio.CommandBars.CommandBarButton)contextMenu.Controls.Add(
                Microsoft.VisualStudio.CommandBars.MsoControlType.msoControlButton,
                Type.Missing, Type.Missing, insertPosition, true);
            _formatSelectedButton.Caption = Localizer.Get("CmdFormatSelected");
            _formatSelectedButton.BeginGroup = true;
            _formatSelectedButton.Click += FormatSelectedButton_Click;

            // 添加 "格式化全部 SQL"
            _formatAllButton = (Microsoft.VisualStudio.CommandBars.CommandBarButton)contextMenu.Controls.Add(
                Microsoft.VisualStudio.CommandBars.MsoControlType.msoControlButton,
                Type.Missing, Type.Missing, insertPosition + 1, true);
            _formatAllButton.Caption = Localizer.Get("CmdFormatAll");
            _formatAllButton.Click += FormatAllButton_Click;

            // 添加 "关键字大写"
            _caseUpperButton = (Microsoft.VisualStudio.CommandBars.CommandBarButton)contextMenu.Controls.Add(
                Microsoft.VisualStudio.CommandBars.MsoControlType.msoControlButton,
                Type.Missing, Type.Missing, insertPosition + 2, true);
            _caseUpperButton.Caption = Localizer.Get("CmdCaseUpper");
            _caseUpperButton.Click += CaseUpperButton_Click;

            // 添加 "关键字小写"
            _caseLowerButton = (Microsoft.VisualStudio.CommandBars.CommandBarButton)contextMenu.Controls.Add(
                Microsoft.VisualStudio.CommandBars.MsoControlType.msoControlButton,
                Type.Missing, Type.Missing, insertPosition + 3, true);
            _caseLowerButton.Caption = Localizer.Get("CmdCaseLower");
            _caseLowerButton.Click += CaseLowerButton_Click;

            // 添加 "插入豁免标记"
            _insertExemptionButton = (Microsoft.VisualStudio.CommandBars.CommandBarButton)contextMenu.Controls.Add(
                Microsoft.VisualStudio.CommandBars.MsoControlType.msoControlButton,
                Type.Missing, Type.Missing, insertPosition + 4, true);
            _insertExemptionButton.Caption = Localizer.Get("CmdInsertExemption");
            _insertExemptionButton.Click += InsertExemptionButton_Click;
        }

        private void FormatSelectedButton_Click(Microsoft.VisualStudio.CommandBars.CommandBarButton ctrl, ref bool cancelDefault)
        {
            FormatSelectedCommand.ExecuteFormat();
        }

        private void FormatAllButton_Click(Microsoft.VisualStudio.CommandBars.CommandBarButton ctrl, ref bool cancelDefault)
        {
            FormatAllCommand.ExecuteFormat();
        }

        private void CaseUpperButton_Click(Microsoft.VisualStudio.CommandBars.CommandBarButton ctrl, ref bool cancelDefault)
        {
            CaseUpperCommand.ExecuteCaseUpper();
        }

        private void CaseLowerButton_Click(Microsoft.VisualStudio.CommandBars.CommandBarButton ctrl, ref bool cancelDefault)
        {
            CaseLowerCommand.ExecuteCaseLower();
        }

        private void InsertExemptionButton_Click(Microsoft.VisualStudio.CommandBars.CommandBarButton ctrl, ref bool cancelDefault)
        {
            InsertExemptionCommand.ExecuteInsertExemption();
        }
    }
}
