using ToolSet.Infrastructure.Configuration;
using ToolSet.Infrastructure.Scripts;
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
            Wsl = new SSHConfigViewModel(AppConfig.Current, AppScripts.Current, "环境部署/WSL_SSH_配置", "sshcall_wsl.bat");
            CentOS = new SSHConfigViewModel(AppConfig.Current, AppScripts.Current, "环境部署/Centos_SSH_配置", "sshcall_centos.bat");
        }

        public SSHConfigViewModel Wsl { get; }

        public SSHConfigViewModel CentOS { get; }

        public LocalMySqlConfigViewModel LocalMySql { get; } = new LocalMySqlConfigViewModel(AppConfig.Current, "环境部署/Local_MySql_配置");
    }
}
