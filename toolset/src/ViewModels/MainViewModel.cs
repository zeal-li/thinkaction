using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using ToolSet.Infrastructure;
using ToolSet.Infrastructure.Configuration;
using ToolSet.ViewModels.EnvironmentDeploy;
using ToolSet.ViewModels.ProjectHelper;
using ToolSet.ViewModels.ProjectManagement;
using ToolSet.Views.ProjectManagement;

namespace ToolSet.ViewModels
{
    public sealed class MainViewModel : ViewModelBase, IShell
    {
        /// <summary>主界面相关配置所在的分组。</summary>
        public const string MainSection = "主界面";

        /// <summary>当前选中项目的配置键。</summary>
        public const string CurrentSelectProjectKey = "current_select_project";

        private static string CurrentSelectProjectPath => $"{MainSection}/{CurrentSelectProjectKey}";

        private readonly ProjectManagementViewModel _projectManagement;
        private readonly RelayCommand _openGitManagementCommand;
        private readonly RelayCommand _openSvnManagementCommand;
        private ModuleViewModelBase _selectedModule = null!;
        private string? _selectedProject;
        private bool _isSynchronizingProjects;

        public MainViewModel()
        {
            OpenDirectoryCommand = new RelayCommand(OpenDirectory);

            _openGitManagementCommand = new RelayCommand(OpenGitManagement, HasSelectedProject);
            _openSvnManagementCommand = new RelayCommand(OpenSvnManagement, HasSelectedProject);
            OpenGitManagementCommand = _openGitManagementCommand;
            OpenSvnManagementCommand = _openSvnManagementCommand;

            Projects = new ObservableCollection<string>();

            _projectManagement = new ProjectManagementViewModel();
            // 项目增删后刷新顶部下拉框，并保持 current_select_project 始终有效
            _projectManagement.Projects.CollectionChanged += (_, _) => SynchronizeProjects();

            Modules = new ObservableCollection<ModuleViewModelBase>
            {
                new EnvironmentDeployViewModel(),
                _projectManagement,
                new ProjectHelperViewModel(),
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

        /// <summary>打开当前选中项目的「Git 版本管理」弹窗。</summary>
        public ICommand OpenGitManagementCommand { get; }

        /// <summary>打开当前选中项目的「Svn 版本管理」弹窗。</summary>
        public ICommand OpenSvnManagementCommand { get; }

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

                RaiseVersionCommandStates();
            }
        }

        public ModuleViewModelBase SelectedModule
        {
            get => _selectedModule;
            private set => SetProperty(ref _selectedModule, value);
        }

        /// <summary>打开当前选中项目「检出根目录/项目名」目录。</summary>
        private void OpenDirectory()
        {
            var project = SelectedProjectItem;
            if (project is null)
            {
                MessageBox.Show(
                    "当前没有可选项目。",
                    "提示",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            var path = Path.Combine(project.CheckoutRootDir, project.Name);
            if (!Directory.Exists(path))
            {
                MessageBox.Show(
                    "项目目录不存在。",
                    "提示",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            Process.Start("explorer.exe", path);
        }

        /// <summary>打开当前选中项目的「Git 版本管理」弹窗。</summary>
        private void OpenGitManagement()
        {
            var project = SelectedProjectItem!;
            ShowVersionManagementWindow(new GitManagementWindow(
                new GitManagementViewModel(project.Name, project.CheckoutRootDir, project.Github)));
        }

        /// <summary>打开当前选中项目的「Svn 版本管理」弹窗。</summary>
        private void OpenSvnManagement()
        {
            var project = SelectedProjectItem!;
            ShowVersionManagementWindow(new SvnManagementWindow(
                new SvnManagementViewModel(project.Name, project.CheckoutRootDir, project.Svn)));
        }

        /// <summary>版本管理弹窗都是模态的，挂在主窗口上。</summary>
        private static void ShowVersionManagementWindow(Window window)
        {
            window.Owner = Application.Current.MainWindow;
            window.ShowDialog();
        }

        /// <summary>当前选中项目对应的卡片 ViewModel，没有选中项目时为 null。</summary>
        private ProjectItemViewModel? SelectedProjectItem
            => _projectManagement.Projects.FirstOrDefault(project => project.Name == SelectedProject);

        private bool HasSelectedProject() => SelectedProjectItem is not null;

        /// <summary>Git / Svn 按钮要跟着选中项目一起变可用状态。</summary>
        private void RaiseVersionCommandStates()
        {
            _openGitManagementCommand.RaiseCanExecuteChanged();
            _openSvnManagementCommand.RaiseCanExecuteChanged();
        }

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
