using System.Xml.Serialization;

namespace SqlFM.Core.Configuration
{
    /// <summary>
    /// DDL 语句格式化设置（分组6）：CREATE TABLE、存储过程、函数、触发器等结构。
    /// </summary>
    public class DdlSettings
    {
        /// <summary>CREATE TABLE 的列定义是否每列占一行，默认 true</summary>
        [XmlElement]
        public bool CreateTableColumnWrap { get; set; } = true;

        /// <summary>约束定义是否纵向对齐，默认 false</summary>
        [XmlElement]
        public bool ConstraintAlign { get; set; } = false;

        /// <summary>存储过程参数是否每行一个，默认 true</summary>
        [XmlElement]
        public bool ProcParamWrap { get; set; } = true;

        /// <summary>函数返回类型的格式化方式（是否换行展开），默认 false</summary>
        [XmlElement]
        public bool FunctionReturnFormat { get; set; } = false;

        /// <summary>触发器中 INSERTED/DELETED 表引用是否缩进对齐，默认 false</summary>
        [XmlElement]
        public bool TriggerInsertedIndent { get; set; } = false;
    }
}
