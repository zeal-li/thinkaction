using ToolSet.Infrastructure.Configuration;

namespace ToolSet.Infrastructure
{
    /// <summary>
    /// 主程序提供给各模块的宿主能力。
    /// </summary>
    public interface IShell
    {
        IConfigService Config { get; }
    }
}
