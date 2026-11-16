using System.Collections.Generic;
using System.Windows.Input;
using Microsoft.Win32;
using ToolSet.Infrastructure;
using ToolSet.Infrastructure.Configuration;

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

        private const string FrontendExportFormatKey = "前端导出格式";
        private const string BackendExportFormatKey = "后端导出格式";
        private const string ExcelConfigDirKey = "Excel配置目录";
        private const string FrontendOutputDirKey = "前端生成目录";
        private const string BackendOutputDirKey = "后端生成目录";

        private readonly IConfigService _config;
        private readonly Dictionary<string, string> _savedValues = new Dictionary<string, string>();

        private string _frontendExportFormat = string.Empty;
        private string _backendExportFormat = string.Empty;
        private string _excelConfigDir = string.Empty;
        private string _frontendOutputDir = string.Empty;
        private string _backendOutputDir = string.Empty;
        private bool _hasPendingChanges;
        private bool _isLoading;

        public ExportConfigViewModel()
            : this(AppConfig.Current)
        {
        }

        public ExportConfigViewModel(IConfigService config)
        {
            _config = config;

            if (!_config.Contains(SectionPath))
            {
                _config.SetValues(SectionPath, new Dictionary<string, string>
                {
                    [FrontendExportFormatKey] = string.Empty,
                    [BackendExportFormatKey] = string.Empty,
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

            LoadFromConfig();
        }

        public string Title => "配置导出";

        public ICommand CancelEditCommand { get; }

        public ICommand ConfirmEditCommand { get; }

        public ICommand BrowseExcelConfigDirCommand { get; }

        public ICommand BrowseFrontendOutputDirCommand { get; }

        public ICommand BrowseBackendOutputDirCommand { get; }

        /// <summary>输入框内容与配置不一致时为 true，用于显示「取消 / 确认」按钮。</summary>
        public bool HasPendingChanges
        {
            get => _hasPendingChanges;
            private set => SetProperty(ref _hasPendingChanges, value);
        }

        /// <summary>前端导出格式。</summary>
        public string FrontendExportFormat
        {
            get => _frontendExportFormat;
            set
            {
                if (SetProperty(ref _frontendExportFormat, value))
                {
                    RefreshPendingChanges();
                }
            }
        }

        /// <summary>后端导出格式。</summary>
        public string BackendExportFormat
        {
            get => _backendExportFormat;
            set
            {
                if (SetProperty(ref _backendExportFormat, value))
                {
                    RefreshPendingChanges();
                }
            }
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
            FrontendExportFormat = _config.Get(PathOf(FrontendExportFormatKey));
            BackendExportFormat = _config.Get(PathOf(BackendExportFormatKey));
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
                [FrontendExportFormatKey] = FrontendExportFormat,
                [BackendExportFormatKey] = BackendExportFormat,
                [ExcelConfigDirKey] = ExcelConfigDir,
                [FrontendOutputDirKey] = FrontendOutputDir,
                [BackendOutputDirKey] = BackendOutputDir,
            });

            MarkAsSaved();
        }

        private void BrowseExcelConfigDir() => ExcelConfigDir = BrowseFolder("选择 Excel 配置目录", ExcelConfigDir);

        private void BrowseFrontendOutputDir() => FrontendOutputDir = BrowseFolder("选择前端生成目录", FrontendOutputDir);

        private void BrowseBackendOutputDir() => BackendOutputDir = BrowseFolder("选择后端生成目录", BackendOutputDir);

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
            _savedValues[FrontendExportFormatKey] = FrontendExportFormat;
            _savedValues[BackendExportFormatKey] = BackendExportFormat;
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
                FrontendExportFormat != _savedValues[FrontendExportFormatKey]
                || BackendExportFormat != _savedValues[BackendExportFormatKey]
                || ExcelConfigDir != _savedValues[ExcelConfigDirKey]
                || FrontendOutputDir != _savedValues[FrontendOutputDirKey]
                || BackendOutputDir != _savedValues[BackendOutputDirKey];
        }

        private static string PathOf(string key) => $"{SectionPath}/{key}";
    }
}
