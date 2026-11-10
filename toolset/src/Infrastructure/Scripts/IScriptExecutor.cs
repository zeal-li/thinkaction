using System;
using System.Threading.Tasks;

namespace ToolSet.Infrastructure.Scripts
{
    /// <summary>
    /// 通用脚本执行接口：通过 cmd.exe 调用 bat/cmd，并回收输出与退出码。
    /// 不做兜底：脚本不存在、启动失败、退出码非 0 都原样暴露给调用方。
    /// </summary>
    public interface IScriptExecutor
    {
        /// <summary>同步执行脚本，阻塞到脚本退出。</summary>
        /// <param name="scriptPath">脚本路径，相对路径按当前工作目录解析。</param>
        /// <param name="arguments">依次传给脚本的参数，每个元素一个参数。</param>
        ScriptResult Run(string scriptPath, params string[] arguments);

        /// <summary>同步执行脚本，<paramref name="onOutput"/> 逐行回调输出（在后台线程触发）。</summary>
        ScriptResult Run(string scriptPath, string[] arguments, Action<string>? onOutput);

        /// <summary>异步执行脚本，不阻塞调用线程。</summary>
        Task<ScriptResult> RunAsync(string scriptPath, params string[] arguments);

        /// <summary>异步执行脚本，<paramref name="onOutput"/> 逐行回调输出（在后台线程触发）。</summary>
        Task<ScriptResult> RunAsync(string scriptPath, string[] arguments, Action<string>? onOutput);

        /// <summary>
        /// 在独立控制台窗口中执行脚本，输出直接打在窗口里；不等待脚本结束（窗口在脚本跑完后等待按键才关闭）。
        /// 该进程挂在 kill-on-close 的 job 上，本进程退出（含被强杀）时它会连同 ssh 等子进程一起结束。
        /// </summary>
        void RunInConsole(string scriptPath, params string[] arguments);
    }
}
