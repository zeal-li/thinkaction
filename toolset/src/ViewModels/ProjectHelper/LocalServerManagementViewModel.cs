using System.Collections.Generic;
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
        private const string GameBasePortKey = "游戏基准端口";
        private const string LogServerPortKey = "日志服端口";

        private readonly IConfigService _config;
        private readonly IScriptExecutor _scripts;
        private readonly Dictionary<string, string> _savedValues = new Dictionary<string, string>();

        private string _gameDatabase = string.Empty;
        private string _gameBasePort = string.Empty;
        private string _logServerPort = string.Empty;
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


        /// <summary>查询（功能待定）。</summary>
        public ICommand QueryCommand { get; }

        /// <summary>创建：建「游戏数据库」，再导入 common\sql\&lt;数据库类型&gt;_table.sql，类型在控制台里选。</summary>
        public ICommand CreateDatabaseCommand { get; }

        /// <summary>删除：删除「游戏数据库」对应的实际库（类型_库名_服ID）。</summary>
        public ICommand DropDatabaseCommand { get; }

        /// <summary>清档：保留库和表结构，清空实际库里所有表的数据。</summary>
        public ICommand ClearDataCommand { get; }

        /// <summary>更新：导入 common\sql\&lt;数据库类型&gt;_change.sql，类型在控制台里选。</summary>
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

        /// <summary>游戏服开服：执行 server_tool.sh -o start，服务器类型和服ID在控制台里让玩家选。</summary>
        public ICommand StartGameServerCommand { get; }

        /// <summary>游戏服关服：执行 server_tool.sh -o stop，服务器类型和服ID在控制台里让玩家选。</summary>
        public ICommand StopGameServerCommand { get; }

        /// <summary>游戏服查看：执行 server_tool.sh -o status，服务器类型和服ID在控制台里让玩家选。</summary>
        public ICommand ViewGameServerCommand { get; }

        /// <summary>顶部切换项目后，按新项目的配置重新加载（未确认的修改丢弃）。</summary>
        public void ReloadForCurrentProject() => LoadFromConfig();

        private void LoadFromConfig()
        {
            _isLoading = true;
            GameDatabase = ReadValue(GameDatabaseKey);
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
                    [GameBasePortKey] = GameBasePort,
                    [LogServerPortKey] = LogServerPort,
                });
            }

            MarkAsSaved();
        }

        private void MarkAsSaved()
        {
            _savedValues[GameDatabaseKey] = GameDatabase;
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

        /// <summary>
        /// 配置里已确认的「游戏数据库」，下发给脚本时用这个，不用输入框的实时值；
        /// 没点「确认」时输入框的改动不会带进脚本。
        /// </summary>
        private string ConfirmedGameDatabase => ReadValue(GameDatabaseKey);

        private void Query()
        {
        }

        /// <summary>
        /// 创建：调 local_server_operate.bat db_create，传项目目录和「游戏数据库」，
        /// 数据库类型（1=dbserver / 2=logserver）由脚本在控制台里让玩家选。
        /// </summary>
        private void CreateDatabase() => RunDatabaseAction("db_create", ConfirmedGameDatabase);

        /// <summary>删除：调 local_server_operate.bat db_delete，删除「游戏数据库」对应的实际库。</summary>
        private void DropDatabase() => RunDatabaseAction("db_delete", ConfirmedGameDatabase);

        /// <summary>清档：调 local_server_operate.bat db_clear，清空实际库所有表的数据（保留库和表结构）。</summary>
        private void ClearData() => RunDatabaseAction("db_clear", ConfirmedGameDatabase);

        /// <summary>更新：调 local_server_operate.bat db_update，导入 common\sql\&lt;数据库类型&gt;_change.sql。</summary>
        private void Update() => RunDatabaseAction("db_update", ConfirmedGameDatabase);

        private void Export()
        {
        }

        private void Import()
        {
        }

        private void Build() => RunServerTool("build");

        private void Rebuild() => RunServerTool("rebuild");

        private void Clean() => RunServerTool("clean");

        private void Publish() => RunServerTool("release");

        /// <summary>
        /// 数据库操作统一走 scripts/local_server_operate.bat：功能名、项目目录、数据库名，
        /// 数据库类型由脚本自己在控制台里让玩家选（1=dbserver，2=logserver）。
        /// </summary>
        /// <param name="action">脚本功能名：db_create / db_delete / db_clear / db_update。</param>
        /// <param name="databaseName">库名中间那段，如 lizexian；脚本里再按类型和服ID拼成 dbserver_lizexian_1 / logserver_lizexian_1。</param>
        private void RunDatabaseAction(string action, string databaseName)
            => _scripts.RunInConsole(
                AppScripts.GetScriptPath(ServerScriptFileName),
                action, CurrentProjectDir, databaseName);

        /// <summary>
        /// 编译类操作统一走 scripts/local_server_operate.bat，功能名就是脚本的动作名：
        /// build / rebuild / clean / release 登录 WSL 进入「项目目录\server\skynet」，
        /// 执行 sudo ./server_tool.sh -o &lt;build|rebuild|clean|release&gt;，只传功能名和项目目录，
        /// 其中 release 成功后脚本自己打开 Svn 提交界面。
        /// </summary>
        /// <param name="action">脚本功能名：build / rebuild / clean / release。</param>
        private void RunServerTool(string action)
            => _scripts.RunInConsole(
                AppScripts.GetScriptPath(ServerScriptFileName),
                action, CurrentProjectDir);

        /// <summary>
        /// 当前选中项目的「检出根目录\项目名」，脚本到它下面的 server\skynet（编译/发布）
        /// 或 common\release\ubuntu（开服/关服/查看）找 server_tool.sh。
        /// </summary>
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

        private void StartGameServer() => RunServerControl("start");

        private void StopGameServer() => RunServerControl("stop");

        private void ViewGameServer() => RunServerControl("status");

        /// <summary>
        /// 开服 / 关服 / 查看统一走 scripts/local_server_operate.bat：只传功能名、项目目录和服务器名
        /// （对应 server_tool.sh 的 -n，用配置里已确认的「游戏数据库」），
        /// 服务器类型（-m）和服ID（-s）由脚本在控制台里让玩家选，选 allserver 时脚本自己逐个类型执行。
        /// </summary>
        /// <param name="action">脚本功能名：start / stop / status。</param>
        private void RunServerControl(string action)
            => _scripts.RunInConsole(
                AppScripts.GetScriptPath(ServerScriptFileName),
                action, CurrentProjectDir, ConfirmedGameDatabase);
    }
}
