namespace ToolSet.Infrastructure
{
    /// <summary>
    /// 主程序提供给各模块的宿主能力。
    /// </summary>
    public interface IShell
    {
        OutputLog Log { get; }

        /// <summary>更新底部状态栏文本。</summary>
        void SetStatus(string message);
    }
}
