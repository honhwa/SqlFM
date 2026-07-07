using System.Xml.Serialization;

namespace SqlFM.Core.Configuration
{
    /// <summary>
    /// 流程控制格式化设置（分组5）：IF/BEGIN/END、TRY/CATCH、DECLARE、EXEC 等结构。
    /// </summary>
    public class FlowSettings
    {
        /// <summary>IF 语句的 BEGIN 是否与 IF 同行，默认 true</summary>
        [XmlElement]
        public bool IfBeginSameLine { get; set; } = true;

        /// <summary>END 是否与对应 IF/BEGIN 的缩进对齐，默认 true</summary>
        [XmlElement]
        public bool EndMatchIfIndent { get; set; } = true;

        /// <summary>IF/ELSE 块之间是否插入空行，默认 false</summary>
        [XmlElement]
        public bool IfElseBlankSplit { get; set; } = false;

        /// <summary>TRY/CATCH 块内语句是否缩进，默认 true</summary>
        [XmlElement]
        public bool TryCatchIndentBlock { get; set; } = true;

        /// <summary>多个 DECLARE 语句是否各占一行（禁止合并），默认 true</summary>
        [XmlElement]
        public bool DeclareVariableEachLine { get; set; } = true;

        /// <summary>EXEC 参数过多时是否换行，默认 true</summary>
        [XmlElement]
        public bool ExecParamWrap { get; set; } = true;
    }
}
