namespace ToolSet.ViewModels.ProjectHelper
{
    /// <summary>
    /// 项目辅助模块：配置导出、协议导出等批量导出功能的入口。
    /// </summary>
    public sealed class ProjectHelperViewModel : ModuleViewModelBase
    {
        /// <summary>本模块的配置分组，各卡片再按项目名分组。</summary>
        public const string SectionName = "项目辅助";

        public ProjectHelperViewModel()
            : base(SectionName)
        {
        }

        /// <summary>「配置导出」卡片。</summary>
        public ExportConfigViewModel ConfigExport { get; } = new ExportConfigViewModel();

        /// <summary>「协议导出」卡片。</summary>
        public ExportProtocolViewModel ProtocolExport { get; } = new ExportProtocolViewModel();

        /// <summary>「本地服管理」卡片（功能待定）。</summary>
        public LocalServerManagementViewModel LocalServerManagement { get; } = new LocalServerManagementViewModel();

        /// <summary>
        /// 顶部选中项目变化后，让各卡片按新项目的配置重新加载。
        /// 各卡片的配置都挂在「项目辅助/{项目名}」下，互不干扰。
        /// </summary>
        public void ReloadForCurrentProject()
        {
            ConfigExport.ReloadForCurrentProject();
            ProtocolExport.ReloadForCurrentProject();
            LocalServerManagement.ReloadForCurrentProject();
        }
    }
}
