using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows.Input;
using ToolSet.Infrastructure;
using ToolSet.Infrastructure.Scripts;

namespace ToolSet.ViewModels.ProjectManagement
{
    /// <summary>
    /// 「Svn 版本管理」弹窗自己的 ViewModel：只管一个项目的 svn 操作。
    /// 项目信息由打开弹窗的一方按参数传进来，不依赖项目卡片的 ViewModel。
    /// 弹窗里列出「检出根目录\项目名」下的直接子目录，勾选的目录才参与更新 / 提交 / 清理 / 查看日志。
    /// </summary>
    public sealed class SvnManagementViewModel : ViewModelBase
    {
        /// <summary>SVN 自己的工作副本元数据目录，不是项目目录，不列出来。</summary>
        private const string SvnMetaDirName = ".svn";

        /// <summary>TortoiseProc 一次处理多个路径时的分隔符：Windows 路径里不会出现 *。</summary>
        private const string MultiPathSeparator = "*";

        private readonly string _projectName;
        private readonly string _checkoutRootDir;
        private readonly string _svn;

        /// <summary>依赖勾选目录的命令，勾选状态变化时要刷新可用状态。</summary>
        private readonly List<RelayCommand> _targetCommands = new List<RelayCommand>();

        /// <param name="projectName">项目名，操作对象是「检出根目录\项目名」。</param>
        /// <param name="checkoutRootDir">项目检出根目录。</param>
        /// <param name="svn">项目 Svn 地址，检出用。</param>
        public SvnManagementViewModel(string projectName, string checkoutRootDir, string svn)
        {
            _projectName = projectName;
            _checkoutRootDir = checkoutRootDir;
            _svn = svn;

            Targets = new ObservableCollection<SvnTarget>(
                LoadTargetNames().Select(name => new SvnTarget(name)));

            foreach (var target in Targets)
            {
                target.PropertyChanged += OnTargetPropertyChanged;
            }

            CheckoutCommand = new RelayCommand(Checkout);

            var updateCommand = new RelayCommand(Update, HasCheckedTarget);
            var commitCommand = new RelayCommand(Commit, HasCheckedTarget);
            var cleanupCommand = new RelayCommand(Cleanup, HasCheckedTarget);
            var showLogCommand = new RelayCommand(ShowLog, HasCheckedTarget);

            UpdateCommand = updateCommand;
            CommitCommand = commitCommand;
            CleanupCommand = cleanupCommand;
            ShowLogCommand = showLogCommand;

            _targetCommands.Add(updateCommand);
            _targetCommands.Add(commitCommand);
            _targetCommands.Add(cleanupCommand);
            _targetCommands.Add(showLogCommand);
        }

        public string ProjectName => _projectName;

        public string CheckoutRootDir => _checkoutRootDir;

        public string Svn => _svn;

        /// <summary>「检出根目录\项目名」下的直接子目录，勾选的才参与 svn 操作。</summary>
        public ObservableCollection<SvnTarget> Targets { get; }

        /// <summary>把 Svn 地址检出到「检出根目录\项目名」，按钮在项目名后面。</summary>
        public ICommand CheckoutCommand { get; }

        /// <summary>对勾选的目录执行 svn update，全部路径一次操作。</summary>
        public ICommand UpdateCommand { get; }

        /// <summary>对勾选的目录执行 svn commit，全部路径一次操作。</summary>
        public ICommand CommitCommand { get; }

        /// <summary>对勾选的目录逐个执行 svn cleanup。</summary>
        public ICommand CleanupCommand { get; }

        /// <summary>逐个查看勾选目录的 svn 日志。</summary>
        public ICommand ShowLogCommand { get; }

        private string ProjectDir => Path.Combine(_checkoutRootDir, _projectName);

        /// <summary>勾选的目录的完整路径。</summary>
        private IEnumerable<string> CheckedPaths
            => Targets.Where(target => target.IsChecked)
                .Select(target => Path.Combine(ProjectDir, target.Name));

        private void Checkout() => RunSvn("checkout", ProjectDir, _svn);

        /// <summary>更新：勾选的目录拼成一次操作，只弹一个 TortoiseSVN 窗口。</summary>
        private void Update() => RunSvn("update", string.Join(MultiPathSeparator, CheckedPaths));

        /// <summary>提交：勾选的目录拼成一次操作，只弹一个 TortoiseSVN 窗口。</summary>
        private void Commit() => RunSvn("commit", string.Join(MultiPathSeparator, CheckedPaths));

        /// <summary>清理：每个勾选的目录单独调用一次脚本，各弹一个窗口。</summary>
        private void Cleanup() => RunEachPath("cleanup");

        /// <summary>查看日志：每个勾选的目录单独调用一次脚本，各弹一个窗口。</summary>
        private void ShowLog() => RunEachPath("showlog");

        private void RunEachPath(string action)
        {
            foreach (var path in CheckedPaths)
            {
                RunSvn(action, path);
            }
        }

        private bool HasCheckedTarget() => Targets.Any(target => target.IsChecked);

        private void OnTargetPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(SvnTarget.IsChecked))
            {
                return;
            }

            foreach (var command in _targetCommands)
            {
                command.RaiseCanExecuteChanged();
            }
        }

        /// <summary>
        /// 项目目录还没检出时没有可勾选的目录，列表为空。
        /// </summary>
        private IEnumerable<string> LoadTargetNames()
        {
            if (!Directory.Exists(ProjectDir))
            {
                return Enumerable.Empty<string>();
            }

            return Directory.EnumerateDirectories(ProjectDir)
                .Select(path => Path.GetFileName(path)!)
                .Where(name => !string.Equals(name, SvnMetaDirName, StringComparison.OrdinalIgnoreCase))
                .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        /// <summary>
        /// svn 操作统一走 scripts/svn_operate.bat，参数按「功能 路径 [Svn地址]」传，
        /// 路径由这里拼好，脚本只负责调 TortoiseProc.exe 打开对应的界面。
        /// </summary>
        private void RunSvn(string action, string path)
            => AppScripts.Current.RunInConsole(
                AppScripts.GetScriptPath("svn_operate.bat"),
                action,
                path);

        /// <summary>checkout 要把 Svn 地址一起交给脚本。</summary>
        private void RunSvn(string action, string path, string svnUrl)
            => AppScripts.Current.RunInConsole(
                AppScripts.GetScriptPath("svn_operate.bat"),
                action,
                path,
                svnUrl);

        /// <summary>「操作目录」列表里的一个勾选项。</summary>
        public sealed class SvnTarget : ViewModelBase
        {
            private bool _isChecked;

            /// <param name="name">目录名，相对「检出根目录\项目名」。</param>
            public SvnTarget(string name)
            {
                Name = name;
            }

            /// <summary>目录名，相对「检出根目录\项目名」。</summary>
            public string Name { get; }

            /// <summary>是否参与 svn 操作。</summary>
            public bool IsChecked
            {
                get => _isChecked;
                set => SetProperty(ref _isChecked, value);
            }
        }
    }
}
