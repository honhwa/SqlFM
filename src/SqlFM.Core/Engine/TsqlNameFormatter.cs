using System;
using System.Text;
using System.Text.RegularExpressions;

namespace SqlFM.Core.Engine
{
    /// <summary>
    /// T-SQL 标识符名称格式化器：统一临时表名（#TableName）和表变量名（@TableVar）的命名风格。
    /// 仅在字符串字面量之外执行替换，不影响字符串内容和注释。
    /// </summary>
    public class TsqlNameFormatter
    {
        /// <summary>
        /// 格式化临时表名称：将 # 后的标识符首字母大写。
        /// 示例：#temp → #Temp，##globaltemp → ##Globaltemp
        /// </summary>
        /// <param name="sql">待处理的 SQL 文本</param>
        /// <returns>临时表名格式化后的 SQL 文本</returns>
        public string FormatTempTableNames(string sql)
        {
            if (string.IsNullOrEmpty(sql))
                return sql;

            return TransformOutsideStrings(sql, text =>
            {
                // 匹配 ## 或 # 后跟标识符（字母/下划线开头，后跟字母数字下划线）
                // 将标识符首字母大写，其余保持不变
                return Regex.Replace(text, @"(#{1,2})([a-zA-Z_]\w*)",
                    m => m.Groups[1].Value + CapitalizeFirst(m.Groups[2].Value));
            });
        }

        /// <summary>
        /// 格式化表变量名称：将 @ 后的标识符首字母大写（排除 @@ 全局变量）。
        /// 示例：@tablevar → @Tablevar，@myvar → @Myvar
        /// </summary>
        /// <param name="sql">待处理的 SQL 文本</param>
        /// <returns>表变量名格式化后的 SQL 文本</returns>
        public string FormatTableVariableNames(string sql)
        {
            if (string.IsNullOrEmpty(sql))
                return sql;

            return TransformOutsideStrings(sql, text =>
            {
                // 匹配单 @（非 @@）后跟标识符
                // 使用负向先行断言排除 @@ 全局变量
                return Regex.Replace(text, @"(?<!@)@([a-zA-Z_]\w*)",
                    m => "@" + CapitalizeFirst(m.Groups[1].Value));
            });
        }

        /// <summary>
        /// 将标识符首字母大写，其余字符保持不变。
        /// </summary>
        /// <param name="name">待处理的标识符</param>
        /// <returns>首字母大写的标识符</returns>
        private static string CapitalizeFirst(string name)
        {
            if (string.IsNullOrEmpty(name))
                return name;
            return char.ToUpperInvariant(name[0]) + name.Substring(1);
        }

        /// <summary>
        /// 在字符串字面量之外对文本执行转换。
        /// 遍历文本，将单引号字符串外的部分传入转换函数，字符串内原样保留。
        /// </summary>
        /// <param name="text">待处理的文本</param>
        /// <param name="transform">非字符串部分的转换函数</param>
        /// <returns>转换后的文本</returns>
        private static string TransformOutsideStrings(string text, Func<string, string> transform)
        {
            var result = new StringBuilder(text.Length);
            int i = 0;

            while (i < text.Length)
            {
                if (text[i] == '\'')
                {
                    // 复制整个字符串字面量（处理 '' 转义）
                    int start = i;
                    i++;
                    while (i < text.Length)
                    {
                        if (text[i] == '\'')
                        {
                            if (i + 1 < text.Length && text[i + 1] == '\'')
                            {
                                i += 2;
                                continue;
                            }
                            i++;
                            break;
                        }
                        i++;
                    }
                    result.Append(text.Substring(start, i - start));
                }
                else
                {
                    // 复制非字符串部分直到下一个单引号
                    int start = i;
                    while (i < text.Length && text[i] != '\'')
                        i++;
                    result.Append(transform(text.Substring(start, i - start)));
                }
            }

            return result.ToString();
        }
    }
}
