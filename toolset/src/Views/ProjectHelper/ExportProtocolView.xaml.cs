using System.Windows.Controls;

namespace ToolSet.Views.ProjectHelper
{
    /// <summary>
    /// 「协议导出」卡片：界面只负责展示并转发命令，操作逻辑在 <see cref="ViewModels.ProjectHelper.ExportProtocolViewModel"/> 里。
    /// </summary>
    public partial class ExportProtocolView : UserControl
    {
        public ExportProtocolView()
        {
            InitializeComponent();
        }
    }
}
