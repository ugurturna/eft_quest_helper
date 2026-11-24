using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TarkovQuestTracker.Core.Services;
using TarkovQuestTracker.Interop;

namespace TarkovQuestTracker.ViewModels;

/// <summary>
/// RAM cleaner (beta): empties the standby list and the other areas the player picks, like Mem Reduct / ISLC. Off until
/// the player turns it on; a manual clean asks Windows for administrator each time, the auto clean needs the app itself
/// to run as administrator (a UAC prompt in the middle of a raid would be worse than the stutter).
/// </summary>
public sealed partial class MainViewModel
{
    DispatcherTimer? _memoryTimer;
    DateTime? _lastMemoryClean;

    public bool IsAdmin => MemoryCleaner.IsAdmin;
    public bool IsNotAdmin => !IsAdmin;

    public bool MemoryCleanEnabled
    {
        get => Settings.MemoryCleanEnabled;
        set
        {
            if (Settings.MemoryCleanEnabled == value) return;
            Settings.MemoryCleanEnabled = value;
            SaveSettings();
            OnPropertyChanged();
            CleanMemoryCommand.NotifyCanExecuteChanged();
        }
    }

    MemoryAreas CleanAreas => MemoryCleanPolicy.Sanitize(Settings.MemoryCleanAreas);

    bool HasArea(MemoryAreas area) => CleanAreas.HasFlag(area);

    void SetArea(MemoryAreas area, bool on, [System.Runtime.CompilerServices.CallerMemberName] string? name = null)
    {
        if (HasArea(area) == on) return;
        Settings.MemoryCleanAreas = (int)(on ? CleanAreas | area : CleanAreas & ~area);
        SaveSettings();
        OnPropertyChanged(name);
        CleanMemoryCommand.NotifyCanExecuteChanged();
    }

    public bool CleanStandby { get => HasArea(MemoryAreas.StandbyList); set => SetArea(MemoryAreas.StandbyList, value); }
    public bool CleanStandbyLow { get => HasArea(MemoryAreas.StandbyListLowPriority); set => SetArea(MemoryAreas.StandbyListLowPriority, value); }
    public bool CleanModified { get => HasArea(MemoryAreas.ModifiedList); set => SetArea(MemoryAreas.ModifiedList, value); }
    public bool CleanFileCache { get => HasArea(MemoryAreas.SystemFileCache); set => SetArea(MemoryAreas.SystemFileCache, value); }
    public bool CleanCombine { get => HasArea(MemoryAreas.CombinePages); set => SetArea(MemoryAreas.CombinePages, value); }
    public bool CleanWorkingSets { get => HasArea(MemoryAreas.WorkingSets); set => SetArea(MemoryAreas.WorkingSets, value); }

    public bool MemoryAutoClean
    {
        get => Settings.MemoryAutoClean;
        set
        {
            if (Settings.MemoryAutoClean == value) return;
            Settings.MemoryAutoClean = value;
            SaveSettings();
            OnPropertyChanged();
        }
    }

    public int MemoryAutoCleanPercent
    {
        get => Math.Clamp(Settings.MemoryAutoCleanPercent, MemoryCleanPolicy.MinPercent, MemoryCleanPolicy.MaxPercent);
        set
        {
            value = Math.Clamp(value, MemoryCleanPolicy.MinPercent, MemoryCleanPolicy.MaxPercent);
            if (Settings.MemoryAutoCleanPercent == value) return;
            Settings.MemoryAutoCleanPercent = value;
            SaveSettings();
            OnPropertyChanged();
        }
    }

    /// <summary>RAM use now, for the bar and the numbers on the page.</summary>
    [ObservableProperty] private int _memoryLoad;
    [ObservableProperty] private string _memoryUsedText = "";
    [ObservableProperty] private string _memoryCacheText = "";
    /// <summary>How the last clean went ("1.4 GB freed · 14:32").</summary>
    [ObservableProperty] private string _memoryCleanStatus = "";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CleanMemoryCommand))]
    private bool _isCleaningMemory;

    /// <summary>The RAM numbers every few seconds, and the auto clean when it is on.</summary>
    public void StartMemoryWatch()
    {
        if (_memoryTimer != null) return;
        _memoryTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
        _memoryTimer.Tick += (_, _) => OnMemoryTick();
        _memoryTimer.Start();
        OnMemoryTick();
    }

    void OnMemoryTick()
    {
        var now = MemoryCleaner.Snapshot();
        if (now.Total == 0) return;
        MemoryLoad = now.LoadPercent;
        MemoryUsedText = Loc.F("mem.used", Gb(now.Used), Gb(now.Total), now.LoadPercent);
        MemoryCacheText = Loc.F("mem.cache", Gb(now.SystemCache), Gb(now.Available));
        if (MemoryCleanEnabled && MemoryAutoClean && IsAdmin && !IsCleaningMemory && CleanAreas != MemoryAreas.None
            && MemoryCleanPolicy.ShouldAutoClean(now.LoadPercent, MemoryAutoCleanPercent, DateTime.UtcNow, _lastMemoryClean))
            _ = CleanMemoryAsync();
    }

    static string Gb(ulong bytes) => (bytes / 1073741824.0).ToString("0.0");

    bool CanCleanMemory() => MemoryCleanEnabled && !IsCleaningMemory && CleanAreas != MemoryAreas.None;

    [RelayCommand(CanExecute = nameof(CanCleanMemory))]
    async Task CleanMemoryAsync()
    {
        IsCleaningMemory = true;
        try
        {
            var areas = CleanAreas;
            var before = MemoryCleaner.Snapshot();
            MemoryAreas? failed = IsAdmin ? await Task.Run(() => MemoryCleaner.Clean(areas)) : await MemoryCleaner.CleanElevatedAsync(areas);
            if (failed is not { } refused)
            {
                MemoryCleanStatus = Loc.T("mem.cancelled");
                return;
            }
            _lastMemoryClean = DateTime.UtcNow;
            var after = MemoryCleaner.Snapshot();
            var freed = after.Available > before.Available ? after.Available - before.Available : 0;
            var time = DateTime.Now.ToString("HH:mm");
            MemoryCleanStatus = refused == MemoryAreas.None
                ? Loc.F("mem.done", Gb(freed), time)
                : refused == areas ? Loc.F("mem.failed", time) : Loc.F("mem.partial", Gb(freed), time);
            OnMemoryTick();
        }
        finally
        {
            IsCleaningMemory = false;
        }
    }

    /// <summary>The app asked to start again as administrator (Windows asks first; the new copy waits for this one to exit).</summary>
    public event Action? RestartAsAdminRequested;

    [RelayCommand]
    void RestartAsAdmin() => RestartAsAdminRequested?.Invoke();

    void ApplyMemorySettings()
    {
        OnPropertyChanged(nameof(MemoryCleanEnabled));
        OnPropertyChanged(nameof(CleanStandby));
        OnPropertyChanged(nameof(CleanStandbyLow));
        OnPropertyChanged(nameof(CleanModified));
        OnPropertyChanged(nameof(CleanFileCache));
        OnPropertyChanged(nameof(CleanCombine));
        OnPropertyChanged(nameof(CleanWorkingSets));
        OnPropertyChanged(nameof(MemoryAutoClean));
        OnPropertyChanged(nameof(MemoryAutoCleanPercent));
        CleanMemoryCommand.NotifyCanExecuteChanged();
    }
}
