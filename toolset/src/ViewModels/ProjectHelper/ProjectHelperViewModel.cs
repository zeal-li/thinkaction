namespace ToolSet.ViewModels.ProjectHelper
{
    /// <summary>
    /// 项目辅助模块：配置导出、协议导出等批量导出功能的入口。
    /// </summary>
    public sealed class ProjectHelperViewModel : ModuleViewModelBase
    {
        public ProjectHelperViewModel()
            : base("项目辅助")
        {
        }

        /// <summary>「协议导出」卡片。</summary>
        public ExportProtocolViewModel ProtocolExport { get; } = new ExportProtocolViewModel();

        /// <summary>「配置导出」卡片。</summary>
        public ExportConfigViewModel ConfigExport { get; } = new ExportConfigViewModel();
    }
}
