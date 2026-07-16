using System;
using System.Collections.Generic;
using SqlFM.Core.Dialects;

namespace SqlFM.Core.Completion
{
    /// <summary>
    /// 关键字补全提供器，基于方言的关键字集合生成补全建议。
    /// </summary>
    public class KeywordCompletionProvider : ICompletionProvider
    {
        /// <inheritdoc/>
        public string Name => "keywords";

        /// <inheritdoc/>
        public string[] SupportedDialects => Array.Empty<string>(); // 所有方言

        /// <inheritdoc/>
        public List<CompletionItem> Provide(CompletionContext context)
        {
            var items = new List<CompletionItem>();

            // 从方言的关键字集合生成补全项
            foreach (var keyword in context.Dialect.ReservedKeywords)
            {
                items.Add(CompletionItem.Keyword(keyword));
            }

            foreach (var keyword in context.Dialect.UnreservedKeywords)
            {
                items.Add(CompletionItem.Keyword(keyword));
            }

            return items;
        }
    }

    /// <summary>
    /// 内置函数补全提供器，基于方言的内置函数集合生成补全建议。
    /// </summary>
    public class FunctionCompletionProvider : ICompletionProvider
    {
        /// <inheritdoc/>
        public string Name => "functions";

        /// <inheritdoc/>
        public string[] SupportedDialects => Array.Empty<string>(); // 所有方言

        /// <inheritdoc/>
        public List<CompletionItem> Provide(CompletionContext context)
        {
            var items = new List<CompletionItem>();

            foreach (var func in context.Dialect.BuiltInFunctions)
            {
                items.Add(CompletionItem.Function(func, func + "()", $"内置函数 {func}"));
            }

            return items;
        }
    }

    /// <summary>
    /// 数据类型补全提供器，基于方言的数据类型集合生成补全建议。
    /// </summary>
    public class DataTypeCompletionProvider : ICompletionProvider
    {
        /// <inheritdoc/>
        public string Name => "datatypes";

        /// <inheritdoc/>
        public string[] SupportedDialects => Array.Empty<string>(); // 所有方言

        /// <inheritdoc/>
        public List<CompletionItem> Provide(CompletionContext context)
        {
            var items = new List<CompletionItem>();

            foreach (var dt in context.Dialect.DataTypes)
            {
                items.Add(CompletionItem.DataType(dt));
            }

            return items;
        }
    }
}
