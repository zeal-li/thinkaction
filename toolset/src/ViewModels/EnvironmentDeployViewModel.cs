namespace ToolSet.ViewModels
{
    /// <summary>
    /// 环境部署模块：WSL 与 CentOS 两套环境配置。
    /// </summary>
    public sealed class EnvironmentDeployViewModel : ModuleViewModelBase
    {
        public EnvironmentDeployViewModel()
            : base("环境部署")
        {
        }

        public RemoteHostConfigViewModel Wsl { get; } = new RemoteHostConfigViewModel();

        public RemoteHostConfigViewModel CentOS { get; } = new RemoteHostConfigViewModel();

        public LocalMySqlConfigViewModel LocalMySql { get; } = new LocalMySqlConfigViewModel();
    }
}
