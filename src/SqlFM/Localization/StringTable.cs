using System.Collections.Generic;

namespace SqlFM.Localization
{
    /// <summary>
    /// 中英文字符串表。Key 全项目唯一；缺失时 Localizer 回退英文。
    /// </summary>
    public static class StringTable
    {
        /// <summary>简体中文</summary>
        public static readonly Dictionary<string, string> ZhCn = new Dictionary<string, string>
        {
            // ── 配置窗口框架 ──
            ["WindowTitle"] = "SqlFM 格式化配置",
            ["StyleLabel"] = "样式：",
            ["BtnNew"] = "新增",
            ["BtnCopy"] = "复制",
            ["BtnRename"] = "重命名",
            ["BtnDelete"] = "删除",
            ["BtnSetDefault"] = "设为默认",
            ["SearchTooltip"] = "搜索配置项",
            ["BtnImport"] = "导入",
            ["BtnExport"] = "导出",
            ["LangLabel"] = "语言：",
            ["LangZh"] = "中文",
            ["LangEn"] = "English",
            ["LangAuto"] = "跟随系统",

            // ── Tab 头 ──
            ["TabGeneral"] = "全局通用",
            ["TabDml"] = "DML语句",
            ["TabCte"] = "CTE",
            ["TabCase"] = "CASE WHEN",
            ["TabFlow"] = "流程控制",
            ["TabDdl"] = "DDL",
            ["TabExpression"] = "表达式",
            ["TabSpecial"] = "特殊T-SQL",

            // ── 全局通用 ──
            ["GrpIndentSpaces"] = "缩进与空格",
            ["LblIndentType"] = "缩进类型",
            ["LblIndentSize"] = "缩进宽度（空格数）",
            ["LblMaxLineWidth"] = "最大行宽",
            ["ChkTrimTrailing"] = "删除行尾空格",
            ["ChkMergeSpaces"] = "合并连续多余空格",
            ["ChkRemoveBlank"] = "移除多余空行",
            ["GrpBlankLines"] = "空行控制",
            ["LblStmtBlank"] = "语句间空行数",
            ["LblGoBefore"] = "GO 前空行数",
            ["LblGoAfter"] = "GO 后空行数",
            ["GrpKeywordCase"] = "关键字大小写",
            ["LblSqlKeyword"] = "SQL 关键字",
            ["LblFunction"] = "内置函数",
            ["LblDataType"] = "数据类型",
            ["LblObjectName"] = "对象名（表名/列名）",
            ["LblVarParam"] = "变量/参数名",
            ["GrpBracketSemicolon"] = "括号与分号",
            ["LblBracket"] = "方括号处理",
            ["LblSemicolon"] = "分号处理",
            ["ChkParenSameLine"] = "左括号与表达式同行",
            ["ChkShortExpr"] = "短表达式压缩为单行",
            ["ChkSingleQuote"] = "标准化单引号",

            // ── DML ──
            ["GrpSelect"] = "SELECT",
            ["LblComma"] = "逗号位置",
            ["LblAsKeyword"] = "AS 关键字处理",
            ["ChkAlignColAlias"] = "对齐列别名",
            ["ChkWrapFunc"] = "长函数调用换行",
            ["ChkAlignColComment"] = "对齐列行内注释",
            ["GrpFromJoin"] = "FROM / JOIN",
            ["ChkJoinNewLine"] = "JOIN 关键字另起新行",
            ["ChkIndentJoin"] = "JOIN 表相对 FROM 缩进",
            ["ChkLogicBefore"] = "逻辑运算符前置",
            ["ChkImplicitJoin"] = "隐式 JOIN 转为 INNER JOIN",
            ["GrpWhere"] = "WHERE",
            ["ChkWhereIndent"] = "WHERE 条件统一缩进",
            ["ChkWrapIn"] = "长 IN 列表换行",
            ["ChkAlignCompare"] = "对齐比较运算符",
            ["ChkNestedParen"] = "嵌套括号内额外缩进",
            ["GrpGroupOrderBy"] = "GROUP BY / ORDER BY",
            ["ChkGroupByEach"] = "GROUP BY 每列换行",
            ["ChkHavingWhere"] = "HAVING 复用 WHERE 风格",
            ["ChkOrderByAlign"] = "ORDER BY 排序方向右对齐",
            ["ChkWindowFunc"] = "窗口函数缩进展开",
            ["GrpIudm"] = "INSERT / UPDATE / DELETE / MERGE",
            ["ChkInsertWrap"] = "INSERT 列列表过多时换行",
            ["ChkValuesAlign"] = "VALUES 多行纵向对齐",
            ["ChkUpdateEqual"] = "UPDATE SET 等号对齐",
            ["ChkMergeIndent"] = "MERGE WHEN 分支额外缩进",
            ["ChkOutputWrap"] = "OUTPUT 子句换行展开",

            // ── CTE ──
            ["ChkCteSingle"] = "简单 CTE 压缩为单行",
            ["ChkCteComma"] = "多 CTE 分隔逗号另起新行",
            ["ChkCteBlank"] = "多个 CTE 之间空行分隔",
            ["ChkCteRecur"] = "递归 CTE UNION ALL 额外缩进",
            ["LblCteIndent"] = "CTE 查询体缩进层数",

            // ── CASE WHEN ──
            ["ChkCaseWhenNew"] = "每个 WHEN 分支另起新行",
            ["ChkThenAlign"] = "THEN 后的值对齐",
            ["ChkElseNew"] = "ELSE 分支另起新行",
            ["ChkEndAlign"] = "END 与 CASE 关键字对齐",
            ["ChkShortCase"] = "单 WHEN 的 CASE 压缩为单行",
            ["LblWhenIndent"] = "WHEN 条件缩进层数",

            // ── 流程控制 ──
            ["ChkIfBegin"] = "IF 的 BEGIN 与 IF 同行",
            ["ChkEndIf"] = "END 与对应 IF 缩进对齐",
            ["ChkIfElseBlank"] = "IF/ELSE 块之间插入空行",
            ["ChkTryCatch"] = "TRY/CATCH 块内语句缩进",
            ["ChkDeclareEach"] = "DECLARE 每个变量各占一行",
            ["ChkExecWrap"] = "EXEC 参数过多时换行",

            // ── DDL ──
            ["ChkCreateTable"] = "CREATE TABLE 每列占一行",
            ["ChkConstraint"] = "约束定义纵向对齐",
            ["ChkProcParam"] = "存储过程参数每行一个",
            ["ChkFuncReturn"] = "函数返回类型换行展开",
            ["ChkTrigger"] = "触发器 INSERTED/DELETED 缩进对齐",

            // ── 表达式 ──
            ["ChkOpSpace"] = "运算符两侧加空格",
            ["ChkInExists"] = "IN/EXISTS 子查询换行展开",
            ["ChkSingleComment"] = "单行注释与代码对齐缩进",
            ["ChkBlockComment"] = "块注释格式化（对齐星号）",
            ["LblSubQueryIndent"] = "子查询缩进层数",

            // ── 特殊 T-SQL ──
            ["ChkAddDbo"] = "自动为对象名添加 dbo. 前缀",
            ["ChkRemoveDbo"] = "自动移除显式 dbo. 前缀",
            ["ChkTempTable"] = "统一临时表名称格式 (#TableName)",
            ["ChkTableVar"] = "统一表变量名称格式 (@TableVar)",
            ["ChkGlobalVar"] = "全局变量 (@@) 大写",

            // ── 预览与按钮 ──
            ["PreviewTitle"] = "SQL 预览（实时）",
            ["BtnOk"] = "确定",
            ["BtnCancel"] = "取消",
            ["BtnApply"] = "应用",

            // ── 命令 / 菜单 ──
            ["CmdFormatSelected"] = "格式化选中",
            ["CmdFormatAll"] = "格式化全部",
            ["CmdFormatOptions"] = "格式选项",
            ["CmdCaseUpper"] = "关键字大写",
            ["CmdCaseLower"] = "关键字小写",
            ["CmdInsertExemption"] = "插入豁免",
            ["MenuToolBar"] = "SqlFM 工具",

            // ── 消息框 ──
            ["MsgTitle"] = "SqlFM",
            ["MsgStyleExists"] = "样式名称 '{0}' 已存在。",
            ["MsgNoRename"] = "系统预设样式不可重命名。",
            ["MsgNoDelete"] = "系统预设样式不可删除。",
            ["MsgConfirmDelete"] = "确定要删除样式 '{0}' 吗？",
            ["MsgSetDefault"] = "已将 '{0}' 设置为默认样式。",
            ["MsgImportOk"] = "样式 '{0}' 导入成功。",
            ["MsgImportFail"] = "导入失败：{0}",
            ["MsgExportOk"] = "样式已导出至：{0}",
            ["MsgExportFail"] = "导出失败：{0}",
            ["DlgImportTitle"] = "导入样式文件",
            ["DlgExportTitle"] = "导出样式文件",
            ["FileFilterSqlStyle"] = "SQL样式文件 (*.sqlstyle)|*.sqlstyle",
            ["FileFilterAll"] = "所有文件 (*.*)|*.*",
            ["PreviewFail"] = "-- 预览失败: {0}",

            // ── 输入对话框 ──
            ["InputNewTitle"] = "新增样式",
            ["InputNewPrompt"] = "请输入新样式名称：",
            ["InputCopyTitle"] = "复制样式",
            ["InputCopyPrompt"] = "请输入新样式名称：",
            ["InputRenameTitle"] = "重命名样式",
            ["InputRenamePrompt"] = "请输入新名称：",
            ["InputOk"] = "确定",
            ["InputCancel"] = "取消",

            // ── 枚举显示名 ──
            ["Enum_IndentType_Spaces"] = "空格",
            ["Enum_IndentType_Tabs"] = "制表符",
            ["Enum_KeywordCase_Upper"] = "大写",
            ["Enum_KeywordCase_Lower"] = "小写",
            ["Enum_KeywordCase_Pascal"] = "首字母大写",
            ["Enum_ObjectNameCase_Keep"] = "保持原样",
            ["Enum_ObjectNameCase_Upper"] = "大写",
            ["Enum_ObjectNameCase_Lower"] = "小写",
            ["Enum_CommaPosition_After"] = "行末",
            ["Enum_CommaPosition_Before"] = "行首",
            ["Enum_BracketMode_AutoAdd"] = "自动添加",
            ["Enum_BracketMode_AutoRemove"] = "自动移除",
            ["Enum_BracketMode_Keep"] = "保持原样",
            ["Enum_SemicolonMode_AutoAdd"] = "自动添加",
            ["Enum_SemicolonMode_AutoRemove"] = "自动移除",
            ["Enum_SemicolonMode_Keep"] = "保持原样",
            ["Enum_AsKeywordMode_Keep"] = "保持原样",
            ["Enum_AsKeywordMode_Remove"] = "移除 AS",
            ["Enum_AsKeywordMode_Align"] = "对齐 AS"
        };

        /// <summary>英文</summary>
        public static readonly Dictionary<string, string> En = new Dictionary<string, string>
        {
            // ── 配置窗口框架 ──
            ["WindowTitle"] = "SqlFM Formatting Options",
            ["StyleLabel"] = "Style:",
            ["BtnNew"] = "New",
            ["BtnCopy"] = "Copy",
            ["BtnRename"] = "Rename",
            ["BtnDelete"] = "Delete",
            ["BtnSetDefault"] = "Set as Default",
            ["SearchTooltip"] = "Search settings",
            ["BtnImport"] = "Import",
            ["BtnExport"] = "Export",
            ["LangLabel"] = "Language:",
            ["LangZh"] = "中文",
            ["LangEn"] = "English",
            ["LangAuto"] = "Follow System",

            // ── Tab 头 ──
            ["TabGeneral"] = "General",
            ["TabDml"] = "DML Statements",
            ["TabCte"] = "CTE",
            ["TabCase"] = "CASE WHEN",
            ["TabFlow"] = "Flow Control",
            ["TabDdl"] = "DDL",
            ["TabExpression"] = "Expressions",
            ["TabSpecial"] = "Special T-SQL",

            // ── 全局通用 ──
            ["GrpIndentSpaces"] = "Indentation & Spaces",
            ["LblIndentType"] = "Indent type",
            ["LblIndentSize"] = "Indent width (spaces)",
            ["LblMaxLineWidth"] = "Max line width",
            ["ChkTrimTrailing"] = "Trim trailing spaces",
            ["ChkMergeSpaces"] = "Merge consecutive spaces",
            ["ChkRemoveBlank"] = "Remove extra blank lines",
            ["GrpBlankLines"] = "Blank line control",
            ["LblStmtBlank"] = "Blank lines between statements",
            ["LblGoBefore"] = "Blank lines before GO",
            ["LblGoAfter"] = "Blank lines after GO",
            ["GrpKeywordCase"] = "Keyword case",
            ["LblSqlKeyword"] = "SQL keywords",
            ["LblFunction"] = "Built-in functions",
            ["LblDataType"] = "Data types",
            ["LblObjectName"] = "Object names (tables/columns)",
            ["LblVarParam"] = "Variables/parameters",
            ["GrpBracketSemicolon"] = "Brackets & semicolons",
            ["LblBracket"] = "Square bracket handling",
            ["LblSemicolon"] = "Semicolon handling",
            ["ChkParenSameLine"] = "Open paren on same line as expression",
            ["ChkShortExpr"] = "Collapse short expressions to one line",
            ["ChkSingleQuote"] = "Standardize single quotes",

            // ── DML ──
            ["GrpSelect"] = "SELECT",
            ["LblComma"] = "Comma position",
            ["LblAsKeyword"] = "AS keyword handling",
            ["ChkAlignColAlias"] = "Align column aliases",
            ["ChkWrapFunc"] = "Wrap long function calls",
            ["ChkAlignColComment"] = "Align inline column comments",
            ["GrpFromJoin"] = "FROM / JOIN",
            ["ChkJoinNewLine"] = "JOIN keyword on new line",
            ["ChkIndentJoin"] = "Indent JOIN table relative to FROM",
            ["ChkLogicBefore"] = "Leading logical operators",
            ["ChkImplicitJoin"] = "Convert implicit JOIN to INNER JOIN",
            ["GrpWhere"] = "WHERE",
            ["ChkWhereIndent"] = "Unified WHERE condition indent",
            ["ChkWrapIn"] = "Wrap long IN list",
            ["ChkAlignCompare"] = "Align comparison operators",
            ["ChkNestedParen"] = "Extra indent in nested parens",
            ["GrpGroupOrderBy"] = "GROUP BY / ORDER BY",
            ["ChkGroupByEach"] = "GROUP BY: one column per line",
            ["ChkHavingWhere"] = "HAVING reuses WHERE style",
            ["ChkOrderByAlign"] = "ORDER BY: right-align sort direction",
            ["ChkWindowFunc"] = "Indent & expand window functions",
            ["GrpIudm"] = "INSERT / UPDATE / DELETE / MERGE",
            ["ChkInsertWrap"] = "Wrap INSERT column list when long",
            ["ChkValuesAlign"] = "Align VALUES rows vertically",
            ["ChkUpdateEqual"] = "Align UPDATE SET equals",
            ["ChkMergeIndent"] = "Extra indent for MERGE WHEN",
            ["ChkOutputWrap"] = "Wrap & expand OUTPUT clause",

            // ── CTE ──
            ["ChkCteSingle"] = "Collapse simple CTE to one line",
            ["ChkCteComma"] = "CTE separators on new line",
            ["ChkCteBlank"] = "Blank line between CTEs",
            ["ChkCteRecur"] = "Extra indent for recursive CTE UNION ALL",
            ["LblCteIndent"] = "CTE body indent level",

            // ── CASE WHEN ──
            ["ChkCaseWhenNew"] = "Each WHEN on new line",
            ["ChkThenAlign"] = "Align THEN values",
            ["ChkElseNew"] = "ELSE on new line",
            ["ChkEndAlign"] = "END aligns with CASE",
            ["ChkShortCase"] = "Collapse single-WHEN CASE to one line",
            ["LblWhenIndent"] = "WHEN condition indent level",

            // ── 流程控制 ──
            ["ChkIfBegin"] = "IF and BEGIN on same line",
            ["ChkEndIf"] = "END aligns with IF",
            ["ChkIfElseBlank"] = "Blank line between IF/ELSE",
            ["ChkTryCatch"] = "Indent statements in TRY/CATCH",
            ["ChkDeclareEach"] = "One DECLARE variable per line",
            ["ChkExecWrap"] = "Wrap EXEC params when many",

            // ── DDL ──
            ["ChkCreateTable"] = "CREATE TABLE: one column per line",
            ["ChkConstraint"] = "Align constraint definitions",
            ["ChkProcParam"] = "One proc param per line",
            ["ChkFuncReturn"] = "Expand function return type",
            ["ChkTrigger"] = "Align trigger INSERTED/DELETED",

            // ── 表达式 ──
            ["ChkOpSpace"] = "Spaces around operators",
            ["ChkInExists"] = "Wrap IN/EXISTS subqueries",
            ["ChkSingleComment"] = "Align single-line comments",
            ["ChkBlockComment"] = "Format block comments (align *)",
            ["LblSubQueryIndent"] = "Subquery indent level",

            // ── 特殊 T-SQL ──
            ["ChkAddDbo"] = "Auto-add dbo. prefix",
            ["ChkRemoveDbo"] = "Auto-remove explicit dbo. prefix",
            ["ChkTempTable"] = "Normalize temp table names (#TableName)",
            ["ChkTableVar"] = "Normalize table variable names (@TableVar)",
            ["ChkGlobalVar"] = "Uppercase global vars (@@)",

            // ── 预览与按钮 ──
            ["PreviewTitle"] = "SQL Preview (live)",
            ["BtnOk"] = "OK",
            ["BtnCancel"] = "Cancel",
            ["BtnApply"] = "Apply",

            // ── 命令 / 菜单 ──
            ["CmdFormatSelected"] = "Format Selected SQL",
            ["CmdFormatAll"] = "Format All SQL",
            ["CmdFormatOptions"] = "Format Options",
            ["CmdCaseUpper"] = "Uppercase Keywords",
            ["CmdCaseLower"] = "Lowercase Keywords",
            ["CmdInsertExemption"] = "Insert Exemption Marker",
            ["MenuToolBar"] = "SqlFM Toolbar",

            // ── 消息框 ──
            ["MsgTitle"] = "SqlFM",
            ["MsgStyleExists"] = "Style name '{0}' already exists.",
            ["MsgNoRename"] = "System presets cannot be renamed.",
            ["MsgNoDelete"] = "System presets cannot be deleted.",
            ["MsgConfirmDelete"] = "Delete style '{0}'?",
            ["MsgSetDefault"] = "Set '{0}' as default style.",
            ["MsgImportOk"] = "Style '{0}' imported.",
            ["MsgImportFail"] = "Import failed: {0}",
            ["MsgExportOk"] = "Style exported to: {0}",
            ["MsgExportFail"] = "Export failed: {0}",
            ["DlgImportTitle"] = "Import Style File",
            ["DlgExportTitle"] = "Export Style File",
            ["FileFilterSqlStyle"] = "SQL style files (*.sqlstyle)|*.sqlstyle",
            ["FileFilterAll"] = "All files (*.*)|*.*",
            ["PreviewFail"] = "-- Preview failed: {0}",

            // ── 输入对话框 ──
            ["InputNewTitle"] = "New Style",
            ["InputNewPrompt"] = "Enter new style name:",
            ["InputCopyTitle"] = "Copy Style",
            ["InputCopyPrompt"] = "Enter new style name:",
            ["InputRenameTitle"] = "Rename Style",
            ["InputRenamePrompt"] = "Enter new name:",
            ["InputOk"] = "OK",
            ["InputCancel"] = "Cancel",

            // ── 枚举显示名 ──
            ["Enum_IndentType_Spaces"] = "Spaces",
            ["Enum_IndentType_Tabs"] = "Tabs",
            ["Enum_KeywordCase_Upper"] = "Upper",
            ["Enum_KeywordCase_Lower"] = "Lower",
            ["Enum_KeywordCase_Pascal"] = "Pascal",
            ["Enum_ObjectNameCase_Keep"] = "Keep",
            ["Enum_ObjectNameCase_Upper"] = "Upper",
            ["Enum_ObjectNameCase_Lower"] = "Lower",
            ["Enum_CommaPosition_After"] = "Trailing",
            ["Enum_CommaPosition_Before"] = "Leading",
            ["Enum_BracketMode_AutoAdd"] = "Auto-add",
            ["Enum_BracketMode_AutoRemove"] = "Auto-remove",
            ["Enum_BracketMode_Keep"] = "Keep",
            ["Enum_SemicolonMode_AutoAdd"] = "Auto-add",
            ["Enum_SemicolonMode_AutoRemove"] = "Auto-remove",
            ["Enum_SemicolonMode_Keep"] = "Keep",
            ["Enum_AsKeywordMode_Keep"] = "Keep",
            ["Enum_AsKeywordMode_Remove"] = "Remove AS",
            ["Enum_AsKeywordMode_Align"] = "Align AS"
        };
    }
}
