using System;
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
        private const string ServerNameKey = "服务器名";
        private const string ServerIdKey = "服务器ID";

        /// <summary>下拉框里的「全部」选项：选中它时对该下拉框里除它以外的所有服务器依次操作。</summary>
        private const string AllServerOption = "allserver";

        /// <summary>公共服下拉框的固定选项，顺序即界面展示顺序。</summary>
        private static readonly string[] PublicServerOptions =
        {
            AllServerOption,
            "logserver",
        };

        /// <summary>
        /// 游戏服下拉框的固定选项，顺序即界面展示顺序，也是 allserver 时的执行顺序；
        /// 名字必须和 server_tool.sh 的 -m 取值一致（网关是 gateway，不是 gateserver）。
        /// </summary>
        private static readonly string[] GameServerOptions =
        {
            AllServerOption,
            "dbserver",
            "loginserver",
            "gateway",
            "gameserver",
        };

        private readonly IConfigService _config;
        private readonly IScriptExecutor _scripts;
        private readonly Dictionary<string, string> _savedValues = new Dictionary<string, string>();

        private string _gameDatabase = string.Empty;
        private string _logDatabase = string.Empty;
        private string _gameBasePort = string.Empty;
        private string _logServerPort = string.Empty;
        private string _serverName = string.Empty;
        private string _serverId = string.Empty;
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
            BuildCommand = new RelayCommand(Build);
            RebuildCommand = new RelayCommand(Rebuild);
            CleanCommand = new RelayCommand(Clean);
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

        /// <summary>服务器名，对应 server_tool.sh 的 -n，启动配置/日志/进程名里都用它。</summary>
        public string ServerName
        {
            get => _serverName;
            set
            {
                if (SetProperty(ref _serverName, value))
                {
                    RefreshPendingChanges();
                }
            }
        }

        /// <summary>服务器ID，对应 server_tool.sh 的 -s，开服/关服/查看时必须是大于 0 的数字。</summary>
        public string ServerId
        {
            get => _serverId;
            set
            {
                if (SetProperty(ref _serverId, value))
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

        /// <summary>编译：在 WSL 里执行 ./server_tool.sh -o build。</summary>
        public ICommand BuildCommand { get; }

        /// <summary>重编：在 WSL 里执行 ./server_tool.sh -o rebuild。</summary>
        public ICommand RebuildCommand { get; }

        /// <summary>清理：在 WSL 里执行 ./server_tool.sh -o clean。</summary>
        public ICommand CleanCommand { get; }

        /// <summary>发布：在 WSL 里执行 ./server_tool.sh -o release，成功后打开 Svn 提交界面。</summary>
        public ICommand PublishCommand { get; }

        /// <summary>公共服开服：对下拉框选中的服务器（选 allserver 时是除 allserver 外的全部）执行 server_tool.sh -o start。</summary>
        public ICommand StartPublicServerCommand { get; }

        /// <summary>公共服关服：执行 server_tool.sh -o stop。</summary>
        public ICommand StopPublicServerCommand { get; }

        /// <summary>公共服查看：执行 server_tool.sh -o status。</summary>
        public ICommand ViewPublicServerCommand { get; }

        /// <summary>游戏服开服：执行 server_tool.sh -o start。</summary>
        public ICommand StartGameServerCommand { get; }

        /// <summary>游戏服关服：执行 server_tool.sh -o stop。</summary>
        public ICommand StopGameServerCommand { get; }

        /// <summary>游戏服查看：执行 server_tool.sh -o status。</summary>
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
            ServerName = ReadValue(ServerNameKey);
            ServerId = ReadValue(ServerIdKey);
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
                    [ServerNameKey] = ServerName,
                    [ServerIdKey] = ServerId,
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
            _savedValues[ServerNameKey] = ServerName;
            _savedValues[ServerIdKey] = ServerId;

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
                || LogServerPort != _savedValues[LogServerPortKey]
                || ServerName != _savedValues[ServerNameKey]
                || ServerId != _savedValues[ServerIdKey];
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

        private void Build() => RunServerTool("build");

        private void Rebuild() => RunServerTool("rebuild");

        private void Clean() => RunServerTool("clean");

        private void Publish() => RunServerTool("publish");

        /// <summary>
        /// 服务器操作统一走 scripts/local_server_operate.bat，功能名就是脚本的动作名：
        /// 脚本登录 WSL 进入「项目目录\server\skynet」执行
        /// sudo ./server_tool.sh -o &lt;build|rebuild|clean|release|start|stop|status&gt;，
        /// 其中 publish 成功后脚本自己打开 Svn 提交界面。
        /// </summary>
        /// <param name="action">脚本功能名：build / rebuild / clean / publish / start / stop / status。</param>
        /// <param name="serverModels">服务器类型，空格分隔；只有 start / stop / status 用得上，其余传空。</param>
        private void RunServerTool(string action, string serverModels = "")
            => _scripts.RunInConsole(
                AppScripts.GetScriptPath(ServerScriptFileName),
                action,
                CurrentProjectDir,
                ServerName,
                ServerId,
                serverModels);

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

        private void StartPublicServer() => RunServerTool("start", SelectedServerModels(PublicServers, SelectedPublicServer));

        private void StopPublicServer() => RunServerTool("stop", SelectedServerModels(PublicServers, SelectedPublicServer));

        private void ViewPublicServer() => RunServerTool("status", SelectedServerModels(PublicServers, SelectedPublicServer));

        private void StartGameServer() => RunServerTool("start", SelectedServerModels(GameServers, SelectedGameServer));

        private void StopGameServer() => RunServerTool("stop", SelectedServerModels(GameServers, SelectedGameServer));

        private void ViewGameServer() => RunServerTool("status", SelectedServerModels(GameServers, SelectedGameServer));

        /// <summary>
        /// 取要操作的服务器类型：选的是 allserver 时展开成该下拉框里除 allserver 外的所有类型（按下拉框顺序），
        /// 否则只取选中的那个；多个类型用空格连接，脚本按顺序逐个执行。
        /// </summary>
        private static string SelectedServerModels(IEnumerable<string> options, string? selected)
        {
            var selectAll = string.Equals(selected, AllServerOption, StringComparison.OrdinalIgnoreCase);
            var models = new List<string>();

            foreach (var option in options)
            {
                if (string.Equals(option, AllServerOption, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (selectAll || string.Equals(option, selected, StringComparison.OrdinalIgnoreCase))
                {
                    models.Add(option);
                }
            }

            return string.Join(" ", models);
        }
    }
}
