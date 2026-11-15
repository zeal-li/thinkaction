using System;
using System.Globalization;
using System.Windows.Input;
using Microsoft.Win32;
using ToolSet.Infrastructure;
using ToolSet.Infrastructure.Configuration;

namespace ToolSet.ViewModels.ProjectManagement
{
    /// <summary>
    /// 项目管理功能区里的一张项目卡片，用于展示并编辑单个项目的配置。
    /// 后续新增配置项时，在这里加属性并在 ProjectManagementView 的卡片模板里补对应显示。
    /// </summary>
    public sealed class ProjectItemViewModel : ViewModelBase
    {
        /// <summary>创建时间在界面上的显示格式（配置里存的是 Unix 秒时间戳）。</summary>
        public const string CreatedAtDisplayFormat = "yyyy-MM-dd HH:mm:ss";

        /// <summary>所有项目所在的分组。</summary>
        public const string ProjectsSection = "项目管理";

        /// <summary>项目创建时间（本项目自己记录，不依赖文件时间）。</summary>
        public const string CreatedAtKey = "创建时间";

        /// <summary>项目检出根目录。</summary>
        public const string CheckoutRootDirKey = "检出根目录";

        /// <summary>项目 Github 地址。</summary>
        public const string GithubKey = "Github";

        /// <summary>项目 Svn 地址。</summary>
        public const string SvnKey = "Svn";

        private readonly IConfigService _config;
        private string _checkoutRootDir = string.Empty;
        private string _github = string.Empty;
        private string _svn = string.Empty;

        public ProjectItemViewModel(IConfigService config, string name, DateTime createdAt)
        {
            _config = config;
            Name = name;
            CreatedAt = createdAt;

            _checkoutRootDir = config.Get(ConfigPath(CheckoutRootDirKey));
            _github = config.Get(ConfigPath(GithubKey));
            _svn = config.Get(ConfigPath(SvnKey));

            BrowseCheckoutRootDirCommand = new RelayCommand(BrowseCheckoutRootDir);
        }

        public string Name { get; }

        public DateTime CreatedAt { get; }

        public string CreatedAtText => $"创建于 {CreatedAt.ToString(CreatedAtDisplayFormat, CultureInfo.InvariantCulture)}";

        /// <summary>浏览选择检出根目录。</summary>
        public ICommand BrowseCheckoutRootDirCommand { get; }

        /// <summary>项目检出根目录。</summary>
        public string CheckoutRootDir
        {
            get => _checkoutRootDir;
            set
            {
                if (SetProperty(ref _checkoutRootDir, value))
                {
                    _config.Set(ConfigPath(CheckoutRootDirKey), value);
                }
            }
        }

        /// <summary>项目 Github 地址。</summary>
        public string Github
        {
            get => _github;
            set
            {
                if (SetProperty(ref _github, value))
                {
                    _config.Set(ConfigPath(GithubKey), value);
                }
            }
        }

        /// <summary>项目 Svn 地址。</summary>
        public string Svn
        {
            get => _svn;
            set
            {
                if (SetProperty(ref _svn, value))
                {
                    _config.Set(ConfigPath(SvnKey), value);
                }
            }
        }

        private string ConfigPath(string key) => $"{ProjectsSection}/{Name}/{key}";

        private void BrowseCheckoutRootDir()
        {
            var dialog = new OpenFolderDialog
            {
                Title = "选择检出根目录",
            };

            if (dialog.ShowDialog() == true)
            {
                CheckoutRootDir = dialog.FolderName;
            }
        }
    }
}
