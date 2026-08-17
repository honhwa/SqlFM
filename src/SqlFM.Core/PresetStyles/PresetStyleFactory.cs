using System.Collections.Generic;
using SqlFM.Core.Configuration;

namespace SqlFM.Core.PresetStyles
{
    /// <summary>
    /// 内置预设样式工厂：提供 5 套常用 SQL 格式化预设，均标记为系统内置（IsSystemPreset = true）。
    /// </summary>
    public static class PresetStyleFactory
    {
        /// <summary>
        /// 默认样式：4 空格缩进、逗号后置、关键字全大写、适中换行。
        /// 适合绝大多数场景，兼顾可读性与规范性。
        /// </summary>
        public static SqlFormatStyle CreateDefault()
        {
            return new SqlFormatStyle
            {
                Name = "Default",
                IsDefault = true,
                IsSystemPreset = true,
                Global = new GlobalSettings
                {
                    IndentType = IndentType.Spaces,
                    IndentSize = 4,
                    TabWidth = 4,
                    MaxLineWidth = 120,
                    StatementBlankLines = 1,
                    ClauseBlankLines = 0,
                    GoBeforeBlankLines = 1,
                    GoAfterBlankLines = 1,
                    TrimTrailingSpaces = true,
                    MergeMultipleSpaces = true,
                    RemoveExtraBlankLines = true,
                    KeywordCase = KeywordCase.Upper,
                    FunctionCase = KeywordCase.Upper,
                    DataTypeCase = KeywordCase.Upper,
                    ConstantCase = KeywordCase.Upper,
                    ObjectNameCase = ObjectNameCase.Keep,
                    VariableParamCase = ObjectNameCase.Keep,
                    SquareBracketMode = BracketMode.Keep,
                    ParenthesisOpenOnSameLine = true,
                    ParenthesisCloseAlign = true,
                    ShortExpressionSingleLine = false,
                    SingleQuoteStandardize = false,
                    SemicolonMode = SemicolonMode.Keep
                },
                Dml = new DmlSettings
                {
                    CommaPosition = CommaPosition.After,
                    AsKeywordMode = AsKeywordMode.Keep,
                    AlignColumnAlias = false,
                    AlignClauseKeyword = true,   // 子句关键字 SELECT/FROM/WHERE... 末尾字母右对齐
                    WrapLongFunction = true,
                    JoinKeywordNewLine = true,
                    IndentJoinTable = false,
                    OnConditionIndent = 1,
                    LogicOperatorBefore = false,
                    WhereIndentAllConditions = true,
                    WrapLongInList = true,
                    AlignCompareOperator = false,
                    NestedParenthesisIndent = true,
                    GroupByEachColumnNewLine = true,
                    HavingReuseWhereStyle = true,
                    OrderBySortAlign = false,
                    WindowFunctionIndent = true,
                    InsertColumnsWrap = true,
                    ValuesRowAlign = false,
                    UpdateSetAlignEqual = false,
                    MergeBranchIndent = true,
                    OutputClauseWrap = true
                }
            };
        }

        /// <summary>
        /// 逗号前置样式：逗号位于行首，方便在 SELECT 列表中增删列。
        /// 4 空格缩进、关键字大写，其余与 Default 相同。
        /// </summary>
        public static SqlFormatStyle CreateCommasBefore()
        {
            var style = CreateDefault();
            style.Name = "CommasBefore";
            style.IsDefault = false;

            style.Dml.CommaPosition = CommaPosition.Before;
            style.Dml.AlignColumnAlias = true;

            return style;
        }

        /// <summary>
        /// 右对齐风格：运算符、别名右对齐，视觉上整齐对称。
        /// 启用列别名对齐、比较运算符对齐、SET 等号对齐、ORDER BY 排序方向对齐。
        /// </summary>
        public static SqlFormatStyle CreateRightAlign()
        {
            var style = CreateDefault();
            style.Name = "RightAlign";
            style.IsDefault = false;

            style.Dml.AlignColumnAlias = true;
            style.Dml.AlignClauseKeyword = true;
            style.Dml.AsKeywordMode = AsKeywordMode.Align;
            style.Dml.AlignColumnComments = true;
            style.Dml.AlignCompareOperator = true;
            style.Dml.UpdateSetAlignEqual = true;
            style.Dml.OrderBySortAlign = true;
            style.Dml.ValuesRowAlign = true;
            style.Case.ThenValueAlign = true;

            return style;
        }

        /// <summary>
        /// 紧凑缩进样式：2 空格缩进、减少空行，适合屏幕空间有限或代码密度要求高的场景。
        /// </summary>
        public static SqlFormatStyle CreateCompactIndented()
        {
            var style = CreateDefault();
            style.Name = "CompactIndented";
            style.IsDefault = false;

            style.Global.IndentSize = 2;
            style.Global.StatementBlankLines = 0;
            style.Global.ClauseBlankLines = 0;
            style.Global.GoBeforeBlankLines = 1;
            style.Global.GoAfterBlankLines = 0;
            style.Global.RemoveExtraBlankLines = true;

            style.Cte.CteBlankLineSplit = false;
            style.Flow.IfElseBlankSplit = false;

            return style;
        }

        /// <summary>
        /// 单行紧凑样式：短语句尽量压缩到单行，行宽限制放大至 200，适合临时 SQL 或脚本快速阅读。
        /// </summary>
        public static SqlFormatStyle CreateSingleLineCompact()
        {
            var style = CreateDefault();
            style.Name = "SingleLineCompact";
            style.IsDefault = false;

            style.Global.MaxLineWidth = 200;
            style.Global.IndentSize = 2;
            style.Global.StatementBlankLines = 0;
            style.Global.ClauseBlankLines = 0;
            style.Global.RemoveExtraBlankLines = true;
            style.Global.ShortExpressionSingleLine = true;

            style.Cte.WithSingleLine = true;
            style.Cte.CteBlankLineSplit = false;
            style.Case.ShortCaseSingleLine = true;

            style.Dml.GroupByEachColumnNewLine = false;

            return style;
        }

        /// <summary>
        /// 获取所有系统内置预设样式的只读列表（按推荐顺序排列）。
        /// </summary>
        /// <returns>包含 5 个预设样式的只读集合</returns>
        public static IReadOnlyList<SqlFormatStyle> GetAllPresets()
        {
            return new List<SqlFormatStyle>
            {
                CreateDefault(),
                CreateCommasBefore(),
                CreateRightAlign(),
                CreateCompactIndented(),
                CreateSingleLineCompact()
            }.AsReadOnly();
        }
    }
}
