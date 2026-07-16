# SqlFM 优化方案：借鉴 sqlfluff，走向全方言 SQL 格式化 + Lint + 补全

> 基于 sqlfluff (v4.2.2) 项目深度分析，结合 SqlFM (.NET Framework 4.8 VSIX) 技术约束，制定分阶段优化路线图。
> 生成日期：2026-07-16

---

## 一、sqlfluff 核心优势提取

### 1. 模块化方言系统（最关键）
sqlfluff 以 ANSI SQL 为基础方言，28+ 方言通过继承 + 覆盖扩展：
- `ansi` → `tsql`, `postgres`, `mysql`, `bigquery`, `snowflake`, `sqlite`, `oracle` 等
- 子方言只需覆盖差异段（`dialect.replace(SomeSegment=NewSegment)`）
- 关键字集合（reserved / unreserved / future_reserved）自动注册为 KeywordSegment
- `ref()` 延迟绑定机制：覆盖部分段后，底层段仍可正常引用

**SqlFM 当前差距：** 仅支持 T-SQL（通过 ScriptDom TSql160Parser），无方言体系。

### 2. 规则引擎 + 自动修复（核心差异化）
75 条规则分 11 组，每条规则：
- 可配置（`config_keywords`）+ 可自动修复（`is_fix_compatible`）
- 通过 `crawl_behaviour` 定义 AST 遍历策略（哪些 segment 类型需检查）
- 修复操作基于 AST 结构而非纯文本替换（"parse-shaped replacements"）
- 模板安全修复检查：修复不破坏 Jinja/dbt 模板结构
- `memory` 机制：规则可在段间传递工作状态（如 AL05 检测未使用别名需跨段追踪）

**SqlFM 当前差距：** 无 Lint 功能，仅有格式化。配置项虽有 85+，但缺少"检测问题 → 报告 → 自动修复"闭环。

### 3. 4 阶段管道架构
```
模板渲染 → 词法分析 → AST 解析 → 规则检查 + 修复
```
每阶段独立、可替换、可组合。Rust 后端可替换 Python 解析器。

**SqlFM 当前差距：** 6 步管道（豁免→预重构→主格式化→后处理→恢复豁免→清理），但后处理是平铺的 if/else 判断而非规则遍历。

### 4. 模板感知
原生支持 Jinja/dbt/Python format/SQL 占位符，格式化模板 SQL 时：
- 模板区域豁免检查，不报告模板块的 lint 错误
- 原始位置映射，lint 错误指向源文件而非渲染结果

**SqlFM 当前差距：** 有 3 种豁免机制（FORMAT OFF/ON、NOFORMAT、正则忽略），但不理解模板语法。

### 5. 可扩展插件系统
dbt 通过独立插件接入，插件可注册新规则、新方言。

**SqlFM 当前差距：** 无插件机制，所有功能硬编码在 Core 库中。

---

## 二、SqlFM 约束条件（不可变更）

| 约束 | 说明 |
|------|------|
| **运行时** | .NET Framework 4.8（SSMS 22.6 内嵌版本，不可升级） |
| **平台** | VSIX 扩展，运行在 SSMS 22.6 (x64) 内 |
| **主引擎** | Poor Man's T-SQL Formatter（NuGet 包，13 参数构造限制） |
| **T-SQL 解析** | ScriptDom TSql160Parser（仅支持 T-SQL） |
| **VSIX 限制** | VS 2022 SDK 的 VSIX 不能使用 .NET 8；UI 需 WPF |
| **部署** | Inno Setup 安装包 + VSIX 侧载（SSMS 不支持 Marketplace） |

---

## 三、优化路线图

### Phase 1：规则引擎 + Lint 体系（核心架构变革）

**目标：** 将 SqlFM 从"纯格式化工具"升级为"格式化 + Lint + 自动修复"工具。

#### 1.1 创建 SqlRuleEngine

```
src/SqlFM.Core/Lint/
├── SqlRule.cs                  — 规则基类（ISqlRule 接口）
├── SqlRuleEngine.cs            — 规则引擎（加载/过滤/执行规则）
├── LintResult.cs               — 检测结果（位置、描述、修复建议）
├── LintFix.cs                  — 修复操作（替换/插入/删除）
├── RuleCrawler.cs              — AST 遍历策略
├── RuleContext.cs              — 规则执行上下文
├── RuleSeverity.cs             — 严重等级（Error/Warning/Info）
└── Rules/
    ├── Layout/
    │   ├── LT01_SpacingRule.cs       — 空格间距
    │   ├── LT02_IndentRule.cs        — 缩进检查
    │   ├── LT05_LongLinesRule.cs     — 行长度检查
    │   ├── LT12_EndOfFileRule.cs     — 文件末尾换行
    │   └── LT15_ExcessiveNewlinesRule.cs — 多余空行
    ├── Capitalisation/
    │   ├── CP01_KeywordCaseRule.cs   — 关键字大小写一致性
    │   ├── CP02_IdentifierCaseRule.cs — 标识符大小写
    │   ├── CP03_FunctionCaseRule.cs  — 函数名大小写
    │   ├── CP04_LiteralCaseRule.cs   — 布尔/NULL字面量大小写
    │   ├── CP05_DataTypeCaseRule.cs  — 数据类型大小写
    ├── Convention/
    │   ├── CV01_NotEqualRule.cs      — != vs <> 一致性
    │   ├── CV03_TrailingCommaRule.cs — SELECT 尾随逗号
    │   ├── CV05_IsNullRule.cs        — IS NULL vs = NULL
    │   ├── CV06_SemicolonRule.cs     — 语句分号结尾
    │   ├── CV12_JoinConditionRule.cs — JOIN ON vs WHERE 连接
    ├── Aliasing/
    │   ├── AL02_ColumnAliasRule.cs   — 列别名 AS 要求
    │   ├── AL04_UniqueAliasRule.cs   — 别名唯一性
    │   ├── AL05_UnusedAliasRule.cs   — 未使用别名检测
    │   ├── AL09_SelfAliasRule.cs     — 自别名检测（col AS col）
    ├── Structure/
    │   ├── ST03_UnusedCteRule.cs     — 未使用 CTE
    │   ├── ST01_ElseNullRule.cs      — ELSE NULL 冗余
    │   ├── ST05_SubqueryRule.cs      — 子查询简化建议
    ├── Ambiguous/
    │   ├── AM01_DistinctGroupByRule.cs — DISTINCT + GROUP BY 冗余
    │   ├── AM02_UnionRule.cs          — UNION DISTINCT/ALL 明确性
    │   ├── AM04_SelectStarRule.cs     — SELECT * 检测
    └── Tsql/
        ├── TQ01_SpPrefixRule.cs       — sp_ 前缀检查
        ├── TQ02_ProcedureBeginEndRule.cs — BEGIN...END 检查
```

#### 1.2 ISqlRule 接口设计

```csharp
public interface ISqlRule
{
    string RuleId { get; }           // "LT01", "CP01" 等
    string RuleName { get; }         // "layout.spacing"
    string Description { get; }      // 规则描述
    RuleSeverity Severity { get; }   // Error/Warning/Info
    bool IsFixCompatible { get; }    // 是否支持自动修复
    string[] ConfigKeywords { get; } // 可配置参数名列表
    string[] Groups { get; }         // 规则组 ("core", "layout", "tsql")
    SqlDialect[] SupportedDialects { get; } // 适用方言

    /// 基于 AST 或 token 流评估，返回 LintResult 列表
    List<LintResult> Evaluate(RuleContext context);
}
```

#### 1.3 LintFix 修复操作

```csharp
public enum LintFixType { Replace, InsertBefore, InsertAfter, Delete }

public class LintFix
{
    public LintFixType Type;
    public int Line;                 // 修复位置行号
    public int Column;               // 修复位置列号
    public string OriginalText;      // 原文本
    public string ReplacementText;   // 替换文本（Replace 时）
    public string InsertText;        // 插入文本（Insert 时）
}
```

#### 1.4 FormatterPipeline 扩展

在现有 6 步管道后增加 Lint 步骤：

```
Step 1: 豁免提取
Step 2: 预重构（JoinConverter）
Step 3: 主格式化（PoorMans）
Step 4: 后处理（大小写 + 对齐 + 重构 + T-SQL 规则）
Step 5: 恢复豁免
Step 6: 最终清理
Step 7: Lint 检查（新） → 返回 LintResult[] + LintFix[]
Step 8: 自动修复（新） → 应用 is_fix_compatible 规则的修复建议
```

#### 1.5 SSMS 集成 — 错误列表窗口

- 通过 `IVsErrorList` / `ErrorListProvider` 将 LintResult 显示在 SSMS 错误列表中
- Error → 红色波浪线（类似 VS Code 的 diagnostic underline）
- Warning → 绿色波浪线
- Info → 蓝色波浪线
- 支持双击跳转到对应行

---

### Phase 2：多方言支持

**目标：** 从纯 T-SQL 扩展到 ANSI + MySQL + PostgreSQL + SQLite + Oracle 等主流方言。

#### 2.1 方言体系设计

```
src/SqlFM.Core/Dialects/
├── SqlDialect.cs               — 方言基类（名称、关键字集合、解析器引用）
├── AnsiDialect.cs              — ANSI SQL 基础方言
├── TsqlDialect.cs              — Transact-SQL（继承 Ansi + ScriptDom）
├── MySqlDialect.cs             — MySQL（继承 Ansi + ANTLR 解析器）
├── PostgreSqlDialect.cs        — PostgreSQL（继承 Ansi + ANTLR 解析器）
├── SqliteDialect.cs            — SQLite（继承 Ansi）
├── OracleDialect.cs            — Oracle PL/SQL（继承 Ansi）
├── DialectRegistry.cs          — 方言注册表（名称 → 方言实例）
└── KeywordSets/
    ├── AnsiKeywords.cs          — ANSI SQL 关键字集合
    ├── TsqlKeywords.cs          — T-SQL 扩展关键字
    ├── MySqlKeywords.cs         — MySQL 扩展关键字
    └── PostgreSqlKeywords.cs    — PostgreSQL 扩展关键字
```

#### 2.2 方言继承机制（借鉴 sqlfluff）

```csharp
public class SqlDialect
{
    public string Name { get; }
    public string InheritsFrom { get; }          // 父方言名
    public HashSet<string> ReservedKeywords { get; }
    public HashSet<string> UnreservedKeywords { get; }
    public HashSet<string> FutureReservedKeywords { get; }
    public IFormatterEngine FormatterEngine { get; }  // 方言专用格式化引擎
    public ISqlParser Parser { get; }                 // 方言专用解析器
    public Dictionary<string, SqlRule> DialectRules { get; } // 方言专属规则
}
```

#### 2.3 多引擎架构

方言与格式化引擎/解析器解耦：

| 方言 | 格式化引擎 | 解析器 |
|------|----------|--------|
| ANSI | PoorMans（默认） | ANTLR ANSI grammar |
| T-SQL | PoorMans + ScriptDom 后处理 | ScriptDom TSql160Parser |
| MySQL | PoorMans + MySQL 后处理 | ANTLR MySQL grammar |
| PostgreSQL | PoorMans + PG 后处理 | ANTLR PostgreSQL grammar |
| SQLite | PoorMans | ANTLR SQLite grammar |
| Oracle | PoorMans + Oracle 后处理 | ANTLR Oracle grammar |

ANTLR 语法文件来自 [grammars-v4](https://github.com/antlr/grammars-v4)，编译为 .NET 4.8 的 C# 解析器。

#### 2.4 方言选择 UI

- 在 WPF 配置窗口添加"方言"下拉（Default/T-SQL/MySQL/PostgreSQL/SQLite/Oracle/ANSI）
- CLI 添加 `--dialect` 参数：`SqlFMCli --dialect mysql input.sql`
- VSIX 命令栏添加方言选择器

---

### Phase 3：SQL 补全功能

**目标：** 在 SSMS 编辑器中提供智能 SQL 补全建议。

#### 3.1 补全类型

| 补全类型 | 数据来源 | 实现方式 |
|---------|---------|---------|
| 关键字补全 | 方言关键字集合 | `IVsCompletionSet` 静态列表 |
| 函数名补全 | 方言内置函数列表 | `IVsCompletionSet` 静态列表 |
| 数据类型补全 | 方言数据类型列表 | `IVsCompletionSet` 静态列表 |
| Snippet 补全 | 预定义 SQL 模板 | `IVsExpansionManager` 代码片段 |
| 表名/列名补全 | 数据库元数据 | 通过 SSMS DTE 获取当前连接 |

#### 3.2 补全引擎设计

```
src/SqlFM.Core/Completion/
├── SqlCompletionEngine.cs       — 补全引擎主类
├── CompletionItem.cs            — 补全项（显示文本、插入文本、图标、描述）
├── CompletionContext.cs         — 补全上下文（光标位置、前缀、方言）
├── KeywordCompletionProvider.cs — 关键字补全
├── FunctionCompletionProvider.cs — 函数名补全
├── DataTypeCompletionProvider.cs — 数据类型补全
├── SnippetCompletionProvider.cs  — SQL Snippet 补全
├── MetadataCompletionProvider.cs — 表名/列名补全（需数据库连接）
└── Snippets/
    ├── SelectSnippet.cs          — SELECT 模板
    ├── InsertSnippet.cs          — INSERT 模板
    ├── UpdateSnippet.cs          — UPDATE 模板
    ├── DeleteSnippet.cs          — DELETE 模板
    ├── JoinSnippet.cs            — JOIN 模板
    ├── CteSnippet.cs             — CTE 模板
    └── IfSnippet.cs              — IF...ELSE 模板
```

#### 3.3 SSMS 集成

- 通过 `IOleCommandTarget` 拦截 Ctrl+Space / Ctrl+J 触发补全
- 通过 `IVsTextView` 获取光标位置和已输入前缀
- 通过 `IVsCompletionSet` 显示补全下拉列表
- 方言关键字集合作为补全数据源（Phase 2 的 KeywordSets 直接复用）

#### 3.4 方言专属补全列表

每个方言定义自己的补全数据：

```csharp
public class TsqlCompletionData
{
    // 关键字
    public static string[] Keywords => new[] { "SELECT", "FROM", "WHERE", "INSERT", ... };

    // 内置函数
    public static CompletionItem[] Functions => new[]
    {
        new CompletionItem("COUNT", "COUNT(*)", FunctionIcon, "Returns the number of rows"),
        new CompletionItem("SUM", "SUM(expression)", FunctionIcon, "Returns the sum of values"),
        // ... 100+ T-SQL 函数
    };

    // 数据类型
    public static string[] DataTypes => new[] { "INT", "VARCHAR", "NVARCHAR", "DATETIME2", ... };

    // Snippets
    public static SnippetItem[] Snippets => new[]
    {
        new SnippetItem("sel", "SELECT $columns$ FROM $table$ WHERE $condition$"),
        new SnippetItem("ins", "INSERT INTO $table$ ($columns$) VALUES ($values$)"),
        // ... 20+ 常用模板
    };
}
```

---

### Phase 4：CLI 增强 + CI/CD 集成

**目标：** 将 SqlFMCli 从简单格式化工具升级为 Lint + 格式化 + 报告的一体化 CLI。

#### 4.1 新增 CLI 命令

```bash
# 格式化（现有）
SqlFMCli format input.sql --dialect tsql --style Default

# Lint 检查（新）
SqlFMCli lint input.sql --dialect tsql --rules core

# 自动修复（新）
SqlFMCli fix input.sql --dialect tsql --rules core

# Lint + 格式化组合（新）
SqlFMCli check input.sql --dialect tsql --format --lint

# 输出报告（新）
SqlFMCli lint input.sql --dialect tsql --output-format json --output-file report.json
SqlFMCli lint input.sql --dialect tsql --output-format sarif --output-file report.sarif
```

#### 4.2 SARIF 输出

生成 SARIF (Static Analysis Results Interchange Format) 报告，兼容 GitHub Actions、Azure DevOps 等平台。

#### 4.3 退出码细化

| 退出码 | 含义 |
|--------|------|
| 0 | 全部通过（格式化 + lint 无问题） |
| 1 | 格式化差异（--check 模式） |
| 2 | Lint 错误（至少 1 个 Error 级规则违规） |
| 3 | Lint 警告（至少 1 个 Warning 级规则违规） |
| 4 | 解析失败（SQL 无法解析） |
| 5 | 配置错误 |

---

### Phase 5：插件系统 + 模板感知

**目标：** 支持 SQL 模板语法（如 Jinja、SSMS 变量替换）和第三方方言/规则插件。

#### 5.1 模板预处理

```
Step 0 (新): 模板渲染 → 将模板语法替换为占位符，豁免模板区域
Step 1: 豁免提取
Step 2: 预重构
...
Step 5: 恢复豁免
Step 6: 最终清理
Step 7: 模板区域还原 → 将占位符恢复为模板语法
Step 8: Lint 检查（跳过模板区域的违规）
Step 9: 自动修复
```

#### 5.2 插件架构

```csharp
public interface ISqlFMPlugin
{
    string Name { get; }
    string Version { get; }
    void RegisterDialects(DialectRegistry registry);
    void RegisterRules(SqlRuleEngine engine);
    void RegisterCompletionProviders(SqlCompletionEngine engine);
}
```

插件 DLL 放入 `%APPDATA%\SqlFM\plugins\` 目录，通过反射加载。

---

## 四、技术实现难点与对策

| 难点 | 对策 |
|------|------|
| **.NET 4.8 无 ANTLR 运行时** | ANTLR4 C# 目标支持 .NET Standard 2.0（兼容 net48）。语法文件从 grammars-v4 下载，用 antlr4-tool 编译为 C# |
| **ScriptDom 仅 T-SQL** | 保留 ScriptDom 作为 T-SQL 方言专用解析器。其他方言用 ANTLR 解析器。统一 ISqlParser 接口 |
| **PoorMans 13 参数限制** | PoorMans 仅用于 ANSI/T-SQL 主格式化。其他方言的格式化差异通过后处理器覆盖 |
| **SSMS 补全 API 复杂** | 先实现关键字/函数静态补全（`IVsCompletionSet`），后续再接入数据库元数据补全 |
| **VSIX 侧载限制** | 保持 Inno Setup 安装包 + VSIX 侧载模式。不支持 Marketplace |
| **规则遍历需要 AST** | ScriptDom 为 T-SQL 提供完整 AST。ANTLR 为其他方言提供 ParseTree。统一为 ISqlAst 接口 |
| **Lint 结果展示** | 使用 `ErrorListProvider` + `IVsTextMarker` 在 SSMS 编辑器中标注违规位置 |

---

## 五、优先级排序

| 优先级 | Phase | 核心交付 | 估算工作量 |
|--------|-------|---------|-----------|
| **P0** | Phase 1 | 规则引擎 + Lint 检查 + 自动修复 + SSMS 错误列表展示 | ★★★★★ |
| **P1** | Phase 2 | ANSI/T-SQL/MySQL/PostgreSQL 方言支持 + ANTLR 解析器集成 | ★★★★ |
| **P2** | Phase 3 | SQL 关键字/函数/数据类型补全 + Snippet 补全 | ★★★ |
| **P3** | Phase 4 | CLI lint/fix/report 增强 + SARIF 输出 | ★★ |
| **P4** | Phase 5 | 模板感知 + 插件系统 | ★ |

---

## 六、sqlfluff 75 条规则 → SqlFM 首批 25 条核心规则映射

借鉴 sqlfluff 的 core 规则体系，优先实现最实用的 25 条（覆盖布局、大小写、约定、别名、歧义、结构）：

| SqlFM RuleId | sqlfluff 对应 | 规则名 | 说明 | 可自动修复 |
|-------------|-------------|--------|------|-----------|
| LT01 | LT01 | layout.spacing | 元素间空格规范 | ✅ |
| LT02 | LT02 | layout.indent | 缩进检查 | ✅ |
| LT05 | LT05 | layout.long_lines | 行长度超限 | ❌（需手动调整） |
| LT12 | LT12 | layout.end_of_file | 文件末尾换行 | ✅ |
| LT15 | LT15 | layout.newlines | 连续空行过多 | ✅ |
| CP01 | CP01 | capitalisation.keywords | 关键字大小写不一致 | ✅ |
| CP02 | CP02 | capitalisation.identifiers | 标识符大小写不一致 | ✅ |
| CP03 | CP03 | capitalisation.functions | 函数名大小写不一致 | ✅ |
| CP04 | CP04 | capitalisation.literals | 布尔/NULL 字面量大小写 | ✅ |
| CP05 | CP05 | capitalisation.types | 数据类型大小写不一致 | ✅ |
| CV01 | CV01 | convention.not_equal | != vs <> 不一致 | ✅ |
| CV03 | CV03 | convention.select_trailing_comma | SELECT 尾随逗号 | ✅ |
| CV05 | CV05 | convention.is_null | IS NULL vs = NULL | ✅ |
| CV06 | CV06 | convention.terminator | 语句分号结尾 | ✅ |
| CV12 | CV12 | convention.join_condition | JOIN ON vs WHERE | ✅ |
| AL02 | AL02 | aliasing.column | 列别名 AS 要求 | ✅ |
| AL04 | AL04 | aliasing.unique.table | 表别名唯一性 | ❌ |
| AL05 | AL05 | aliasing.unused | 未使用别名 | ❌ |
| AL09 | AL09 | aliasing.self_alias.column | 自别名检测 | ✅ |
| AM01 | AM01 | ambiguous.distinct | DISTINCT + GROUP BY | ❌ |
| AM02 | AM02 | ambiguous.union | UNION DISTINCT/ALL | ✅ |
| AM04 | AM04 | ambiguous.column_count | SELECT * 检测 | ❌ |
| ST01 | ST01 | structure.else_null | ELSE NULL 冗余 | ✅ |
| ST03 | ST03 | structure.unused_cte | 未使用 CTE | ❌ |
| ST08 | ST08 | structure.distinct | DISTINCT 使用不一致 | ✅ |

**可自动修复比例：** 18/25 (72%)，与 sqlfluff 的 "auto-fix most linting errors" 目标一致。

---

## 七、架构总览图

```
┌───────────────────────────────────────────────────────────┐
│                    SqlFM VSIX Extension                     │
│  ┌─────────────────────────────────────────────────────┐  │
│  │              SSMS Integration Layer                   │  │
│  │  SqlFMPackage │ ErrorListProvider │ IVsCompletionSet │  │
│  │  FormatCommands │ LintCommand │ CompletionHandler    │  │
│  └─────────────────────────────────────────────────────┘  │
│                           │                                │
│  ┌─────────────────────────────────────────────────────┐  │
│  │              SqlFM.Core — Core Library                 │  │
│  │                                                        │  │
│  │  ┌─────────────────┐  ┌──────────────────────────┐   │  │
│  │  │  Dialect System  │  │    Formatter Pipeline     │   │  │
│  │  │ AnsiDialect      │  │ Step0: Template preprocess│   │  │
│  │  │ TsqlDialect      │  │ Step1: Exemption extract  │   │  │
│  │  │ MySqlDialect     │  │ Step2: Pre-refactor       │   │  │
│  │  │ PgDialect        │  │ Step3: Main format        │   │  │
│  │  │ DialectRegistry  │  │ Step4: Post-processing    │   │  │
│  │  └─────────────────┘  │ Step5: Exemption restore   │   │  │
│  │           │            │ Step6: Final cleanup       │   │  │
│  │           │            │ Step7: Lint check          │   │  │
│  │  ┌─────────────────┐  │ Step8: Auto-fix            │   │  │
│  │  │  Rule Engine     │  └──────────────────────────┘   │  │
│  │  │ SqlRuleEngine    │                                 │  │
│  │  │ 25 core rules    │  ┌──────────────────────────┐   │  │
│  │  │ RuleCrawler      │  │   Completion Engine       │   │  │
│  │  │ LintResult/Fix   │  │ KeywordProvider           │   │  │
│  │  └─────────────────┘  │ FunctionProvider           │   │  │
│  │           │            │ DataTypeProvider           │   │  │
│  │           │            │ SnippetProvider            │   │  │
│  │  ┌─────────────────┐  │ MetadataProvider           │   │  │
│  │  │  Parser Layer    │  └──────────────────────────┘   │  │
│  │  │ ISqlParser       │                                 │  │
│  │  │ ScriptDom (TSQL) │  ┌──────────────────────────┐   │  │
│  │  │ ANTLR (ANSI/My..)│  │    Configuration          │   │  │
│  │  └─────────────────┘  │ SqlFormatStyle (85+ props) │   │  │
│  │                        │ Dialect-specific overrides │   │  │
│  │                        │ Rule enable/disable config │   │  │
│  │                        └──────────────────────────┘   │  │
│  └─────────────────────────────────────────────────────┘  │
│                           │                                │
│  ┌─────────────────────────────────────────────────────┐  │
│  │              SqlFM.Cli — CLI Tool                      │  │
│  │  format │ lint │ fix │ check │ report (SARIF/JSON)    │  │
│  └─────────────────────────────────────────────────────┘  │
└───────────────────────────────────────────────────────────┘
```

---

## 八、立即开始：Phase 1 规则引擎实现

本方案已完整规划。建议从 Phase 1 开始编码，先构建规则引擎核心框架 + 25 条核心规则 + SSMS 错误列表集成。这是最核心的架构变革，后续 Phase 均依赖此基础设施。
