using ToolSet.Infrastructure;

namespace ToolSet.ViewModels.ProjectManagement
{
    /// <summary>
    /// 「新建项目」界面的 ViewModel。后续字段在这里续加。
    /// </summary>
    public sealed class CreateProjectViewModel : ViewModelBase
    {
        private string _projectName = string.Empty;
        private string _checkoutRootDir = string.Empty;

        public string ProjectName
        {
            get => _projectName;
            set
            {
                if (SetProperty(ref _projectName, value))
                {
                    OnPropertyChanged(nameof(HasProjectName));
                }
            }
        }

        /// <summary>项目检出的根目录。</summary>
        public string CheckoutRootDir
        {
            get => _checkoutRootDir;
            set => SetProperty(ref _checkoutRootDir, value);
        }

        public bool HasProjectName => !string.IsNullOrWhiteSpace(ProjectName);
    }
}
