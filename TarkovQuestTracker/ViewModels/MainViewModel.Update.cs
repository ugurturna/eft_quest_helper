using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TarkovQuestTracker.Core.Services;

namespace TarkovQuestTracker.ViewModels;

/// <summary>Self-update from the GitHub releases: check at start, banner, download + install + restart.</summary>
public sealed partial class MainViewModel
{
    /// <summary>A newer release on GitHub; null when up to date (or not checked yet).</summary>
    [ObservableProperty] private ReleaseInfo? _update;
    [ObservableProperty] private string _updateStatus = "";
    [ObservableProperty] private bool _isInstallingUpdate;

    public bool HasUpdate => Update != null;
    public string UpdateText => Update == null ? "" : Loc.F("u.available", Update.Tag, AppVersion);

    partial void OnUpdateChanged(ReleaseInfo? value)
    {
        OnPropertyChanged(nameof(HasUpdate));
        OnPropertyChanged(nameof(UpdateText));
    }

    /// <summary>Raised after the new files are in place; the app restarts into the new version.</summary>
    public event Action? RestartRequested;

    public static Version CurrentVersion => typeof(MainViewModel).Assembly.GetName().Version ?? new Version(0, 0, 0);

    /// <summary>
    /// False when running from the source folder (dotnet run / bin\Debug): installing there would overwrite the build output,
    /// so the update button opens the release page instead.
    /// </summary>
    public static bool CanSelfUpdate => WikiUpdater.Find(AppContext.BaseDirectory) == null;

    public bool CheckForUpdates
    {
        get => Settings.CheckForUpdates;
        set
        {
            if (Settings.CheckForUpdates == value) return;
            Settings.CheckForUpdates = value;
            SaveSettings();
            OnPropertyChanged();
        }
    }

    /// <summary>Start-up check (silent, only when enabled) or the Settings button (reports "up to date" / errors).</summary>
    public async Task CheckUpdatesAsync(bool manual)
    {
        if (!manual && !Settings.CheckForUpdates) return;
        if (manual) UpdateStatus = Loc.T("u.checking");
        try
        {
            Update = await AppUpdater.CheckAsync(CurrentVersion);
            if (manual) UpdateStatus = Update == null ? Loc.F("u.upToDate", AppVersion) : "";
        }
        catch (Exception e) when (AppUpdater.IsUpdateError(e))
        {
            if (manual) UpdateStatus = Loc.F("u.checkFailed", e.Message);
        }
    }

    [RelayCommand]
    Task CheckUpdates() => CheckUpdatesAsync(manual: true);

    [RelayCommand]
    async Task InstallUpdate()
    {
        if (Update is not { } release || IsInstallingUpdate) return;
        if (!CanSelfUpdate)
        {
            OpenUrl(release.PageUrl);
            return;
        }
        IsInstallingUpdate = true;
        var updater = new AppUpdater(AppContext.BaseDirectory);
        try
        {
            var progress = new Progress<double>(p => UpdateStatus = Loc.F("u.downloading", release.Tag, p));
            var root = await Task.Run(() => updater.DownloadAsync(release, progress));
            UpdateStatus = Loc.T("u.installing");
            await Task.Run(() => updater.Apply(root));
            SaveSettings();
            SaveProgress();
            RestartRequested?.Invoke();
        }
        catch (Exception e) when (AppUpdater.IsUpdateError(e))
        {
            UpdateStatus = Loc.F("u.failed", e.Message);
            IsInstallingUpdate = false;
        }
    }

    [RelayCommand]
    void OpenReleaseNotes()
    {
        if (Update != null) OpenUrl(Update.PageUrl);
    }

    [RelayCommand]
    void DismissUpdate()
    {
        Update = null;
        UpdateStatus = "";
    }

    static void OpenUrl(string url) => Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
}
