using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace SqlFM.Setup
{
    /// <summary>
    /// SqlFM 自包含安装程序。
    /// 将 SqlFM.vsix 作为资源内嵌，双击即安装到 SSMS 22。
    /// 关键设计：安装时无论如何都会把"扩展装到哪个目录"写入注册表，
    /// 因此卸载（无论是控制面板"应用"按钮，还是 SqlFMSetup.exe /uninstall）
    /// 都能按记录精准删除扩展目录并清理 SSMS 缓存，不再依赖 VSIXInstaller 是否存在。
    /// 用法：
    ///   SqlFMSetup.exe                      安装（交互）
    ///   SqlFMSetup.exe /quiet               静默安装
    ///   SqlFMSetup.exe /uninstall /quiet    卸载（系统"应用"里的卸载按钮会这样调用）
    ///   SqlFMSetup.exe /vsixinstaller:"路径" 指定 VSIXInstaller.exe 路径
    /// </summary>
    internal static class Program
    {
        // 必须与 source.extension.vsixmanifest 中的 Identity Id 一致
        private const string ExtId = "SqlFM.B4AB3D7A-F5E7-485D-A68E-F9037042028C";
        private const string GuidFolder = "B4AB3D7A-F5E7-485D-A68E-F9037042028C";
        private const string AppName = "SqlFM - T-SQL 格式化工具";
        private const string Version = "1.0.0";
        private const string Publisher = "SqlFM";
        private const string Url = "https://github.com/SqlFM";
        private const string RegKey = @"Software\Microsoft\Windows\CurrentVersion\Uninstall\SqlFM";
        // 自定义值：记录扩展实际安装目录与安装方式，供卸载精准清理
        private const string RegInstallLocation = "SqlFMInstallLocation";
        private const string RegInstallMethod = "SqlFMInstallMethod";

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern int MessageBoxW(IntPtr hWnd, string text, string caption, uint type);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool MoveFileExW(string lpExistingFileName, string lpNewFileName, int dwFlags);

        // MessageBox 样式
        private const uint MB_ICONERROR = 0x10;
        private const uint MB_ICONINFORMATION = 0x40;
        private const int MOVEFILE_DELAY_UNTIL_REBOOT = 0x4;

        private static void Show(string text, uint icon)
        {
            try { MessageBoxW(IntPtr.Zero, text, AppName, icon); }
            catch { Console.WriteLine(text); }
        }

        private static int Main(string[] args)
        {
            bool quiet = false, uninstall = false;
            string vsixInstaller = null;
            foreach (var a in args)
            {
                var u = a.Trim();
                if (u.StartsWith("/uninstall", StringComparison.OrdinalIgnoreCase) ||
                    u.StartsWith("-uninstall", StringComparison.OrdinalIgnoreCase))
                    uninstall = true;
                else if (u.Equals("/quiet", StringComparison.OrdinalIgnoreCase) ||
                         u.Equals("/silent", StringComparison.OrdinalIgnoreCase) ||
                         u.Equals("-quiet", StringComparison.OrdinalIgnoreCase))
                    quiet = true;
                else if (u.StartsWith("/vsixinstaller:", StringComparison.OrdinalIgnoreCase) ||
                         u.StartsWith("-vsixinstaller:", StringComparison.OrdinalIgnoreCase))
                {
                    vsixInstaller = u.Substring(u.IndexOf(':') + 1).Trim('"', '\'');
                }
            }

            try
            {
                return uninstall
                    ? (DoUninstall(quiet, vsixInstaller) ? 0 : 1)
                    : (DoInstall(quiet, vsixInstaller) ? 0 : 1);
            }
            catch (Exception ex)
            {
                if (!quiet) Show("操作过程中发生错误：\n" + ex.Message, MB_ICONERROR);
                return 1;
            }
        }

        /// <summary>在嵌入资源中定位 VSIX（按 .vsix 后缀，兼容不同打包命名）。</summary>
        private static string FindEmbeddedVsix()
        {
            var asm = Assembly.GetExecutingAssembly();
            foreach (var n in asm.GetManifestResourceNames())
            {
                if (n.EndsWith(".vsix", StringComparison.OrdinalIgnoreCase)) return n;
            }
            return null;
        }

        /// <summary>在常见安装路径中查找 SSMS 的 VSIXInstaller.exe。</summary>
        private static string FindVsixInstaller()
        {
            var candidates = new System.Collections.Generic.List<string>();

            // 1) 从注册表读 SSMS 安装目录
            var regPaths = new[]
            {
                @"SOFTWARE\Microsoft\Microsoft SQL Server Management Studio\22.0",
                @"SOFTWARE\WOW6432Node\Microsoft\Microsoft SQL Server Management Studio\22.0",
                @"SOFTWARE\Microsoft\Microsoft SQL Server Management Studio\21.0",
                @"SOFTWARE\WOW6432Node\Microsoft\Microsoft SQL Server Management Studio\21.0",
                @"SOFTWARE\Microsoft\Microsoft SQL Server Management Studio\20.0",
                @"SOFTWARE\WOW6432Node\Microsoft\Microsoft SQL Server Management Studio\20.0",
                @"SOFTWARE\Microsoft\Microsoft SQL Server Management Studio\19.0",
                @"SOFTWARE\WOW6432Node\Microsoft\Microsoft SQL Server Management Studio\19.0"
            };
            foreach (var rp in regPaths)
            {
                try
                {
                    using var key = Registry.LocalMachine.OpenSubKey(rp);
                    if (key == null) continue;
                    var dir = key.GetValue("InstallDir") as string ?? key.GetValue("Path") as string;
                    if (!string.IsNullOrEmpty(dir)) candidates.Add(dir.TrimEnd('\\'));
                }
                catch { }
            }

            // 2) 从 Program Files / x86 枚举 SSMS 目录
            var roots = new[]
            {
                Environment.GetEnvironmentVariable("ProgramFiles"),
                Environment.GetEnvironmentVariable("ProgramFiles(x86)"),
                @"C:\Program Files",
                @"C:\Program Files (x86)"
            };
            foreach (var root in roots)
            {
                if (string.IsNullOrEmpty(root) || !Directory.Exists(root)) continue;
                try
                {
                    foreach (var ssms in Directory.GetDirectories(root, "Microsoft SQL Server Management Studio *"))
                        candidates.Add(ssms.TrimEnd('\\'));
                }
                catch { /* 忽略无权限目录 */ }
            }

            // 3) 扫描所有可用盘符根目录下的 Program Files（处理 D/E 盘安装）
            try
            {
                foreach (var drive in DriveInfo.GetDrives())
                {
                    if (!drive.IsReady) continue;
                    foreach (var pf in new[] { "Program Files", "Program Files (x86)" })
                    {
                        var baseDir = Path.Combine(drive.RootDirectory.FullName, pf);
                        if (!Directory.Exists(baseDir)) continue;
                        try
                        {
                            foreach (var ssms in Directory.GetDirectories(baseDir, "Microsoft SQL Server Management Studio *"))
                                candidates.Add(ssms.TrimEnd('\\'));
                        }
                        catch { }
                    }
                }
            }
            catch { }

            // 4) 固定兜底路径
            candidates.AddRange(new[]
            {
                @"C:\Program Files\Microsoft SQL Server Management Studio 22",
                @"C:\Program Files (x86)\Microsoft SQL Server Management Studio 22",
                @"C:\Program Files\Microsoft SQL Server Management Studio 19",
                @"C:\Program Files (x86)\Microsoft SQL Server Management Studio 19"
            });

            var subPaths = new[] { @"Common7\IDE\VSIXInstaller.exe", @"Release\VSIXInstaller.exe", @"IDE\VSIXInstaller.exe" };
            foreach (var basePath in candidates)
            {
                foreach (var sub in subPaths)
                {
                    var p = Path.Combine(basePath, sub);
                    if (File.Exists(p)) return p;
                }
            }
            return null;
        }

        /// <summary>安装：优先 VSIXInstaller，缺失时降级为手动解压到用户级扩展目录。</summary>
        private static bool DoInstall(bool quiet, string explicitVsixInstaller)
        {
            // 释放内嵌的 VSIX 到临时目录
            var tmpVsix = Path.Combine(Path.GetTempPath(), "SqlFM.vsix");
            var resName = FindEmbeddedVsix();
            using (var s = resName == null ? null : Assembly.GetExecutingAssembly().GetManifestResourceStream(resName))
            {
                if (s == null)
                {
                    if (!quiet) Show("安装包内部缺少 VSIX 资源，可能已损坏。", MB_ICONERROR);
                    return false;
                }
                using (var fs = File.Create(tmpVsix)) s.CopyTo(fs);
            }

            string installLocation = null;
            string installMethod = "vsixinstaller";

            var vsix = explicitVsixInstaller;
            if (string.IsNullOrEmpty(vsix) || !File.Exists(vsix))
                vsix = FindVsixInstaller();

            if (!string.IsNullOrEmpty(vsix))
            {
                // 方式一：标准 VSIXInstaller 安装
                var psi = new ProcessStartInfo(vsix, "/quiet \"" + tmpVsix + "\"")
                {
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using (var p = Process.Start(psi)) p.WaitForExit();

                // 安装后探测实际扩展目录，记录到注册表以便精准卸载
                installLocation = FindInstalledExtensionDir();
            }
            else
            {
                // 方式二：降级安装 —— 直接把 VSIX 解压到用户级 SSMS 扩展目录
                if (!InstallViaManualDeploy(tmpVsix, out installLocation))
                {
                    if (!quiet)
                        Show("未找到 VSIXInstaller.exe，且无法写入用户级扩展目录。\n\n" +
                             "请确认已安装 SSMS 22，或手动从 SSMS 扩展管理器安装 SqlFM.vsix。",
                             MB_ICONERROR);
                    return false;
                }
                installMethod = "manual";
            }

            // 将自身持久化到用户目录，供系统"应用"里的卸载按钮调用
            var appDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Programs", "SqlFM");
            var self = Assembly.GetExecutingAssembly().Location;
            var persisted = Path.Combine(appDir, "SqlFMSetup.exe");
            try
            {
                if (!string.Equals(self, persisted, StringComparison.OrdinalIgnoreCase))
                {
                    Directory.CreateDirectory(appDir);
                    File.Copy(self, persisted, true);
                }
            }
            catch { /* 复制失败不影响安装，仅影响自动卸载体验 */ }

            // 注册卸载项（出现在 Windows 设置 → 应用），并记录扩展目录
            RegisterUninstall(persisted, installLocation, installMethod);

            if (!quiet)
            {
                var tip = "SqlFM 已成功安装！\n\n请重启 SQL Server Management Studio 22 以使扩展生效。\n";
                if (installMethod == "manual")
                    tip += "（本次使用内置解压方式部署，若 SSMS 中未出现菜单，请运行 installer\\find-vsixinstaller.ps1 定位 VSIXInstaller 后用 /vsixinstaller: 重装）\n";
                tip += "在 SSMS 编辑器中右键菜单将出现“SqlFM 格式化”选项。";
                Show(tip, MB_ICONINFORMATION);
            }
            return true;
        }

        /// <summary>降级安装：把 VSIX 解压到用户级 SSMS 扩展目录。</summary>
        private static bool InstallViaManualDeploy(string vsixPath, out string deployedDir)
        {
            deployedDir = null;
            var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var ssmsBase = Path.Combine(local, "Microsoft", "SSMS");

            // 枚举用户级 SSMS 版本目录；没有则建一个 22.0 默认目录
            var extRoots = new System.Collections.Generic.List<string>();
            if (Directory.Exists(ssmsBase))
            {
                foreach (var ver in Directory.GetDirectories(ssmsBase))
                {
                    var ext = Path.Combine(ver, "Extensions");
                    Directory.CreateDirectory(ext);
                    extRoots.Add(ext);
                }
            }
            if (extRoots.Count == 0)
            {
                var fallback = Path.Combine(ssmsBase, "22.0_00000000", "Extensions");
                Directory.CreateDirectory(fallback);
                extRoots.Add(fallback);
            }

            deployedDir = Path.Combine(extRoots[0], "SqlFM_" + GuidFolder);
            try
            {
                if (Directory.Exists(deployedDir)) Directory.Delete(deployedDir, true);
                ZipFile.ExtractToDirectory(vsixPath, deployedDir);
                return true;
            }
            catch
            {
                deployedDir = null;
                return false;
            }
        }

        /// <summary>安装后扫描 SSMS 扩展目录，定位 SqlFM.pkgdef 所在文件夹。</summary>
        private static string FindInstalledExtensionDir()
        {
            var roots = new[]
            {
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                Environment.GetEnvironmentVariable("ProgramFiles"),
                Environment.GetEnvironmentVariable("ProgramFiles(x86)")
            };
            foreach (var root in roots)
            {
                if (string.IsNullOrEmpty(root) || !Directory.Exists(root)) continue;
                try
                {
                    var hit = Directory.GetFiles(root, "SqlFM.pkgdef", SearchOption.AllDirectories);
                    if (hit.Length > 0) return Path.GetDirectoryName(hit[0]);
                }
                catch { }
            }
            return null;
        }

        private static void RegisterUninstall(string exePath, string installLocation, string installMethod)
        {
            using var key = Registry.CurrentUser.CreateSubKey(RegKey);
            if (key == null) return;
            key.SetValue("DisplayName", AppName, RegistryValueKind.String);
            key.SetValue("DisplayVersion", Version, RegistryValueKind.String);
            key.SetValue("Publisher", Publisher, RegistryValueKind.String);
            key.SetValue("URLInfoAbout", Url, RegistryValueKind.String);
            key.SetValue("UninstallString", "\"" + exePath + "\" /uninstall /quiet", RegistryValueKind.String);
            key.SetValue("QuietUninstallString", "\"" + exePath + "\" /uninstall /quiet", RegistryValueKind.String);
            key.SetValue("InstallLocation", Path.GetDirectoryName(exePath), RegistryValueKind.String);
            key.SetValue(RegInstallLocation, installLocation ?? "", RegistryValueKind.String);
            key.SetValue(RegInstallMethod, installMethod ?? "vsixinstaller", RegistryValueKind.String);
            key.SetValue("NoModify", 1, RegistryValueKind.DWord);
            key.SetValue("NoRepair", 1, RegistryValueKind.DWord);
            key.SetValue("EstimatedSize", 2400, RegistryValueKind.DWord);
        }

        /// <summary>卸载：按注册表记录的目录精准删除，并清理 SSMS 缓存。</summary>
        private static bool DoUninstall(bool quiet, string explicitVsixInstaller)
        {
            // 1) 读取安装时记录的扩展目录
            string installLocation = null;
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RegKey);
                installLocation = key?.GetValue(RegInstallLocation) as string;
            }
            catch { }

            // 2) 直接删除记录的扩展目录（不依赖 VSIXInstaller）
            //    若目录被 SSMS 占用（文件锁定），预约重启后删除，避免破坏正在运行的实例
            if (!string.IsNullOrEmpty(installLocation) && Directory.Exists(installLocation))
            {
                try { Directory.Delete(installLocation, true); }
                catch (IOException)
                {
                    try { ScheduleRebootDelete(installLocation); } catch { }
                }
                catch { /* 其他异常忽略 */ }
            }

            // 3) 清理 SSMS 扩展注册表缓存（privateregistry.bin 等），避免"未能加载包"报错
            CleanSsmsCaches();

            // 4) 若 VSIXInstaller 存在，也调用它做一次标准卸载（双保险）
            var vsix = explicitVsixInstaller;
            if (string.IsNullOrEmpty(vsix) || !File.Exists(vsix))
                vsix = FindVsixInstaller();
            if (!string.IsNullOrEmpty(vsix))
            {
                var psi = new ProcessStartInfo(vsix, "/quiet /uninstall:" + ExtId)
                {
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                try { using var p = Process.Start(psi); p.WaitForExit(); }
                catch { }
            }

            // 5) 兜底：全网搜索 SqlFM.pkgdef 残留并删除（应对早期手动安装、无记录的情况）
            RemoveOrphanExtensionDirs();

            // 5.5) 删除用户级配置目录 %AppData%\SqlFM（自定义样式 *.sqlstyle 与 settings.xml）
            //      避免卸载后残留冗余配置；仅删除 SqlFM 专属目录，不影响其他应用
            RemoveUserConfig();

            // 6) 删除注册表卸载项（从"应用"列表移除）
            try { Registry.CurrentUser.DeleteSubKeyTree(RegKey, false); }
            catch { }

            // 7) 清理持久化目录与自身（运行中文件预约重启后删除，避免文件锁定导致残留）
            try
            {
                var appDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Programs", "SqlFM");
                var self = Assembly.GetExecutingAssembly().Location;
                try { if (File.Exists(self)) ScheduleRebootDelete(self); } catch { }
                try
                {
                    if (Directory.Exists(appDir))
                    {
                        Directory.Delete(appDir, true);
                    }
                }
                catch (IOException)
                {
                    // 目录被占用（本程序自身尚在运行），预约重启后移除以彻底清理
                    try { ScheduleRebootDelete(appDir); } catch { }
                }
                catch { }
            }
            catch { }

            if (!quiet)
                Show("SqlFM 已卸载。\n\n请重启 SQL Server Management Studio 22 以使更改生效。", MB_ICONINFORMATION);
            return true;
        }

        /// <summary>清理所有 SSMS 22 用户实例下的 SqlFM 残留与扩展缓存文件。</summary>
        private static void CleanSsmsCaches()
        {
            var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var ssmsRoot = Path.Combine(local, "Microsoft", "SSMS");
            if (!Directory.Exists(ssmsRoot)) return;

            foreach (var verDir in Directory.GetDirectories(ssmsRoot))
            {
                // 仅删除 SqlFM 专属的残留文件/目录（按名称精确匹配），
                // 绝不触碰 SSMS 自身的注册表缓存文件（privateregistry.bin 等），
                // 以免破坏同一实例下的其他扩展组件
                try
                {
                    foreach (var f in Directory.GetFiles(verDir, "*SqlFM*", SearchOption.AllDirectories))
                        SafeDeleteFile(f);
                    foreach (var d in Directory.GetDirectories(verDir, "*SqlFM*", SearchOption.AllDirectories))
                        SafeDeleteDir(d);
                    foreach (var d in Directory.GetDirectories(verDir, "*" + GuidFolder + "*", SearchOption.AllDirectories))
                        SafeDeleteDir(d);
                }
                catch { }
            }
        }

        /// <summary>全网搜索 SqlFM.pkgdef 残留目录并删除（兜底，针对无注册表记录的旧安装）。</summary>
        private static void RemoveOrphanExtensionDirs()
        {
            var roots = new[]
            {
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                Environment.GetEnvironmentVariable("ProgramFiles"),
                Environment.GetEnvironmentVariable("ProgramFiles(x86)")
            };
            foreach (var root in roots)
            {
                if (string.IsNullOrEmpty(root) || !Directory.Exists(root)) continue;
                try
                {
                    foreach (var f in Directory.GetFiles(root, "SqlFM.pkgdef", SearchOption.AllDirectories))
                    {
                        var dir = Path.GetDirectoryName(f);
                        if (!string.IsNullOrEmpty(dir)) SafeDeleteDir(dir);
                    }
                }
                catch { }
            }
        }

        private static void SafeDeleteFile(string path)
        {
            try { File.Delete(path); } catch { }
        }

        private static void SafeDeleteDir(string path)
        {
            try { Directory.Delete(path, true); } catch { }
        }

        /// <summary>
        /// 预约在系统下一次重启时删除/移走文件或目录，解决文件被占用
        /// （如 SSMS 正在运行、本程序自身尚在运行）时无法立即删除的问题。
        /// 文件：重启时直接删除；目录：重启时重命名到临时位置（移出原路径），避免残留。
        /// </summary>
        private static void ScheduleRebootDelete(string path)
        {
            if (string.IsNullOrEmpty(path)) return;
            if (Directory.Exists(path) && !File.Exists(path))
            {
                var tmp = path.TrimEnd('\\') + ".deleted_" + Guid.NewGuid().ToString("N");
                MoveFileExW(path, tmp, MOVEFILE_DELAY_UNTIL_REBOOT);
            }
            else
            {
                MoveFileExW(path, null, MOVEFILE_DELAY_UNTIL_REBOOT);
            }
        }

        /// <summary>
        /// 删除用户级配置目录 %AppData%\SqlFM（自定义样式 *.sqlstyle 与 settings.xml）。
        /// 仅删除 SqlFM 专属目录，不影响其他应用；被占用时预约重启删除。
        /// </summary>
        private static void RemoveUserConfig()
        {
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "SqlFM");
            if (!Directory.Exists(dir)) return;
            try { Directory.Delete(dir, true); }
            catch (IOException) { try { ScheduleRebootDelete(dir); } catch { } }
            catch { }
        }
    }
}
