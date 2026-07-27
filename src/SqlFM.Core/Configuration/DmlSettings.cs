using System.Xml.Serialization;

namespace SqlFM.Core.Configuration
{
    /// <summary>
    /// DML 语句格式化设置（分组2）：SELECT/FROM/JOIN/WHERE/GROUP BY/ORDER BY/INSERT/UPDATE/DELETE/MERGE 等。
    /// </summary>
    public class DmlSettings
    {
        // ── SELECT ──────────────────────────────────────────────────────────────

        /// <summary>SELECT 列列表的逗号位置，默认后置（行末）</summary>
        [XmlElement]
        public CommaPosition CommaPosition { get; set; } = CommaPosition.After;

        /// <summary>是否将 SELECT * 展开为实际列（需连接数据库元数据，此处仅标记），默认 false</summary>
        [XmlElement]
        public bool ExpandSelectStar { get; set; } = false;

        /// <summary>列别名 AS 关键字处理模式，默认保持</summary>
        [XmlElement]
        public AsKeywordMode AsKeywordMode { get; set; } = AsKeywordMode.Keep;

        /// <summary>是否对齐列别名（在 AS 后纵向对齐），默认 false</summary>
        [XmlElement]
        public bool AlignColumnAlias { get; set; } = false;

        /// <summary>是否将 SELECT 列列表的后续列名对齐到首列名起始列，默认 true</summary>
        [XmlElement]
        public bool SelectListColumnAlign { get; set; } = true;

        /// <summary>是否在超过行宽时对长函数调用换行，默认 true</summary>
        [XmlElement]
        public bool WrapLongFunction { get; set; } = true;

        /// <summary>是否对齐列行内注释，默认 false</summary>
        [XmlElement]
        public bool AlignColumnComments { get; set; } = false;

        // ── FROM / JOIN ──────────────────────────────────────────────────────────

        /// <summary>JOIN 关键字是否另起新行，默认 true</summary>
        [XmlElement]
        public bool JoinKeywordNewLine { get; set; } = true;

        /// <summary>JOIN 的表是否相对 FROM 缩进，默认 false</summary>
        [XmlElement]
        public bool IndentJoinTable { get; set; } = false;

        /// <summary>ON 条件的缩进量（相对 JOIN），默认 1 层缩进</summary>
        [XmlElement]
        public int OnConditionIndent { get; set; } = 1;

        /// <summary>逻辑运算符（AND/OR）是否前置于条件行首，默认 false</summary>
        [XmlElement]
        public bool LogicOperatorBefore { get; set; } = false;

        /// <summary>是否将隐式 JOIN（逗号分隔）转换为显式 INNER JOIN，默认 false</summary>
        [XmlElement]
        public bool ConvertImplicitJoin { get; set; } = false;

        // ── WHERE ────────────────────────────────────────────────────────────────

        /// <summary>WHERE 下所有条件是否统一缩进（含第一个条件），默认 true</summary>
        [XmlElement]
        public bool WhereIndentAllConditions { get; set; } = true;

        /// <summary>IN 列表过长时是否换行，默认 true</summary>
        [XmlElement]
        public bool WrapLongInList { get; set; } = true;

        /// <summary>是否对齐比较运算符（=/<>/< 等），默认 false</summary>
        [XmlElement]
        public bool AlignCompareOperator { get; set; } = false;

        /// <summary>嵌套括号内的条件是否额外缩进，默认 true</summary>
        [XmlElement]
        public bool NestedParenthesisIndent { get; set; } = true;

        /// <summary>是否将 BETWEEN expr1 AND expr2 的 AND 部分保持在同一行，默认 true</summary>
        [XmlElement]
        public bool KeepBetweenAndOnSameLine { get; set; } = true;

        // ── GROUP BY / HAVING / ORDER BY ────────────────────────────────────────

        /// <summary>GROUP BY 中每列是否各占一行，默认 true</summary>
        [XmlElement]
        public bool GroupByEachColumnNewLine { get; set; } = true;

        /// <summary>HAVING 是否复用 WHERE 的缩进/换行风格，默认 true</summary>
        [XmlElement]
        public bool HavingReuseWhereStyle { get; set; } = true;

        /// <summary>ORDER BY 中 ASC/DESC 是否右对齐，默认 false</summary>
        [XmlElement]
        public bool OrderBySortAlign { get; set; } = false;

        /// <summary>窗口函数（OVER 子句）是否缩进展开，默认 true</summary>
        [XmlElement]
        public bool WindowFunctionIndent { get; set; } = true;

        // ── INSERT / UPDATE / DELETE / MERGE ────────────────────────────────────

        /// <summary>INSERT 的列列表过多时是否换行，默认 true</summary>
        [XmlElement]
        public bool InsertColumnsWrap { get; set; } = true;

        /// <summary>VALUES 中多行数据是否纵向对齐，默认 false</summary>
        [XmlElement]
        public bool ValuesRowAlign { get; set; } = false;

        /// <summary>UPDATE SET 中的赋值等号是否对齐，默认 false</summary>
        [XmlElement]
        public bool UpdateSetAlignEqual { get; set; } = false;

        /// <summary>MERGE 语句的 WHEN 分支是否额外缩进，默认 true</summary>
        [XmlElement]
        public bool MergeBranchIndent { get; set; } = true;

        /// <summary>OUTPUT 子句是否换行展开，默认 true</summary>
        [XmlElement]
        public bool OutputClauseWrap { get; set; } = true;
    }
}
