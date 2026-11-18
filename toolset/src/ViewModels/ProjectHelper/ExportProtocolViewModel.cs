using System.Collections.Generic;
using System.Windows.Input;
using Microsoft.Win32;
using ToolSet.Infrastructure;
using ToolSet.Infrastructure.Configuration;
using ToolSet.Infrastructure.Scripts;

namespace ToolSet.ViewModels.ProjectHelper
{
    /// <summary>
    /// 「协议导出」卡片：导出项目的协议文件。
    /// 配置持久化在 config/config.json 的「项目辅助/{项目名}/协议导出」分组下，
    /// 修改后可取消（恢复为配置值）或确认（写回配置）。
    /// </summary>
    public sealed class ExportProtocolViewModel : ViewModelBase
    {
        /// <summary>本卡片在项目分组下的名称。</summary>
        private const string CardSection = "协议导出";

        private const string ProtoDirKey = "proto协议目录";
        private const string FrontendOutputDirKey = "前端生成目录";
        private const string BackendOutputDirKey = "后端生成目录";

        /// <summary>协议生成用的脚本文件名，位于 exe 同级 scripts 目录。</summary>
        private const string ExportScriptFileName = "export_protocol.bat";

        /// <summary>前端生成目标在脚本里的参数值。</summary>
        private const string FrontendTarget = "前端";

        /// <summary>后端生成目标在脚本里的参数值。</summary>
        private const string BackendTarget = "后端";

        private readonly IConfigService _config;
        private readonly IScriptExecutor _scripts;
        private readonly Dictionary<string, string> _savedValues = new Dictionary<string, string>();

        private string _protoDir = string.Empty;
        private string _frontendOutputDir = string.Empty;
        private string _backendOutputDir = string.Empty;
        private bool _hasPendingChanges;
        private bool _isLoading;

        public ExportProtocolViewModel()
            : this(AppConfig.Current, AppScripts.Current)
        {
        }

        public ExportProtocolViewModel(IConfigService config, IScriptExecutor scripts)
        {
            _config = config;
            _scripts = scripts;

            CancelEditCommand = new RelayCommand(CancelEdit);
            ConfirmEditCommand = new RelayCommand(ConfirmEdit);
            BrowseProtoDirCommand = new RelayCommand(BrowseProtoDir);
            BrowseFrontendOutputDirCommand = new RelayCommand(BrowseFrontendOutputDir);
            BrowseBackendOutputDirCommand = new RelayCommand(BrowseBackendOutputDir);
            GenerateFrontendProtocolCommand = new RelayCommand(GenerateFrontendProtocol);
            GenerateBackendProtocolCommand = new RelayCommand(GenerateBackendProtocol);

            LoadFromConfig();
        }

        public string Title => "协议导出";

        public ICommand CancelEditCommand { get; }

        public ICommand ConfirmEditCommand { get; }

        public ICommand BrowseProtoDirCommand { get; }

        public ICommand BrowseFrontendOutputDirCommand { get; }

        public ICommand BrowseBackendOutputDirCommand { get; }

        /// <summary>生成前端协议（功能待定）。</summary>
        public ICommand GenerateFrontendProtocolCommand { get; }

        /// <summary>生成后端协议（功能待定）。</summary>
        public ICommand GenerateBackendProtocolCommand { get; }

        /// <summary>输入框内容与配置不一致时为 true，用于显示「取消 / 确认」按钮。</summary>
        public bool HasPendingChanges
        {
            get => _hasPendingChanges;
            private set => SetProperty(ref _hasPendingChanges, value);
        }

        /// <summary>proto 协议目录。</summary>
        public string ProtoDir
        {
            get => _protoDir;
            set
            {
                if (SetProperty(ref _protoDir, value))
                {
                    RefreshPendingChanges();
                }
            }
        }

        /// <summary>前端生成目录。</summary>
        public string FrontendOutputDir
        {
            get => _frontendOutputDir;
            set
            {
                if (SetProperty(ref _frontendOutputDir, value))
                {
                    RefreshPendingChanges();
                }
            }
        }

        /// <summary>后端生成目录。</summary>
        public string BackendOutputDir
        {
            get => _backendOutputDir;
            set
            {
                if (SetProperty(ref _backendOutputDir, value))
                {
                    RefreshPendingChanges();
                }
            }
        }

        /// <summary>顶部切换项目后，按新项目的配置重新加载（未确认的修改丢弃）。</summary>
        public void ReloadForCurrentProject() => LoadFromConfig();

        private void LoadFromConfig()
        {
            _isLoading = true;
            ProtoDir = ReadValue(ProtoDirKey);
            FrontendOutputDir = ReadValue(FrontendOutputDirKey);
            BackendOutputDir = ReadValue(BackendOutputDirKey);
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
                    [ProtoDirKey] = ProtoDir,
                    [FrontendOutputDirKey] = FrontendOutputDir,
                    [BackendOutputDirKey] = BackendOutputDir,
                });
            }

            MarkAsSaved();
        }

        private void BrowseProtoDir() => ProtoDir = BrowseFolder("选择 proto 协议目录", ProtoDir);

        private void BrowseFrontendOutputDir() => FrontendOutputDir = BrowseFolder("选择前端生成目录", FrontendOutputDir);

        private void BrowseBackendOutputDir() => BackendOutputDir = BrowseFolder("选择后端生成目录", BackendOutputDir);

        /// <summary>生成前端协议：调 scripts/export_protocol.bat 把协议生成到「前端生成目录」。</summary>
        private void GenerateFrontendProtocol() => Generate(FrontendTarget, FrontendOutputDir);

        /// <summary>生成后端协议：调 scripts/export_protocol.bat 把协议生成到「后端生成目录」。</summary>
        private void GenerateBackendProtocol() => Generate(BackendTarget, BackendOutputDir);

        /// <summary>
        /// 调 scripts/export_protocol.bat，用 proto 协议目录里的 lua.exe 执行 generate_sproto.lua。
        /// </summary>
        private void Generate(string target, string outputDir)
        {
            _scripts.RunInConsole(
                AppScripts.GetScriptPath(ExportScriptFileName),
                target,
                ProtoDir,
                outputDir);
        }

        /// <summary>弹出目录选择框，确认后返回所选目录，取消则返回原值。</summary>
        private static string BrowseFolder(string title, string currentDir)
        {
            var dialog = new OpenFolderDialog
            {
                Title = title,
                InitialDirectory = currentDir,
            };

            return dialog.ShowDialog() == true ? dialog.FolderName : currentDir;
        }

        private void MarkAsSaved()
        {
            _savedValues[ProtoDirKey] = ProtoDir;
            _savedValues[FrontendOutputDirKey] = FrontendOutputDir;
            _savedValues[BackendOutputDirKey] = BackendOutputDir;

            HasPendingChanges = false;
        }

        private void RefreshPendingChanges()
        {
            if (_isLoading)
            {
                return;
            }

            HasPendingChanges =
                ProtoDir != _savedValues[ProtoDirKey]
                || FrontendOutputDir != _savedValues[FrontendOutputDirKey]
                || BackendOutputDir != _savedValues[BackendOutputDirKey];
        }

        /// <summary>顶部当前选中的项目名，没有选中项目时为空串。</summary>
        private string CurrentProjectName =>
            _config.Get($"{MainViewModel.MainSection}/{MainViewModel.CurrentSelectProjectKey}");

        /// <summary>本卡片按项目隔离的配置分组，如「项目辅助/stock/协议导出」；没有选中项目时为 null。</summary>
        private string? SectionPath =>
            string.IsNullOrEmpty(CurrentProjectName)
                ? null
                : $"{ProjectHelperViewModel.SectionName}/{CurrentProjectName}/{CardSection}";

        /// <summary>读取本卡片的配置值，没有选中项目时返回空串。</summary>
        private string ReadValue(string key)
            => SectionPath is string section ? _config.Get($"{section}/{key}") : string.Empty;
    }
}
