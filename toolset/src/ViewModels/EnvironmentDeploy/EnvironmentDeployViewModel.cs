using ToolSet.ViewModels;

namespace ToolSet.ViewModels.EnvironmentDeploy
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

        public SSHConfigViewModel Wsl { get; } = new SSHConfigViewModel();

        public SSHConfigViewModel CentOS { get; } = new SSHConfigViewModel();

        public MySqlConfigViewModel LocalMySql { get; } = new MySqlConfigViewModel();
    }
}
