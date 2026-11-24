namespace TarkovQuestTracker.Core.Services;

/// <summary>
/// The parts of the RAM the memory cleaner (beta) can empty, the way Mem Reduct / ISLC do it. Stored in the settings
/// as a number and passed to the elevated helper process as one, so the values must never change.
/// </summary>
[Flags]
public enum MemoryAreas
{
    None = 0,
    /// <summary>Every process' working set to the page file: the game too, which then stutters while it reads it back.</summary>
    WorkingSets = 1,
    /// <summary>The system file cache's own working set.</summary>
    SystemFileCache = 2,
    /// <summary>Changed pages waiting to be written: written out, then free to reuse.</summary>
    ModifiedList = 4,
    /// <summary>The whole standby list (cached files and programs that are closed): the main cause of Tarkov's RAM stutters.</summary>
    StandbyList = 8,
    /// <summary>Only the low priority part of the standby list (the gentle option).</summary>
    StandbyListLowPriority = 16,
    /// <summary>Identical memory pages merged into one (Windows 8.1+).</summary>
    CombinePages = 32,

    All = WorkingSets | SystemFileCache | ModifiedList | StandbyList | StandbyListLowPriority | CombinePages,
    /// <summary>What a new profile cleans: safe while the game runs (no working sets, no page combining).</summary>
    Default = SystemFileCache | ModifiedList | StandbyList | StandbyListLowPriority,
}

/// <summary>When the memory cleaner's auto clean runs: RAM use over the player's limit, and not again too soon.</summary>
public static class MemoryCleanPolicy
{
    public const int MinPercent = 50, MaxPercent = 98, DefaultPercent = 85;
    /// <summary>Two cleans at least this far apart: cleaning right after a clean frees nothing and costs a stutter.</summary>
    public static readonly TimeSpan Cooldown = TimeSpan.FromMinutes(3);

    /// <summary>Areas from the settings or the helper's command line; unknown bits are dropped.</summary>
    public static MemoryAreas Sanitize(int value) => (MemoryAreas)value & MemoryAreas.All;

    public static bool ShouldAutoClean(int memoryLoadPercent, int limitPercent, DateTime now, DateTime? lastClean) =>
        memoryLoadPercent >= Math.Clamp(limitPercent, MinPercent, MaxPercent)
        && (lastClean is not { } last || now - last >= Cooldown);
}
