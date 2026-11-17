using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows.Input;
using Microsoft.Win32;
using ToolSet.Infrastructure;
using ToolSet.Infrastructure.Configuration;
using ToolSet.Infrastructure.Scripts;
using ToolSet.ViewModels.ProjectManagement;

namespace ToolSet.ViewModels.ProjectHelper
{
    /// <summary>
    /// 「配置导出」卡片：导出项目的配置数据。
    /// 配置持久化在 config/config.json 的「项目辅助/配置导出」分组下，
    /// 修改后可取消（恢复为配置值）或确认（写回配置）。
    /// </summary>
    public sealed class ExportConfigViewModel : ViewModelBase
    {
        /// <summary>本卡片的配置分组。</summary>
        public const string SectionPath = "项目辅助/配置导出";

        private const string ExcelConfigDirKey = "Excel配置目录";
        private const string FrontendOutputDirKey = "前端生成目录";
        private const string BackendOutputDirKey = "后端生成目录";

        /// <summary>下拉框中的「全部」选项，固定排在第一位。</summary>
        public const string AllConfigFilesOption = "全部";

        /// <summary>导出用的脚本文件名，位于 exe 同级 scripts 目录。</summary>
        private const string ExportScriptFileName = "export_config.bat";

        /// <summary>前端导出目标在脚本里的参数值。</summary>
        private const string FrontendTarget = "前端";

        /// <summary>后端导出目标在脚本里的参数值。</summary>
        private const string BackendTarget = "后端";

        private readonly IConfigService _config;
        private readonly IScriptExecutor _scripts;
        private readonly Dictionary<string, string> _savedValues = new Dictionary<string, string>();
        private readonly RelayCommand _exportFrontendCommand;
        private readonly RelayCommand _exportBackendCommand;

        private string _excelConfigDir = string.Empty;
        private string _frontendOutputDir = string.Empty;
        private string _backendOutputDir = string.Empty;
        private string? _selectedExcelConfigFile;
        private bool _hasPendingChanges;
        private bool _isLoading;

        public ExportConfigViewModel()
            : this(AppConfig.Current, AppScripts.Current)
        {
        }

        public ExportConfigViewModel(IConfigService config, IScriptExecutor scripts)
        {
            _config = config;
            _scripts = scripts;

            if (!_config.Contains(SectionPath))
            {
                _config.SetValues(SectionPath, new Dictionary<string, string>
                {
                    [ExcelConfigDirKey] = string.Empty,
                    [FrontendOutputDirKey] = string.Empty,
                    [BackendOutputDirKey] = string.Empty,
                });
            }

            CancelEditCommand = new RelayCommand(CancelEdit);
            ConfirmEditCommand = new RelayCommand(ConfirmEdit);
            BrowseExcelConfigDirCommand = new RelayCommand(BrowseExcelConfigDir);
            BrowseFrontendOutputDirCommand = new RelayCommand(BrowseFrontendOutputDir);
            BrowseBackendOutputDirCommand = new RelayCommand(BrowseBackendOutputDir);

            // 没选配置文件时导出按钮不可用
            _exportFrontendCommand = new RelayCommand(ExportToFrontend, HasSelectedExcelConfigFile);
            _exportBackendCommand = new RelayCommand(ExportToBackend, HasSelectedExcelConfigFile);
            ExportFrontendCommand = _exportFrontendCommand;
            ExportBackendCommand = _exportBackendCommand;

            LoadFromConfig();
        }

        public string Title => "配置导出";

        /// <summary>Excel 配置目录下找到的配置文件（仅文件名），作为下拉框选项。</summary>
        public ObservableCollection<string> ExcelConfigFiles { get; } = new ObservableCollection<string>();

        /// <summary>下拉框里当前选中的配置文件。</summary>
        public string? SelectedExcelConfigFile
        {
            get => _selectedExcelConfigFile;
            set
            {
                if (SetProperty(ref _selectedExcelConfigFile, value))
                {
                    _exportFrontendCommand.RaiseCanExecuteChanged();
                    _exportBackendCommand.RaiseCanExecuteChanged();
                }
            }
        }

        public ICommand CancelEditCommand { get; }

        public ICommand ConfirmEditCommand { get; }

        public ICommand BrowseExcelConfigDirCommand { get; }

        public ICommand BrowseFrontendOutputDirCommand { get; }

        public ICommand BrowseBackendOutputDirCommand { get; }

        /// <summary>把选中的配置表导出到「前端生成目录」。</summary>
        public ICommand ExportFrontendCommand { get; }

        /// <summary>把选中的配置表导出到「后端生成目录」。</summary>
        public ICommand ExportBackendCommand { get; }

        /// <summary>输入框内容与配置不一致时为 true，用于显示「取消 / 确认」按钮。</summary>
        public bool HasPendingChanges
        {
            get => _hasPendingChanges;
            private set => SetProperty(ref _hasPendingChanges, value);
        }

        /// <summary>Excel 配置目录。</summary>
        public string ExcelConfigDir
        {
            get => _excelConfigDir;
            set
            {
                if (SetProperty(ref _excelConfigDir, value))
                {
                    RefreshPendingChanges();
                    RefreshExcelConfigFiles();
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
            ExcelConfigDir = _config.Get(PathOf(ExcelConfigDirKey));
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
                [ExcelConfigDirKey] = ExcelConfigDir,
                [FrontendOutputDirKey] = FrontendOutputDir,
                [BackendOutputDirKey] = BackendOutputDir,
            });

            MarkAsSaved();
        }

        private void BrowseExcelConfigDir() => ExcelConfigDir = BrowseFolder("选择 Excel 配置目录", ExcelConfigDir);

        private void BrowseFrontendOutputDir() => FrontendOutputDir = BrowseFolder("选择前端生成目录", FrontendOutputDir);

        private void BrowseBackendOutputDir() => BackendOutputDir = BrowseFolder("选择后端生成目录", BackendOutputDir);

        private void ExportToFrontend() => Export(FrontendTarget, FrontendOutputDir);

        private void ExportToBackend() => Export(BackendTarget, BackendOutputDir);

        /// <summary>
        /// 调 scripts/export_config.bat 把选中的配置表导出到对应目录：
        /// 选中「全部」时把整个 Excel 配置目录交给脚本，否则传单个配置文件全路径。
        /// </summary>
        private void Export(string target, string outputDir)
        {
            var inputPath = SelectedExcelConfigFile == AllConfigFilesOption
                ? ExcelConfigDir
                : Path.Combine(ExcelConfigDir, SelectedExcelConfigFile!);

            _scripts.RunInConsole(
                AppScripts.GetScriptPath(ExportScriptFileName),
                target,
                inputPath,
                outputDir,
                CurrentProjectDir);
        }

        /// <summary>当前选中项目的「检出根目录\项目名」，导表工具就在这个目录的 common\tools\ExportTool\dist 下。</summary>
        private string CurrentProjectDir
        {
            get
            {
                var projectName = _config.Get($"{MainViewModel.MainSection}/{MainViewModel.CurrentSelectProjectKey}");
                var checkoutRootDir = _config.Get(
                    $"{ProjectItemViewModel.ProjectsSection}/{projectName}/{ProjectItemViewModel.CheckoutRootDirKey}");

                return Path.Combine(checkoutRootDir, projectName);
            }
        }

        /// <summary>下拉框里选了具体配置文件（含「全部」）才允许导出。</summary>
        private bool HasSelectedExcelConfigFile() => !string.IsNullOrEmpty(SelectedExcelConfigFile);

        /// <summary>列出 Excel 配置目录下的所有文件（按文件名排序）。</summary>
        private void RefreshExcelConfigFiles()
        {
            ExcelConfigFiles.Clear();
            ExcelConfigFiles.Add(AllConfigFilesOption);

            if (Directory.Exists(ExcelConfigDir))
            {
                foreach (var file in Directory.GetFiles(ExcelConfigDir)
                                           .OrderBy(file => Path.GetFileName(file), StringComparer.OrdinalIgnoreCase))
                {
                    ExcelConfigFiles.Add(Path.GetFileName(file));
                }
            }

            // 列表重建后 WPF 会把选中项清空，默认落回「全部」，导出按钮才可用
            SelectedExcelConfigFile = AllConfigFilesOption;
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
            _savedValues[ExcelConfigDirKey] = ExcelConfigDir;
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
                ExcelConfigDir != _savedValues[ExcelConfigDirKey]
                || FrontendOutputDir != _savedValues[FrontendOutputDirKey]
                || BackendOutputDir != _savedValues[BackendOutputDirKey];
        }

        private static string PathOf(string key) => $"{SectionPath}/{key}";
    }
}
