using System.Windows.Input;
using Microsoft.Win32;
using ToolSet.Infrastructure;

namespace ToolSet.ViewModels.ProjectHelper
{
    /// <summary>
    /// 「配置导出」卡片：导出项目的配置数据。
    /// </summary>
    public sealed class ExportConfigViewModel : ViewModelBase
    {
        private string _frontendExportFormat = string.Empty;
        private string _backendExportFormat = string.Empty;
        private string _excelConfigDir = string.Empty;
        private string _frontendOutputDir = string.Empty;
        private string _backendOutputDir = string.Empty;

        public ExportConfigViewModel()
        {
            BrowseExcelConfigDirCommand = new RelayCommand(BrowseExcelConfigDir);
            BrowseFrontendOutputDirCommand = new RelayCommand(BrowseFrontendOutputDir);
            BrowseBackendOutputDirCommand = new RelayCommand(BrowseBackendOutputDir);
        }

        public string Title => "配置导出";

        /// <summary>前端导出格式。</summary>
        public string FrontendExportFormat
        {
            get => _frontendExportFormat;
            set => SetProperty(ref _frontendExportFormat, value);
        }

        /// <summary>后端导出格式。</summary>
        public string BackendExportFormat
        {
            get => _backendExportFormat;
            set => SetProperty(ref _backendExportFormat, value);
        }

        /// <summary>Excel 配置目录。</summary>
        public string ExcelConfigDir
        {
            get => _excelConfigDir;
            set => SetProperty(ref _excelConfigDir, value);
        }

        /// <summary>前端生成目录。</summary>
        public string FrontendOutputDir
        {
            get => _frontendOutputDir;
            set => SetProperty(ref _frontendOutputDir, value);
        }

        /// <summary>后端生成目录。</summary>
        public string BackendOutputDir
        {
            get => _backendOutputDir;
            set => SetProperty(ref _backendOutputDir, value);
        }

        public ICommand BrowseExcelConfigDirCommand { get; }

        public ICommand BrowseFrontendOutputDirCommand { get; }

        public ICommand BrowseBackendOutputDirCommand { get; }

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
    }
}
