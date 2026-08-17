using System;
using System.ComponentModel.Design;
using System.IO;
using System.Reflection;
using Microsoft.VisualStudio.Shell;
using SqlFM.Options;

namespace SqlFM.Commands
{
    /// <summary>
    /// Format options command handler.
    /// Opens the SqlFM configuration dialog.
    ///
    /// Strategy: Uses CodeBehindSettingsWindow (pure C# UI) as the primary
    /// to avoid WPF XAML BAML loader failures in mixed environments
    /// (e.g. SSMS 22 + SQL Server 2008 R2 coexistence).
    /// </summary>
    internal sealed class FormatOptionsCommand
    {
        public static readonly Guid CommandSet = new Guid("E8F2A3D4-5B6C-7D8E-9F0A-1B2C3D4E5F6A");
        public const int CommandId = 0x0102;

        private static FormatOptionsCommand? _instance;
        private readonly AsyncPackage _package;

        private FormatOptionsCommand(AsyncPackage package)
        {
            _package = package ?? throw new ArgumentNullException(nameof(package));
        }

        public static async System.Threading.Tasks.Task InitializeAsync(AsyncPackage package)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var commandService = await package.GetServiceAsync(typeof(IMenuCommandService))
                as OleMenuCommandService;

            if (commandService != null)
            {
                var menuCommandId = new CommandID(CommandSet, CommandId);
                var menuItem = new OleMenuCommand((s, e) => Execute(s, e), menuCommandId);
                commandService.AddCommand(menuItem);
            }

            _instance = new FormatOptionsCommand(package);
        }

        /// <summary>
        /// Opens settings using pure code-behind window (no XAML parsing).
        /// This avoids BindToMethod/CreateInstanceWithCtorType crashes that occur
        /// when SQL Server 2008 R2 and SSMS 22 are installed on the same machine.
        /// </summary>
        private static void Execute(object? sender, EventArgs e)
        {
            try
            {
                ThreadHelper.ThrowIfNotOnUIThread();

                // Load current style from persisted storage
                var currentStyle = SqlFM.Services.StyleManager.GetDefaultStyle() ??
                    SqlFM.Core.PresetStyles.PresetStyleFactory.CreateDefault();

                // Use code-behind window — zero XAML, zero BAML, zero markup extensions
                var window = new CodeBehindSettingsWindow(currentStyle);

                // Set owner to SSMS main window
                if (_instance != null)
                {
                    try
                    {
                        var dte = Microsoft.VisualStudio.Shell.ServiceProvider.GlobalProvider.GetService(typeof(EnvDTE.DTE)) as EnvDTE.DTE;
                        if (dte?.MainWindow?.HWnd != null && dte.MainWindow.HWnd != IntPtr.Zero)
                        {
                            var helper = new System.Windows.Interop.WindowInteropHelper(window);
                            helper.Owner = dte.MainWindow.HWnd;
                            window.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner;
                        }
                    }
                    catch { /* non-critical: proceed without owner */ }
                }

                window.ShowDialog();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"Failed to open format options:\n{ex.Message}\n\n{ex.StackTrace}",
                    "SqlFM Error",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
            }
        }
    }
}
