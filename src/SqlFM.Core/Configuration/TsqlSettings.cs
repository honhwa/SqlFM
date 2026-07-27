using System.Xml.Serialization;

namespace SqlFM.Core.Configuration
{
    /// <summary>
    /// T-SQL 专属格式化设置（分组8）：dbo 架构、临时表、表变量、全局变量等 SQL Server 特有规则。
    /// </summary>
    public class TsqlSettings
    {
        /// <summary>是否自动为无架构的对象名添加 dbo. 前缀，默认 false</summary>
        [XmlElement]
        public bool AutoAddDboSchema { get; set; } = false;

        /// <summary>是否自动移除显式的 dbo. 前缀，默认 false</summary>
        [XmlElement]
        public bool AutoRemoveDboSchema { get; set; } = false;

        /// <summary>临时表名称格式（是否统一前缀格式 #TableName），默认 false 表示保持</summary>
        [XmlElement]
        public bool TempTableFormat { get; set; } = false;

        /// <summary>表变量名称格式（是否统一前缀格式 @TableVar），默认 false 表示保持</summary>
        [XmlElement]
        public bool TableVariableFormat { get; set; } = false;

        /// <summary>全局变量（@@变量）是否大写，默认 true</summary>
        [XmlElement]
        public bool GlobalVariableFormat { get; set; } = true;

        /// <summary>
        /// DECLARE 多变量声明时，后续变量是否与首变量纵向对齐，默认 true。
        /// 示例：
        ///   DECLARE @a INT,
        ///           @b VARCHAR(10)
        /// </summary>
        [XmlElement]
        public bool AlignDeclareVariables { get; set; } = true;
    }
}
