using System.Windows;
using ToolSet.ViewModels.ProjectManagement;

namespace ToolSet.Views.ProjectManagement
{
    /// <summary>
    /// 「Svn 版本管理」弹窗：界面只负责展示并转发命令，操作逻辑在 <see cref="SvnManagementViewModel"/> 里。
    /// </summary>
    public partial class SvnManagementWindow : Window
    {
        public SvnManagementWindow(SvnManagementViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}
