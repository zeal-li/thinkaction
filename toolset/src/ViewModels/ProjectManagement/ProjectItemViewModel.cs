using System;
using ToolSet.Infrastructure;

namespace ToolSet.ViewModels.ProjectManagement
{
    /// <summary>
    /// 项目管理功能区里的一张项目卡片，用于展示单个项目的配置。
    /// 后续新增配置项时，在这里加属性并在 ProjectManagementView 的卡片模板里补对应显示。
    /// </summary>
    public sealed class ProjectItemViewModel : ViewModelBase
    {
        public ProjectItemViewModel(string name, DateTime createdAt)
        {
            Name = name;
            CreatedAt = createdAt;
        }

        public string Name { get; }

        public DateTime CreatedAt { get; }

        public string CreatedAtText => $"创建于 {CreatedAt:yyyy-MM-dd HH:mm}";
    }
}
