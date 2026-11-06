using System;
using System.Collections.ObjectModel;

namespace ToolSet.Infrastructure
{
    public sealed class LogEntry
    {
        public LogEntry(DateTime time, string message)
        {
            Time = time;
            Message = message;
        }

        public DateTime Time { get; }

        public string Message { get; }

        public string TimeText => Time.ToString("HH:mm:ss");
    }

    /// <summary>
    /// 全局运行日志。后续接入具体功能时，命令执行结果统一写入这里。
    /// </summary>
    public sealed class OutputLog
    {
        public ObservableCollection<LogEntry> Entries { get; } = new ObservableCollection<LogEntry>();

        public void Write(string message) => Entries.Add(new LogEntry(DateTime.Now, message));

        public void Clear() => Entries.Clear();
    }
}
