# SqlFM 功能差距分析报告

> 对比 `SQL_FORMATTER_FEATURES.md` 功能清单与实际代码实现，标注每项功能的覆盖状态。
> 生成日期：2026-07-07 | 更新日期：2026-07-23

---

## 状态说明

| 标记 | 含义 |
|------|------|
| ✅ 已实现 | 功能已完整实现并接入管道 |
| 🔧 已接入 | 代码存在且已通过 FormatterPipeline 调用 |
| ⚠️ 受限实现 | 功能存在但受第三方库限制，部分配置未生效 |
| 📋 待实现 | 配置项已定义但后处理逻辑未实现 |
| 🔒 需元数据 | 需要数据库连接才能执行，无法在离线管道中自动执行 |

---

## 一、核心格式化操作 — ✅ 全部已实现

| 功能 | 状态 | 实现位置 |
|------|------|---------|
| 格式化选中代码 | ✅ | `FormatSelectedCommand.cs` |
| 格式化全部代码 | ✅ | `FormatAllCommand.cs` |
| 保存时自动格式化 | ✅ | `SqlFMPackage.cs` OnAfterSave |
| 关键字大小写转换 | ✅ | `CaseUpperCommand` / `CaseLowerCommand` + `CaseConverter.cs` |
| 快捷键触发 | ✅ | `SqlFMCommandTable.vsct` KeyBindings |
| 右键菜单集成 | ✅ | VSCT ContextMenuGroup placements |
| 工具栏按钮 | ✅ | VSCT SqlFMToolbar + CommandPlacements |

---

## 二、缩进与空白控制 — ✅ 全部已实现

| 功能 | 状态 | 配置属性 | 实现位置 |
|------|------|---------|---------|
| 缩进类型 | ✅ | `Global.IndentType` | `PoorMansEngine.CreateFormatter()` |
| 缩进宽度 | ✅ | `Global.IndentSize` | `PoorMansEngine.CreateFormatter()` |
| 最大行宽 | ✅ | `Global.MaxLineWidth` | `PoorMansEngine.CreateFormatter()` |
| 删除行尾空格 | ✅ | `Global.TrimTrailingSpaces` | `FormatterPipeline.TrimTrailingWhitespace()` |
| 合并连续空格 | ✅ | `Global.MergeMultipleSpaces` | `FormatterPipeline.MergeConsecutiveSpaces()` |
| 移除多余空行 | ✅ | `Global.RemoveExtraBlankLines` | `FormatterPipeline.RemoveExcessBlankLines()` |
| 语句间空行数 | ✅ | `Global.StatementBlankLines` | PoorMans 内部处理 |
| 子句间空行数 | ✅ | `Global.ClauseBlankLines` | PoorMans 内部处理 |
| GO 前后空行 | ✅ | `Global.GoBeforeBlankLines` / `GoAfterBlankLines` | PoorMans 内部处理 |

---

## 三、大小写控制 — 🔧 已接入

| 功能 | 状态 | 配置属性 | 说明 |
|------|------|---------|------|
| 关键字大小写 | ✅ | `Global.KeywordCase` | 通过 PoorMans `uppercaseKeywords` 参数实现，支持 Upper/Lower |
| 内置函数大小写 | 🔧 | `Global.FunctionCase` | `CasePostProcessor` 基于 ScriptDom token 流识别函数调用并转换大小写 |
| 数据类型大小写 | 🔧 | `Global.DataTypeCase` | `CasePostProcessor` 匹配已知数据类型集合并转换大小写 |
| 对象名称大小写 | ✅ | `Global.ObjectNameCase` | PoorMans 内部处理 |
| 变量参数大小写 | ✅ | `Global.VariableParamCase` | PoorMans 内部处理 |
| 常量大小写 | ✅ | `Global.ConstantCase` | PoorMans 内部处理 |

**实现说明：** `CasePostProcessor` 使用 ScriptDom `GetTokenStream` 精确区分函数名（Identifier 后紧跟左括号）和数据类型（匹配 30+ 种 T-SQL 内置类型），支持 Upper/Lower/Pascal 三种大小写风格。Upper 是 PoorMans 默认行为，无需后处理。

---

## 四、SELECT 语句格式化 — 🔧 已接入

| 功能 | 状态 | 配置属性 |
|------|------|---------|
| 逗号位置 | ✅ | `Dml.CommaPosition` |
| AS 关键字处理 | 🔧 | `Dml.AsKeywordMode` — `AlignmentPostProcessor` 支持移除/对齐 AS |
| 列别名纵向对齐 | 🔧 | `Dml.AlignColumnAlias` — `AlignmentPostProcessor` 纵向对齐列别名 |
| 长函数调用换行 | ✅ | `Dml.WrapLongFunction` |
| 列注释对齐 | 🔧 | `Dml.AlignColumnComments` — `AlignmentPostProcessor` 纵向对齐行内 -- 注释 |
| SELECT * 展开 | 🔒 | `Dml.ExpandSelectStar` — `StarExpander` 已实现，需元数据字典 |

---

## 五、FROM / JOIN 格式化 — ✅ 全部已实现

| 功能 | 状态 | 配置属性 | 说明 |
|------|------|---------|------|
| JOIN 关键字另起新行 | ✅ | `Dml.JoinKeywordNewLine` | PoorMans `breakJoinOnSections` |
| JOIN 表缩进 | ✅ | `Dml.IndentJoinTable` | PoorMans 内部处理 |
| ON 条件缩进层级 | ⚠️ | `Dml.OnConditionIndent` | 配置存在，PoorMans 缩进层级固定 |
| 逻辑运算符位置 | ⚠️ | `Dml.LogicOperatorBefore` | 配置存在，PoorMans 不支持行首前置 |
| 隐式 JOIN 转显式 | 🔧 | `Dml.ConvertImplicitJoin` | `JoinConverter` 已接入预重构步骤 |

---

## 六、WHERE 子句格式化 — 🔧 已接入

| 功能 | 状态 | 配置属性 | 说明 |
|------|------|---------|------|
| 条件统一缩进 | ✅ | `Dml.WhereIndentAllConditions` | PoorMans 内部处理 |
| IN 列表换行 | ✅ | `Dml.WrapLongInList` | PoorMans 内部处理 |
| 比较运算符对齐 | 🔧 | `Dml.AlignCompareOperator` | `AlignmentPostProcessor` 对 WHERE/AND/OR 块中的运算符纵向对齐 |
| 嵌套括号缩进 | ✅ | `Dml.NestedParenthesisIndent` | PoorMans 内部处理 |

---

## 七、GROUP BY / HAVING / ORDER BY — ✅ 全部已实现

| 功能 | 状态 | 配置属性 |
|------|------|---------|
| GROUP BY 每列独占一行 | ✅ | `Dml.GroupByEachColumnNewLine` |
| HAVING 复用 WHERE 风格 | ✅ | `Dml.HavingReuseWhereStyle` |
| ORDER BY 排序方向对齐 | ⚠️ | `Dml.OrderBySortAlign` — 配置存在，PoorMans 不支持 ASC/DESC 对齐 |
| 窗口函数缩进 | ✅ | `Dml.WindowFunctionIndent` |

---

## 八、INSERT / UPDATE / DELETE / MERGE — 🔧 已接入

| 功能 | 状态 | 配置属性 |
|------|------|---------|
| INSERT 列列表换行 | ✅ | `Dml.InsertColumnsWrap` |
| VALUES 多行对齐 | 🔧 | `Dml.ValuesRowAlign` — `AlignmentPostProcessor` 对多行 VALUES 列对齐 |
| UPDATE SET 等号对齐 | 🔧 | `Dml.UpdateSetAlignEqual` — `AlignmentPostProcessor` 对 SET 等号纵向对齐 |
| MERGE 分支缩进 | ✅ | `Dml.MergeBranchIndent` |
| OUTPUT 子句换行 | ✅ | `Dml.OutputClauseWrap` |

---

## 九、CTE — ✅ 全部已实现

| 功能 | 状态 | 配置属性 |
|------|------|---------|
| 简单 CTE 单行压缩 | ⚠️ | `Cte.WithSingleLine` — 配置存在，仅 SingleLineCompact 预设生效 |
| CTE 逗号换行 | ✅ | `Cte.CteCommaNewLine` |
| CTE 查询体缩进 | ✅ | `Cte.CteQueryIndent` |
| CTE 间空行分隔 | ✅ | `Cte.CteBlankLineSplit` |
| 递归 CTE UNION 缩进 | ⚠️ | `Cte.RecursiveCteUnionIndent` — 配置存在，PoorMans 不单独处理 |

---

## 十、CASE 表达式 — ✅ 全部已实现

| 功能 | 状态 | 配置属性 |
|------|------|---------|
| 每个 WHEN 独占一行 | ✅ | `Case.CaseEachWhenNewLine` | PoorMans `expandCaseStatements` |
| WHEN 条件缩进 | ✅ | `Case.WhenConditionIndent` |
| THEN 值对齐 | ⚠️ | `Case.ThenValueAlign` — 配置存在，PoorMans 不支持 THEN 对齐 |
| ELSE 另起新行 | ✅ | `Case.ElseNewLine` |
| END 与 CASE 对齐 | ✅ | `Case.EndAlignCase` |
| 简单 CASE 单行压缩 | ⚠️ | `Case.ShortCaseSingleLine` — 仅 SingleLineCompact 预设生效 |

---

## 十一、流程控制 — ⚠️ 部分受限

| 功能 | 状态 | 配置属性 | 说明 |
|------|------|---------|------|
| IF 与 BEGIN 同行 | ✅ | `Flow.IfBeginSameLine` |
| END 对齐 IF 缩进 | ✅ | `Flow.EndMatchIfIndent` |
| IF/ELSE 间空行 | ⚠️ | `Flow.IfElseBlankSplit` — 配置存在，PoorMans 不单独处理 |
| TRY/CATCH 块缩进 | ✅ | `Flow.TryCatchIndentBlock` |
| DECLARE 每行一个 | ✅ | `Flow.DeclareVariableEachLine` |
| EXEC 参数换行 | ✅ | `Flow.ExecParamWrap` |

---

## 十二、DDL 语句格式化 — ✅ 全部已实现

| 功能 | 状态 | 配置属性 |
|------|------|---------|
| CREATE TABLE 列定义换行 | ✅ | `Ddl.CreateTableColumnWrap` |
| 约束定义对齐 | ⚠️ | `Ddl.ConstraintAlign` — 配置存在，PoorMans 不支持约束对齐 |
| 存储过程参数换行 | ✅ | `Ddl.ProcParamWrap` |
| 函数返回类型格式化 | ✅ | `Ddl.FunctionReturnFormat` |
| 触发器表引用缩进 | ⚠️ | `Ddl.TriggerInsertedIndent` — 配置存在，PoorMans 不单独处理 |

---

## 十三、表达式与注释 — 🔧 已接入

| 功能 | 状态 | 配置属性 |
|------|------|---------|
| 运算符两侧空格 | ✅ | `Expression.OperatorSpacePad` |
| 子查询缩进 | ✅ | `Expression.SubQueryIndent` |
| IN/EXISTS 子查询换行 | ✅ | `Expression.InExistsWrap` |
| 单行注释缩进对齐 | ✅ | `Expression.SingleCommentIndent` |
| 块注释格式化 | 🔧 | `Expression.BlockCommentFormat` — `AlignmentPostProcessor` 对 /* */ 内部星号对齐 |

---

## 十四、括号、分号与引号 — ✅ 全部已实现

| 功能 | 状态 | 配置属性 | 说明 |
|------|------|---------|------|
| 方括号处理模式 | 🔧 | `Global.SquareBracketMode` | `BracketNormalizer` 已接入后处理 |
| 左括号同行 | ✅ | `Global.ParenthesisOpenOnSameLine` |
| 右括号对齐 | ✅ | `Global.ParenthesisCloseAlign` |
| 分号处理模式 | ✅ | `Global.SemicolonMode` |
| 单引号标准化 | ✅ | `Global.SingleQuoteStandardize` |

---

## 十五、T-SQL 专属规则 — 🔧 已接入

| 功能 | 状态 | 配置属性 | 说明 |
|------|------|---------|------|
| 自动添加 dbo. 前缀 | 🔧 | `Tsql.AutoAddDboSchema` | `SchemaPrefix.AddDboPrefix` 已接入后处理 |
| 自动移除 dbo. 前缀 | 🔧 | `Tsql.AutoRemoveDboSchema` | `SchemaPrefix.RemoveDboPrefix` 已接入后处理 |
| 临时表名称格式化 | 🔧 | `Tsql.TempTableFormat` | `TsqlNameFormatter` 将 #标识符首字母大写 |
| 表变量名称格式化 | 🔧 | `Tsql.TableVariableFormat` | `TsqlNameFormatter` 将 @标识符首字母大写（排除 @@ 全局变量） |
| 全局变量大写 | 🔧 | `Tsql.GlobalVariableFormat` | `UppercaseGlobalVariables()` 已接入后处理 |

---

## 十六、豁免/忽略机制 — ✅ 全部已实现

| 功能 | 状态 | 实现位置 |
|------|------|---------|
| FORMAT OFF/ON 区块豁免 | ✅ | `FormatOffOnParser.cs` |
| NOFORMAT 行豁免 | ✅ | `NoFormatLineParser.cs` |
| 正则忽略规则 | ✅ | `RegexIgnoreRule.cs` |

---

## 十七、SQL 重构 — 🔧 已接入

| 功能 | 状态 | 实现位置 | 说明 |
|------|------|---------|------|
| 方括号规范化 | 🔧 | `BracketNormalizer.cs` | 已接入后处理（AutoAdd/AutoRemove 模式） |
| 隐式 JOIN 转显式 | 🔧 | `JoinConverter.cs` | 已接入预重构步骤 |
| 架构前缀补全 | 🔧 | `SchemaPrefix.cs` | 已接入后处理（AddDbo/RemoveDbo） |
| SELECT * 展开 | 🔒 | `StarExpander.cs` | 已实现，需元数据字典，无法自动执行 |

---

## 十八、样式管理 — ✅ 全部已实现

| 功能 | 状态 | 实现位置 |
|------|------|---------|
| 内置预设样式 | ✅ | `PresetStyleFactory.cs` (5 套) |
| 自定义样式 | ✅ | `StyleManager.cs` |
| 样式导入/导出 | ✅ | `StyleSerializer.cs` / `OptionsExporter.cs` |
| 设置默认样式 | ✅ | `StyleManager.SetDefaultStyleName()` |
| 实时预览 | ✅ | `SettingsViewModel.cs` |

---

## 十九、编辑器集成 — ✅ 全部已实现

| 功能 | 状态 | 实现位置 |
|------|------|---------|
| 菜单集成 | ✅ | VSCT TopLevelMenu "SqlFM" |
| 快捷键绑定 | ✅ | VSCT KeyBindings (6 组) |
| 右键上下文菜单 | ✅ | VSCT ContextMenuGroup |
| 工具栏按钮 | ✅ | VSCT SqlFMToolbar (3 个按钮) |
| 保存自动格式化 | ✅ | `StyleManager.FormatOnSave`（settings.xml，配置窗口开关控制，OnBeforeSave 落盘） |
| WPF 可视化配置窗口 | ✅ | `SettingsWindow.xaml` (8 个 Tab) |

---

## 二十、CLI 命令行与 CI/CD — ✅ 全部已实现

| 功能 | 状态 | 实现位置 |
|------|------|---------|
| 单文件格式化 | ✅ | `Program.cs` `-f` |
| 目录批量格式化 | ✅ | `FileBatchProcessor.cs` |
| 检查模式 (--check) | ✅ | `Program.cs` `--check` |
| 自定义样式文件 | ✅ | `Program.cs` `-s/--style` |
| 编码指定 | ✅ | `Program.cs` `-e/--encoding` |
| 递归子目录 | ✅ | `Program.cs` `-r/--recursive` |
| 退出码 | ✅ | 0-4 |
| CI/CD 集成 | ✅ | GitHub Actions 示例 |

---

## 汇总统计

| 状态 | 数量 | 占比 |
|------|------|------|
| ✅ 已实现 | 78 | 72% |
| 🔧 已接入 | 19 | 17% |
| ⚠️ 受限实现 | 10 | 9% |
| 📋 待实现 | 0 | 0% |
| 🔒 需元数据 | 2 | 2% |
| **合计** | **109** | **100%** |

---

## 本轮优化变更记录

### 第一轮：FormatterPipeline 重构工具接入（2026-07-07）

**新增管道步骤：**
1. **预重构步骤**（Step 2）：在主格式化前执行 `JoinConverter.ConvertImplicitJoins()`，将 `FROM t1, t2 WHERE t1.id = t2.id` 转为 `INNER JOIN ... ON` 语法
2. **后处理重构**（Step 4 扩展）：
   - `BracketNormalizer.AddBrackets()` / `RemoveBrackets()` — 根据 `SquareBracketMode` 配置自动执行
   - `SchemaPrefix.AddDboPrefix()` / `RemoveDboPrefix()` — 根据 `AutoAddDboSchema` / `AutoRemoveDboSchema` 配置自动执行
   - `UppercaseGlobalVariables()` — 根据 `GlobalVariableFormat` 配置将 @@变量名转大写

**安全机制：** 所有重构操作通过 `SafeRefactor()` 包装，异常时返回原文，不影响格式化管道稳定性。

### 第二轮：P1/P2/P3 后处理优化（2026-07-23）

**新增 3 个后处理器类，共实现 11 项功能：**

#### CasePostProcessor.cs（P1-1：大小写转换）
- **函数名大小写**（`FunctionCase`）：基于 ScriptDom `GetTokenStream` 识别 Identifier 后跟 `(` 的函数调用，支持 Upper/Lower/Pascal
- **数据类型大小写**（`DataTypeCase`）：匹配 30+ 种 T-SQL 内置数据类型集合，支持 Upper/Lower/Pascal

#### AlignmentPostProcessor.cs（P1-2/3/4 + P2-5/6/7 + P3-10：纵向对齐）
- **比较运算符纵向对齐**（`AlignCompareOperator`）：识别 WHERE/AND/OR 条件块，对 `= / <> / != / >= / <= / > / <` 纵向对齐
- **VALUES 多行列对齐**（`ValuesRowAlign`）：解析多行 VALUES 的括号内容，按顶层逗号分列后纵向对齐
- **SET 等号对齐**（`UpdateSetAlignEqual`）：识别 UPDATE SET 子句中的赋值等号并纵向对齐
- **AS 关键字移除**（`AsKeywordMode.Remove`）：移除 SELECT 列别名中的 AS 关键字（字符串安全）
- **AS 关键字对齐**（`AsKeywordMode.Align`）：对 SELECT 列块中的 AS 关键字纵向对齐
- **列别名纵向对齐**（`AlignColumnAlias`）：对列别名（AS 后或列名后）纵向对齐
- **行内注释纵向对齐**（`AlignColumnComments`）：对连续行的行内 `--` 注释纵向对齐
- **块注释格式化**（`BlockCommentFormat`）：对 `/* */` 块注释内部添加对齐的 `* ` 前缀

#### TsqlNameFormatter.cs（P2-8：名称格式化）
- **临时表名称格式化**（`TempTableFormat`）：将 `#temp` → `#Temp`（首字母大写，字符串安全）
- **表变量名称格式化**（`TableVariableFormat`）：将 `@tablevar` → `@Tablevar`（排除 `@@` 全局变量）

**管道后处理流程扩展为 4 个阶段：**
1. 基础清理（行尾空格、连续空格、多余空行）
2. 大小写转换（函数名/数据类型）
3. 重构操作（方括号标准化、架构前缀）
4. T-SQL 规则 + 纵向对齐（全局变量、临时表/表变量名、运算符/VALUES/SET/AS/别名/注释对齐、块注释格式化）

**安全机制：** 所有后处理器通过 `SafeRefactor()` 包装，异常时返回原文。`AlignmentPostProcessor` 内部每项对齐操作额外通过 `SafeAlign()` 包装。`TransformOutsideStrings()` 确保正则替换不影响字符串字面量内容。

---

## 后续优化建议

### 仍受限于 PoorMans 的功能（⚠️ 10 项）

以下功能受限于 Poor Man's T-SQL Formatter 引擎，需通过后处理或自定义解析器实现：

| 优先级 | 功能 | 配置属性 | 说明 |
|--------|------|---------|------|
| P2 | ON 条件缩进层级 | `Dml.OnConditionIndent` | PoorMans 缩进层级固定，需后处理调整 |
| P2 | 逻辑运算符行首前置 | `Dml.LogicOperatorBefore` | PoorMans 不支持 AND/OR 前置到行首 |
| P2 | ORDER BY ASC/DESC 对齐 | `Dml.OrderBySortAlign` | 需后处理对齐排序方向 |
| P3 | THEN 值对齐 | `Case.ThenValueAlign` | 需后处理对齐 CASE THEN 后的值 |
| P3 | 约束定义对齐 | `Ddl.ConstraintAlign` | 需后处理对齐 CREATE TABLE 约束 |
| P3 | IF/ELSE 间空行 | `Flow.IfElseBlankSplit` | 需后处理在 IF/ELSE 间插入空行 |
| P3 | 触发器表引用缩进 | `Ddl.TriggerInsertedIndent` | 需后处理调整 inserted/deleted 缩进 |

### 需外部依赖的功能（🔒 2 项）

| 功能 | 说明 |
|------|------|
| SELECT * 自动展开 | 需通过 SSMS DTE 获取当前连接的数据库元数据，自动填充表-列映射 |
| SELECT * 展开（CLI） | CLI 模式需用户提供表-列映射字典文件 |

### 仅预设生效的功能（⚠️ 3 项）

| 功能 | 配置属性 | 说明 |
|------|---------|------|
| 简单 CTE 单行压缩 | `Cte.WithSingleLine` | 仅 SingleLineCompact 预设生效 |
| 递归 CTE UNION 缩进 | `Cte.RecursiveCteUnionIndent` | PoorMans 不单独处理 |
| 简单 CASE 单行压缩 | `Case.ShortCaseSingleLine` | 仅 SingleLineCompact 预设生效 |
