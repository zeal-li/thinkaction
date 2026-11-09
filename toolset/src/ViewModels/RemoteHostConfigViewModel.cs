using ToolSet.Infrastructure;

namespace ToolSet.ViewModels
{
    /// <summary>
    /// 单个环境的连接配置（hostname / port / username / userkey）。
    /// </summary>
    public sealed class RemoteHostConfigViewModel : ViewModelBase
    {
        private string _hostname = string.Empty;
        private string _port = string.Empty;
        private string _username = string.Empty;
        private string _userKey = string.Empty;

        public string Hostname
        {
            get => _hostname;
            set => SetProperty(ref _hostname, value);
        }

        public string Port
        {
            get => _port;
            set => SetProperty(ref _port, value);
        }

        public string Username
        {
            get => _username;
            set => SetProperty(ref _username, value);
        }

        public string UserKey
        {
            get => _userKey;
            set => SetProperty(ref _userKey, value);
        }
    }
}
