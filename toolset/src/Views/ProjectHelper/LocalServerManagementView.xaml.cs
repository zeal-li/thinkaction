using System.Windows.Controls;

namespace ToolSet.Views.ProjectHelper
{
    /// <summary>
    /// 「本地服管理」卡片：界面只负责展示并转发命令，操作逻辑在 <see cref="ViewModels.ProjectHelper.LocalServerManagementViewModel"/> 里。
    /// </summary>
    public partial class LocalServerManagementView : UserControl
    {
        public LocalServerManagementView()
        {
            InitializeComponent();
        }
    }
}
