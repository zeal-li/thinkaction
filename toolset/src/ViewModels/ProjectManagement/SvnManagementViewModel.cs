using System.Windows.Input;
using ToolSet.Infrastructure;
using ToolSet.Infrastructure.Scripts;

namespace ToolSet.ViewModels.ProjectManagement
{
    /// <summary>
    /// 「Svn 版本管理」弹窗自己的 ViewModel：只管一个项目的 svn 操作。
    /// 项目信息由打开弹窗的一方按参数传进来，不依赖项目卡片的 ViewModel。
    /// </summary>
    public sealed class SvnManagementViewModel : ViewModelBase
    {
        private readonly string _projectName;
        private readonly string _checkoutRootDir;
        private readonly string _svn;

        /// <param name="projectName">项目名，操作对象是「检出根目录\项目名」。</param>
        /// <param name="checkoutRootDir">项目检出根目录。</param>
        /// <param name="svn">项目 Svn 地址，Checkout 用。</param>
        public SvnManagementViewModel(string projectName, string checkoutRootDir, string svn)
        {
            _projectName = projectName;
            _checkoutRootDir = checkoutRootDir;
            _svn = svn;

            CheckoutCommand = new RelayCommand(Checkout);
            UpdateCommand = new RelayCommand(Update);
            CommitCommand = new RelayCommand(Commit);
            CleanupCommand = new RelayCommand(Cleanup);
            ShowLogCommand = new RelayCommand(ShowLog);
        }

        public string ProjectName => _projectName;

        public string CheckoutRootDir => _checkoutRootDir;

        public string Svn => _svn;

        /// <summary>在「检出根目录」下检出「项目名」目录。</summary>
        public ICommand CheckoutCommand { get; }

        /// <summary>对「检出根目录/项目名」执行 svn update。</summary>
        public ICommand UpdateCommand { get; }

        /// <summary>对「检出根目录/项目名」执行 svn commit。</summary>
        public ICommand CommitCommand { get; }

        /// <summary>对「检出根目录/项目名」执行 svn cleanup。</summary>
        public ICommand CleanupCommand { get; }

        /// <summary>查看「检出根目录/项目名」的 svn 日志。</summary>
        public ICommand ShowLogCommand { get; }

        private void Checkout() => RunSvn("checkout");

        private void Update() => RunSvn("update");

        private void Commit() => RunSvn("commit");

        private void Cleanup() => RunSvn("cleanup");

        private void ShowLog() => RunSvn("showlog");

        /// <summary>
        /// svn 操作统一走 scripts/svn_operate.bat（内部调用 TortoiseProc.exe 打开 TortoiseSVN 界面），
        /// 参数按「功能 检出根目录 项目名 Svn地址」传。
        /// </summary>
        private void RunSvn(string action)
            => AppScripts.Current.RunInConsole(
                AppScripts.GetScriptPath("svn_operate.bat"),
                action,
                _checkoutRootDir,
                _projectName,
                _svn);
    }
}
