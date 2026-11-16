using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using ToolSet.Infrastructure;
using ToolSet.Infrastructure.Configuration;
using ToolSet.Infrastructure.Scripts;
using ToolSet.Views.ProjectManagement;

namespace ToolSet.ViewModels.ProjectManagement
{
    /// <summary>
    /// 项目管理功能区里的一张项目卡片，用于展示并编辑单个项目的配置。
    /// 配置持久化在 config/config.json 的「项目管理/{项目名}」分组下，
    /// 修改后可取消（恢复为配置值）或确认（写回配置）。
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
        private readonly Dictionary<string, string> _savedValues = new Dictionary<string, string>();

        private string _checkoutRootDir = string.Empty;
        private string _github = string.Empty;
        private string _svn = string.Empty;
        private bool _hasPendingChanges;
        private bool _isLoading;

        public ProjectItemViewModel(IConfigService config, string name, DateTime createdAt)
        {
            _config = config;
            Name = name;
            CreatedAt = createdAt;

            CancelEditCommand = new RelayCommand(CancelEdit);
            ConfirmEditCommand = new RelayCommand(ConfirmEdit);
            BrowseCheckoutRootDirCommand = new RelayCommand(BrowseCheckoutRootDir);
            OpenSvnManagementCommand = new RelayCommand(OpenSvnManagement);
            GitCloneCommand = new RelayCommand(GitClone);
            GitPullCommand = new RelayCommand(GitPull);
            GitPushCommand = new RelayCommand(GitPush);
            SvnCheckoutCommand = new RelayCommand(SvnCheckout);
            SvnUpdateCommand = new RelayCommand(SvnUpdate);
            SvnCommitCommand = new RelayCommand(SvnCommit);
            SvnCleanupCommand = new RelayCommand(SvnCleanup);
            SvnShowLogCommand = new RelayCommand(SvnShowLog);

            LoadFromConfig();
        }

        public string Name { get; }

        public DateTime CreatedAt { get; }

        public string CreatedAtText => $"创建于 {CreatedAt.ToString(CreatedAtDisplayFormat, CultureInfo.InvariantCulture)}";

        public ICommand CancelEditCommand { get; }

        public ICommand ConfirmEditCommand { get; }

        /// <summary>浏览选择检出根目录。</summary>
        public ICommand BrowseCheckoutRootDirCommand { get; }

        /// <summary>打开「版本管理」弹窗，集中做该项目的 svn 操作。</summary>
        public ICommand OpenSvnManagementCommand { get; }

        /// <summary>在「检出根目录」下检出「项目名」目录，地址取当前填写的 Github。</summary>
        public ICommand GitCloneCommand { get; }

        /// <summary>对「检出根目录/项目名」执行 git pull。</summary>
        public ICommand GitPullCommand { get; }

        /// <summary>对「检出根目录/项目名」执行 git push。</summary>
        public ICommand GitPushCommand { get; }

        /// <summary>在「检出根目录」下检出「项目名」目录，地址取当前填写的 Svn。</summary>
        public ICommand SvnCheckoutCommand { get; }

        /// <summary>对「检出根目录/项目名」执行 svn update。</summary>
        public ICommand SvnUpdateCommand { get; }

        /// <summary>对「检出根目录/项目名」执行 svn commit。</summary>
        public ICommand SvnCommitCommand { get; }

        /// <summary>对「检出根目录/项目名」执行 svn cleanup。</summary>
        public ICommand SvnCleanupCommand { get; }

        /// <summary>查看「检出根目录/项目名」的 svn 日志。</summary>
        public ICommand SvnShowLogCommand { get; }

        /// <summary>输入框内容与配置不一致时为 true，用于显示「取消 / 确认」按钮。</summary>
        public bool HasPendingChanges
        {
            get => _hasPendingChanges;
            private set => SetProperty(ref _hasPendingChanges, value);
        }

        /// <summary>项目检出根目录。</summary>
        public string CheckoutRootDir
        {
            get => _checkoutRootDir;
            set
            {
                if (SetProperty(ref _checkoutRootDir, value))
                {
                    RefreshPendingChanges();
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
                    RefreshPendingChanges();
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
                    RefreshPendingChanges();
                }
            }
        }

        private void LoadFromConfig()
        {
            _isLoading = true;
            CheckoutRootDir = _config.Get(ConfigPath(CheckoutRootDirKey));
            Github = _config.Get(ConfigPath(GithubKey));
            Svn = _config.Get(ConfigPath(SvnKey));
            _isLoading = false;

            MarkAsSaved();
        }

        private void CancelEdit() => LoadFromConfig();

        private void ConfirmEdit()
        {
            _config.SetValues(SectionPath, new Dictionary<string, string>
            {
                [CheckoutRootDirKey] = CheckoutRootDir,
                [GithubKey] = Github,
                [SvnKey] = Svn,
            });

            MarkAsSaved();
        }

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

        private void OpenSvnManagement()
        {
            var window = new SvnManagementWindow
            {
                Owner = Application.Current.MainWindow,
                DataContext = this,
            };

            window.ShowDialog();
        }

        private void GitClone() => RunGit("clone");

        private void GitPull() => RunGit("pull");

        private void GitPush() => RunGit("push");

        /// <summary>
        /// git 操作统一走 scripts/git_operate.bat，参数按「功能 检出根目录 项目名 Github地址」传，
        /// 用的是输入框里的当前值。
        /// </summary>
        private void RunGit(string action)
            => AppScripts.Current.RunInConsole(
                AppScripts.GetScriptPath("git_operate.bat"),
                action,
                CheckoutRootDir,
                Name,
                Github);

        private void SvnCheckout() => RunSvn("checkout");

        private void SvnUpdate() => RunSvn("update");

        private void SvnCommit() => RunSvn("commit");

        private void SvnCleanup() => RunSvn("cleanup");

        private void SvnShowLog() => RunSvn("showlog");

        /// <summary>
        /// svn 操作统一走 scripts/svn_operate.bat（内部调用 TortoiseProc.exe 打开 TortoiseSVN 界面），
        /// 参数按「功能 检出根目录 项目名 Svn地址」传，用的是输入框里的当前值。
        /// </summary>
        private void RunSvn(string action)
            => AppScripts.Current.RunInConsole(
                AppScripts.GetScriptPath("svn_operate.bat"),
                action,
                CheckoutRootDir,
                Name,
                Svn);

        private void MarkAsSaved()
        {
            _savedValues[CheckoutRootDirKey] = CheckoutRootDir;
            _savedValues[GithubKey] = Github;
            _savedValues[SvnKey] = Svn;

            HasPendingChanges = false;
        }

        private void RefreshPendingChanges()
        {
            if (_isLoading)
            {
                return;
            }

            HasPendingChanges =
                CheckoutRootDir != _savedValues[CheckoutRootDirKey]
                || Github != _savedValues[GithubKey]
                || Svn != _savedValues[SvnKey];
        }

        private string SectionPath => $"{ProjectsSection}/{Name}";

        private string ConfigPath(string key) => $"{SectionPath}/{key}";
    }
}
