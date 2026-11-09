using System.IO;
using System.Windows;
using ToolSet.Infrastructure.Configuration;
using ToolSet.ViewModels;

namespace ToolSet
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // 首次启动生成空配置文件，便于手工编辑；已存在则保持原样，不重写
            if (!File.Exists(AppConfig.Current.FilePath))
            {
                AppConfig.Current.Save();
            }

            var window = new MainWindow
            {
                DataContext = new MainViewModel()
            };

            window.Show();
        }
    }
}
