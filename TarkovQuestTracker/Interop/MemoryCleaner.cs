using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using TarkovQuestTracker.Core.Services;

namespace TarkovQuestTracker.Interop;

/// <summary>RAM in use right now (bytes).</summary>
public readonly record struct MemorySnapshot(ulong Total, ulong Available, ulong SystemCache, int LoadPercent)
{
    public ulong Used => Total - Available;
}

/// <summary>
/// RAM cleaner (beta), the same Windows calls Mem Reduct and ISLC make. They are system-wide: no handle to any other
/// process (the game included) is ever opened. Windows only lets an administrator make them, so an app started
/// normally runs them in a copy of itself started as administrator (<see cref="CleanElevatedAsync"/>, one UAC prompt).
/// </summary>
static class MemoryCleaner
{
    /// <summary>"--clean-memory &lt;areas&gt;": the elevated copy cleans, exits with the areas that failed, and shows no window.</summary>
    public const string CleanArg = "--clean-memory";

    const int SystemMemoryListInformation = 80, SystemCombinePhysicalMemoryInformation = 130;
    const int MemoryEmptyWorkingSets = 2, MemoryFlushModifiedList = 3, MemoryPurgeStandbyList = 4, MemoryPurgeLowPriorityStandbyList = 5;
    const uint TOKEN_ADJUST_PRIVILEGES = 0x20, TOKEN_QUERY = 0x8, SE_PRIVILEGE_ENABLED = 0x2;
    const int ERROR_NOT_ALL_ASSIGNED = 1300, ERROR_CANCELLED = 1223;

    [StructLayout(LayoutKind.Sequential)]
    struct MEMORYSTATUSEX
    {
        public uint dwLength, dwMemoryLoad;
        public ulong ullTotalPhys, ullAvailPhys, ullTotalPageFile, ullAvailPageFile, ullTotalVirtual, ullAvailVirtual, ullAvailExtendedVirtual;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct PERFORMANCE_INFORMATION
    {
        public uint cb;
        public UIntPtr CommitTotal, CommitLimit, CommitPeak, PhysicalTotal, PhysicalAvailable, SystemCache, KernelTotal, KernelPaged,
            KernelNonpaged, PageSize;
        public uint HandleCount, ProcessCount, ThreadCount;
    }

    // the LUID is two DWORDs: 4-byte packing keeps it right after the count
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    struct TOKEN_PRIVILEGES
    {
        public uint PrivilegeCount;
        public long Luid;
        public uint Attributes;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct MEMORY_COMBINE_INFORMATION_EX
    {
        public IntPtr Handle;
        public UIntPtr PagesCombined;
        public uint Flags;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX status);

    [DllImport("psapi.dll", SetLastError = true)]
    static extern bool GetPerformanceInfo(out PERFORMANCE_INFORMATION info, uint size);

    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool SetSystemFileCacheSize(IntPtr minimum, IntPtr maximum, uint flags);

    [DllImport("ntdll.dll")]
    static extern int NtSetSystemInformation(int infoClass, IntPtr info, int length);

    [DllImport("advapi32.dll", SetLastError = true)]
    static extern bool OpenProcessToken(IntPtr process, uint access, out IntPtr token);

    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    static extern bool LookupPrivilegeValue(string? system, string name, out long luid);

    [DllImport("advapi32.dll", SetLastError = true)]
    static extern bool AdjustTokenPrivileges(IntPtr token, bool disableAll, ref TOKEN_PRIVILEGES state, uint length, IntPtr previous, IntPtr returned);

    [DllImport("kernel32.dll")]
    static extern bool CloseHandle(IntPtr handle);

    /// <summary>The app runs as administrator: it can clean by itself (auto clean) without a UAC prompt.</summary>
    public static bool IsAdmin => Environment.IsPrivilegedProcess;

    public static MemorySnapshot Snapshot()
    {
        var status = new MEMORYSTATUSEX { dwLength = (uint)Marshal.SizeOf<MEMORYSTATUSEX>() };
        if (!GlobalMemoryStatusEx(ref status)) return default;
        ulong cache = 0;
        if (GetPerformanceInfo(out var perf, (uint)Marshal.SizeOf<PERFORMANCE_INFORMATION>()))
            cache = (ulong)perf.SystemCache * (ulong)perf.PageSize;
        return new(status.ullTotalPhys, status.ullAvailPhys, cache, (int)status.dwMemoryLoad);
    }

    /// <summary>Empties the areas asked for, in Mem Reduct's order; returns the ones Windows refused. Needs administrator.</summary>
    public static MemoryAreas Clean(MemoryAreas areas)
    {
        var failed = MemoryAreas.None;
        // SeProfileSingleProcess: the memory lists; SeIncreaseQuota: the working sets and the file cache
        if (!EnablePrivilege("SeProfileSingleProcessPrivilege") | !EnablePrivilege("SeIncreaseQuotaPrivilege")) return areas;

        if (areas.HasFlag(MemoryAreas.WorkingSets) && !MemoryListCommand(MemoryEmptyWorkingSets)) failed |= MemoryAreas.WorkingSets;
        // (SIZE_T)-1 for both: Windows empties the file cache's working set
        if (areas.HasFlag(MemoryAreas.SystemFileCache) && !SetSystemFileCacheSize(new IntPtr(-1), new IntPtr(-1), 0))
            failed |= MemoryAreas.SystemFileCache;
        if (areas.HasFlag(MemoryAreas.ModifiedList) && !MemoryListCommand(MemoryFlushModifiedList)) failed |= MemoryAreas.ModifiedList;
        if (areas.HasFlag(MemoryAreas.StandbyList) && !MemoryListCommand(MemoryPurgeStandbyList)) failed |= MemoryAreas.StandbyList;
        if (areas.HasFlag(MemoryAreas.StandbyListLowPriority) && !MemoryListCommand(MemoryPurgeLowPriorityStandbyList))
            failed |= MemoryAreas.StandbyListLowPriority;
        if (areas.HasFlag(MemoryAreas.CombinePages) && !CombinePages()) failed |= MemoryAreas.CombinePages;
        return failed;
    }

    /// <summary>
    /// Runs <see cref="Clean"/> in a copy of the app started as administrator (Windows asks first). Returns the areas
    /// that failed, or null when the player said no to the prompt.
    /// </summary>
    public static async Task<MemoryAreas?> CleanElevatedAsync(MemoryAreas areas)
    {
        Process? helper;
        try
        {
            helper = Process.Start(new ProcessStartInfo(Environment.ProcessPath!, $"{CleanArg} {(int)areas}")
            {
                UseShellExecute = true,
                Verb = "runas",
                WindowStyle = ProcessWindowStyle.Hidden,
            });
        }
        catch (Win32Exception e) when (e.NativeErrorCode == ERROR_CANCELLED)
        {
            return null;
        }
        if (helper == null) return areas;
        using (helper)
        {
            await helper.WaitForExitAsync();
            return MemoryCleanPolicy.Sanitize(helper.ExitCode);
        }
    }

    /// <summary>Starts the app again as administrator; false when the player said no to the prompt.</summary>
    public static bool RestartElevated(string args)
    {
        try
        {
            Process.Start(new ProcessStartInfo(Environment.ProcessPath!, args) { UseShellExecute = true, Verb = "runas" })?.Dispose();
            return true;
        }
        catch (Win32Exception e) when (e.NativeErrorCode == ERROR_CANCELLED)
        {
            return false;
        }
    }

    static bool MemoryListCommand(int command)
    {
        var buffer = Marshal.AllocHGlobal(sizeof(int));
        try
        {
            Marshal.WriteInt32(buffer, command);
            return NtSetSystemInformation(SystemMemoryListInformation, buffer, sizeof(int)) >= 0;
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    static bool CombinePages()
    {
        var size = Marshal.SizeOf<MEMORY_COMBINE_INFORMATION_EX>();
        var buffer = Marshal.AllocHGlobal(size);
        try
        {
            Marshal.StructureToPtr(new MEMORY_COMBINE_INFORMATION_EX(), buffer, false);
            return NtSetSystemInformation(SystemCombinePhysicalMemoryInformation, buffer, size) >= 0;
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    static bool EnablePrivilege(string name)
    {
        using var self = Process.GetCurrentProcess();
        if (!OpenProcessToken(self.Handle,TOKEN_ADJUST_PRIVILEGES | TOKEN_QUERY, out var token)) return false;
        try
        {
            if (!LookupPrivilegeValue(null, name, out var luid)) return false;
            var state = new TOKEN_PRIVILEGES { PrivilegeCount = 1, Luid = luid, Attributes = SE_PRIVILEGE_ENABLED };
            // succeeds even when the token doesn't hold the privilege: that is told by the last error
            return AdjustTokenPrivileges(token, false, ref state, 0, IntPtr.Zero, IntPtr.Zero)
                   && Marshal.GetLastWin32Error() != ERROR_NOT_ALL_ASSIGNED;
        }
        finally
        {
            CloseHandle(token);
        }
    }
}
