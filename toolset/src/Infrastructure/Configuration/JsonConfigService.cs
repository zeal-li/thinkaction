using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace ToolSet.Infrastructure.Configuration
{
    /// <summary>
    /// 基于 JSON 文件的通用分层配置存储，无第三方依赖。
    /// 分组与值共用一棵树，分组可无限嵌套，文件形如：
    /// <code>
    /// {
    ///   "配置功能项目1": {
    ///     "配置1": "配置数据",
    ///     "子分组": { "配置2": "配置数据" }
    ///   }
    /// }
    /// </code>
    /// 使用方式：
    /// <code>
    /// var config = AppConfig.Current;
    /// config.Set("辅助工具/导表/输出目录", @"D:\out");
    /// var dir = config.Get("辅助工具/导表/输出目录");
    /// var keys = config.GetKeys("辅助工具/导表");
    /// </code>
    /// 注意：节点名里不支持 /。
    /// </summary>
    public sealed class JsonConfigService : IConfigService
    {
        private const char Separator = '/';

        private static readonly JsonDocumentOptions ReadOptions = new JsonDocumentOptions
        {
            CommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
        };

        private readonly object _sync = new object();
        private Dictionary<string, object?> _root = new Dictionary<string, object?>(StringComparer.Ordinal);

        public JsonConfigService(string filePath, bool autoSave = true)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException("配置文件路径不能为空。", nameof(filePath));
            }

            FilePath = Path.GetFullPath(filePath);
            AutoSave = autoSave;
            Reload();
        }

        public string FilePath { get; }

        public bool AutoSave { get; set; }

        public bool Contains(string path)
        {
            lock (_sync)
            {
                return TryGetRaw(path, out _);
            }
        }

        public string? Find(string path)
        {
            lock (_sync)
            {
                return TryGetRaw(path, out var raw) ? raw as string : null;
            }
        }

        public string Get(string path)
        {
            lock (_sync)
            {
                if (!TryGetRaw(path, out var raw))
                {
                    throw new KeyNotFoundException($"配置项不存在：{path}");
                }

                if (raw is not string text)
                {
                    throw new InvalidOperationException($"路径 {path} 是配置分组，不是配置值。");
                }

                return text;
            }
        }

        public int GetInt(string path) => int.Parse(Get(path), CultureInfo.InvariantCulture);

        public bool GetBool(string path) => bool.Parse(Get(path));

        public double GetDouble(string path) => double.Parse(Get(path), CultureInfo.InvariantCulture);

        public IReadOnlyList<string> GetKeys(string sectionPath = "")
        {
            lock (_sync)
            {
                return new List<string>(ResolveSection(sectionPath).Keys);
            }
        }

        public IReadOnlyDictionary<string, string?> GetValues(string sectionPath = "")
        {
            lock (_sync)
            {
                var values = new Dictionary<string, string?>(StringComparer.Ordinal);
                foreach (var pair in ResolveSection(sectionPath))
                {
                    if (pair.Value is not Dictionary<string, object?>)
                    {
                        values[pair.Key] = pair.Value as string;
                    }
                }

                return values;
            }
        }

        public void Set(string path, string value)
        {
            SetCore(path, value);
            if (AutoSave)
            {
                Save();
            }
        }

        public void Set(string path, int value) => Set(path, value.ToString(CultureInfo.InvariantCulture));

        public void Set(string path, bool value) => Set(path, value ? "true" : "false");

        public void Set(string path, double value) => Set(path, value.ToString("R", CultureInfo.InvariantCulture));

        public void SetValues(string sectionPath, IReadOnlyDictionary<string, string> values)
        {
            foreach (var pair in values)
            {
                SetCore(Combine(sectionPath, pair.Key), pair.Value);
            }

            if (AutoSave)
            {
                Save();
            }
        }

        public void Remove(string path)
        {
            lock (_sync)
            {
                var segments = SplitPath(path);
                var node = _root;
                for (var i = 0; i < segments.Length - 1; i++)
                {
                    if (!node.TryGetValue(segments[i], out var child) || child is not Dictionary<string, object?> childNode)
                    {
                        throw new KeyNotFoundException($"配置节点不存在：{path}");
                    }

                    node = childNode;
                }

                if (!node.Remove(segments[^1]))
                {
                    throw new KeyNotFoundException($"配置节点不存在：{path}");
                }
            }

            if (AutoSave)
            {
                Save();
            }
        }

        public void Clear()
        {
            lock (_sync)
            {
                _root.Clear();
            }

            if (AutoSave)
            {
                Save();
            }
        }

        public void Save()
        {
            lock (_sync)
            {
                var directory = Path.GetDirectoryName(FilePath);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // 先写临时文件再替换，避免写到一半留下坏文件
                var tempPath = FilePath + ".tmp";
                using (var stream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions
                    {
                        Indented = true,
                        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                    });
                    WriteNode(writer, _root);
                }

                File.Move(tempPath, FilePath, overwrite: true);
            }
        }

        public void Reload()
        {
            lock (_sync)
            {
                _root = File.Exists(FilePath)
                    ? ReadRoot(FilePath)
                    : new Dictionary<string, object?>(StringComparer.Ordinal);
            }
        }

        private static Dictionary<string, object?> ReadRoot(string filePath)
        {
            using var document = JsonDocument.Parse(File.ReadAllText(filePath), ReadOptions);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                throw new InvalidDataException($"配置文件根节点必须是 JSON 对象：{filePath}");
            }

            return (Dictionary<string, object?>)ToNode(document.RootElement)!;
        }

        private static object? ToNode(JsonElement element)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                    var node = new Dictionary<string, object?>(StringComparer.Ordinal);
                    foreach (var property in element.EnumerateObject())
                    {
                        node[property.Name] = ToNode(property.Value);
                    }

                    return node;

                case JsonValueKind.String:
                    return element.GetString();

                case JsonValueKind.Number:
                    return element.GetRawText();

                case JsonValueKind.True:
                    return "true";

                case JsonValueKind.False:
                    return "false";

                case JsonValueKind.Null:
                    return null;

                default:
                    throw new InvalidDataException("配置值只支持分组、字符串、数字、布尔和 null。");
            }
        }

        private static void WriteNode(Utf8JsonWriter writer, Dictionary<string, object?> node)
        {
            writer.WriteStartObject();
            foreach (var pair in node)
            {
                writer.WritePropertyName(pair.Key);
                switch (pair.Value)
                {
                    case Dictionary<string, object?> child:
                        WriteNode(writer, child);
                        break;

                    case null:
                        writer.WriteNullValue();
                        break;

                    case string text:
                        writer.WriteStringValue(text);
                        break;

                    default:
                        throw new InvalidOperationException($"不支持的配置值类型：{pair.Value.GetType()}");
                }
            }

            writer.WriteEndObject();
        }

        private static string[] SplitPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("配置路径不能为空。", nameof(path));
            }

            var segments = path.Split(Separator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (segments.Length == 0)
            {
                throw new ArgumentException("配置路径不能为空。", nameof(path));
            }

            return segments;
        }

        private static string Combine(string sectionPath, string key)
            => string.IsNullOrWhiteSpace(sectionPath)
                ? key
                : $"{sectionPath.Trim().TrimEnd(Separator)}{Separator}{key}";

        /// <summary>定位配置值；节点不存在时返回 false，节点是分组时 true 且 raw 为分组对象。</summary>
        private bool TryGetRaw(string path, out object? raw)
        {
            var segments = SplitPath(path);
            var node = _root;
            for (var i = 0; i < segments.Length - 1; i++)
            {
                if (!node.TryGetValue(segments[i], out var child) || child is not Dictionary<string, object?> childNode)
                {
                    raw = null;
                    return false;
                }

                node = childNode;
            }

            return node.TryGetValue(segments[^1], out raw);
        }

        private Dictionary<string, object?> ResolveSection(string sectionPath)
        {
            if (string.IsNullOrWhiteSpace(sectionPath))
            {
                return _root;
            }

            var node = _root;
            foreach (var segment in SplitPath(sectionPath))
            {
                if (!node.TryGetValue(segment, out var child))
                {
                    throw new KeyNotFoundException($"配置分组不存在：{sectionPath}");
                }

                if (child is not Dictionary<string, object?> childNode)
                {
                    throw new InvalidOperationException($"路径 {sectionPath} 不是配置分组。");
                }

                node = childNode;
            }

            return node;
        }

        private void SetCore(string path, string value)
        {
            if (value is null)
            {
                throw new ArgumentNullException(nameof(value), $"配置值不能为 null：{path}");
            }

            lock (_sync)
            {
                var segments = SplitPath(path);
                var node = _root;
                for (var i = 0; i < segments.Length - 1; i++)
                {
                    if (node.TryGetValue(segments[i], out var child))
                    {
                        if (child is not Dictionary<string, object?> childNode)
                        {
                            var parentPath = string.Join(Separator, segments, 0, i + 1);
                            throw new InvalidOperationException($"配置项 {parentPath} 已有值，不能再作为配置分组。");
                        }

                        node = childNode;
                    }
                    else
                    {
                        var created = new Dictionary<string, object?>(StringComparer.Ordinal);
                        node[segments[i]] = created;
                        node = created;
                    }
                }

                node[segments[^1]] = value;
            }
        }
    }
}
