using System.Windows.Input;
using ToolSet.Infrastructure;
using ToolSet.Infrastructure.Scripts;

namespace ToolSet.ViewModels.ProjectManagement
{
    /// <summary>
    /// 「Git 版本管理」弹窗自己的 ViewModel：只管一个项目的 git 操作。
    /// 项目信息由打开弹窗的一方按参数传进来，不依赖项目卡片的 ViewModel。
    /// </summary>
    public sealed class GitManagementViewModel : ViewModelBase
    {
        private readonly string _projectName;
        private readonly string _checkoutRootDir;
        private readonly string _github;

        /// <param name="projectName">项目名，操作对象是「检出根目录\项目名」。</param>
        /// <param name="checkoutRootDir">项目检出根目录。</param>
        /// <param name="github">项目 Github 地址，Clone 用。</param>
        public GitManagementViewModel(string projectName, string checkoutRootDir, string github)
        {
            _projectName = projectName;
            _checkoutRootDir = checkoutRootDir;
            _github = github;

            CloneCommand = new RelayCommand(Clone);
            PullCommand = new RelayCommand(Pull);
            PushCommand = new RelayCommand(Push);
        }

        public string ProjectName => _projectName;

        public string CheckoutRootDir => _checkoutRootDir;

        public string Github => _github;

        /// <summary>在「检出根目录」下检出「项目名」目录。</summary>
        public ICommand CloneCommand { get; }

        /// <summary>对「检出根目录/项目名」执行 git pull。</summary>
        public ICommand PullCommand { get; }

        /// <summary>对「检出根目录/项目名」执行 git push。</summary>
        public ICommand PushCommand { get; }

        private void Clone() => RunGit("clone");

        private void Pull() => RunGit("pull");

        private void Push() => RunGit("push");

        /// <summary>
        /// git 操作统一走 scripts/git_operate.bat，参数按「功能 检出根目录 项目名 Github地址」传。
        /// </summary>
        private void RunGit(string action)
            => AppScripts.Current.RunInConsole(
                AppScripts.GetScriptPath("git_operate.bat"),
                action,
                _checkoutRootDir,
                _projectName,
                _github);
    }
}
