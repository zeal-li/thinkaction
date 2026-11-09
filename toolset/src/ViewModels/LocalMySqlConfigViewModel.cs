using ToolSet.Infrastructure;

namespace ToolSet.ViewModels
{
    /// <summary>
    /// 本地 MySQL 配置（mysqlpath / username / passwd）。
    /// </summary>
    public sealed class LocalMySqlConfigViewModel : ViewModelBase
    {
        private string _mysqlPath = string.Empty;
        private string _username = string.Empty;
        private string _passwd = string.Empty;

        public string MysqlPath
        {
            get => _mysqlPath;
            set => SetProperty(ref _mysqlPath, value);
        }

        public string Username
        {
            get => _username;
            set => SetProperty(ref _username, value);
        }

        public string Passwd
        {
            get => _passwd;
            set => SetProperty(ref _passwd, value);
        }
    }
}
