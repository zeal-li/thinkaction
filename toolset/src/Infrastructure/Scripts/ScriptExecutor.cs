using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace ToolSet.Infrastructure.Scripts
{
    /// <summary>
    /// <see cref="IScriptExecutor"/> 的默认实现。
    /// 每个脚本用一个独立的 cmd.exe 进程执行（<c>cmd /d /s /c</c>），
    /// 工作目录为脚本所在目录，输出按控制台 OEM 代码页解码（中文系统即 936）。
    /// <see cref="RunInConsole"/> 则新开一个可见的控制台窗口执行，输出直接打在窗口里。
    /// </summary>
    public sealed class ScriptExecutor : IScriptExecutor
    {
        private static readonly Encoding OemEncoding = CreateOemEncoding();

        /// <summary>控制台进程都挂在这个 job 上，本进程一退出（含被强杀）它们就一起结束。</summary>
        private static readonly ConsoleProcessJob ConsoleJob = new ConsoleProcessJob();

        public ScriptResult Run(string scriptPath, params string[] arguments)
            => Run(scriptPath, arguments, null);

        public ScriptResult Run(string scriptPath, string[] arguments, Action<string>? onOutput)
        {
            var fullPath = Path.GetFullPath(scriptPath);
            var output = new StringBuilder();
            var error = new StringBuilder();

            using var process = new Process { StartInfo = CreateStartInfo(fullPath, arguments) };

            process.OutputDataReceived += (_, e) =>
            {
                if (e.Data is null) { return; }
                lock (output) { output.AppendLine(e.Data); }
                onOutput?.Invoke(e.Data);
            };
            process.ErrorDataReceived += (_, e) =>
            {
                if (e.Data is null) { return; }
                lock (error) { error.AppendLine(e.Data); }
                onOutput?.Invoke(e.Data);
            };

            process.Start();
            ConsoleJob.Add(process);   // 程序被强杀时也别把脚本进程留下来
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.WaitForExit();

            string resultOf(StringBuilder builder)
            {
                lock (builder) { return builder.ToString().TrimEnd('\r', '\n'); }
            }

            return new ScriptResult(process.ExitCode, resultOf(output), resultOf(error));
        }

        public Task<ScriptResult> RunAsync(string scriptPath, params string[] arguments)
            => RunAsync(scriptPath, arguments, null);

        public Task<ScriptResult> RunAsync(string scriptPath, string[] arguments, Action<string>? onOutput)
            => Task.Run(() => Run(scriptPath, arguments, onOutput));

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

        private static ProcessStartInfo CreateStartInfo(string scriptPath, IReadOnlyList<string> arguments)
            => new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/d /s /c \"{BuildCommandLine(scriptPath, arguments)}\"",
                WorkingDirectory = Path.GetDirectoryName(scriptPath)!,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = OemEncoding,
                StandardErrorEncoding = OemEncoding,
            };

        /// <summary>拼出 <c>"脚本" 参数1 参数2</c> 形式的命令行（/d /s /c 的外层引号由调用方加）。</summary>
        private static string BuildCommandLine(string scriptPath, IReadOnlyList<string> arguments)
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

        private static Encoding CreateOemEncoding()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            return Encoding.GetEncoding(CultureInfo.CurrentCulture.TextInfo.OEMCodePage);
        }
    }
}
