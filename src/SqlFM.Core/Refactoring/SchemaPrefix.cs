using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace SqlFM.Core.Refactoring
{
    /// <summary>
    /// dbo 架构前缀标准化工具。
    /// 使用 ScriptDom 解析 AST，精确定位 <see cref="NamedTableReference"/> 节点后进行文本替换，
    /// 不影响注释、字符串字面量及其他非标识符内容。
    /// </summary>
    public class SchemaPrefix
    {
        private readonly TSql160Parser _parser;

        /// <summary>初始化 SchemaPrefix 处理器</summary>
        public SchemaPrefix()
        {
            _parser = new TSql160Parser(initialQuotedIdentifiers: false);
        }

        /// <summary>
        /// 为 SQL 中所有未限定架构的表引用添加 <c>dbo.</c> 前缀。
        /// 例如：<c>FROM Orders</c> → <c>FROM dbo.Orders</c>
        /// </summary>
        /// <param name="sql">原始 SQL 文本</param>
        /// <returns>添加前缀后的 SQL；若解析失败则返回原文</returns>
        public string AddDboPrefix(string sql)
        {
            if (string.IsNullOrWhiteSpace(sql)) return sql;

            var fragment = Parse(sql);
            if (fragment == null) return sql;

            var visitor = new NoPrefixTableVisitor();
            fragment.Accept(visitor);

            if (visitor.Targets.Count == 0) return sql;

            // 从后向前替换，避免偏移量失效
            var sb = new StringBuilder(sql);
            for (int i = visitor.Targets.Count - 1; i >= 0; i--)
            {
                var info = visitor.Targets[i];
                // 在对象名起始位置插入 "dbo."
                sb.Insert(info.InsertOffset, "dbo.");
            }

            return sb.ToString();
        }

        /// <summary>
        /// 移除 SQL 中所有 <c>dbo.</c> 架构前缀（仅移除 dbo 前缀，保留其他架构前缀）。
        /// 例如：<c>FROM dbo.Orders</c> → <c>FROM Orders</c>
        /// </summary>
        /// <param name="sql">原始 SQL 文本</param>
        /// <returns>移除前缀后的 SQL；若解析失败则返回原文</returns>
        public string RemoveDboPrefix(string sql)
        {
            if (string.IsNullOrWhiteSpace(sql)) return sql;

            var fragment = Parse(sql);
            if (fragment == null) return sql;

            var visitor = new DboSchemaTableVisitor();
            fragment.Accept(visitor);

            if (visitor.Targets.Count == 0) return sql;

            // 从后向前删除 "dbo." 前缀文本
            var sb = new StringBuilder(sql);
            for (int i = visitor.Targets.Count - 1; i >= 0; i--)
            {
                var info = visitor.Targets[i];
                // RemoveOffset 是 "dbo." 在原始 SQL 中的起始偏移
                sb.Remove(info.RemoveOffset, info.RemoveLength);
            }

            return sb.ToString();
        }

        // ── 私有辅助方法 ────────────────────────────────────────────────

        /// <summary>解析 SQL 为 AST，语法错误时返回 null（不影响原文本）。</summary>
        private TSqlFragment? Parse(string sql)
        {
            using (var reader = new StringReader(sql))
            {
                IList<ParseError> errors;
                var fragment = _parser.Parse(reader, out errors);
                return errors.Count == 0 ? fragment : null;
            }
        }

        // ── 内部 Visitor：查找无架构前缀的表引用 ─────────────────────

        private class NoPrefixTableVisitor : TSqlFragmentVisitor
        {
            /// <summary>需要在此偏移量处插入 "dbo." 的位置列表</summary>
            public readonly List<InsertInfo> Targets = new List<InsertInfo>();

            public override void Visit(NamedTableReference node)
            {
                var schema = node.SchemaObject;
                if (schema == null) return;

                // SchemaIdentifier 为 null 或空 → 无架构前缀
                if (schema.SchemaIdentifier != null
                    && !string.IsNullOrEmpty(schema.SchemaIdentifier.Value))
                    return;

                // BaseIdentifier 即为表名节点，在其起始偏移处插入 "dbo."
                var baseId = schema.BaseIdentifier;
                if (baseId != null)
                {
                    Targets.Add(new InsertInfo { InsertOffset = baseId.StartOffset });
                }
            }
        }

        private class InsertInfo
        {
            public int InsertOffset { get; set; }
        }

        // ── 内部 Visitor：查找 schema=dbo 的表引用 ──────────────────

        private class DboSchemaTableVisitor : TSqlFragmentVisitor
        {
            public readonly List<RemoveInfo> Targets = new List<RemoveInfo>();

            public override void Visit(NamedTableReference node)
            {
                var schema = node.SchemaObject;
                if (schema?.SchemaIdentifier == null) return;

                if (!string.Equals(schema.SchemaIdentifier.Value, "dbo",
                        System.StringComparison.OrdinalIgnoreCase))
                    return;

                // 需要删除从 SchemaIdentifier 开头到 "." 的内容，即 "dbo."
                // SchemaIdentifier.StartOffset 是 "dbo" 的起始位置
                // BaseIdentifier.StartOffset 是表名的起始位置
                var schemaStart = schema.SchemaIdentifier.StartOffset;
                var tableStart  = schema.BaseIdentifier.StartOffset;

                // 删除长度 = 表名起始 - schema起始（含中间的 "."）
                int removeLength = tableStart - schemaStart;
                if (removeLength > 0)
                {
                    Targets.Add(new RemoveInfo
                    {
                        RemoveOffset = schemaStart,
                        RemoveLength = removeLength
                    });
                }
            }
        }

        private class RemoveInfo
        {
            public int RemoveOffset { get; set; }
            public int RemoveLength { get; set; }
        }
    }
}
