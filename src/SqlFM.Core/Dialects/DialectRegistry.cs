using System;
using System.Collections.Generic;

namespace SqlFM.Core.Dialects
{
    /// <summary>
    /// 方言注册表，借鉴 sqlfluff 的 dialect_selector / dialect_readout 设计。
    /// 管理所有可用方言的注册、查找和列举。方言通过名称查找，支持懒加载。
    /// </summary>
    public class DialectRegistry
    {
        /// <summary>已注册的方言实例映射</summary>
        private readonly Dictionary<string, SqlDialect> _dialects = new Dictionary<string, SqlDialect>();

        /// <summary>已注册方言总数</summary>
        public int Count => _dialects.Count;

        /// <summary>注册方言实例</summary>
        public void Register(SqlDialect dialect)
        {
            if (_dialects.ContainsKey(dialect.Name))
                throw new InvalidOperationException($"方言 '{dialect.Name}' 已注册，不可重复注册。");

            _dialects[dialect.Name] = dialect;
        }

        /// <summary>通过名称查找方言</summary>
        public SqlDialect? GetByName(string name)
        {
            return _dialects.TryGetValue(name.ToLowerInvariant(), out var dialect) ? dialect : null;
        }

        /// <summary>获取所有已注册方言的名称列表</summary>
        public List<string> GetAllNames()
        {
            return new List<string>(_dialects.Keys);
        }

        /// <summary>获取所有已注册方言的信息列表</summary>
        public List<DialectInfo> GetAllInfo()
        {
            var result = new List<DialectInfo>();
            foreach (var dialect in _dialects.Values)
            {
                result.Add(new DialectInfo
                {
                    Name = dialect.Name,
                    FormattedName = dialect.FormattedName,
                    InheritsFrom = dialect.InheritsFrom ?? "nothing",
                    Docstring = dialect.Docstring
                });
            }
            return result;
        }

        /// <summary>获取默认方言</summary>
        public SqlDialect GetDefault()
        {
            return _dialects.TryGetValue("ansi", out var dialect) ? dialect : AnsiDialect.Instance;
        }

        /// <summary>创建并注册内置方言</summary>
        public static DialectRegistry CreateWithBuiltInDialects()
        {
            var registry = new DialectRegistry();
            registry.Register(AnsiDialect.Instance);
            registry.Register(TsqlDialect.Instance);
            return registry;
        }
    }

    /// <summary>
    /// 方言信息摘要，用于列举可用方言时展示。
    /// 借鉴 sqlfluff 的 DialectTuple 结构。
    /// </summary>
    public class DialectInfo
    {
        /// <summary>方言名称</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>格式化显示名称</summary>
        public string FormattedName { get; set; } = string.Empty;

        /// <summary>继承来源</summary>
        public string InheritsFrom { get; set; } = string.Empty;

        /// <summary>方言描述</summary>
        public string Docstring { get; set; } = string.Empty;
    }
}
