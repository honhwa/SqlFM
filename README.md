# SqlFM

> 一款专为 SQL Server Management Studio 22.6 打造的 T-SQL 格式化扩展——直接在编辑器里把乱糟糟的 SQL 整干净。

---

**格式化前：**

```sql
select a.id,a.name,b.amount from orders a inner join order_details b on a.id=b.order_id where a.status='active' and b.amount>100 order by b.amount desc
```

**格式化后：**

```sql
SELECT
    a.id,
    a.name,
    b.amount
FROM orders AS a
INNER JOIN order_details AS b
    ON a.id = b.order_id
WHERE
    a.status = 'active'
    AND b.amount > 100
ORDER BY b.amount DESC
```

---

## 它和别的格式化工具有什么不同

**嵌入 SSMS，不是外部工具。** 选中 SQL，按 `Ctrl+K, Ctrl+F`，完事。不需要复制粘贴到网页，不需要切换窗口。

**85+ 项可配，但默认就够用。** 从缩进宽度到逗号位置，从 JOIN 换行规则到 CTE 格式，全部可调。懒得调？5 个内置预设直接选。

**WPF 可视化配置窗口，所见即所得。** 实时预览格式化效果，改一个参数立刻看到变化。支持导出 `.sqlstyle` 文件给团队共享。

**豁免标记保护关键代码。** 写了精心排版的手写 SQL 不想被破坏？加一行注释就行。

**不止格式化，还能重构。** SELECT * 展开、隐式 JOIN 转换、方括号标准化、dbo 架构前缀——都是 SQL 审计里的高频需求。

**菜单 + 右键 + 工具栏 + 快捷键，四种触发方式。** 怎么顺手怎么来。

---

## 快速上手

1. 下载 [SqlFM.vsix](../../releases/latest)
2. **关闭 SSMS**，双击安装
3. 勾选 **SQL Server Management Studio 22**，点安装
4. 重启 SSMS，打开一个查询窗口
5. 写一段 SQL，按 `Ctrl+K, Ctrl+F`

菜单栏会出现 **"SqlFM"** 菜单，右键菜单和工具栏里也有同样选项。

---

## 功能一览

### 格式化

| 操作 | 怎么触发 |
|------|---------|
| 格式化选中 SQL | 菜单 / 右键 / 工具栏 / `Ctrl+K, Ctrl+F` |
| 格式化全部 SQL | 菜单 / 右键 / 工具栏 / `Ctrl+K, Ctrl+D` |
| 格式化选项 | 菜单 / 右键 / 工具栏 / `Ctrl+K, Ctrl+O` |
| 关键字转大写 | 菜单 / 右键 / `Ctrl+B, Ctrl+U` |
| 关键字转小写 | 菜单 / 右键 / `Ctrl+B, Ctrl+L` |
| 插入豁免标记 | 菜单 / 右键 / `Ctrl+D, Ctrl+I` |
| 保存时自动格式化 | 工具 → 选项 → SqlFM → 勾选开启 |

### 精细化配置

打开 **SqlFM → 格式化选项**（或按 `Ctrl+K, Ctrl+O`），进入可视化配置窗口：

- **8 个配置分组**：全局通用、DML 语句、CTE、CASE WHEN、流程控制、DDL、表达式、T-SQL 专有
- **实时预览**：改参数立刻在示例 SQL 上看到效果
- **样式管理**：新建、复制、重命名、删除、导入、导出 `.sqlstyle` 文件
- **设为默认**：选一个样式设为全局默认，覆盖工具 → 选项页面

部分关键配置项：

| 配置项 | 可选值 |
|--------|--------|
| 缩进宽度 | 2 / 4 / 8 空格 |
| 关键字大小写 | 全大写 / 全小写 / 保持 |
| 逗号位置 | 行末后置 / 行首前置 |
| 分号处理 | 自动添加 / 自动移除 / 保持 |
| JOIN 新行 | 是 / 否 |
| 最大行宽 | 80–200 |

### 豁免标记

不想被格式化的代码块，用标记包起来即可。支持三种语法：

**块豁免（推荐）：**

```sql
SELECT id, name FROM users;

/* FORMAT OFF */
/* 这段代码排版是精心设计过的，请保持原样    */
SELECT   a,  b,  c   FROM   legacy_table;
/* FORMAT ON */

SELECT * FROM logs;
```

**单行豁免：**

```sql
SELECT 1+1 AS result; -- NOFORMAT
```

**正则豁免：** 在配置窗口的"忽略规则"中填写正则表达式，匹配到的代码段自动跳过格式化。

### SQL 重构

| 功能 | 说明 | 示例 |
|------|------|------|
| SELECT * 展开 | 将 `*` 替换为实际列名 | `SELECT *` → `SELECT Id, Name, Amount` |
| 隐式 JOIN 转换 | 逗号分隔的 FROM 转为显式 INNER JOIN | `FROM t1, t2 WHERE t1.id = t2.id` → `FROM t1 INNER JOIN t2 ON t1.id = t2.id` |
| 方括号标准化 | 统一添加或移除方括号 | 自动判断保留关键字，避免语法错误 |
| dbo 架构前缀 | 为无架构前缀的表自动补 `dbo.` | `FROM Orders` → `FROM dbo.Orders` |

### 命令行工具

独立 CLI 工具 `SqlFMCli`，适合 CI/CD 管线和批量处理：

```bash
# 格式化单个文件（原地覆写）
SqlFMCli -f script.sql

# 递归格式化目录
SqlFMCli -f ./sql-folder

# 仅检查不修改（CI 模式）
SqlFMCli -f ./sql-folder --check

# 自定义样式 + 指定编码
SqlFMCli -f legacy.sql -s team.sqlstyle -e gbk

# 输出到独立目录，保留目录结构
SqlFMCli -f ./sql -o ./formatted
```

退出码适配 CI：`0` = 全部合规，`1` = 有文件被格式化，`2` = 部分失败，`3` = 参数错误，`4` = 致命错误。

```yaml
# GitHub Actions 示例
- name: Check SQL formatting
  run: SqlFMCli -f ./sql --check
```

---

## 预设样式

内置 5 个预设，开箱即用：

| 预设 | 缩进 | 特点 | 适用场景 |
|------|------|------|---------|
| **Default** | 4 空格 | 关键字大写、逗号后置、行宽 120 | 通用，大多数团队的首选 |
| **CommasBefore** | 4 空格 | 逗号前置、列别名对齐 | 方便 SELECT 列表中增删字段 |
| **RightAlign** | 4 空格 | 全面对齐：列别名、运算符、ORDER BY、VALUES | 追求视觉整齐对称 |
| **CompactIndented** | 2 空格 | 语句间无空行、紧凑布局 | 屏幕空间有限或代码密度需求 |
| **SingleLineCompact** | 2 空格 | 行宽 200、CTE 和简单 CASE 单行显示 | 临时查询、脚本快速阅读 |

---

## 快捷键

| 快捷键 | 功能 |
|--------|------|
| `Ctrl+K, Ctrl+F` | 格式化选中 SQL |
| `Ctrl+K, Ctrl+D` | 格式化全部 SQL |
| `Ctrl+K, Ctrl+O` | 打开格式化选项 |
| `Ctrl+B, Ctrl+U` | 关键字转大写 |
| `Ctrl+B, Ctrl+L` | 关键字转小写 |
| `Ctrl+D, Ctrl+I` | 插入豁免标记 |

> 如快捷键不生效（部分 SSMS 版本可能存在冲突），可在 **工具 → 选项 → 环境 → 键盘** 中重新绑定 `SqlFM.FormatAll` 等命令。

---

## 安装

**推荐方式：双击 VSIX 安装**

1. [下载最新 VSIX](../../releases/latest)
2. 关闭 SSMS
3. 双击 `SqlFM.vsix`，勾选 SQL Server Management Studio 22，安装
4. 重启 SSMS

**卸载：** SSMS → 扩展 → 管理扩展 → 找到 SqlFM → 卸载 → 重启 SSMS。

其他安装方式（exe 安装包、SSMS 扩展管理器）见 [docs/INSTALL.md](docs/INSTALL.md)。

---

## 系统要求

| 组件 | 要求 |
|------|------|
| SQL Server Management Studio | 22.6.0 (x64) |
| 操作系统 | Windows 10 / 11 (x64) |
| 运行时 | .NET Framework 4.8（SSMS 22 自带，无需单独安装） |

---

## 从源码构建

```powershell
# Debug 构建
dotnet build SqlFM.sln --configuration Debug

# Release 构建（生成 VSIX，需要使用 VS 自带的 MSBuild）
& "C:\Program Files\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe" `
    src\SqlFM\SqlFM.csproj `
    /p:Configuration=Release /restore
```

构建产物：`src\SqlFM\bin\Release\net48\SqlFM.vsix`

环境要求、调试方式、VS 实验实例详见 [docs/BUILD.md](docs/BUILD.md)。

---

## 贡献

欢迎提 Issue 和 PR。

**报告 Bug** 请包含：SSMS 版本、SqlFM 版本、最小复现 SQL、期望 vs 实际输出。

**提交 PR** 流程：Fork → `feat/xxx` 分支 → 补充测试 → 确保 `dotnet test` 通过 → 提交 PR。

核心格式化逻辑在 `SqlFM.Core`（无 VS 依赖），新增规则需在 `FormatterTests.cs` 中覆盖正反向用例。

---

## 许可证

[MIT](LICENSE)
