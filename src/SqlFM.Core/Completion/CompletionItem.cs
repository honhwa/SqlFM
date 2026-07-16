namespace SqlFM.Core.Completion
{
    /// <summary>
    /// SQL 补全项，描述一个补全建议的显示文本、插入文本、图标类型和描述信息。
    /// </summary>
    public class CompletionItem
    {
        /// <summary>补全项显示文本（下拉列表中的标签）</summary>
        public string DisplayText { get; set; } = string.Empty;

        /// <summary>补全项插入文本（选中后替换光标前缀的完整文本）</summary>
        public string InsertText { get; set; } = string.Empty;

        /// <summary>补全项描述（下拉列表中的辅助信息）</summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>补全项图标类型</summary>
        public CompletionIcon Icon { get; set; } = CompletionIcon.Keyword;

        /// <summary>排序优先级（数值越小优先级越高）</summary>
        public int Priority { get; set; } = 100;

        /// <summary>创建关键字补全项</summary>
        public static CompletionItem Keyword(string keyword)
        {
            return new CompletionItem
            {
                DisplayText = keyword,
                InsertText = keyword,
                Description = "SQL 关键字",
                Icon = CompletionIcon.Keyword,
                Priority = 10
            };
        }

        /// <summary>创建函数补全项</summary>
        public static CompletionItem Function(string name, string signature, string desc)
        {
            return new CompletionItem
            {
                DisplayText = name,
                InsertText = signature,
                Description = desc,
                Icon = CompletionIcon.Function,
                Priority = 20
            };
        }

        /// <summary>创建数据类型补全项</summary>
        public static CompletionItem DataType(string typeName)
        {
            return new CompletionItem
            {
                DisplayText = typeName,
                InsertText = typeName,
                Description = "数据类型",
                Icon = CompletionIcon.DataType,
                Priority = 30
            };
        }

        /// <summary>创建 Snippet 补全项</summary>
        public static CompletionItem Snippet(string shortcut, string expansion, string desc)
        {
            return new CompletionItem
            {
                DisplayText = shortcut,
                InsertText = expansion,
                Description = desc,
                Icon = CompletionIcon.Snippet,
                Priority = 50
            };
        }
    }

    /// <summary>
    /// 补全项图标类型分类
    /// </summary>
    public enum CompletionIcon
    {
        /// <summary>SQL 关键字</summary>
        Keyword,

        /// <summary>内置函数</summary>
        Function,

        /// <summary>数据类型</summary>
        DataType,

        /// <summary>代码片段/模板</summary>
        Snippet,

        /// <summary>表名</summary>
        Table,

        /// <summary>列名</summary>
        Column,

        /// <summary>别名</summary>
        Alias
    }
}
