using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using SqlFM.Core.Engine;

namespace SqlFM.Core.Batch
{
    /// <summary>
    /// 数据库元数据批量格式化处理器。
    /// 读取存储过程/视图/函数/触发器定义，格式化后通过 ALTER 语句覆盖保存。
    /// </summary>
    public class DbMetadataBatch
    {
        private readonly FormatterPipeline _pipeline;

        /// <summary>
        /// 初始化数据库元数据批量处理器
        /// </summary>
        /// <param name="pipeline">格式化管道实例</param>
        public DbMetadataBatch(FormatterPipeline pipeline)
        {
            _pipeline = pipeline;
        }

        /// <summary>
        /// 获取数据库中所有用户自定义可编程对象（存储过程、视图、标量函数、表值函数、触发器）
        /// </summary>
        /// <param name="connectionString">SQL Server 连接字符串</param>
        /// <returns>数据库对象列表</returns>
        public IList<DbObject> GetProgrammableObjects(string connectionString)
        {
            var objects = new List<DbObject>();
            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();
                // 查询存储过程、视图、标量函数、表值函数、触发器
                const string sql = @"
                    SELECT
                        o.object_id,
                        s.name  AS SchemaName,
                        o.name  AS ObjectName,
                        o.type_desc AS TypeDesc,
                        m.definition AS Definition
                    FROM sys.objects o
                    JOIN sys.schemas     s ON o.schema_id = s.schema_id
                    JOIN sys.sql_modules m ON o.object_id = m.object_id
                    WHERE o.is_ms_shipped = 0
                      AND o.type IN ('P','V','FN','IF','TF','TR')
                    ORDER BY o.type, s.name, o.name";

                using (var cmd = new SqlCommand(sql, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        objects.Add(new DbObject
                        {
                            ObjectId    = reader.GetInt32(0),
                            SchemaName  = reader.GetString(1),
                            ObjectName  = reader.GetString(2),
                            TypeDescription = reader.GetString(3),
                            Definition  = reader.IsDBNull(4) ? null : reader.GetString(4)
                        });
                    }
                }
            }
            return objects;
        }

        /// <summary>
        /// 批量格式化并通过 ALTER 语句将结果回写到数据库。
        /// 仅当格式化后内容与原始定义不同时才执行 ALTER。
        /// </summary>
        /// <param name="connectionString">SQL Server 连接字符串</param>
        /// <param name="objects">待处理的数据库对象列表</param>
        /// <param name="progress">进度回调（当前索引, 总数, 对象全名）</param>
        /// <returns>批量处理结果摘要</returns>
        public BatchResult FormatAndSave(
            string connectionString,
            IList<DbObject> objects,
            Action<int, int, string>? progress = null)
        {
            var result = new BatchResult { TotalFiles = objects.Count };

            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();
                for (int i = 0; i < objects.Count; i++)
                {
                    var obj = objects[i];
                    var fullName = $"{obj.SchemaName}.{obj.ObjectName}";
                    progress?.Invoke(i + 1, objects.Count, fullName);

                    if (string.IsNullOrEmpty(obj.Definition))
                    {
                        result.FailedFiles.Add(new BatchFailure
                        {
                            FilePath = fullName,
                            Error = "Definition is null (encrypted or inaccessible)"
                        });
                        continue;
                    }

                    try
                    {
                        var formatResult = _pipeline.Format(obj.Definition!);
                        if (!formatResult.Success)
                        {
                            result.FailedFiles.Add(new BatchFailure
                            {
                                FilePath = fullName,
                                Error = formatResult.ErrorMessage ?? "Format failed"
                            });
                            continue;
                        }

                        if (formatResult.FormattedSql != obj.Definition)
                        {
                            // 将 CREATE 替换为 ALTER 后执行
                            var alterSql = ConvertCreateToAlter(formatResult.FormattedSql);
                            using (var cmd = new SqlCommand(alterSql, conn))
                            {
                                cmd.ExecuteNonQuery();
                            }
                            result.ModifiedFiles++;
                        }
                        result.SuccessFiles++;
                    }
                    catch (Exception ex)
                    {
                        result.FailedFiles.Add(new BatchFailure
                        {
                            FilePath = fullName,
                            Error = ex.Message
                        });
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// 将 SQL 定义中第一个 CREATE 关键字替换为 ALTER。
        /// 支持 CREATE PROCEDURE / VIEW / FUNCTION / TRIGGER，不区分大小写。
        /// </summary>
        /// <param name="sql">包含 CREATE 的 SQL 定义</param>
        /// <returns>替换后的 ALTER SQL</returns>
        private static string ConvertCreateToAlter(string sql)
        {
            // 仅替换语句开头处的 CREATE，避免误改 CREATE TABLE 等内嵌语句
            return System.Text.RegularExpressions.Regex.Replace(
                sql,
                @"^\s*CREATE\s+",
                "ALTER ",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase
                | System.Text.RegularExpressions.RegexOptions.Multiline);
        }
    }

    /// <summary>
    /// 数据库可编程对象信息
    /// </summary>
    public class DbObject
    {
        /// <summary>sys.objects.object_id</summary>
        public int ObjectId { get; set; }

        /// <summary>架构名称（如 dbo）</summary>
        public string SchemaName { get; set; } = string.Empty;

        /// <summary>对象名称</summary>
        public string ObjectName { get; set; } = string.Empty;

        /// <summary>对象类型描述（如 SQL_STORED_PROCEDURE）</summary>
        public string TypeDescription { get; set; } = string.Empty;

        /// <summary>对象定义 SQL（加密对象为 null）</summary>
        public string? Definition { get; set; }
    }
}
