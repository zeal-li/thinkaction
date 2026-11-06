using System;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Threading;
using ToolSet.ViewModels;

namespace ToolSet
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;

            // 高分屏下按可用工作区收敛窗口大小，避免底部内容被裁掉
            var workArea = SystemParameters.WorkArea;
            MaxWidth = workArea.Width;
            MaxHeight = workArea.Height;
            Width = Math.Min(Width, workArea.Width);
            Height = Math.Min(Height, workArea.Height);
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            // DataContext 切换时先解绑旧的日志集合，避免重复订阅
            if (e.OldValue is MainViewModel oldViewModel)
            {
                oldViewModel.Log.Entries.CollectionChanged -= OnLogEntriesChanged;
            }

            if (e.NewValue is MainViewModel newViewModel)
            {
                newViewModel.Log.Entries.CollectionChanged += OnLogEntriesChanged;
            }
        }

        private void OnLogEntriesChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            // 等布局完成后再滚动，否则新增项尚未生成容器
            Dispatcher.BeginInvoke(new Action(ScrollLogToEnd), DispatcherPriority.Loaded);
        }

        private void ScrollLogToEnd()
        {
            if (LogList.Items.Count > 0)
            {
                LogList.ScrollIntoView(LogList.Items[LogList.Items.Count - 1]);
            }
        }
    }
}
