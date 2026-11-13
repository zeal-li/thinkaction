using System;
using System.IO;

namespace ToolSet.Infrastructure.Scripts
{
    /// <summary>
    /// 全局默认脚本执行器实例与脚本目录约定。
    /// 脚本目录固定为 exe 同级的 scripts 目录，各模块统一通过 <see cref="GetScriptPath"/> 取脚本全路径。
    /// </summary>
    public static class AppScripts
    {
        /// <summary>脚本目录：exe 同级 scripts 目录。</summary>
        public static string ScriptDirectory { get; } = Path.Combine(AppContext.BaseDirectory, "scripts");

        /// <summary>全局默认执行器，供拿不到 <see cref="IShell"/> 注入的模块直接使用。</summary>
        public static IScriptExecutor Current { get; } = new ScriptExecutor();

        /// <summary>把脚本文件名拼成脚本目录下的全路径，例如 <c>GetScriptPath("sshcall_wsl.bat")</c>。</summary>
        public static string GetScriptPath(string fileName) => Path.Combine(ScriptDirectory, fileName);
    }
}
