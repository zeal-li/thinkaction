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

        private const string CreatedAtFormat = "yyyy-MM-dd HH:mm:ss";

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

            LoadProjects();
        }

        public ObservableCollection<ProjectItemViewModel> Projects { get; } = new ObservableCollection<ProjectItemViewModel>();

        public ICommand NewProjectCommand { get; }

        private void LoadProjects()
        {
            if (!_config.Contains(ProjectsSection))
            {
                return;
            }

            var projects = new List<ProjectItemViewModel>();
            foreach (var name in _config.GetKeys(ProjectsSection))
            {
                var createdAt = DateTime.ParseExact(
                    _config.Get(ProjectPath(name, CreatedAtKey)),
                    CreatedAtFormat,
                    CultureInfo.InvariantCulture);

                projects.Add(new ProjectItemViewModel(name, createdAt));
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

            var createdAt = DateTime.Now;
            _config.Set(ProjectPath(name, CreatedAtKey), createdAt.ToString(CreatedAtFormat, CultureInfo.InvariantCulture));

            InsertSorted(new ProjectItemViewModel(name, createdAt));
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
