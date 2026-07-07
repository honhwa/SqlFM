using System.Collections.Generic;
using SqlFM.Core.Engine;

namespace SqlFM.Core.Refactoring
{
    /// <summary>
    /// SELECT * 展开为完整字段列表。
    /// 依赖 <see cref="ScriptDomEngine.ExpandSelectStar"/> 实现基于 AST 的精确展开。
    /// 调用方需提供表名到列名的映射字典，才能得到具体的字段名称。
    /// </summary>
    public class StarExpander
    {
        private readonly ScriptDomEngine _scriptDom;

        /// <summary>
        /// 初始化 StarExpander
        /// </summary>
        /// <param name="scriptDom">ScriptDom 引擎实例</param>
        public StarExpander(ScriptDomEngine scriptDom)
        {
            _scriptDom = scriptDom;
        }

        /// <summary>
        /// 将 SQL 中的 <c>SELECT *</c>（或 <c>SELECT t.*</c>）展开为完整字段列表。
        /// <para>
        /// 示例（提供 tableColumns = { "Orders": ["Id","Amount","Status"] }）：
        /// <c>SELECT * FROM Orders</c> → <c>SELECT Id, Amount, Status FROM Orders</c>
        /// </para>
        /// </summary>
        /// <param name="sql">原始 SQL 文本</param>
        /// <param name="tableColumns">
        /// 表名（或别名）→ 列名列表 的映射字典，键比较不区分大小写。
        /// 若只有一张表且 SELECT * 无前缀，则自动使用该表的列列表。
        /// </param>
        /// <returns>展开后的 SQL；若无 SELECT * 或未匹配到映射则返回原文</returns>
        public string ExpandStar(string sql, IDictionary<string, IList<string>> tableColumns)
        {
            return _scriptDom.ExpandSelectStar(sql, tableColumns);
        }
    }
}
