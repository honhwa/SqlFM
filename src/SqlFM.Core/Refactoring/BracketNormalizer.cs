using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace SqlFM.Core.Refactoring
{
    /// <summary>
    /// 标识符方括号标准化工具。
    /// 使用 ScriptDom 解析 AST 定位所有标识符节点，基于偏移量进行精确文本替换。
    /// </summary>
    public class BracketNormalizer
    {
        private readonly TSql160Parser _parser;

        // SQL Server 保留关键字集合（方括号对保留字是必要的）
        private static readonly HashSet<string> _reservedKeywords = new HashSet<string>(
            System.StringComparer.OrdinalIgnoreCase)
        {
            "ADD","ALL","ALTER","AND","ANY","AS","ASC","AUTHORIZATION","BACKUP","BEGIN",
            "BETWEEN","BREAK","BROWSE","BULK","BY","CASCADE","CASE","CHECK","CHECKPOINT",
            "CLOSE","CLUSTERED","COALESCE","COLLATE","COLUMN","COMMIT","COMPUTE","CONSTRAINT",
            "CONTAINS","CONTAINSTABLE","CONTINUE","CONVERT","CREATE","CROSS","CURRENT",
            "CURRENT_DATE","CURRENT_TIME","CURRENT_TIMESTAMP","CURRENT_USER","CURSOR",
            "DATABASE","DBCC","DEALLOCATE","DECLARE","DEFAULT","DELETE","DENY","DESC",
            "DISK","DISTINCT","DISTRIBUTED","DOUBLE","DROP","DUMP","ELSE","END","ERRLVL",
            "ESCAPE","EXCEPT","EXEC","EXECUTE","EXISTS","EXIT","EXTERNAL","FETCH","FILE",
            "FILLFACTOR","FOR","FOREIGN","FREETEXT","FREETEXTTABLE","FROM","FULL","FUNCTION",
            "GOTO","GRANT","GROUP","HAVING","HOLDLOCK","IDENTITY","IDENTITY_INSERT","IDENTITYCOL",
            "IF","IN","INDEX","INNER","INSERT","INTERSECT","INTO","IS","JOIN","KEY","KILL",
            "LEFT","LIKE","LINENO","LOAD","MERGE","NATIONAL","NOCHECK","NONCLUSTERED","NOT",
            "NULL","NULLIF","OF","OFF","OFFSETS","ON","OPEN","OPENDATASOURCE","OPENQUERY",
            "OPENROWSET","OPENXML","OPTION","OR","ORDER","OUTER","OVER","PERCENT","PIVOT",
            "PLAN","PRECISION","PRIMARY","PRINT","PROC","PROCEDURE","PUBLIC","RAISERROR",
            "READ","READTEXT","RECONFIGURE","REFERENCES","REPLICATION","RESTORE","RESTRICT",
            "RETURN","REVERT","REVOKE","RIGHT","ROLLBACK","ROWCOUNT","ROWGUIDCOL","RULE",
            "SAVE","SCHEMA","SECURITYAUDIT","SELECT","SEMANTICKEYPHRASETABLE",
            "SEMANTICSIMILARITYDETAILSTABLE","SEMANTICSIMILARITYTABLE","SESSION_USER",
            "SET","SETUSER","SHUTDOWN","SOME","STATISTICS","SYSTEM_USER","TABLE","TABLESAMPLE",
            "TEXTSIZE","THEN","TO","TOP","TRAN","TRANSACTION","TRIGGER","TRUNCATE","TRY_CONVERT",
            "TSEQUAL","UNION","UNIQUE","UNPIVOT","UPDATE","UPDATETEXT","USE","USER","VALUES",
            "VARYING","VIEW","WAITFOR","WHEN","WHERE","WHILE","WITH","WITHIN","WRITETEXT"
        };

        /// <summary>初始化 BracketNormalizer</summary>
        public BracketNormalizer()
        {
            _parser = new TSql160Parser(initialQuotedIdentifiers: false);
        }

        /// <summary>
        /// 给 SQL 中所有未加方括号的对象名标识符添加方括号。
        /// 例如：<c>SELECT Name FROM Orders</c> → <c>SELECT [Name] FROM [Orders]</c>
        /// </summary>
        /// <param name="sql">原始 SQL 文本</param>
        /// <returns>添加方括号后的 SQL；若解析失败则返回原文</returns>
        public string AddBrackets(string sql)
        {
            if (string.IsNullOrWhiteSpace(sql)) return sql;

            var fragment = Parse(sql);
            if (fragment == null) return sql;

            var visitor = new IdentifierVisitor();
            fragment.Accept(visitor);

            if (visitor.Identifiers.Count == 0) return sql;

            var sb = new StringBuilder(sql);
            // 从后向前，避免偏移失效
            for (int i = visitor.Identifiers.Count - 1; i >= 0; i--)
            {
                var id = visitor.Identifiers[i];
                if (id.QuoteType == QuoteType.SquareBracket) continue; // 已有方括号

                int start  = id.StartOffset;
                int length = id.FragmentLength;

                sb.Remove(start, length);
                sb.Insert(start, "[" + id.Value + "]");
            }

            return sb.ToString();
        }

        /// <summary>
        /// 移除 SQL 中不必要的方括号（非保留字不需要方括号）。
        /// 例如：<c>SELECT [CustomerName] FROM [dbo].[Orders]</c>
        ///      → <c>SELECT CustomerName FROM [dbo].Orders</c>（Orders 非保留字，CustomerName 非保留字）
        /// </summary>
        /// <param name="sql">原始 SQL 文本</param>
        /// <returns>清理方括号后的 SQL；若解析失败则返回原文</returns>
        public string RemoveBrackets(string sql)
        {
            if (string.IsNullOrWhiteSpace(sql)) return sql;

            var fragment = Parse(sql);
            if (fragment == null) return sql;

            var visitor = new IdentifierVisitor();
            fragment.Accept(visitor);

            if (visitor.Identifiers.Count == 0) return sql;

            var sb = new StringBuilder(sql);
            for (int i = visitor.Identifiers.Count - 1; i >= 0; i--)
            {
                var id = visitor.Identifiers[i];
                if (id.QuoteType != QuoteType.SquareBracket) continue; // 无方括号，跳过

                // 是保留字则保留方括号，不做移除
                if (_reservedKeywords.Contains(id.Value)) continue;

                // 标识符包含空格或特殊字符时，方括号是必要的，不移除
                if (NeedsQuoting(id.Value)) continue;

                // 移除方括号：原文是 [Name]，长度 = value.Length + 2
                int start  = id.StartOffset;
                int length = id.FragmentLength; // 含方括号

                sb.Remove(start, length);
                sb.Insert(start, id.Value);
            }

            return sb.ToString();
        }

        // ── 私有辅助 ────────────────────────────────────────────────────

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

        /// <summary>判断标识符值本身是否需要方括号（含非标准字符时返回 true）</summary>
        private static bool NeedsQuoting(string name)
        {
            if (string.IsNullOrEmpty(name)) return true;
            foreach (char c in name)
            {
                if (!(char.IsLetterOrDigit(c) || c == '_' || c == '@' || c == '#' || c == '$'))
                    return true;
            }
            // 以数字开头也需要引号
            if (char.IsDigit(name[0])) return true;
            return false;
        }

        // ── 内部 Visitor：收集所有 Identifier 节点 ──────────────────

        private class IdentifierVisitor : TSqlFragmentVisitor
        {
            public readonly List<Identifier> Identifiers = new List<Identifier>();

            public override void Visit(Identifier node)
            {
                // 仅收集有实际值的标识符
                if (!string.IsNullOrEmpty(node.Value))
                    Identifiers.Add(node);
            }
        }
    }
}
