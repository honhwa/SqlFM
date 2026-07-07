# SqlFM 编译与打包指南

## 环境要求

| 组件 | 版本要求 |
|------|---------|
| Visual Studio | 2022（17.x）或更高版本，需安装 **"Visual Studio 扩展开发"** 工作负载 |
| .NET Framework | .NET Framework 4.8 SDK（随 Visual Studio 附带） |
| 操作系统 | Windows 10 / Windows 11（x64） |
| NuGet 包 | `Microsoft.VisualStudio.SDK 17.6.*`、`Microsoft.VSSDK.BuildTools 18.5.*` |

> **说明**：项目目标框架为 `net48`（.NET Framework 4.8），WPF 支持通过 `<UseWPF>true</UseWPF>` 启用。BuildTools 18.x 本身依赖 .NET 8 运行，但通过构建后 Target（`CleanNet8DllsFromVsix`）将 .NET 8 相关 DLL 从最终 VSIX 产物中彻底移除，最终扩展 100% 运行在 .NET Framework 4.8 上。

---

## 从源码构建

### 方法一：命令行构建（推荐）

打开 **Developer PowerShell for VS 2022**（确保 `msbuild` 已在 PATH 中），在项目根目录执行：

> **重要**：VSIX 打包依赖 VS 安装目录中的 `Microsoft.VsSDK.targets`，必须使用 **VS 自带的 MSBuild**（而非 `dotnet build`）才能生成 `.vsix` 文件。

```powershell
# 切换到解决方案根目录
cd D:\AIGC\SqlFM

# Debug 构建（开发调试用，不生成 vsix）
dotnet build SqlFM.sln --configuration Debug

# Release 构建（发布打包用，使用 VS MSBuild 生成 vsix）
& "C:\Program Files\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe" `
    src\SqlFM\SqlFM.csproj `
    /p:Configuration=Release `
    /p:VisualStudioVersion=17.0 `
    /restore
```

构建产物位于：
- Debug：`src\SqlFM\bin\Debug\net48\`
- Release：`src\SqlFM\bin\Release\net48\`

### 方法二：Visual Studio 构建

1. 打开 Visual Studio 2022，选择 **"打开项目或解决方案"**
2. 定位并打开 `D:\AIGC\SqlFM\SqlFM.sln`
3. 在工具栏的配置下拉框中选择 **Release**（或 **Debug**）
4. 按 **Ctrl+Shift+B**，或菜单 **生成 → 生成解决方案**
5. 查看 **输出** 窗口确认构建是否成功

---

## 打包 VSIX

VSIX 文件在 **Release 模式**使用 VS MSBuild 构建完成后自动生成。

> **注意**：`dotnet build` 不会触发 VSIX 打包流程，必须使用 VS 安装目录中的 `MSBuild.exe`。

```powershell
# 使用 VS 2022 自带的 MSBuild 执行 Release 构建
& "C:\Program Files\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe" `
    src\SqlFM\SqlFM.csproj `
    /p:Configuration=Release `
    /p:VisualStudioVersion=17.0 `
    /restore
```

构建完成后，VSIX 文件位于：

```
src\SqlFM\bin\Release\net48\SqlFM.vsix
```

> **注意**：`source.extension.vsixmanifest` 中声明的扩展版本为 `1.0.0`，发布新版本时需同步更新该文件中的 `Version` 属性。此外，安装目标已配置为 `Microsoft.VisualStudio.Ssms`（SSMS 22.0 及以上），而非 Visual Studio。

---

## 运行测试

测试项目位于 `tests\SqlFM.Tests\`，包含词法分析、语法分析和格式化的单元测试。

```powershell
# 在解决方案根目录执行
cd D:\AIGC\SqlFM

# 运行全部测试
dotnet test SqlFM.sln

# 运行测试并输出详细日志
dotnet test SqlFM.sln --verbosity normal

# 仅运行特定测试项目
dotnet test tests\SqlFM.Tests\SqlFM.Tests.csproj
```

测试文件说明：

| 文件 | 说明 |
|------|------|
| `LexerTests.cs` | SQL 词法分析单元测试 |
| `ParserTests.cs` | SQL 语法解析单元测试 |
| `FormatterTests.cs` | 格式化输出结果单元测试 |

---

## 项目结构说明

```
SqlFM\
├── SqlFM.sln                      # 解决方案文件
│
├── src\SqlFM\                     # VSIX 扩展主项目（net48）
│   ├── SqlFMPackage.cs            # 扩展包入口，注册命令和选项页
│   ├── source.extension.vsixmanifest      # VSIX 元数据（版本、描述、目标平台）
│   ├── SqlFM.pkgdef               # 手动维护的注册表项定义
│   ├── CommandTable\
│   │   └── SqlFMCommandTable.vsct # 菜单、按钮及快捷键定义
│   ├── Commands\                           # 6 个菜单命令实现
│   │   ├── FormatSelectedCommand.cs       # "格式化选中 SQL"
│   │   ├── FormatAllCommand.cs            # "格式化全部 SQL"
│   │   ├── CaseUpperCommand.cs            # "关键字大写"
│   │   ├── CaseLowerCommand.cs            # "关键字小写"
│   │   ├── InsertExemptionCommand.cs      # "插入豁免标记"
│   │   └── FormatOptionsCommand.cs        # "格式化选项"
│   ├── Editor\
│   │   └── EditorHelper.cs               # VS 编辑器交互（获取/替换文本）
│   ├── Options\
│   │   ├── GeneralOptionsPage.cs          # 工具 → 选项页（FormatOnSave 开关）
│   │   ├── SettingsWindow.xaml            # WPF 样式配置窗口
│   │   └── SettingsViewModel.cs           # 配置窗口的 MVVM ViewModel
│   └── Services\
│       ├── FormatService.cs               # 格式化服务（VSIX 侧调用入口）
│       └── StyleManager.cs                # 样式持久化管理
│
├── src\SqlFM.Core\                # 格式化核心库（net48，无 VS 依赖）
│   ├── Engine\                             # 格式化引擎
│   │   ├── FormatterPipeline.cs           # 格式化管道（豁免 → 格式化 → 后处理）
│   │   ├── PoorMansEngine.cs              # 主格式化引擎
│   │   ├── ScriptDomEngine.cs             # ScriptDom 语法验证
│   │   └── CaseConverter.cs               # 关键字大小写转换
│   ├── Configuration\                      # 85+ 项配置模型（8 分组）
│   ├── Exemption\                          # 豁免标记处理
│   ├── PresetStyles\                       # 5 个内置预设样式
│   ├── Refactoring\                        # SQL 重构（SELECT * 展开、JOIN 转换等）
│   └── Batch\                              # 批量格式化 + 数据库元数据批处理
│
├── src\SqlFM.Cli\                  # 命令行工具（net48）
│   ├── Program.cs                         # CLI 入口
│   └── CliOptions.cs                      # 参数解析
│
├── tests\SqlFM.Tests\              # 单元测试
│   ├── LexerTests.cs
│   ├── ParserTests.cs
│   └── FormatterTests.cs
│
└── installer\                              # Inno Setup 安装包构建
    ├── SqlFMSetup.iss
    └── build-installer.ps1
```

---

## 调试扩展

### 方法一：F5 启动实验实例（推荐）

项目已预配置启动参数，按 F5 即可在 VS 实验实例中调试：

- **启动程序**：`$(DevEnvDir)devenv.exe`（Visual Studio 主程序）
- **启动参数**：`/rootsuffix Exp`（使用独立的实验注册表配置）

操作步骤：
1. 在 Visual Studio 中打开解决方案
2. 将 `SqlFM` 设为启动项目
3. 按 **F5** 或点击 **调试 → 启动调试**
4. Visual Studio 实验实例启动后，可在其中测试扩展功能
5. 在主 Visual Studio 实例中设置断点，即可命中调试

> 实验实例与正式 VS 环境相互隔离，安全进行调试，不影响日常开发环境。

### 方法二：附加到 SSMS 进程调试

若需在真实 SSMS 环境中调试：

1. 先将扩展安装到 SSMS（参见 [INSTALL.md](./INSTALL.md)）
2. 启动 SSMS（`ssms.exe`）
3. 在 Visual Studio 中选择菜单 **调试 → 附加到进程**（`Ctrl+Alt+P`）
4. 在进程列表中找到 `ssms.exe`，点击 **附加**
5. 在源码中设置断点，触发格式化操作即可命中

> 附加调试前确保 Debug 构建的 PDB 符号文件（`.pdb`）与已安装的 DLL 版本一致。
