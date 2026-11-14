using System.Collections.Generic;
using System.Windows.Input;
using Microsoft.Win32;
using ToolSet.Infrastructure;
using ToolSet.Infrastructure.Configuration;

namespace ToolSet.ViewModels.EnvironmentDeploy
{
    /// <summary>
    /// 本地 MySQL 配置（servicename / dirpath / hostname / port / username / password）。
    /// 配置持久化在 config/config.json 的指定分组下，修改后可取消（恢复为配置值）或确认（写回配置）。
    /// </summary>
    public sealed class LocalMySqlConfigViewModel : ViewModelBase
    {
        private const string ServiceNameKey = "servicename";
        private const string DirPathKey = "dirpath";
        private const string HostnameKey = "hostname";
        private const string PortKey = "port";
        private const string UsernameKey = "username";
        private const string PasswordKey = "password";

        private readonly IConfigService _config;
        private readonly string _sectionPath;
        private readonly Dictionary<string, string> _savedValues = new Dictionary<string, string>();

        private string _serviceName = string.Empty;
        private string _dirPath = string.Empty;
        private string _hostname = string.Empty;
        private string _port = string.Empty;
        private string _username = string.Empty;
        private string _password = string.Empty;
        private bool _hasPendingChanges;
        private bool _isLoading;

        /// <param name="config">配置读写接口。</param>
        /// <param name="sectionPath">本配置的分组，如 <c>环境部署/Local_MySql_配置</c>。</param>
        public LocalMySqlConfigViewModel(IConfigService config, string sectionPath)
        {
            _config = config;
            _sectionPath = sectionPath;

            if (!_config.Contains(_sectionPath))
            {
                _config.SetValues(_sectionPath, new Dictionary<string, string>
                {
                    [ServiceNameKey] = string.Empty,
                    [DirPathKey] = string.Empty,
                    [HostnameKey] = string.Empty,
                    [PortKey] = string.Empty,
                    [UsernameKey] = string.Empty,
                    [PasswordKey] = string.Empty,
                });
            }

            CancelEditCommand = new RelayCommand(CancelEdit);
            ConfirmEditCommand = new RelayCommand(ConfirmEdit);
            BrowseDirPathCommand = new RelayCommand(BrowseDirPath);
            ConfigureServiceCommand = new RelayCommand(ConfigureService);

            LoadFromConfig();
        }

        public ICommand CancelEditCommand { get; }

        public ICommand ConfirmEditCommand { get; }

        /// <summary>浏览选择 MySQL 所在目录。</summary>
        public ICommand BrowseDirPathCommand { get; }

        /// <summary>配置服务（功能待定）。</summary>
        public ICommand ConfigureServiceCommand { get; }

        /// <summary>输入框内容与配置不一致时为 true，用于显示「取消 / 确认」按钮。</summary>
        public bool HasPendingChanges
        {
            get => _hasPendingChanges;
            private set => SetProperty(ref _hasPendingChanges, value);
        }

        public string ServiceName
        {
            get => _serviceName;
            set
            {
                if (SetProperty(ref _serviceName, value))
                {
                    RefreshPendingChanges();
                }
            }
        }

        public string DirPath
        {
            get => _dirPath;
            set
            {
                if (SetProperty(ref _dirPath, value))
                {
                    RefreshPendingChanges();
                }
            }
        }

        public string Hostname
        {
            get => _hostname;
            set
            {
                if (SetProperty(ref _hostname, value))
                {
                    RefreshPendingChanges();
                }
            }
        }

        public string Port
        {
            get => _port;
            set
            {
                if (SetProperty(ref _port, value))
                {
                    RefreshPendingChanges();
                }
            }
        }

        public string Username
        {
            get => _username;
            set
            {
                if (SetProperty(ref _username, value))
                {
                    RefreshPendingChanges();
                }
            }
        }

        public string Password
        {
            get => _password;
            set
            {
                if (SetProperty(ref _password, value))
                {
                    RefreshPendingChanges();
                }
            }
        }

        private void LoadFromConfig()
        {
            _isLoading = true;
            ServiceName = _config.Get(PathOf(ServiceNameKey));
            DirPath = _config.Get(PathOf(DirPathKey));
            Hostname = _config.Get(PathOf(HostnameKey));
            Port = _config.Get(PathOf(PortKey));
            Username = _config.Get(PathOf(UsernameKey));
            Password = _config.Get(PathOf(PasswordKey));
            _isLoading = false;

            MarkAsSaved();
        }

        private void CancelEdit() => LoadFromConfig();

        private void ConfirmEdit()
        {
            _config.SetValues(_sectionPath, new Dictionary<string, string>
            {
                [ServiceNameKey] = ServiceName,
                [DirPathKey] = DirPath,
                [HostnameKey] = Hostname,
                [PortKey] = Port,
                [UsernameKey] = Username,
                [PasswordKey] = Password,
            });

            MarkAsSaved();
        }

        private void BrowseDirPath()
        {
            var dialog = new OpenFolderDialog
            {
                Title = "选择 MySQL 目录",
            };

            if (dialog.ShowDialog() == true)
            {
                DirPath = dialog.FolderName;
            }
        }

        private void ConfigureService()
        {
            // TODO: 功能待定。
        }

        private void MarkAsSaved()
        {
            _savedValues[ServiceNameKey] = ServiceName;
            _savedValues[DirPathKey] = DirPath;
            _savedValues[HostnameKey] = Hostname;
            _savedValues[PortKey] = Port;
            _savedValues[UsernameKey] = Username;
            _savedValues[PasswordKey] = Password;

            HasPendingChanges = false;
        }

        private void RefreshPendingChanges()
        {
            if (_isLoading)
            {
                return;
            }

            HasPendingChanges =
                ServiceName != _savedValues[ServiceNameKey]
                || DirPath != _savedValues[DirPathKey]
                || Hostname != _savedValues[HostnameKey]
                || Port != _savedValues[PortKey]
                || Username != _savedValues[UsernameKey]
                || Password != _savedValues[PasswordKey];
        }

        private string PathOf(string key) => $"{_sectionPath}/{key}";
    }
}
