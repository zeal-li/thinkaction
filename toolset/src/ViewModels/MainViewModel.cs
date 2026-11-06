using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Reflection;
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
        private string _statusMessage = "就绪";

        public MainViewModel()
        {
            OpenDirectoryCommand = new RelayCommand(OpenDirectory);
            ClearLogCommand = new RelayCommand(Log.Clear);

            Branches = new ObservableCollection<string> { "wc16_apkt01gh0.0.1" };
            SelectedBranch = Branches[0];

            Modules = new ObservableCollection<ModuleViewModelBase>
            {
                new PlaceholderModuleViewModel("管理项目", "项目/分支的切换、目录维护等管理功能。"),
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

        public OutputLog Log { get; } = new OutputLog();

        public ObservableCollection<ModuleViewModelBase> Modules { get; }

        public ObservableCollection<string> Branches { get; }

        public ICommand OpenDirectoryCommand { get; }

        public ICommand ClearLogCommand { get; }

        /// <summary>状态栏右侧显示的版本号，跟随 csproj 的 Version。</summary>
        public string AppVersion
        {
            get
            {
                var version = Assembly.GetExecutingAssembly().GetName().Version;
                return version == null ? string.Empty : $"v{version.Major}.{version.Minor}.{version.Build}";
            }
        }

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

        public string StatusMessage
        {
            get => _statusMessage;
            private set => SetProperty(ref _statusMessage, value);
        }

        public void SetStatus(string message)
        {
            StatusMessage = message;
            Log.Write(message);
        }

        private void OpenDirectory() => Process.Start("explorer.exe", ToolSetPath);
    }
}
