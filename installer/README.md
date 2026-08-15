# SqlFM 安装包说明

## 产物

| 文件 | 说明 |
|------|------|
| `output/SqlFMSetup.exe` | **主安装包**（自包含单文件）。双击即装，在 Windows 设置 → 应用 中可一键卸载。 |
| `output/SqlFMSetup_vX.X.X.exe` | （可选）Inno Setup 生成的安装包，需本机安装 Inno Setup 6/7 才会产出。 |

## 方案选型说明

原先的 `SqlFMSetup.iss` 依赖 Inno Setup 编译器，而本机构建环境未安装且无法联网下载。
因此改用一个**零依赖的自包含安装程序** `SqlFMSetup.exe`（C# / .NET Framework 4.8 编写），
将 `SqlFM.vsix` 作为资源内嵌，单文件即可分发。

## 安装包行为

### 安装（双击 `SqlFMSetup.exe`）
1. 从自身资源中提取内嵌的 `SqlFM.vsix` 到临时目录；
2. 在 `C:\Program Files\Microsoft SQL Server Management Studio 22\...` 等路径中查找 `VSIXInstaller.exe`；
3. 调用 `VSIXInstaller.exe /quiet SqlFM.vsix` 静默安装扩展到 SSMS 22；
4. 将自身持久化到 `%LOCALAPPDATA%\Programs\SqlFM\SqlFMSetup.exe`；
5. 在注册表 `HKCU\Software\Microsoft\Windows\CurrentVersion\Uninstall\SqlFM` 写入卸载项，
   使本程序出现在 **Windows 设置 → 应用** 列表中。

### 卸载（设置 → 应用 → SqlFM - T-SQL 格式化工具 → 卸载）
1. **卸载前确认**：双击 `SqlFMSetup.exe /uninstall`（交互模式）会先弹出确认框，确认后才继续；从系统"应用"列表点卸载时由系统统一确认（以 `/quiet` 调用，不重复打扰）。
2. **关闭 SSMS 进程**：卸载前自动关闭正在运行的 SQL Server Management Studio（优雅关闭超时后，交互模式会询问是否强制关闭，静默模式直接结束），避免扩展文件被占用导致卸载不彻底。
3. 按注册表记录的扩展目录精准删除；若被 SSMS 占用则**预约系统重启后删除**（不破坏其他组件）；
4. 调用 `VSIXInstaller.exe /quiet /uninstall:SqlFM.B4AB3D7A-F5E7-485D-A68E-F9037042028C` 做标准卸载（双保险）；
5. 全网兜底删除 `SqlFM.pkgdef` 残留目录与 SqlFM 相关快捷方式；
6. 删除用户级配置目录 `%AppData%\SqlFM`（自定义样式与设置）；
7. 删除注册表卸载项（从应用列表中消失），并预约重启后清理持久化目录与安装程序自身。

### 命令行
```
SqlFMSetup.exe               交互式安装
SqlFMSetup.exe /quiet        静默安装
SqlFMSetup.exe /uninstall /quiet   静默卸载（系统“应用”的卸载按钮即以此方式调用）
```

## 前置条件
- 目标机器已安装 **SQL Server Management Studio 22**（安装包会检测 `VSIXInstaller.exe`，缺失则提示）；
- 安装到用户目录，**无需管理员权限**；
- 安装后需**重启 SSMS 22** 使扩展生效。

## 构建（打包）

```powershell
# 在项目根目录执行，自动完成：主方案 Release 构建 → 安装程序构建 → 清理冗余文件
powershell -ExecutionPolicy Bypass -File installer\build-installer.ps1
```

脚本会：
1. `dotnet build SqlFM.sln --configuration Release`（产出 `SqlFM.vsix`）；
2. `dotnet build setup\SqlFM.Setup.csproj --configuration Release`（内嵌 VSIX，产出 `output\SqlFMSetup.exe`）；
3. 删除 `SqlFMSetup.pdb` / `.config`，保留单一 exe；
4. 若检测到 Inno Setup，额外生成 `SqlFMSetup_vX.X.X.exe`。

## 关键文件
- `setup/SqlFM.Setup.csproj` — 安装程序项目（net48，内嵌 VSIX）
- `setup/Program.cs` — 安装/卸载逻辑（提取资源、调用 VSIXInstaller、注册表卸载项）
- `installer/build-installer.ps1` — 一键打包脚本
- `installer/SqlFMSetup.iss` — （可选）Inno Setup 脚本，保留备用
