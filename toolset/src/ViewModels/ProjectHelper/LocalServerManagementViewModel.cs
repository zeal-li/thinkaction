using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Input;
using ToolSet.Infrastructure;
using ToolSet.Infrastructure.Configuration;
using ToolSet.Infrastructure.Scripts;
using ToolSet.ViewModels.ProjectManagement;

namespace ToolSet.ViewModels.ProjectHelper
{
    /// <summary>
    /// 「本地服管理」卡片：本地服配置、数据库操作和服务器操作的入口。
    /// 服配置持久化在 config/config.json 的「项目辅助/{项目名}/本地服管理」分组下，
    /// 修改后可取消（恢复为配置值）或确认（写回配置）。
    /// </summary>
    public sealed class LocalServerManagementViewModel : ViewModelBase
    {
        /// <summary>本卡片在项目分组下的名称。</summary>
        private const string CardSection = "本地服管理";

        /// <summary>服务器操作用的脚本文件名，位于 exe 同级 scripts 目录。</summary>
        private const string ServerScriptFileName = "local_server_operate.bat";

        private const string GameDatabaseKey = "游戏数据库";
        private const string LogDatabaseKey = "日志数据库";
        private const string GameBasePortKey = "游戏基准端口";
        private const string LogServerPortKey = "日志服端口";

        /// <summary>公共服下拉框的固定选项，顺序即界面展示顺序。</summary>
        private static readonly string[] PublicServerOptions =
        {
            "allserver",
            "logserver",
        };

        /// <summary>游戏服下拉框的固定选项，顺序即界面展示顺序。</summary>
        private static readonly string[] GameServerOptions =
        {
            "allserver",
            "dbserver",
            "loginserver",
            "gameserver",
            "gateserver",
        };

        private readonly IConfigService _config;
        private readonly IScriptExecutor _scripts;
        private readonly Dictionary<string, string> _savedValues = new Dictionary<string, string>();

        private string _gameDatabase = string.Empty;
        private string _logDatabase = string.Empty;
        private string _gameBasePort = string.Empty;
        private string _logServerPort = string.Empty;
        private string? _selectedPublicServer;
        private string? _selectedGameServer;
        private bool _hasPendingChanges;
        private bool _isLoading;

        public LocalServerManagementViewModel()
            : this(AppConfig.Current, AppScripts.Current)
        {
        }

        public LocalServerManagementViewModel(IConfigService config, IScriptExecutor scripts)
        {
            _config = config;
            _scripts = scripts;

            foreach (var server in PublicServerOptions)
            {
                PublicServers.Add(server);
            }

            foreach (var server in GameServerOptions)
            {
                GameServers.Add(server);
            }

            SelectedPublicServer = PublicServerOptions[0];
            SelectedGameServer = GameServerOptions[0];

            CancelEditCommand = new RelayCommand(CancelEdit);
            ConfirmEditCommand = new RelayCommand(ConfirmEdit);
            QueryCommand = new RelayCommand(Query);
            CreateDatabaseCommand = new RelayCommand(CreateDatabase);
            DropDatabaseCommand = new RelayCommand(DropDatabase);
            ClearDataCommand = new RelayCommand(ClearData);
            UpdateCommand = new RelayCommand(Update);
            ExportCommand = new RelayCommand(Export);
            ImportCommand = new RelayCommand(Import);
            PublishCommand = new RelayCommand(Publish);
            StartPublicServerCommand = new RelayCommand(StartPublicServer);
            StopPublicServerCommand = new RelayCommand(StopPublicServer);
            ViewPublicServerCommand = new RelayCommand(ViewPublicServer);
            StartGameServerCommand = new RelayCommand(StartGameServer);
            StopGameServerCommand = new RelayCommand(StopGameServer);
            ViewGameServerCommand = new RelayCommand(ViewGameServer);

            LoadFromConfig();
        }

        public string Title => "本地服管理";

        public ICommand CancelEditCommand { get; }

        public ICommand ConfirmEditCommand { get; }

        /// <summary>输入框内容与配置不一致时为 true，用于显示「取消 / 确认」按钮。</summary>
        public bool HasPendingChanges
        {
            get => _hasPendingChanges;
            private set => SetProperty(ref _hasPendingChanges, value);
        }

        /// <summary>游戏数据库名。</summary>
        public string GameDatabase
        {
            get => _gameDatabase;
            set
            {
                if (SetProperty(ref _gameDatabase, value))
                {
                    RefreshPendingChanges();
                }
            }
        }

        /// <summary>日志数据库名。</summary>
        public string LogDatabase
        {
            get => _logDatabase;
            set
            {
                if (SetProperty(ref _logDatabase, value))
                {
                    RefreshPendingChanges();
                }
            }
        }

        /// <summary>游戏基准端口。</summary>
        public string GameBasePort
        {
            get => _gameBasePort;
            set
            {
                if (SetProperty(ref _gameBasePort, value))
                {
                    RefreshPendingChanges();
                }
            }
        }

        /// <summary>日志服端口。</summary>
        public string LogServerPort
        {
            get => _logServerPort;
            set
            {
                if (SetProperty(ref _logServerPort, value))
                {
                    RefreshPendingChanges();
                }
            }
        }

        /// <summary>公共服下拉框的选项。</summary>
        public ObservableCollection<string> PublicServers { get; } = new ObservableCollection<string>();

        /// <summary>游戏服下拉框的选项。</summary>
        public ObservableCollection<string> GameServers { get; } = new ObservableCollection<string>();

        /// <summary>公共服下拉框里当前选中的服务器。</summary>
        public string? SelectedPublicServer
        {
            get => _selectedPublicServer;
            set => SetProperty(ref _selectedPublicServer, value);
        }

        /// <summary>游戏服下拉框里当前选中的服务器。</summary>
        public string? SelectedGameServer
        {
            get => _selectedGameServer;
            set => SetProperty(ref _selectedGameServer, value);
        }

        /// <summary>查询（功能待定）。</summary>
        public ICommand QueryCommand { get; }

        /// <summary>创建（功能待定）。</summary>
        public ICommand CreateDatabaseCommand { get; }

        /// <summary>删除（功能待定）。</summary>
        public ICommand DropDatabaseCommand { get; }

        /// <summary>清档（功能待定）。</summary>
        public ICommand ClearDataCommand { get; }

        /// <summary>更新（功能待定）。</summary>
        public ICommand UpdateCommand { get; }

        /// <summary>导出（功能待定）。</summary>
        public ICommand ExportCommand { get; }

        /// <summary>导入（功能待定）。</summary>
        public ICommand ImportCommand { get; }

        /// <summary>发布：在 WSL 里执行 ./server_tool.sh -o release。</summary>
        public ICommand PublishCommand { get; }

        /// <summary>公共服开服（功能待定）。</summary>
        public ICommand StartPublicServerCommand { get; }

        /// <summary>公共服关服（功能待定）。</summary>
        public ICommand StopPublicServerCommand { get; }

        /// <summary>公共服查看（功能待定）。</summary>
        public ICommand ViewPublicServerCommand { get; }

        /// <summary>游戏服开服（功能待定）。</summary>
        public ICommand StartGameServerCommand { get; }

        /// <summary>游戏服关服（功能待定）。</summary>
        public ICommand StopGameServerCommand { get; }

        /// <summary>游戏服查看（功能待定）。</summary>
        public ICommand ViewGameServerCommand { get; }

        /// <summary>顶部切换项目后，按新项目的配置重新加载（未确认的修改丢弃）。</summary>
        public void ReloadForCurrentProject() => LoadFromConfig();

        private void LoadFromConfig()
        {
            _isLoading = true;
            GameDatabase = ReadValue(GameDatabaseKey);
            LogDatabase = ReadValue(LogDatabaseKey);
            GameBasePort = ReadValue(GameBasePortKey);
            LogServerPort = ReadValue(LogServerPortKey);
            _isLoading = false;

            MarkAsSaved();
        }

        private void CancelEdit() => LoadFromConfig();

        private void ConfirmEdit()
        {
            if (SectionPath is string section)
            {
                _config.SetValues(section, new Dictionary<string, string>
                {
                    [GameDatabaseKey] = GameDatabase,
                    [LogDatabaseKey] = LogDatabase,
                    [GameBasePortKey] = GameBasePort,
                    [LogServerPortKey] = LogServerPort,
                });
            }

            MarkAsSaved();
        }

        private void MarkAsSaved()
        {
            _savedValues[GameDatabaseKey] = GameDatabase;
            _savedValues[LogDatabaseKey] = LogDatabase;
            _savedValues[GameBasePortKey] = GameBasePort;
            _savedValues[LogServerPortKey] = LogServerPort;

            HasPendingChanges = false;
        }

        private void RefreshPendingChanges()
        {
            if (_isLoading)
            {
                return;
            }

            HasPendingChanges =
                GameDatabase != _savedValues[GameDatabaseKey]
                || LogDatabase != _savedValues[LogDatabaseKey]
                || GameBasePort != _savedValues[GameBasePortKey]
                || LogServerPort != _savedValues[LogServerPortKey];
        }

        /// <summary>顶部当前选中的项目名，没有选中项目时为空串。</summary>
        private string CurrentProjectName =>
            _config.Get($"{MainViewModel.MainSection}/{MainViewModel.CurrentSelectProjectKey}");

        /// <summary>本卡片按项目隔离的配置分组，如「项目辅助/stock/本地服管理」；没有选中项目时为 null。</summary>
        private string? SectionPath =>
            string.IsNullOrEmpty(CurrentProjectName)
                ? null
                : $"{ProjectHelperViewModel.SectionName}/{CurrentProjectName}/{CardSection}";

        /// <summary>读取本卡片的配置值，没有选中项目时返回空串。</summary>
        private string ReadValue(string key)
            => SectionPath is string section ? _config.Get($"{section}/{key}") : string.Empty;

        private void Query()
        {
        }

        private void CreateDatabase()
        {
        }

        private void DropDatabase()
        {
        }

        private void ClearData()
        {
        }

        private void Update()
        {
        }

        private void Export()
        {
        }

        private void Import()
        {
        }

        /// <summary>发布：调 scripts/local_server_operate.bat，登录 WSL 进入「项目目录\server\skynet」执行 ./server_tool.sh -o release。</summary>
        private void Publish()
            => _scripts.RunInConsole(
                AppScripts.GetScriptPath(ServerScriptFileName),
                "publish",
                CurrentProjectDir);

        /// <summary>当前选中项目的「检出根目录\项目名」，脚本到它下面的 server\skynet 找 server_tool.sh。</summary>
        private string CurrentProjectDir
        {
            get
            {
                var projectName = CurrentProjectName;
                var checkoutRootDir = _config.Get(
                    $"{ProjectItemViewModel.ProjectsSection}/{projectName}/{ProjectItemViewModel.CheckoutRootDirKey}");

                return Path.Combine(checkoutRootDir, projectName);
            }
        }

        private void StartPublicServer()
        {
        }

        private void StopPublicServer()
        {
        }

        private void ViewPublicServer()
        {
        }

        private void StartGameServer()
        {
        }

        private void StopGameServer()
        {
        }

        private void ViewGameServer()
        {
        }
    }
}
