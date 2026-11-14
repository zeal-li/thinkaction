using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using ToolSet.Infrastructure;
using ToolSet.Infrastructure.Configuration;
using ToolSet.Views.ProjectManagement;

namespace ToolSet.ViewModels.ProjectManagement
{
    /// <summary>
    /// 项目管理模块：顶部「新建项目」入口 + 已创建项目的卡片列表。
    /// 项目存放在 config/config.json 的「项目管理/{项目名}」分组下，启动时读回。
    /// </summary>
    public sealed class ProjectManagementViewModel : ModuleViewModelBase
    {
        /// <summary>所有项目所在的分组。</summary>
        public const string ProjectsSection = "项目管理";

        /// <summary>项目创建时间（本项目自己记录，不依赖文件时间）。</summary>
        private const string CreatedAtKey = "创建时间";

        /// <summary>项目名排序：中文按拼音（zh-CN 排序规则），忽略大小写。</summary>
        private static readonly StringComparer NameComparer =
            StringComparer.Create(CultureInfo.GetCultureInfo("zh-CN"), ignoreCase: true);

        private readonly IConfigService _config;

        public ProjectManagementViewModel()
            : this(AppConfig.Current)
        {
        }

        public ProjectManagementViewModel(IConfigService config)
            : base(ProjectsSection)
        {
            _config = config;
            NewProjectCommand = new RelayCommand(NewProject);
            OpenProjectDirectoryCommand = new RelayCommand(OpenProjectDirectory);
            DeleteProjectCommand = new RelayCommand(DeleteProject);

            LoadProjects();
        }

        public ObservableCollection<ProjectItemViewModel> Projects { get; } = new ObservableCollection<ProjectItemViewModel>();

        public ICommand NewProjectCommand { get; }

        /// <summary>打开项目目录，功能待定。</summary>
        public ICommand OpenProjectDirectoryCommand { get; }

        /// <summary>删除项目（从 config.json 移除并刷新列表），参数为 <see cref="ProjectItemViewModel"/>。</summary>
        public ICommand DeleteProjectCommand { get; }

        private void LoadProjects()
        {
            if (!_config.Contains(ProjectsSection))
            {
                return;
            }

            var projects = new List<ProjectItemViewModel>();
            foreach (var name in _config.GetKeys(ProjectsSection))
            {
                projects.Add(new ProjectItemViewModel(name, ReadCreatedAt(name)));
            }

            // 全部读完再统一按名称排序后展示，与 config.json 里的书写顺序无关
            foreach (var project in projects.OrderBy(project => project.Name, NameComparer))
            {
                Projects.Add(project);
            }
        }

        private void NewProject()
        {
            var dialog = new CreateProjectWindow
            {
                Owner = Application.Current.MainWindow,
            };

            if (dialog.ShowDialog() != true)
            {
                return;
            }

            var name = dialog.ProjectName;
            if (name.Contains('/'))
            {
                // / 是配置路径的分隔符，不能出现在项目名里
                MessageBox.Show(
                    "项目名称不能包含 / 。",
                    "提示",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            if (_config.Contains(ProjectPath(name)))
            {
                MessageBox.Show(
                    $"项目「{name}」已存在。",
                    "提示",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            // 配置里只存 Unix 时间戳，显示时再转成本地时间格式
            var createdAt = DateTimeOffset.Now;
            _config.Set(
                ProjectPath(name, CreatedAtKey),
                createdAt.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture));

            InsertSorted(new ProjectItemViewModel(name, createdAt.LocalDateTime));
        }

        /// <summary>读取配置里的创建时间戳（Unix 秒）并转成本地时间。</summary>
        private DateTime ReadCreatedAt(string name)
        {
            var timestamp = long.Parse(
                _config.Get(ProjectPath(name, CreatedAtKey)),
                CultureInfo.InvariantCulture);

            return DateTimeOffset.FromUnixTimeSeconds(timestamp).LocalDateTime;
        }

        /// <summary>打开选中的项目目录，功能待定。</summary>
        private void OpenProjectDirectory(object? parameter)
        {
            // TODO: 项目的目录配置确定后在这里打开，参数是 ProjectItemViewModel
        }

        private void DeleteProject(object? parameter)
        {
            var project = (ProjectItemViewModel)parameter!;

            var result = MessageBox.Show(
                $"确定删除项目「{project.Name}」吗？删除后不可恢复。",
                "删除项目",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning,
                MessageBoxResult.No);

            if (result != MessageBoxResult.Yes)
            {
                return;
            }

            _config.Remove(ProjectPath(project.Name));
            Projects.Remove(project);
        }

        /// <summary>新建后按同样的名称规则插入到对应位置，刷新排序。</summary>
        private void InsertSorted(ProjectItemViewModel project)
        {
            var index = 0;
            while (index < Projects.Count
                   && NameComparer.Compare(Projects[index].Name, project.Name) < 0)
            {
                index++;
            }

            Projects.Insert(index, project);
        }

        /// <summary>某个项目分组的路径，如 <c>项目管理/demo</c>。</summary>
        private static string ProjectPath(string name) => $"{ProjectsSection}/{name}";

        /// <summary>某个项目下配置项的路径，如 <c>项目管理/demo/创建时间</c>。</summary>
        private static string ProjectPath(string name, string key) => $"{ProjectPath(name)}/{key}";
    }
}
