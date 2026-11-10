using ToolSet.Infrastructure.Configuration;
using ToolSet.Infrastructure.Scripts;

namespace ToolSet.Infrastructure
{
    /// <summary>
    /// 主程序提供给各模块的宿主能力。
    /// </summary>
    public interface IShell
    {
        IConfigService Config { get; }

        /// <summary>通用脚本执行器，用于调用 bat 脚本。</summary>
        IScriptExecutor Scripts { get; }
    }
}
