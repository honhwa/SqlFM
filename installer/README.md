# SqlFM 安装包构建说明

## 概述

本目录包含将 SqlFM VSIX 打包为 Windows 安装程序（.exe）的脚本。

生成的安装包具备：
- 双击即可在目标机器安装（前提：已安装 SSMS 22）
- 在 Windows **设置 → 应用** 中可见，名称为"SqlFM - T-SQL 格式化工具"
- 可通过系统的"卸载"功能一键卸载

## 前置条件

| 工具 | 说明 |
|------|------|
| .NET SDK | 构建 net48 项目（已配置 MSBuild）|
| Inno Setup 6 | 免费安装包制作工具，[下载地址](https://jrsoftware.org/isdl.php) |
| SSMS 22 | 仅目标机器需要安装，构建机器不需要 |

## 构建步骤

### 方式一：自动脚本（推荐）

```powershell
# 在项目根目录或 installer\ 目录下执行
cd D:\AIGC\SqlFM\installer
powershell -ExecutionPolicy Bypass -File build-installer.ps1
```

脚本会自动完成：
1. 执行 `dotnet build --configuration Release`
2. 验证 VSIX 文件是否生成
3. 查找 Inno Setup 编译器
4. 编译安装包到 `..\output\SqlFMSetup_v1.0.0.exe`

### 方式二：手动编译

1. 先构建项目：
   ```powershell
   dotnet build D:\AIGC\SqlFM\SqlFM.sln --configuration Release
   ```

2. 打开 Inno Setup GUI，加载脚本：
   ```
   D:\AIGC\SqlFM\installer\SqlFMSetup.iss
   ```
   点击 **Build → Compile** 即可。

## 安装包行为说明

### 安装时
1. Inno Setup 将 `SqlFM.vsix` 释放到临时目录
2. 调用 SSMS 自带的 `VSIXInstaller.exe /quiet` 完成扩展安装
3. SSMS 扩展文件安装到：`%LOCALAPPDATA%\Microsoft\SSMS\22.0_*\Extensions\`
4. 在 Windows 程序列表注册卸载信息（`%LOCALAPPDATA%\Programs\SqlFM`）

### 卸载时
1. 从 Windows 设置 → 应用 中点击卸载
2. 调用 `VSIXInstaller.exe /quiet /uninstall:SqlFM.B4AB3D7A-F5E7-485D-A68E-F9037042028C`
3. 清理注册信息

## 输出文件

```
output\
└── SqlFMSetup_v1.0.0.exe    ← 分发给用户的安装包
```

## 注意事项

- 目标机器必须已安装 **SSMS 22**，安装包会检测 `VSIXInstaller.exe` 是否存在
- 安装过程不需要管理员权限（安装到用户目录）
- 安装完成后需要**重启 SSMS** 才能看到扩展
- 更新版本时，修改 `.iss` 文件中的 `MyAppVersion` 值即可
