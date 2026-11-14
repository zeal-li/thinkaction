using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows.Input;
using ToolSet.Infrastructure;
using ToolSet.Infrastructure.Configuration;
using ToolSet.ViewModels.Common;
using ToolSet.ViewModels.EnvironmentDeploy;
using ToolSet.ViewModels.ProjectManagement;

namespace ToolSet.ViewModels
{
    public sealed class MainViewModel : ViewModelBase, IShell
    {
        // TODO: 后续改为读取配置文件（对应原 ToolSet.bat 的 scripts/config.ini）
        public const string ToolSetPath = @"C:\data\ikun_01_cn\common\ToolSet";

        private ModuleViewModelBase _selectedModule = null!;
        private string? _selectedBranch;

        public MainViewModel()
        {
            OpenDirectoryCommand = new RelayCommand(OpenDirectory);

            Branches = new ObservableCollection<string>();

            Modules = new ObservableCollection<ModuleViewModelBase>
            {
                new EnvironmentDeployViewModel(),
                new ProjectManagementViewModel(),
                new PlaceholderModuleViewModel("辅助工具", "导出表、导出 Sproto、表结构导出等辅助功能。"),
            };

            foreach (var module in Modules)
            {
                module.PropertyChanged += (_, e) =>
                {
                    if (e.PropertyName == nameof(ModuleViewModelBase.IsSelected) && module.IsSelected)
                    {
                        SelectedModule = module;
                    }
                };
            }

            Modules[0].IsSelected = true;
            SelectedModule = Modules[0];
        }

        public IConfigService Config { get; } = AppConfig.Current;

        public ObservableCollection<ModuleViewModelBase> Modules { get; }

        public ObservableCollection<string> Branches { get; }

        public ICommand OpenDirectoryCommand { get; }

        public string? SelectedBranch
        {
            get => _selectedBranch;
            set => SetProperty(ref _selectedBranch, value);
        }

        public ModuleViewModelBase SelectedModule
        {
            get => _selectedModule;
            private set => SetProperty(ref _selectedModule, value);
        }

        private void OpenDirectory() => Process.Start("explorer.exe", ToolSetPath);
    }
}
