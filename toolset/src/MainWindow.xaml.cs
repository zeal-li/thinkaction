using System;
using System.Windows;

namespace ToolSet
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            // 高分屏下按可用工作区收敛窗口大小，避免底部内容被裁掉
            var workArea = SystemParameters.WorkArea;
            MaxWidth = workArea.Width;
            MaxHeight = workArea.Height;
            Width = Math.Min(Width, workArea.Width);
            Height = Math.Min(Height, workArea.Height);
        }
    }
}
