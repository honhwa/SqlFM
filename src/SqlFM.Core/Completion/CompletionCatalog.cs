using System;
using SqlFM.Core.Completion.Snippets;

namespace SqlFM.Core.Completion
{
    /// <summary>
    /// 补全提供器目录：集中注册所有 <see cref="ICompletionProvider"/> 实现，构建可直接使用的 <see cref="SqlCompletionEngine"/>。
    /// 解决此前 SqlCompletionEngine「已建成未接线」的问题——关键字/函数/数据类型/片段等提供器虽已实现，
    /// 却从未被集中注册，导致引擎实例化后无补全项。
    /// 新增提供器只需实现 ICompletionProvider，在此显式注册即可接入编辑器智能感知。
    /// </summary>
    public static class CompletionCatalog
    {
        private static readonly Lazy<SqlCompletionEngine> _defaultEngine =
            new Lazy<SqlCompletionEngine>(BuildEngine);

        /// <summary>获取注册了全部提供器的默认补全引擎单例（线程安全，按需构建一次）。</summary>
        public static SqlCompletionEngine DefaultEngine => _defaultEngine.Value;

        /// <summary>
        /// 构建并注册所有内置补全提供器的引擎。
        /// </summary>
        /// <returns>已注册全部提供器的 <see cref="SqlCompletionEngine"/> 实例</returns>
        public static SqlCompletionEngine BuildEngine()
        {
            var engine = new SqlCompletionEngine();
            engine.Register(new KeywordCompletionProvider());
            engine.Register(new FunctionCompletionProvider());
            engine.Register(new DataTypeCompletionProvider());
            engine.Register(new SnippetCompletionProvider());
            return engine;
        }
    }
}
