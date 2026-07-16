using System;
using System.Collections.Generic;
using SqlFM.Core.Dialects;

namespace SqlFM.Core.Completion.Snippets
{
    /// <summary>
    /// SQL 代码片段补全提供器，基于预定义的常用 SQL 模板生成补全建议。
    /// 借鉴 VS Code SQL 扩展的 Snippet 机制，提供 SELECT/INSERT/UPDATE/DELETE 等常用模板。
    /// </summary>
    public class SnippetCompletionProvider : ICompletionProvider
    {
        /// <inheritdoc/>
        public string Name => "snippets";

        /// <inheritdoc/>
        public string[] SupportedDialects => Array.Empty<string>(); // 所有方言

        /// <inheritdoc/>
        public List<CompletionItem> Provide(CompletionContext context)
        {
            return GetCommonSnippets(context.Dialect);
        }

        /// <summary>获取通用 SQL Snippets（适用于 ANSI 和所有方言）</summary>
        private static List<CompletionItem> GetCommonSnippets(SqlDialect dialect)
        {
            var items = new List<CompletionItem>();

            // SELECT 基础模板
            items.Add(CompletionItem.Snippet("sel",
                "SELECT ${1:columns}\nFROM ${2:table}\nWHERE ${3:condition};",
                "SELECT 查询模板"));

            // SELECT DISTINCT
            items.Add(CompletionItem.Snippet("sel-dist",
                "SELECT DISTINCT ${1:columns}\nFROM ${2:table}\nWHERE ${3:condition};",
                "SELECT DISTINCT 查询模板"));

            // SELECT JOIN
            items.Add(CompletionItem.Snippet("sel-join",
                "SELECT ${1:columns}\nFROM ${2:table_a}\nINNER JOIN ${3:table_b} ON ${4:table_a.id} = ${5:table_b.id}\nWHERE ${6:condition};",
                "SELECT JOIN 查询模板"));

            // INSERT
            items.Add(CompletionItem.Snippet("ins",
                "INSERT INTO ${1:table} (${2:columns})\nVALUES (${3:values});",
                "INSERT 插入模板"));

            // UPDATE
            items.Add(CompletionItem.Snippet("upd",
                "UPDATE ${1:table}\nSET ${2:column} = ${3:value}\nWHERE ${4:condition};",
                "UPDATE 更新模板"));

            // DELETE
            items.Add(CompletionItem.Snippet("del",
                "DELETE FROM ${1:table}\nWHERE ${2:condition};",
                "DELETE 删除模板"));

            // CTE
            items.Add(CompletionItem.Snippet("cte",
                "WITH ${1:cte_name} AS (\n    ${2:select_query}\n)\nSELECT ${3:columns}\nFROM ${4:cte_name};",
                "CTE 公用表表达式模板"));

            // CASE WHEN
            items.Add(CompletionItem.Snippet("case",
                "CASE\n    WHEN ${1:condition1} THEN ${2:result1}\n    WHEN ${3:condition2} THEN ${4:result2}\n    ELSE ${5:default_result}\nEND",
                "CASE WHEN 条件表达式模板"));

            // IF ELSE（T-SQL 方言专属）
            if (dialect.Name == "tsql")
            {
                items.Add(CompletionItem.Snippet("if",
                    "IF ${1:condition}\nBEGIN\n    ${2:statement}\nEND\nELSE\nBEGIN\n    ${3:statement}\nEND",
                    "IF...ELSE T-SQL 流程控制模板"));

                items.Add(CompletionItem.Snippet("while",
                    "WHILE ${1:condition}\nBEGIN\n    ${2:statement}\nEND",
                    "WHILE T-SQL 循环模板"));

                items.Add(CompletionItem.Snippet("try",
                    "BEGIN TRY\n    ${1:statement}\nEND TRY\nBEGIN CATCH\n    ${2:error_handling}\nEND CATCH",
                    "TRY...CATCH T-SQL 异常处理模板"));

                items.Add(CompletionItem.Snippet("sp",
                    "CREATE PROCEDURE ${1:procedure_name}\n    ${2:@param} ${3:type}\nAS\nBEGIN\n    ${4:statement}\nEND",
                    "CREATE PROCEDURE T-SQL 存储过程模板"));

                items.Add(CompletionItem.Snippet("fn",
                    "CREATE FUNCTION ${1:function_name} (${2:@param} ${3:type})\nRETURNS ${4:return_type}\nAS\nBEGIN\n    RETURN ${5:expression}\nEND",
                    "CREATE FUNCTION T-SQL 函数模板"));
            }

            // CREATE TABLE
            items.Add(CompletionItem.Snippet("ct",
                "CREATE TABLE ${1:table_name} (\n    ${2:id} INT PRIMARY KEY,\n    ${3:column} ${4:type}\n);",
                "CREATE TABLE 建表模板"));

            // CREATE INDEX
            items.Add(CompletionItem.Snippet("ci",
                "CREATE INDEX ${1:index_name} ON ${2:table_name} (${3:column});",
                "CREATE INDEX 索引模板"));

            // SUBQUERY
            items.Add(CompletionItem.Snippet("sub",
                "SELECT ${1:columns}\nFROM ${2:table}\nWHERE ${3:column} IN (\n    SELECT ${4:column}\n    FROM ${5:table}\n    WHERE ${6:condition}\n);",
                "子查询模板"));

            return items;
        }
    }
}
