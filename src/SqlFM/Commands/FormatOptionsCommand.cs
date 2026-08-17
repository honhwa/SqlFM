using System;
using System.ComponentModel.Design;
using System.IO;
using System.Reflection;
using Microsoft.VisualStudio.Shell;
using SqlFM.Options;
using SqlFM.Localization;

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
        /// 命令 ID，与 VSCT 中定义的 FormatOptionsCmdId 一致。
        /// </summary>
        public const int CommandId = 0x0102;

        /// <summary>
        /// 命令实例，确保单例。
        /// </summary>
        private static FormatOptionsCommand? _instance;

        /// <summary>
        /// 扩展安装目录（VSIX 解压位置），用于运行时程序集解析。
        /// </summary>
        private static string? _extensionDir;

        /// <summary>
        /// 异步包引用，用于显示选项页。
        /// </summary>
        private readonly AsyncPackage _package;

        /// <summary>
        /// 静态构造函数：注册全局 AssemblyResolve 处理器，
        /// 确保 WPF XAML 加载器能找到扩展目录下的所有依赖程序集（SqlFM.dll / SqlFM.Core.dll 等）。
        /// </summary>
        static FormatOptionsCommand()
        {
            // 从当前执行程序集位置推导扩展安装目录
            var myAssembly = Assembly.GetExecutingAssembly();
            if (!string.IsNullOrEmpty(myAssembly.Location))
            {
                _extensionDir = Path.GetDirectoryName(myAssembly.Location);
            }

            // 注册 AppDomain 级别的程序集解析回调
            // 当 CLR 或 WPF XAML 加载器找不到某个程序集时触发，从扩展目录补查
            AppDomain.CurrentDomain.AssemblyResolve += ResolveExtensionAssembly;
        }

        /// <summary>
        /// 程序集解析回调：在扩展安装目录中查找并加载缺失的程序集。
        /// 解决 SSMS/VS 扩展环境下 WPF XAML 无法定位扩展 DLL 的经典问题。
        /// </summary>
        private static Assembly? ResolveExtensionAssembly(object sender, ResolveEventArgs args)
        {
            if (_extensionDir == null || string.IsNullOrEmpty(args.Name)) return null;

            try
            {
                // 截取简单名称（去掉版本、文化、公钥标记等后缀）
                var parts = args.Name.Split(',');
                var simpleName = parts[0].Trim();

                // 拼接可能的文件名：先试 .dll 再试 .exe
                foreach (var ext in new[] { ".dll", ".exe" })
                {
                    var candidate = Path.Combine(_extensionDir, simpleName + ext);
                    if (File.Exists(candidate))
                    {
                        return Assembly.LoadFrom(candidate);
                    }
                }
            }
            catch
            {
                // 加载失败时静默返回 null，让 CLR 继续走其他解析路径
            }
            return null;
        }

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
                var menuItem = CommandLocalizer.Create(menuCommandId, Execute, "CmdFormatOptions");

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
            try
            {
                // 确保在 UI 线程（SSMS 环境下可能从非 UI 上下文触发）
                ThreadHelper.ThrowIfNotOnUIThread();

                // 显式预加载核心程序集，避免 WPF XAML 加载时找不到
                EnsureAssembliesLoaded();

                var window = new SettingsWindow();

                // 设置窗口所有者为 SSMS 主窗口，防止对话框被遮挡或丢失焦点
                if (_instance != null)
                {
                    var dte = Microsoft.VisualStudio.Shell.ServiceProvider.GlobalProvider.GetService(typeof(EnvDTE.DTE)) as EnvDTE.DTE;
                    if (dte?.MainWindow?.HWnd != null && dte.MainWindow.HWnd != IntPtr.Zero)
                    {
                        var helper = new System.Windows.Interop.WindowInteropHelper(window);
                        helper.Owner = dte.MainWindow.HWnd;
                    }
                }

                window.ShowDialog();
            }
            catch (Exception ex)
            {
                // 不再静默吞掉异常——弹出消息框让用户能看到具体错误
                System.Windows.MessageBox.Show(
                    $"打开格式选项失败：{ex.Message}\n\n{ex.StackTrace}",
                    "SqlFM 错误",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 显式预加载扩展目录下的核心程序集到当前 AppDomain，
        /// 配合 AssemblyResolve 回调彻底解决 WPF XAML 找不到 SqlFM 程序集的问题。
        /// </summary>
        private static void EnsureAssembliesLoaded()
        {
            if (_extensionDir == null) return;

            // 扩展必须包含的核心 DLL 列表
            var requiredDlls = new[] { "SqlFM.dll", "SqlFM.Core.dll" };

            foreach (var dll in requiredDlls)
            {
                var path = Path.Combine(_extensionDir, dll);
                if (File.Exists(path))
                {
                    try { Assembly.LoadFrom(path); } catch { /* 已加载则忽略 */ }
                }
            }
        }
    }
}
