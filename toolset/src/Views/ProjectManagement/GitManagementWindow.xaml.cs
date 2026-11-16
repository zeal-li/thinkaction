using System.Windows;

namespace ToolSet.Views.ProjectManagement
{
    /// <summary>
    /// 项目卡片的「版本管理」弹窗（Github 输入框后面那个）：集中放该项目的 git 操作。
    /// DataContext 是 <see cref="ToolSet.ViewModels.ProjectManagement.ProjectItemViewModel"/>，界面上直接绑它的命令。
    /// </summary>
    public partial class GitManagementWindow : Window
    {
        public GitManagementWindow()
        {
            InitializeComponent();
        }
    }
}
