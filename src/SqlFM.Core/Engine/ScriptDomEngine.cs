using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace SqlFM.Core.Engine
{
    /// <summary>
    /// Microsoft ScriptDom 辅助引擎（语法校验、AST 解析、对象名提取）
    /// </summary>
    public class ScriptDomEngine
    {
        private readonly TSql160Parser _parser;

        /// <summary>初始化 ScriptDomEngine，创建 SQL Server 2022 (TSql160) 解析器实例。</summary>
        public ScriptDomEngine()
        {
            _parser = new TSql160Parser(initialQuotedIdentifiers: false);
        }

        /// <summary>
        /// 验证 SQL 语法是否合法。
        /// </summary>
        /// <param name="sql">待校验的 SQL 文本</param>
        /// <param name="errors">校验失败时输出的 <see cref="ParseError"/> 列表</param>
        /// <returns>无语法错误返回 true，否则返回 false</returns>
        public bool Validate(string sql, out IList<ParseError> errors)
        {
            using (var reader = new StringReader(sql))
            {
                _parser.Parse(reader, out errors);
                return errors.Count == 0;
            }
        }

        // ── 解析结果缓存（性能优化）──
        // ScriptDom 解析是管线中最昂贵的操作，且同一段 SQL 在大小写/对齐/对象名提取等多处被重复解析。
        // TSqlFragment 为不可变只读结构（访问者仅遍历、不修改），按原文缓存安全且可显著提速。
        // 采用有界 FIFO 缓存，避免大批量处理时无限增长内存。
        private const int ParseCacheCapacity = 256;
        private static readonly object _cacheLock = new object();
        private static readonly Dictionary<string, TSqlFragment> _parseCache =
            new Dictionary<string, TSqlFragment>(StringComparer.Ordinal);
        private static readonly Queue<string> _parseCacheKeys = new Queue<string>();

        /// <summary>
        /// 解析 SQL 获取 AST（抽象语法树）。结果按原文缓存（有界 FIFO），重复解析直接命中缓存。
        /// </summary>
        /// <param name="sql">待解析的 SQL 文本</param>
        /// <returns>TSqlFragment AST 根节点；解析失败返回 null</returns>
        public TSqlFragment? Parse(string sql)
        {
            if (string.IsNullOrEmpty(sql))
            {
                using (var reader = new StringReader(sql))
                    return _parser.Parse(reader, out _);
            }

            lock (_cacheLock)
            {
                if (_parseCache.TryGetValue(sql, out var cached))
                    return cached;
            }

            TSqlFragment fragment;
            using (var reader = new StringReader(sql))
            {
                fragment = _parser.Parse(reader, out _);
            }

            if (fragment != null)
            {
                lock (_cacheLock)
                {
                    if (_parseCache.Count >= ParseCacheCapacity)
                    {
                        while (_parseCacheKeys.Count > 0 && _parseCache.Count >= ParseCacheCapacity)
                            _parseCache.Remove(_parseCacheKeys.Dequeue());
                    }
                    _parseCache[sql] = fragment;
                    _parseCacheKeys.Enqueue(sql);
                }
            }

            return fragment;
        }

        /// <summary>
        /// 提取 SQL 中引用的所有对象名（表名/视图名等），去重后返回。
        /// </summary>
        /// <param name="sql">待分析的 SQL 文本</param>
        /// <returns>对象名称列表（不重复）</returns>
        public IList<string> ExtractObjectNames(string sql)
        {
            var result = new List<string>();
            var fragment = Parse(sql);
            if (fragment == null) return result;

            var visitor = new ObjectNameVisitor();
            fragment.Accept(visitor);

            foreach (var name in visitor.ObjectNames)
            {
                if (!result.Contains(name))
                    result.Add(name);
            }
            return result;
        }

        /// <summary>
        /// 获取 SQL 中各语句的类型列表（如 SELECT、INSERT、UPDATE、DELETE 等），去重后返回。
        /// </summary>
        /// <param name="sql">待分析的 SQL 文本</param>
        /// <returns>语句类型名称列表（不重复）</returns>
        public IList<string> GetStatementTypes(string sql)
        {
            var result = new List<string>();
            var fragment = Parse(sql);
            if (fragment == null) return result;

            var visitor = new StatementTypeVisitor();
            fragment.Accept(visitor);
            return visitor.StatementTypes;
        }

        /// <summary>
        /// 将 SELECT * 展开为字段列表（需调用方提供表-列映射）
        /// </summary>
        /// <param name="sql">原始 SQL</param>
        /// <param name="tableColumns">表名 → 列名列表 的映射字典（键不区分大小写）</param>
        /// <returns>展开后的 SQL；若无 SELECT * 则返回原文</returns>
        public string ExpandSelectStar(string sql,
            IDictionary<string, IList<string>> tableColumns)
        {
            if (tableColumns == null || tableColumns.Count == 0)
                return sql;

            var fragment = Parse(sql);
            if (fragment == null) return sql;

            // 收集所有 SELECT * 位置信息
            var visitor = new SelectStarVisitor();
            fragment.Accept(visitor);

            if (visitor.StarColumns.Count == 0)
                return sql;

            // 从后向前替换，避免偏移量错乱
            var chars = new System.Text.StringBuilder(sql);
            for (int i = visitor.StarColumns.Count - 1; i >= 0; i--)
            {
                var star = visitor.StarColumns[i];
                string? tableName = star.TableName;

                IList<string>? columns = null;
                if (!string.IsNullOrEmpty(tableName))
                {
                    // 不区分大小写查找
                    foreach (var kv in tableColumns)
                    {
                        if (string.Equals(kv.Key, tableName,
                                System.StringComparison.OrdinalIgnoreCase))
                        {
                            columns = kv.Value;
                            break;
                        }
                    }
                }
                else if (tableColumns.Count == 1)
                {
                    // 只有一张表时直接使用
                    foreach (var kv in tableColumns)
                        columns = kv.Value;
                }

                if (columns == null || columns.Count == 0)
                    continue;

                string prefix = string.IsNullOrEmpty(tableName) ? "" : tableName + ".";
                string expanded = string.Join(", ", System.Linq.Enumerable.Select(columns, c => prefix + c));

                int startOffset = star.StartOffset;
                int length = star.FragmentLength;
                chars.Remove(startOffset, length);
                chars.Insert(startOffset, expanded);
            }

            return chars.ToString();
        }

        // ── 内部访问者 ──────────────────────────────────────────────────

        /// <summary>收集 FROM/JOIN 子句中的表/视图引用名</summary>
        private class ObjectNameVisitor : TSqlFragmentVisitor
        {
            public readonly List<string> ObjectNames = new List<string>();

            public override void Visit(NamedTableReference node)
            {
                if (node.SchemaObject != null)
                {
                    var name = node.SchemaObject.BaseIdentifier?.Value;
                    if (!string.IsNullOrEmpty(name))
                        ObjectNames.Add(name!);
                }
            }
        }

        /// <summary>收集各顶层语句的类型名称</summary>
        private class StatementTypeVisitor : TSqlFragmentVisitor
        {
            public readonly List<string> StatementTypes = new List<string>();

            public override void Visit(SelectStatement node)  => Add("SELECT");
            public override void Visit(InsertStatement node)  => Add("INSERT");
            public override void Visit(UpdateStatement node)  => Add("UPDATE");
            public override void Visit(DeleteStatement node)  => Add("DELETE");
            public override void Visit(MergeStatement node)   => Add("MERGE");
            public override void Visit(CreateTableStatement node)  => Add("CREATE TABLE");
            public override void Visit(AlterTableStatement node)   => Add("ALTER TABLE");
            public override void Visit(DropTableStatement node)    => Add("DROP TABLE");
            public override void Visit(CreateProcedureStatement node) => Add("CREATE PROCEDURE");
            public override void Visit(AlterProcedureStatement node)  => Add("ALTER PROCEDURE");
            public override void Visit(CreateFunctionStatement node)  => Add("CREATE FUNCTION");
            public override void Visit(AlterFunctionStatement node)   => Add("ALTER FUNCTION");
            public override void Visit(CreateViewStatement node)   => Add("CREATE VIEW");
            public override void Visit(AlterViewStatement node)    => Add("ALTER VIEW");

            private void Add(string type)
            {
                if (!StatementTypes.Contains(type))
                    StatementTypes.Add(type);
            }
        }

        /// <summary>收集 SELECT 列表中的星号列信息</summary>
        private class SelectStarVisitor : TSqlFragmentVisitor
        {
            public readonly List<SelectStarInfo> StarColumns = new List<SelectStarInfo>();

            public override void Visit(SelectStarExpression node)
            {
                string? tableName = null;
                if (node.Qualifier != null && node.Qualifier.Identifiers.Count > 0)
                    tableName = node.Qualifier.Identifiers[node.Qualifier.Identifiers.Count - 1].Value;

                StarColumns.Add(new SelectStarInfo
                {
                    StartOffset = node.StartOffset,
                    FragmentLength = node.FragmentLength,
                    TableName = tableName
                });
            }
        }

        private class SelectStarInfo
        {
            /// <summary>星号表达式在原始 SQL 中的起始偏移量</summary>
            public int StartOffset { get; set; }

            /// <summary>星号表达式的字符长度</summary>
            public int FragmentLength { get; set; }

            /// <summary>限定前缀表名（如 t.* 中的 t）；无前缀时为 null</summary>
            public string? TableName { get; set; }
        }
    }
}
