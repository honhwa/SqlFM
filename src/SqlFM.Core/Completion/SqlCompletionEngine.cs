using System;
using System.Collections.Generic;
using System.Linq;
using SqlFM.Core.Dialects;

namespace SqlFM.Core.Completion
{
    /// <summary>
    /// 补全上下文，描述触发补全时编辑器的状态信息。
    /// </summary>
    public class CompletionContext
    {
        /// <summary>光标所在行号（从 1 开始）</summary>
        public int Line { get; set; } = 1;

        /// <summary>光标所在列号（从 1 开始）</summary>
        public int Column { get; set; } = 1;

        /// <summary>光标前已输入的前缀文本（用于过滤补全列表）</summary>
        public string Prefix { get; set; } = string.Empty;

        /// <summary>当前方言</summary>
        public SqlDialect Dialect { get; set; } = AnsiDialect.Instance;

        /// <summary>完整 SQL 文本</summary>
        public string FullSql { get; set; } = string.Empty;

        /// <summary>触发类型（Ctrl+Space / 自动触发 / 字符触发）</summary>
        public CompletionTrigger Trigger { get; set; } = CompletionTrigger.Manual;
    }

    /// <summary>
    /// 补全触发类型
    /// </summary>
    public enum CompletionTrigger
    {
        /// <summary>手动触发（Ctrl+Space）</summary>
        Manual,

        /// <summary>自动触发（输入特定字符后自动弹出）</summary>
        AutoChar,

        /// <summary>过滤触发（继续输入时缩小补全列表）</summary>
        Filter
    }

    /// <summary>
    /// 补全提供器接口，每种补全类型实现自己的提供逻辑。
    /// </summary>
    public interface ICompletionProvider
    {
        /// <summary>提供器名称</summary>
        string Name { get; }

        /// <summary>适用的方言名称列表（空数组表示所有方言）</summary>
        string[] SupportedDialects { get; }

        /// <summary>根据上下文提供补全项列表</summary>
        List<CompletionItem> Provide(CompletionContext context);
    }

    /// <summary>
    /// SQL 补全引擎，协调各补全提供器，根据上下文和方言返回补全建议列表。
    /// </summary>
    public class SqlCompletionEngine
    {
        /// <summary>已注册的补全提供器</summary>
        private readonly List<ICompletionProvider> _providers = new List<ICompletionProvider>();

        /// <summary>注册补全提供器</summary>
        public void Register(ICompletionProvider provider)
        {
            _providers.Add(provider);
        }

        /// <summary>获取补全建议列表</summary>
        public List<CompletionItem> GetCompletions(CompletionContext context)
        {
            var results = new List<CompletionItem>();

            foreach (var provider in _providers)
            {
                // 过滤不适用于当前方言的提供器
                if (provider.SupportedDialects.Length > 0 &&
                    !provider.SupportedDialects.Contains(context.Dialect.Name))
                    continue;

                var items = provider.Provide(context);
                if (items != null && items.Count > 0)
                    results.AddRange(items);
            }

            // 根据前缀过滤
            if (!string.IsNullOrEmpty(context.Prefix))
            {
                string prefixUpper = context.Prefix.ToUpperInvariant();
                results = results.FindAll(item =>
                    item.DisplayText.ToUpperInvariant().StartsWith(prefixUpper));
            }

            // 按优先级排序
            results.Sort((a, b) => a.Priority - b.Priority);

            return results;
        }
    }
}
