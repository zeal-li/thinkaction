using System.Windows.Input;
using Microsoft.Win32;
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
        private string _github = string.Empty;
        private string _svn = string.Empty;

        public CreateProjectViewModel()
        {
            BrowseCheckoutRootDirCommand = new RelayCommand(BrowseCheckoutRootDir);
        }

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

        /// <summary>浏览选择检出根目录。</summary>
        public ICommand BrowseCheckoutRootDirCommand { get; }

        /// <summary>项目 Github 地址。</summary>
        public string Github
        {
            get => _github;
            set => SetProperty(ref _github, value);
        }

        /// <summary>项目 Svn 地址。</summary>
        public string Svn
        {
            get => _svn;
            set => SetProperty(ref _svn, value);
        }

        public bool HasProjectName => !string.IsNullOrWhiteSpace(ProjectName);

        private void BrowseCheckoutRootDir()
        {
            var dialog = new OpenFolderDialog
            {
                Title = "选择检出根目录",
            };

            if (dialog.ShowDialog() == true)
            {
                CheckoutRootDir = dialog.FolderName;
            }
        }
    }
}
