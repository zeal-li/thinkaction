using System.Windows;
using ToolSet.ViewModels.ProjectManagement;

namespace ToolSet.Views.ProjectManagement
{
    public partial class CreateProjectWindow : Window
    {
        private readonly CreateProjectViewModel _viewModel = new CreateProjectViewModel();

        public CreateProjectWindow()
        {
            InitializeComponent();
            DataContext = _viewModel;
            Loaded += (_, _) => ProjectNameBox.Focus();
        }

        /// <summary>用户填写的项目名称。</summary>
        public string ProjectName => _viewModel.ProjectName.Trim();

        private void Create_Click(object sender, RoutedEventArgs e) => DialogResult = true;
    }
}
