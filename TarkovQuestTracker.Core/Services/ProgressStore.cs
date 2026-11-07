using System.Text.Json;
using TarkovQuestTracker.Core.Models;

namespace TarkovQuestTracker.Core.Services;

/// <summary>
/// Per-profile storage under %APPDATA%\TarkovQuestTracker:
/// <code>
/// app.json                          { "currentProfile": "Varsayılan" }
/// profiles\{name}\settings.json
/// profiles\{name}\progress_pvp.json
/// profiles\{name}\progress_pve.json
/// </code>
/// Files from the pre-profile layout (directly in the root) are moved into the default profile.
/// </summary>
public sealed class ProgressStore
{
    public const string DefaultProfile = "Varsayılan";

    sealed class AppState
    {
        public string CurrentProfile { get; set; } = DefaultProfile;
    }

    /// <summary>Overrides the storage folder (portable installs, isolated test runs).</summary>
    public const string HomeVariable = "TARKOV_TRACKER_HOME";

    public ProgressStore(string? folder = null)
    {
        var home = Environment.GetEnvironmentVariable(HomeVariable);
        if (!string.IsNullOrEmpty(folder))
        {
            Folder = folder;
        }
        else if (!string.IsNullOrEmpty(home))
        {
            Folder = home;
        }
        else
        {
            // Portable mode: if "data" directory or a "portable.txt" marker exists next to the exe, use it.
            var localData = Path.Combine(AppContext.BaseDirectory, "data");
            var portableMarker = Path.Combine(AppContext.BaseDirectory, "portable.txt");
            if (Directory.Exists(localData) || File.Exists(portableMarker))
            {
                Folder = Directory.Exists(localData) ? localData : AppContext.BaseDirectory;
            }
            else
            {
                Folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TarkovQuestTracker");
            }
        }
        MigrateLegacyFiles();
        var state = Read<AppState>(AppStatePath) ?? new AppState();
        Profile = ProfileExists(state.CurrentProfile) ? state.CurrentProfile : ListProfiles().FirstOrDefault() ?? DefaultProfile;
        Directory.CreateDirectory(ProfileFolder);
    }

    public string Folder { get; }
    public string Profile { get; private set; }
    public string ProfileFolder => ProfilePath(Profile);

    string ProfilesRoot => Path.Combine(Folder, "profiles");
    string AppStatePath => Path.Combine(Folder, "app.json");
    string ProfilePath(string name) => Path.Combine(ProfilesRoot, name);
    string ProgressPath(GameMode mode) => Path.Combine(ProfileFolder, $"progress_{mode.ToString().ToLowerInvariant()}.json");
    string SettingsPath => Path.Combine(ProfileFolder, "settings.json");


    public IReadOnlyList<string> ListProfiles() =>
        Directory.Exists(ProfilesRoot)
            ? Directory.GetDirectories(ProfilesRoot).Select(Path.GetFileName).OfType<string>()
                       .Order(StringComparer.CurrentCultureIgnoreCase).ToList()
            : [];

    bool ProfileExists(string name) => Directory.Exists(ProfilePath(name));

    /// <summary>Null when the name is usable, otherwise an error key (err.profile.*) for the UI string table.</summary>
    public string? ValidateName(string? name, string? ignore = null)
    {
        name = name?.Trim();
        if (string.IsNullOrEmpty(name)) return "err.profile.empty";
        if (name.Length > 40) return "err.profile.long";
        if (name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || name.EndsWith('.'))
            return "err.profile.chars";
        if (!string.Equals(name, ignore, StringComparison.CurrentCultureIgnoreCase)
            && ListProfiles().Any(p => string.Equals(p, name, StringComparison.CurrentCultureIgnoreCase)))
            return "err.profile.exists";
        return null;
    }

    public void SwitchProfile(string name)
    {
        if (!ProfileExists(name)) throw new DirectoryNotFoundException(name);
        Profile = name;
        Write(AppStatePath, new AppState { CurrentProfile = name });
    }

    /// <summary>Creates an empty profile; new profiles start with a copy of the current overlay/language settings.</summary>
    public void CreateProfile(string name)
    {
        name = name.Trim();
        if (ValidateName(name) is { } error) throw new ArgumentException(error);
        var settings = LoadSettings();
        Directory.CreateDirectory(ProfilePath(name));
        Write(Path.Combine(ProfilePath(name), "settings.json"), settings);
    }

    public void RenameProfile(string oldName, string newName)
    {
        newName = newName.Trim();
        if (ValidateName(newName, ignore: oldName) is { } error) throw new ArgumentException(error);
        if (oldName == newName) return;
        // case-only rename on Windows needs a temporary hop
        var tmp = ProfilePath(oldName) + ".renaming";
        Directory.Move(ProfilePath(oldName), tmp);
        Directory.Move(tmp, ProfilePath(newName));
        if (Profile == oldName) SwitchProfile(newName);
    }

    /// <summary>Deletes a profile (not the current one, and never the last one).</summary>
    public void DeleteProfile(string name)
    {
        if (name == Profile) throw new InvalidOperationException("Aktif profil silinemez; önce başka profile geç.");
        if (ListProfiles().Count <= 1) throw new InvalidOperationException("Son profil silinemez.");
        Directory.Delete(ProfilePath(name), recursive: true);
    }

    void MigrateLegacyFiles()
    {
        var legacy = new[] { "settings.json", "progress_pvp.json", "progress_pve.json" }
            .Select(f => Path.Combine(Folder, f)).Where(File.Exists).ToList();
        if (legacy.Count == 0 && Directory.Exists(ProfilesRoot)) return;
        var target = ProfilePath(DefaultProfile);
        Directory.CreateDirectory(target);
        foreach (var file in legacy)
        {
            var dest = Path.Combine(target, Path.GetFileName(file));
            if (!File.Exists(dest)) File.Move(file, dest);
        }
    }


    public Progress LoadProgress(GameMode mode) => Read<Progress>(ProgressPath(mode)) ?? new Progress();

    public void SaveProgress(GameMode mode, Progress progress) => Write(ProgressPath(mode), progress);

    public AppSettings LoadSettings() => Read<AppSettings>(SettingsPath) ?? new AppSettings();

    public void SaveSettings(AppSettings settings) => Write(SettingsPath, settings);

    public void Export(GameMode mode, string path)
    {
        if (!File.Exists(ProgressPath(mode))) SaveProgress(mode, new Progress());
        File.Copy(ProgressPath(mode), path, overwrite: true);
    }

    public Progress Import(GameMode mode, string path)
    {
        var p = Read<Progress>(path) ?? throw new InvalidDataException("Geçersiz progress dosyası.");
        SaveProgress(mode, p);
        return p;
    }

    static T? Read<T>(string path)
    {
        if (!File.Exists(path)) return default;
        try
        {
            return JsonSerializer.Deserialize<T>(File.ReadAllText(path), QuestRepository.Json);
        }
        catch (JsonException)
        {
            // keep the broken file for the user instead of silently losing progress
            File.Copy(path, path + ".broken", overwrite: true);
            return default;
        }
    }

    static void Write<T>(string path, T value)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var tmp = path + ".tmp";
        File.WriteAllText(tmp, JsonSerializer.Serialize(value, QuestRepository.Json));
        File.Move(tmp, path, overwrite: true);
    }
}
