using System.Collections.Generic;
using System.Windows.Input;
using Microsoft.Win32;
using ToolSet.Infrastructure;
using ToolSet.Infrastructure.Configuration;

namespace ToolSet.ViewModels.EnvironmentDeploy
{
    /// <summary>
    /// 单个环境的 SSH 连接配置（hostname / port / username / userkey）。
    /// 配置持久化在 config/config.json 的指定分组下，修改后可取消（恢复为配置值）或确认（写回配置）。
    /// </summary>
    public sealed class SSHConfigViewModel : ViewModelBase
    {
        private const string HostnameKey = "hostname";
        private const string PortKey = "port";
        private const string UsernameKey = "username";
        private const string UserKeyKey = "userkey";

        private readonly IConfigService _config;
        private readonly string _sectionPath;
        private readonly Dictionary<string, string> _savedValues = new Dictionary<string, string>();

        private string _hostname = string.Empty;
        private string _port = string.Empty;
        private string _username = string.Empty;
        private string _userKey = string.Empty;
        private bool _hasPendingChanges;
        private bool _isLoading;

        public SSHConfigViewModel(IConfigService config, string sectionPath)
        {
            _config = config;
            _sectionPath = sectionPath;

            if (!_config.Contains(_sectionPath))
            {
                _config.SetValues(_sectionPath, new Dictionary<string, string>
                {
                    [HostnameKey] = string.Empty,
                    [PortKey] = string.Empty,
                    [UsernameKey] = string.Empty,
                    [UserKeyKey] = string.Empty,
                });
            }

            CancelEditCommand = new RelayCommand(CancelEdit);
            ConfirmEditCommand = new RelayCommand(ConfirmEdit);
            BrowseUserKeyCommand = new RelayCommand(BrowseUserKey);

            LoadFromConfig();
        }

        public ICommand CancelEditCommand { get; }

        public ICommand ConfirmEditCommand { get; }

        public ICommand BrowseUserKeyCommand { get; }

        /// <summary>输入框内容与配置不一致时为 true，用于显示「取消 / 确认」按钮。</summary>
        public bool HasPendingChanges
        {
            get => _hasPendingChanges;
            private set => SetProperty(ref _hasPendingChanges, value);
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

        public string UserKey
        {
            get => _userKey;
            set
            {
                if (SetProperty(ref _userKey, value))
                {
                    RefreshPendingChanges();
                }
            }
        }

        private void LoadFromConfig()
        {
            _isLoading = true;
            Hostname = _config.Get(PathOf(HostnameKey));
            Port = _config.Get(PathOf(PortKey));
            Username = _config.Get(PathOf(UsernameKey));
            UserKey = _config.Get(PathOf(UserKeyKey));
            _isLoading = false;

            MarkAsSaved();
        }

        private void CancelEdit() => LoadFromConfig();

        private void BrowseUserKey()
        {
            var dialog = new OpenFileDialog
            {
                Title = "选择密钥文件",
                Filter = "所有文件 (*.*)|*.*",
            };

            if (dialog.ShowDialog() == true)
            {
                UserKey = dialog.FileName;
            }
        }

        private void ConfirmEdit()
        {
            _config.SetValues(_sectionPath, new Dictionary<string, string>
            {
                [HostnameKey] = Hostname,
                [PortKey] = Port,
                [UsernameKey] = Username,
                [UserKeyKey] = UserKey,
            });

            MarkAsSaved();
        }

        private void MarkAsSaved()
        {
            _savedValues[HostnameKey] = Hostname;
            _savedValues[PortKey] = Port;
            _savedValues[UsernameKey] = Username;
            _savedValues[UserKeyKey] = UserKey;

            HasPendingChanges = false;
        }

        private void RefreshPendingChanges()
        {
            if (_isLoading)
            {
                return;
            }

            HasPendingChanges =
                Hostname != _savedValues[HostnameKey]
                || Port != _savedValues[PortKey]
                || Username != _savedValues[UsernameKey]
                || UserKey != _savedValues[UserKeyKey];
        }

        private string PathOf(string key) => $"{_sectionPath}/{key}";
    }
}
