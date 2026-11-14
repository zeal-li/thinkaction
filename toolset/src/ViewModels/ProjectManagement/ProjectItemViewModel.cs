using System;
using System.Globalization;
using ToolSet.Infrastructure;

namespace ToolSet.ViewModels.ProjectManagement
{
    /// <summary>
    /// 项目管理功能区里的一张项目卡片，用于展示单个项目的配置。
    /// 后续新增配置项时，在这里加属性并在 ProjectManagementView 的卡片模板里补对应显示。
    /// </summary>
    public sealed class ProjectItemViewModel : ViewModelBase
    {
        /// <summary>创建时间在界面上的显示格式（配置里存的是 Unix 秒时间戳）。</summary>
        public const string CreatedAtDisplayFormat = "yyyy-MM-dd HH:mm:ss";

        public ProjectItemViewModel(string name, DateTime createdAt)
        {
            Name = name;
            CreatedAt = createdAt;
        }

        public string Name { get; }

        public DateTime CreatedAt { get; }

        public string CreatedAtText => $"创建于 {CreatedAt.ToString(CreatedAtDisplayFormat, CultureInfo.InvariantCulture)}";
    }
}
