using System.Collections.Generic;
using System.Windows.Input;
using Microsoft.Win32;
using ToolSet.Infrastructure;
using ToolSet.Infrastructure.Configuration;
using ToolSet.Infrastructure.Scripts;

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
        private readonly IScriptExecutor _scripts;
        private readonly string _sectionPath;
        private readonly string _testScriptFileName;
        private readonly Dictionary<string, string> _savedValues = new Dictionary<string, string>();

        private string _hostname = string.Empty;
        private string _port = string.Empty;
        private string _username = string.Empty;
        private string _userKey = string.Empty;
        private bool _hasPendingChanges;
        private bool _isLoading;

        /// <param name="config">配置读写接口。</param>
        /// <param name="scripts">脚本执行器，用于「测试SSH」。</param>
        /// <param name="sectionPath">本环境的配置分组，如 <c>环境部署/WSL_SSH_配置</c>。</param>
        /// <param name="testScriptFileName">测试连接用的脚本文件名，如 <c>sshcall_wsl.bat</c>（脚本自行读配置）。</param>
        public SSHConfigViewModel(IConfigService config, IScriptExecutor scripts, string sectionPath, string testScriptFileName)
        {
            _config = config;
            _scripts = scripts;
            _sectionPath = sectionPath;
            _testScriptFileName = testScriptFileName;

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
            TestSshCommand = new RelayCommand(TestSsh);

            LoadFromConfig();
        }

        public ICommand CancelEditCommand { get; }

        public ICommand ConfirmEditCommand { get; }

        public ICommand BrowseUserKeyCommand { get; }

        /// <summary>测试当前输入的 SSH 连接：新开控制台窗口调用 sshcall_wsl/sshcall_centos.bat 执行 whoami。</summary>
        public ICommand TestSshCommand { get; }

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

        /// <summary>新开控制台窗口执行对应脚本的 whoami，连接信息由脚本自己读 config.json。</summary>
        private void TestSsh()
        {
            // 脚本读的是配置文件，界面上的改动还没确认时先落盘，否则测的是旧配置
            if (HasPendingChanges)
            {
                ConfirmEdit();
            }

            _scripts.RunInConsole(AppScripts.GetScriptPath(_testScriptFileName), "whoami");
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
