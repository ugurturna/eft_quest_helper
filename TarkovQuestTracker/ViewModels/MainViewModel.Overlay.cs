using CommunityToolkit.Mvvm.ComponentModel;

namespace TarkovQuestTracker.ViewModels;

/// <summary>Overlay settings: how many quests, done objectives, auto-hide.</summary>
public sealed partial class MainViewModel
{
    /// <summary>Choices for <see cref="OverlayMaxKey"/>; "0" = all.</summary>
    public IReadOnlyList<Option> OverlayMaxChoices { get; } =
        [Option.Fixed("5", "5"), Option.Fixed("8", "8"), Option.Fixed("10", "10"), Option.Fixed("15", "15"),
         Option.Fixed("20", "20"), Option.Localized("0", "s.ov.all")];

    public string OverlayMaxKey
    {
        get => Settings.OverlayMaxQuests.ToString();
        set
        {
            if (!int.TryParse(value, out var max) || max == Settings.OverlayMaxQuests) return;
            Settings.OverlayMaxQuests = Math.Max(0, max);
            SaveSettings();
            OnPropertyChanged();
            if (_repo != null) RefreshActive();
        }
    }

    /// <summary>Quests left off the overlay by <see cref="AppSettings.OverlayMaxQuests"/>.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasOverlayHidden), nameof(OverlayHiddenText))]
    private int _overlayHiddenCount;
    public bool HasOverlayHidden => OverlayHiddenCount > 0;
    public string OverlayHiddenText => Loc.F("ov.more", OverlayHiddenCount);

    public bool OverlayHideDoneObjectives
    {
        get => Settings.OverlayHideDoneObjectives;
        set
        {
            if (Settings.OverlayHideDoneObjectives == value) return;
            Settings.OverlayHideDoneObjectives = value;
            SaveSettings();
            OnPropertyChanged();
        }
    }

    public bool OverlayAutoHide
    {
        get => Settings.OverlayAutoHide;
        set
        {
            if (Settings.OverlayAutoHide == value) return;
            Settings.OverlayAutoHide = value;
            UpdateDetector();
            SaveSettings();
            OnPropertyChanged();
        }
    }

    public const int MinAutoHideSeconds = 3, MaxAutoHideSeconds = 30;

    public int OverlayAutoHideSeconds
    {
        get => Math.Clamp(Settings.OverlayAutoHideSeconds, MinAutoHideSeconds, MaxAutoHideSeconds);
        set
        {
            value = Math.Clamp(value, MinAutoHideSeconds, MaxAutoHideSeconds);
            if (Settings.OverlayAutoHideSeconds == value) return;
            Settings.OverlayAutoHideSeconds = value;
            SaveSettings();
            OnPropertyChanged();
        }
    }

    /// <summary>"O" was pressed in game (its clock or exfil list is on screen): an auto-hidden overlay shows for a while.</summary>
    public event Action? OverlayPeek;

    /// <summary>The top-right corner is read for the raid detection and for the auto-hide's "O".</summary>
    public bool ReadsScreen => Settings.AutoRaidDetect || Settings.OverlayAutoHide;

    void UpdateDetector()
    {
        Detector.Enabled = ReadsScreen;
        OnPropertyChanged(nameof(ReadsScreen));
    }

    /// <summary>After a profile's settings load: the overlay settings' bindings and the detector follow them.</summary>
    void ApplyOverlaySettings()
    {
        OnPropertyChanged(nameof(OverlayMaxKey));
        OnPropertyChanged(nameof(OverlayHideDoneObjectives));
        OnPropertyChanged(nameof(OverlayAutoHide));
        OnPropertyChanged(nameof(OverlayAutoHideSeconds));
        UpdateDetector();
    }
}
