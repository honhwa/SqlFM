using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Text;

namespace SqlFM.Core.Refactoring
{
    /// <summary>
    /// 表-列元数据提供器：为 SELECT * 展开（<see cref="StarExpander"/>）与代码补全提供表结构来源。
    /// 支持两种来源：
    /// 1) 数据库：查询 INFORMATION_SCHEMA.COLUMNS（需 SQL Server 连接）；
    /// 2) JSON 文件：{ "TableName": ["col1","col2"] } 形式的轻量映射（无外部依赖）。
    /// 解决 SELECT * 展开「需元数据」的限制，使其可离线（JSON）或在线（DB）执行。
    /// </summary>
    public static class MetadataProvider
    {
        /// <summary>
        /// 从 SQL Server 数据库读取所有用户基表的列结构。
        /// </summary>
        /// <param name="connectionString">SQL Server 连接字符串</param>
        /// <returns>表名 → 列名列表 的映射（键使用 OrdinalIgnoreCase 比较）</returns>
        public static Dictionary<string, IList<string>> FromDatabase(string connectionString)
        {
            var map = new Dictionary<string, IList<string>>(StringComparer.OrdinalIgnoreCase);

            const string sql = @"
                SELECT TABLE_SCHEMA, TABLE_NAME, COLUMN_NAME
                FROM INFORMATION_SCHEMA.COLUMNS
                WHERE TABLE_TYPE = 'BASE TABLE'
                ORDER BY TABLE_SCHEMA, TABLE_NAME, ORDINAL_POSITION";

            using (var conn = new SqlConnection(connectionString))
            using (var cmd = new SqlCommand(sql, conn))
            {
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string table = reader.GetString(1);
                        string column = reader.GetString(2);

                        if (!map.TryGetValue(table, out var cols))
                        {
                            cols = new List<string>();
                            map[table] = cols;
                        }
                        cols.Add(column);
                    }
                }
            }

            return map;
        }

        /// <summary>
        /// 从 JSON 文件加载表-列映射。格式：{ "TableName": ["col1","col2"], ... }。
        /// 使用内置极简解析（仅依赖 BCL），不引入第三方 JSON 库，避免给 Core 增加新依赖。
        /// </summary>
        /// <param name="filePath">JSON 映射文件路径</param>
        /// <returns>表名 → 列名列表 的映射</returns>
        public static Dictionary<string, IList<string>> FromJson(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException("元数据文件未找到: " + filePath, filePath);

            string text = File.ReadAllText(filePath, Encoding.UTF8);
            return ParseJsonMap(text);
        }

        /// <summary>
        /// 解析 { "k": ["v", ...], ... } 形态的 JSON 映射（内部使用，便于单元测试）。
        /// </summary>
        internal static Dictionary<string, IList<string>> ParseJsonMap(string text)
        {
            var map = new Dictionary<string, IList<string>>(StringComparer.OrdinalIgnoreCase);
            int i = 0;
            SkipWs(text, ref i);
            Expect(text, ref i, '{');
            SkipWs(text, ref i);

            if (Peek(text, i) == '}')
            {
                i++;
                return map;
            }

            while (i < text.Length)
            {
                SkipWs(text, ref i);
                string key = ReadString(text, ref i);
                SkipWs(text, ref i);
                Expect(text, ref i, ':');
                SkipWs(text, ref i);
                var values = ReadStringArray(text, ref i);
                map[key] = values;
                SkipWs(text, ref i);

                char c = Peek(text, i);
                if (c == ',')
                {
                    i++;
                    continue;
                }
                if (c == '}')
                {
                    i++;
                    break;
                }
                break;
            }

            return map;
        }

        // ── 极简 JSON 词法辅助（仅支持字符串与字符串数组）──────

        private static void SkipWs(string s, ref int i)
        {
            while (i < s.Length && char.IsWhiteSpace(s[i]))
                i++;
        }

        private static char Peek(string s, int i)
        {
            return i < s.Length ? s[i] : '\0';
        }

        private static void Expect(string s, ref int i, char expected)
        {
            SkipWs(s, ref i);
            if (Peek(s, i) != expected)
                throw new FormatException($"元数据 JSON 格式错误：期望 '{expected}'，实际位置 {i} 为 '{(i < s.Length ? s[i].ToString() : "EOF")}'");
            i++;
        }

        private static string ReadString(string s, ref int i)
        {
            SkipWs(s, ref i);
            if (Peek(s, i) != '"')
                throw new FormatException("元数据 JSON 格式错误：期望字符串开始引号");
            i++; // 跳过 "
            var sb = new StringBuilder();
            while (i < s.Length)
            {
                char c = s[i++];
                if (c == '"')
                    break;
                if (c == '\\' && i < s.Length)
                {
                    char esc = s[i++];
                    sb.Append(esc == '"' ? '"' : esc == '\\' ? '\\' : esc);
                }
                else
                {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }

        private static IList<string> ReadStringArray(string s, ref int i)
        {
            Expect(s, ref i, '[');
            var list = new List<string>();
            SkipWs(s, ref i);
            if (Peek(s, i) == ']')
            {
                i++;
                return list;
            }
            while (i < s.Length)
            {
                list.Add(ReadString(s, ref i));
                SkipWs(s, ref i);
                char c = Peek(s, i);
                if (c == ',')
                {
                    i++;
                    continue;
                }
                if (c == ']')
                {
                    i++;
                    break;
                }
                break;
            }
            return list;
        }
    }
}
