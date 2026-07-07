using System.Collections.Generic;
using System.Xml.Serialization;

namespace SqlFM.Core.Configuration
{
    /// <summary>
    /// SQL 格式化完整样式定义：聚合所有 8 个分组设置，支持 XML 序列化/反序列化。
    /// 文件扩展名建议使用 .sqlstyle。
    /// </summary>
    [XmlRoot("SqlFormatStyle")]
    public class SqlFormatStyle
    {
        /// <summary>样式名称，默认 "Default"</summary>
        [XmlAttribute]
        public string Name { get; set; } = "Default";

        /// <summary>是否为默认应用样式</summary>
        [XmlAttribute]
        public bool IsDefault { get; set; } = false;

        /// <summary>是否为系统内置预设（不可删除）</summary>
        [XmlAttribute]
        public bool IsSystemPreset { get; set; } = false;

        /// <summary>全局设置（分组1）</summary>
        [XmlElement("Global")]
        public GlobalSettings Global { get; set; } = new GlobalSettings();

        /// <summary>DML 设置（分组2）</summary>
        [XmlElement("Dml")]
        public DmlSettings Dml { get; set; } = new DmlSettings();

        /// <summary>CTE 设置（分组3）</summary>
        [XmlElement("Cte")]
        public CteSettings Cte { get; set; } = new CteSettings();

        /// <summary>CASE 表达式设置（分组4）</summary>
        [XmlElement("Case")]
        public CaseSettings Case { get; set; } = new CaseSettings();

        /// <summary>流程控制设置（分组5）</summary>
        [XmlElement("Flow")]
        public FlowSettings Flow { get; set; } = new FlowSettings();

        /// <summary>DDL 设置（分组6）</summary>
        [XmlElement("Ddl")]
        public DdlSettings Ddl { get; set; } = new DdlSettings();

        /// <summary>表达式设置（分组7）</summary>
        [XmlElement("Expression")]
        public ExpressionSettings Expression { get; set; } = new ExpressionSettings();

        /// <summary>T-SQL 专属设置（分组8）</summary>
        [XmlElement("Tsql")]
        public TsqlSettings Tsql { get; set; } = new TsqlSettings();

        /// <summary>忽略规则配置（正则表达式列表，匹配的代码段跳过格式化）</summary>
        [XmlElement("IgnoreConfig")]
        public IgnoreConfig IgnoreConfig { get; set; } = new IgnoreConfig();

        /// <summary>
        /// 创建当前样式的深拷贝（通过 XML 序列化/反序列化实现）。
        /// </summary>
        /// <returns>与当前实例内容完全相同的新实例</returns>
        public SqlFormatStyle Clone()
        {
            var xml = StyleSerializer.SerializeToString(this);
            return StyleSerializer.DeserializeFromString(xml);
        }
    }

    /// <summary>
    /// 格式化忽略配置：使用正则表达式标记需要跳过格式化的代码段。
    /// </summary>
    public class IgnoreConfig
    {
        /// <summary>正则忽略规则列表，匹配的内容原样输出，不做格式化处理</summary>
        [XmlArray("RegexIgnoreRules")]
        [XmlArrayItem("Rule")]
        public List<string> RegexIgnoreRules { get; set; } = new List<string>();
    }
}
