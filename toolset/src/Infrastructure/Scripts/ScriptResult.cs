namespace ToolSet.Infrastructure.Scripts
{
    /// <summary>
    /// 脚本执行结果。
    /// </summary>
    /// <param name="ExitCode">脚本退出码，即脚本里 <c>exit /b</c> 的值，0 表示成功。</param>
    /// <param name="Output">标准输出全文，按换行符拼接。</param>
    /// <param name="Error">标准错误全文，按换行符拼接。</param>
    public sealed record ScriptResult(int ExitCode, string Output, string Error)
    {
        /// <summary>退出码是否为 0。</summary>
        public bool Success => ExitCode == 0;
    }
}
