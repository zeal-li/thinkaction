using System;
using System.IO;

namespace ToolSet.Infrastructure.Configuration
{
    /// <summary>
    /// 全局默认配置实例，配置文件位于 exe 同级的 config/config.json。
    /// 各功能模块通过 <see cref="Current"/> 读写自己的配置项。
    /// </summary>
    public static class AppConfig
    {
        public static IConfigService Current { get; } = new JsonConfigService(
            Path.Combine(AppContext.BaseDirectory, "config", "config.json"));
    }
}
