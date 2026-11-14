using ToolSet.Infrastructure;

namespace ToolSet.ViewModels.ProjectManagement
{
    /// <summary>
    /// 「新建项目」界面的 ViewModel。当前只有项目名称，后续字段在这里续加。
    /// </summary>
    public sealed class CreateProjectViewModel : ViewModelBase
    {
        private string _projectName = string.Empty;

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

        public bool HasProjectName => !string.IsNullOrWhiteSpace(ProjectName);
    }
}
