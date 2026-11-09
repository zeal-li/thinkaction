using System.Collections.Generic;

namespace ToolSet.Infrastructure.Configuration
{
    /// <summary>
    /// 通用分层配置读写接口。
    /// 配置以「节点路径」定位，路径用 / 分隔，例如 <c>辅助工具/导表/输出目录</c>。
    /// 节点有两种形态：配置值（字符串）和配置分组（可无限嵌套的子节点集合）。
    /// </summary>
    public interface IConfigService
    {
        /// <summary>配置文件绝对路径。</summary>
        string FilePath { get; }

        /// <summary>写入后是否立即落盘，默认 true。</summary>
        bool AutoSave { get; set; }

        /// <summary>节点是否存在（值或分组都算）。</summary>
        bool Contains(string path);

        /// <summary>读取配置值，节点不存在或本身是分组时返回 null。</summary>
        string? Find(string path);

        /// <summary>读取配置值，节点不存在或本身是分组时抛出异常。</summary>
        string Get(string path);

        int GetInt(string path);

        bool GetBool(string path);

        double GetDouble(string path);

        /// <summary>列出分组的直接子节点名（含子分组）。sectionPath 传空串表示根节点。</summary>
        IReadOnlyList<string> GetKeys(string sectionPath = "");

        /// <summary>读取分组下直接子节点的配置值（子分组会被跳过）。sectionPath 传空串表示根节点。</summary>
        IReadOnlyDictionary<string, string?> GetValues(string sectionPath = "");

        /// <summary>写入配置值，路径上的分组不存在时自动创建。值不能为 null（空字符串允许）。</summary>
        void Set(string path, string value);

        void Set(string path, int value);

        void Set(string path, bool value);

        void Set(string path, double value);

        /// <summary>批量写入一个分组下的多个配置值。值不能为 null（空字符串允许）。</summary>
        void SetValues(string sectionPath, IReadOnlyDictionary<string, string> values);

        /// <summary>删除一个配置值或整个配置分组。</summary>
        void Remove(string path);

        /// <summary>清空全部配置。</summary>
        void Clear();

        /// <summary>把内存中的配置写入文件。</summary>
        void Save();

        /// <summary>丢弃内存中的修改，重新从文件加载。</summary>
        void Reload();
    }
}
