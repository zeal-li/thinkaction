using ToolSet.Infrastructure;

namespace ToolSet.ViewModels
{
    /// <summary>
    /// 左侧导航中的一个功能模块。
    /// 新增模块：继承本类 -> 建对应 View -> 在 App.xaml 里登记 DataTemplate -> 在 MainViewModel 里加入 Modules。
    /// </summary>
    public abstract class ModuleViewModelBase : ViewModelBase
    {
        private bool _isSelected;

        protected ModuleViewModelBase(string title)
        {
            Title = title;
        }

        public string Title { get; }

        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }
    }
}
