using System.Windows.Controls;

namespace ToolSet.Views.ProjectHelper
{
    /// <summary>
    /// 「配置导出」卡片：界面只负责展示并转发命令，操作逻辑在 <see cref="ViewModels.ProjectHelper.ExportConfigViewModel"/> 里。
    /// </summary>
    public partial class ExportConfigView : UserControl
    {
        public ExportConfigView()
        {
            InitializeComponent();
        }
    }
}
