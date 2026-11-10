using System.Diagnostics;
using System.IO;
using System.Text;

namespace ToolSet.Infrastructure.Scripts
{
    /// <summary>
    /// <see cref="IScriptExecutor"/> 的默认实现。
    /// 每个脚本用一个独立的 cmd.exe 进程在新建的控制台窗口中执行（<c>cmd /d /s /c</c>），
    /// 输出直接打在窗口里，脚本跑完后 <c>pause</c> 停住等按键。
    /// </summary>
    public sealed class ScriptExecutor : IScriptExecutor
    {
        /// <summary>控制台进程都挂在这个 job 上，本进程一退出（含被强杀）它们就一起结束。</summary>
        private static readonly ConsoleProcessJob ConsoleJob = new ConsoleProcessJob();

        public void RunInConsole(string scriptPath, params string[] arguments)
        {
            var fullPath = Path.GetFullPath(scriptPath);

            var process = Process.Start(new ProcessStartInfo
            {
                FileName = "cmd.exe",
                // 窗口保持可见,脚本跑完后 pause 停住,按键才关闭,方便看日志
                Arguments = $"/d /s /c \"{BuildCommandLine(fullPath, arguments)} & pause\"",
                // 窗口会长时间停留,工作目录用临时目录,避免把脚本目录锁住导致 build.bat 删不掉 build\scripts
                WorkingDirectory = Path.GetTempPath(),
                UseShellExecute = false,
                CreateNoWindow = false,
            })!;

            ConsoleJob.Add(process);

            // 进程自己退出的（按了键）释放句柄，否则每次点「测试SSH」都会漏一个 Process 句柄
            process.EnableRaisingEvents = true;
            process.Exited += (_, _) => process.Dispose();
        }

        /// <summary>拼出 <c>"脚本" 参数1 参数2</c> 形式的命令行（/d /s /c 的外层引号由调用方加）。</summary>
        private static string BuildCommandLine(string scriptPath, string[] arguments)
        {
            var commandLine = new StringBuilder(Quote(scriptPath));
            foreach (var argument in arguments)
            {
                commandLine.Append(' ').Append(Quote(argument));
            }

            return commandLine.ToString();
        }

        /// <summary>含空白或为空时加引号，参数里自带引号的情况由调用方自行处理。</summary>
        private static string Quote(string value)
            => value.Length == 0 || value.Contains(' ') || value.Contains('\t')
                ? $"\"{value}\""
                : value;
    }
}
