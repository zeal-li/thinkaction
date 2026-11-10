using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace ToolSet.Infrastructure.Scripts
{
    /// <summary>
    /// 把控制台进程挂到一个「句柄关闭即终止」的 Job 上。
    /// 本进程退出（包括被任务管理器强杀、崩溃）时 job 句柄关闭，job 内所有进程
    /// （cmd 以及它派生出的 ssh 等子进程）一起结束，不会留下孤儿进程占住目录。
    /// </summary>
    internal sealed class ConsoleProcessJob
    {
        private const int ExtendedLimitInformationClass = 9;
        private const uint JobObjectLimitKillOnJobClose = 0x2000;

        private readonly IntPtr _handle = CreateJobObject(IntPtr.Zero, null);

        public ConsoleProcessJob()
        {
            var info = new JobObjectExtendedLimitInformation();
            info.BasicLimitInformation.LimitFlags = JobObjectLimitKillOnJobClose;
            SetInformationJobObject(_handle, ExtendedLimitInformationClass, ref info, (uint)Marshal.SizeOf(info));
        }

        /// <summary>把已启动的进程加进 job，它之后再派生的子进程会自动继承。</summary>
        public void Add(Process process) => AssignProcessToJobObject(_handle, process.Handle);

        [StructLayout(LayoutKind.Sequential)]
        private struct JobObjectBasicLimitInformation
        {
            public long PerProcessUserTimeLimit;
            public long PerJobUserTimeLimit;
            public uint LimitFlags;
            public UIntPtr MinimumWorkingSetSize;
            public UIntPtr MaximumWorkingSetSize;
            public uint ActiveProcessLimit;
            public UIntPtr Affinity;
            public uint PriorityClass;
            public uint SchedulingClass;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct IoCounters
        {
            public ulong ReadOperationCount;
            public ulong WriteOperationCount;
            public ulong OtherOperationCount;
            public ulong ReadTransferCount;
            public ulong WriteTransferCount;
            public ulong OtherTransferCount;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct JobObjectExtendedLimitInformation
        {
            public JobObjectBasicLimitInformation BasicLimitInformation;
            public IoCounters IoInfo;
            public UIntPtr ProcessMemoryLimit;
            public UIntPtr JobMemoryLimit;
            public UIntPtr PeakProcessMemoryUsed;
            public UIntPtr PeakJobMemoryUsed;
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr CreateJobObject(IntPtr lpJobAttributes, string? lpName);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetInformationJobObject(
            IntPtr hJob, int jobObjectInformationClass, ref JobObjectExtendedLimitInformation lpJobObjectInformation, uint cbJobObjectInformationLength);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool AssignProcessToJobObject(IntPtr hJob, IntPtr hProcess);
    }
}
