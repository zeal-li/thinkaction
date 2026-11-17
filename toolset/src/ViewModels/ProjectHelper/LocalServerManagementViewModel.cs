using System.Windows.Input;
using ToolSet.Infrastructure;

namespace ToolSet.ViewModels.ProjectHelper
{
    /// <summary>
    /// 「本地服管理」卡片：本地服相关操作的入口。
    /// 功能待定，当前只提供按钮外壳。
    /// </summary>
    public sealed class LocalServerManagementViewModel : ViewModelBase
    {
        public LocalServerManagementViewModel()
        {
            StartServerCommand = new RelayCommand(StartServer);
            StopServerCommand = new RelayCommand(StopServer);
            ViewCommand = new RelayCommand(View);
            PublishCommand = new RelayCommand(Publish);
        }

        public string Title => "本地服管理";

        /// <summary>开服（功能待定）。</summary>
        public ICommand StartServerCommand { get; }

        /// <summary>关服（功能待定）。</summary>
        public ICommand StopServerCommand { get; }

        /// <summary>查看（功能待定）。</summary>
        public ICommand ViewCommand { get; }

        /// <summary>发布（功能待定）。</summary>
        public ICommand PublishCommand { get; }

        private void StartServer()
        {
        }

        private void StopServer()
        {
        }

        private void View()
        {
        }

        private void Publish()
        {
        }
    }
}
