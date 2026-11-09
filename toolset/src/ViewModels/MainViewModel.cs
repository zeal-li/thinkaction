using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows.Input;
using ToolSet.Infrastructure;

namespace ToolSet.ViewModels
{
    public sealed class MainViewModel : ViewModelBase, IShell
    {
        // TODO: 后续改为读取配置文件（对应原 ToolSet.bat 的 scripts/config.ini）
        public const string ToolSetPath = @"C:\data\ikun_01_cn\common\ToolSet";

        private ModuleViewModelBase _selectedModule = null!;
        private string _selectedBranch = string.Empty;

        public MainViewModel()
        {
            OpenDirectoryCommand = new RelayCommand(OpenDirectory);

            Branches = new ObservableCollection<string> { "wc16_apkt01gh0.0.1" };
            SelectedBranch = Branches[0];

            Modules = new ObservableCollection<ModuleViewModelBase>
            {
                new PlaceholderModuleViewModel("管理项目", "项目/分支的切换、目录维护等管理功能。"),
                new PlaceholderModuleViewModel("辅助工具", "导出表、导出 Sproto、表结构导出等辅助功能。"),
                new PlaceholderModuleViewModel("环境部署", "环境安装与部署流程（功能待定）。"),
                new PlaceholderModuleViewModel("设置", "工具参数与个人偏好设置（功能待定）。"),
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

        public OutputLog Log { get; } = new OutputLog();

        public ObservableCollection<ModuleViewModelBase> Modules { get; }

        public ObservableCollection<string> Branches { get; }

        public ICommand OpenDirectoryCommand { get; }

        public string SelectedBranch
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
