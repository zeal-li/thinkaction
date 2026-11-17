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
    /// 配置持久化在 config/config.json 的「项目辅助/协议导出」分组下，
    /// 修改后可取消（恢复为配置值）或确认（写回配置）。
    /// </summary>
    public sealed class ExportProtocolViewModel : ViewModelBase
    {
        /// <summary>本卡片的配置分组。</summary>
        public const string SectionPath = "项目辅助/协议导出";

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

            if (!_config.Contains(SectionPath))
            {
                _config.SetValues(SectionPath, new Dictionary<string, string>
                {
                    [ProtoDirKey] = string.Empty,
                    [FrontendOutputDirKey] = string.Empty,
                    [BackendOutputDirKey] = string.Empty,
                });
            }

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

        private void LoadFromConfig()
        {
            _isLoading = true;
            ProtoDir = _config.Get(PathOf(ProtoDirKey));
            FrontendOutputDir = _config.Get(PathOf(FrontendOutputDirKey));
            BackendOutputDir = _config.Get(PathOf(BackendOutputDirKey));
            _isLoading = false;

            MarkAsSaved();
        }

        private void CancelEdit() => LoadFromConfig();

        private void ConfirmEdit()
        {
            _config.SetValues(SectionPath, new Dictionary<string, string>
            {
                [ProtoDirKey] = ProtoDir,
                [FrontendOutputDirKey] = FrontendOutputDir,
                [BackendOutputDirKey] = BackendOutputDir,
            });

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

        private static string PathOf(string key) => $"{SectionPath}/{key}";
    }
}
