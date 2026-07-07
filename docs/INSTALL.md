# SqlFM 安装与使用指南

## 系统要求

| 组件 | 要求 |
|------|------|
| SQL Server Management Studio | **22.6.0**（x64） |
| 操作系统 | Windows 10 / Windows 11（x64） |
| .NET 运行时 | .NET 8 运行时（随 SSMS 22.6 一起安装，通常无需单独安装） |

> **注意**：`source.extension.vsixmanifest` 中声明的安装目标为 SSMS（`Microsoft.VisualStudio.Ssms`）22.0 及以上版本。VSIX 安装程序启动后将自动检测并显示 **SQL Server Management Studio 22**，而非 Visual Studio。

---

## 安装方法

### 方法一：双击 VSIX 文件安装（推荐）

1. 从发布包或自行编译（参见 [BUILD.md](./BUILD.md)）获取 `SqlFM.vsix`
2. **关闭正在运行的 SSMS**
3. 双击 `SqlFM.vsix`，VSIX 安装程序启动
4. 安装程序自动检测已安装的 SSMS，在目标列表中勾选 **SQL Server Management Studio 22**
5. 点击 **安装（Install）**
6. 安装完成后重新启动 SSMS，扩展即生效

### 方法二：通过 SSMS 扩展管理器安装

1. 启动 SSMS，在菜单栏点击 **扩展（Extensions）**
2. 选择 **管理扩展（Manage Extensions）**
3. 在扩展管理器左侧选择 **已安装**，点击右上角 **从文件安装（Install from File）**
4. 浏览定位 `SqlFM.vsix` 文件，点击 **打开**
5. 根据提示完成安装，重启 SSMS 后生效

---

## 使用方法

### 顶级菜单

安装成功后，SSMS 菜单栏会新增 **"SQL 格式化"** 顶级菜单，包含以下三项：

| 菜单项 | 说明 |
|--------|------|
| 格式化选中 SQL | 对当前查询编辑器中选中的 SQL 文本进行格式化 |
| 格式化全部 SQL | 对当前查询编辑器中全部 SQL 文本进行格式化 |
| 格式化选项 | 打开格式化参数配置对话框 |

### 右键菜单

在查询编辑器中右键单击，上下文菜单中同样包含：
- **格式化选中 SQL**
- **格式化全部 SQL**

右键菜单与顶级菜单功能完全一致，方便快速操作。

### 快捷键

| 快捷键 | 功能 |
|--------|------|
| `Ctrl+K, Ctrl+F` | 格式化选中 SQL |
| `Ctrl+K, Ctrl+D` | 格式化全部 SQL |

> 快捷键由 `SqlFMCommandTable.vsct` 定义，绑定到标准 VS 编辑器上下文（`guidVSStd97`）。

#### 快捷键冲突说明

`Ctrl+K, Ctrl+D` 在部分版本的 SSMS 中已被占用（默认绑定为"设置文档格式"）。若快捷键不生效，可按以下步骤重新绑定：

1. 打开 SSMS，点击菜单 **工具（Tools） → 选项（Options）**
2. 导航至 **环境 → 键盘（Environment → Keyboard）**
3. 在 **"显示包含以下内容的命令"** 中搜索 `SqlFM`
4. 选中 `SqlFM.FormatAll` 命令
5. 在 **"按快捷键"** 文本框中输入新的组合键，点击 **指定（Assign）**

---

## 配置选项

格式化选项路径：**工具（Tools） → 选项（Options） → SqlFM → 格式化选项**

| 配置项 | 类型 | 默认值 | 说明 |
|--------|------|--------|------|
| 缩进宽度（IndentWidth） | 整数 | `4` | 每级缩进的空格数，支持 `2`、`4`、`8` |
| 关键字大写（UppercaseKeywords） | 布尔 | `true` | 是否将 SQL 关键字（SELECT、FROM、WHERE 等）转为大写 |
| 逗号位置（CommaPosition） | 枚举 | `Trailing` | `Trailing`：逗号后置（`col1, col2`）；`Leading`：逗号前置（换行后置于行首） |
| 子句强制换行（ForceClauseNewLine） | 布尔 | `true` | 是否在 SELECT、FROM、WHERE、GROUP BY 等子句前强制换行 |
| 注释跟随缩进（IndentComments） | 布尔 | `true` | 注释是否跟随对应语句的缩进级别 |

### 导出配置

可将当前配置导出为 JSON 文件，便于团队共享或备份：

1. 点击菜单 **SQL 格式化 → 格式化选项**
2. 在选项对话框中点击 **导出配置**
3. 选择保存路径，生成 `.json` 配置文件

导出的 JSON 文件示例：

```json
{
  "indentWidth": 4,
  "uppercaseKeywords": true,
  "commaPosition": "Trailing",
  "forceClauseNewLine": true,
  "indentComments": true
}
```

### 导入配置

1. 点击菜单 **SQL 格式化 → 格式化选项**
2. 在选项对话框中点击 **导入配置**
3. 选择之前导出的 `.json` 配置文件，配置立即生效

> JSON 属性名采用驼峰命名法（camelCase），枚举值以字符串形式存储（如 `"Trailing"`、`"Leading"`）。

---

## 卸载方法

1. 启动 SSMS，点击菜单 **扩展（Extensions） → 管理扩展（Manage Extensions）**
2. 在左侧选择 **已安装（Installed）**
3. 找到 **SqlFM - T-SQL 格式化工具**
4. 点击 **卸载（Uninstall）**
5. 按提示重启 SSMS，扩展完全移除

---

## 常见问题排查

### 扩展安装后菜单未出现

- 确认 SSMS 版本为 **22.6.0**（可在 SSMS 中点击 **帮助 → 关于** 查看）
- 确认安装时 VSIX 安装程序已勾选 SSMS 作为安装目标
- 尝试以**管理员权限**重新运行 VSIX 安装程序
- 检查 SSMS 的 ActivityLog：
  - 以 `/log` 参数启动 SSMS：`ssms.exe /log`
  - 日志默认位于 `%AppData%\Microsoft\VisualStudio\<版本>\ActivityLog.xml`
  - 在日志中搜索 `SqlFM` 查找错误信息

### 扩展无法加载 / 加载出错

- 确认系统已安装 **.NET 8 运行时**（可运行 `dotnet --list-runtimes` 检查）
- 若提示程序集版本冲突，尝试卸载后重新安装最新版 VSIX
- 查看 ActivityLog.xml（路径同上），搜索 `Error` 或 `Exception` 关键词

### 快捷键不生效

1. 确认快捷键未被其他扩展或 SSMS 自身占用
2. 按照 [快捷键冲突说明](#快捷键冲突说明) 中的步骤重新绑定快捷键
3. 重启 SSMS 后再次测试

### 格式化结果不符预期

- 检查 **工具 → 选项 → SqlFM** 中的配置项是否符合预期
- 确认输入的 SQL 语法正确（语法错误的 SQL 可能导致格式化结果异常）
- 对于复杂嵌套查询，建议先选中目标片段使用 **格式化选中 SQL** 进行局部格式化
- 若发现格式化 Bug，可提交 Issue 并附上最小复现 SQL 示例

### 配置导入失败

- 确认 JSON 文件编码为 **UTF-8**
- 确认 JSON 属性名为驼峰命名法（`indentWidth` 而非 `IndentWidth`）
- 确认枚举值为字符串格式（`"Trailing"` 而非 `0`）
- 可参考 [导出配置](#导出配置) 章节中的 JSON 示例进行比对
