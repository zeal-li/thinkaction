namespace ToolSet.ViewModels
{
    /// <summary>
    /// 尚未实现的模块占位页。
    /// </summary>
    public sealed class PlaceholderModuleViewModel : ModuleViewModelBase
    {
        public PlaceholderModuleViewModel(string title, string description)
            : base(title)
        {
            Description = description;
        }

        public string Description { get; }
    }
}
