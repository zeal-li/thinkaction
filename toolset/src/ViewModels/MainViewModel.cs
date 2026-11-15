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

        /// <summary>主界面相关配置所在的分组。</summary>
        public const string MainSection = "主界面";

        /// <summary>当前选中项目的配置键。</summary>
        public const string CurrentSelectProjectKey = "current_select_project";

        private static string CurrentSelectProjectPath => $"{MainSection}/{CurrentSelectProjectKey}";

        private readonly ProjectManagementViewModel _projectManagement;
        private ModuleViewModelBase _selectedModule = null!;
        private string? _selectedProject;
        private bool _isSynchronizingProjects;

        public MainViewModel()
        {
            OpenDirectoryCommand = new RelayCommand(OpenDirectory);

            Projects = new ObservableCollection<string>();

            _projectManagement = new ProjectManagementViewModel();
            // 项目增删后刷新顶部下拉框，并保持 current_select_project 始终有效
            _projectManagement.Projects.CollectionChanged += (_, _) => SynchronizeProjects();

            Modules = new ObservableCollection<ModuleViewModelBase>
            {
                new EnvironmentDeployViewModel(),
                _projectManagement,
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

            SynchronizeProjects();
        }

        public IConfigService Config { get; } = AppConfig.Current;

        public ObservableCollection<ModuleViewModelBase> Modules { get; }

        public ObservableCollection<string> Projects { get; }

        public ICommand OpenDirectoryCommand { get; }

        public string? SelectedProject
        {
            get => _selectedProject;
            set
            {
                if (!SetProperty(ref _selectedProject, value))
                {
                    return;
                }

                // 用户手动切换时立即写回配置；同步过程中统一由 SynchronizeProjects 写回
                if (!_isSynchronizingProjects)
                {
                    Config.Set(CurrentSelectProjectPath, value ?? string.Empty);
                }
            }
        }

        public ModuleViewModelBase SelectedModule
        {
            get => _selectedModule;
            private set => SetProperty(ref _selectedModule, value);
        }

        private void OpenDirectory() => Process.Start("explorer.exe", ToolSetPath);

        /// <summary>
        /// 根据项目管理里的项目池刷新顶部下拉框，并解析当前选中项目：
        /// 优先保留配置里的 current_select_project；当它失效（已删除或为空）时回退到池子里第一个项目，池子为空则置空。
        /// </summary>
        private void SynchronizeProjects()
        {
            var saved = Config.Get(CurrentSelectProjectPath);

            _isSynchronizingProjects = true;
            try
            {
                Projects.Clear();
                foreach (var project in _projectManagement.Projects)
                {
                    Projects.Add(project.Name);
                }

                if (!string.IsNullOrEmpty(saved) && Projects.Contains(saved))
                {
                    SelectedProject = saved;
                }
                else if (Projects.Count > 0)
                {
                    SelectedProject = Projects[0];
                }
                else
                {
                    SelectedProject = null;
                }
            }
            finally
            {
                _isSynchronizingProjects = false;
            }

            var resolved = SelectedProject ?? string.Empty;
            if (resolved != saved)
            {
                Config.Set(CurrentSelectProjectPath, resolved);
            }
        }
    }
}
