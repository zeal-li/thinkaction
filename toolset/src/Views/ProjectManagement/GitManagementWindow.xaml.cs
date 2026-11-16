using System.Windows;
using ToolSet.ViewModels.ProjectManagement;

namespace ToolSet.Views.ProjectManagement
{
    /// <summary>
    /// 「Git 版本管理」弹窗：界面只负责展示并转发命令，操作逻辑在 <see cref="GitManagementViewModel"/> 里。
    /// </summary>
    public partial class GitManagementWindow : Window
    {
        public GitManagementWindow(GitManagementViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}
