using System.Windows;

namespace ToolSet.Views.ProjectManagement
{
    /// <summary>
    /// 项目卡片的「版本管理」弹窗：集中放该项目的 svn 操作。
    /// DataContext 是 <see cref="ToolSet.ViewModels.ProjectManagement.ProjectItemViewModel"/>，界面上直接绑它的命令。
    /// </summary>
    public partial class SvnManagementWindow : Window
    {
        public SvnManagementWindow()
        {
            InitializeComponent();
        }
    }
}
