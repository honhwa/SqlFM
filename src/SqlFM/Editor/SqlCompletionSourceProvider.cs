using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;
using SqlFM.Core.Completion;
using SqlFM.Core.Dialects;

namespace SqlFM.Editor
{
    /// <summary>
    /// 将 CompletionCatalog 接入 SSMS 编辑器的 IntelliSense 补全（建议 #11）。
    /// 通过 MEF 导出 ICompletionSourceProvider，由编辑器在触发补全会话时回调本类，
    /// 由 <see cref="SqlCompletionEngine"/> 依据当前方言与前缀生成补全项。
    /// 注：运行时是否真正被 SSMS 编辑器接管，取决于 SSMS 使用的 SQL 内容类型是否匹配 "SQL"，
    /// 若 SSMS 使用专有内容类型，可能需在 [ContentType] 上调整为对应值；此为环境集成事项，不影响编译与逻辑正确性。
    /// </summary>
    [Export(typeof(ICompletionSourceProvider))]
    [ContentType("SQL")]
    [Name("SqlFMCompletionSourceProvider")]
    internal class SqlCompletionSourceProvider : ICompletionSourceProvider
    {
        /// <inheritdoc/>
        public ICompletionSource TryCreateCompletionSource(ITextBuffer textBuffer)
        {
            return new SqlCompletionSource(textBuffer);
        }
    }

    /// <summary>
    /// 具体的补全源：根据光标位置构建 <see cref="CompletionContext"/> 并向编辑器提供补全集合。
    /// </summary>
    internal class SqlCompletionSource : ICompletionSource
    {
        private readonly ITextBuffer _buffer;
        private bool _disposed;

        /// <summary>
        /// 初始化补全源。
        /// </summary>
        /// <param name="buffer">绑定的文本缓冲区</param>
        public SqlCompletionSource(ITextBuffer buffer)
        {
            _buffer = buffer;
        }

        /// <inheritdoc/>
        public void AugmentCompletionSession(ICompletionSession session, IList<CompletionSet> completionSets)
        {
            if (_disposed)
            {
                return;
            }

            var snapshot = _buffer.CurrentSnapshot;
            int position = session.GetTriggerPoint(snapshot)?.Position ?? snapshot.Length;
            string text = snapshot.GetText();

            // 计算光标前的标识符前缀（用于过滤补全列表）
            int start = position;
            while (start > 0 && IsIdentifierChar(text[start - 1]))
            {
                start--;
            }
            string prefix = text.Substring(start, position - start);

            var line = snapshot.GetLineFromPosition(position);
            var context = new CompletionContext
            {
                Line = line.LineNumber + 1,
                Column = position - line.Start.Position + 1,
                Prefix = prefix,
                FullSql = text,
                Dialect = TsqlDialect.Instance
            };

            var engine = CompletionCatalog.DefaultEngine;
            var items = engine.GetCompletions(context);
            if (items == null || items.Count == 0)
            {
                return;
            }

            var completions = new List<Completion>();
            foreach (var ci in items)
            {
                completions.Add(new Completion(ci.DisplayText, ci.InsertText, ci.Description, null, null));
            }

            var trackingSpan = snapshot.CreateTrackingSpan(
                start, position - start, SpanTrackingMode.EdgeInclusive);
            completionSets.Add(new CompletionSet("SqlFM", "SqlFM", trackingSpan, completions, null));
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            _disposed = true;
        }

        /// <summary>
        /// 判断字符是否属于 SQL 标识符（用于截取前缀）。
        /// </summary>
        private static bool IsIdentifierChar(char c)
        {
            return char.IsLetterOrDigit(c) || c == '_' || c == '@' || c == '#' || c == '$';
        }
    }
}
