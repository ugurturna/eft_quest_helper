using System.Text.Json;
using System.Text.Json.Serialization;
using TarkovQuestTracker.Core.Models;

namespace TarkovQuestTracker.Core.Services;

/// <summary>
/// Loads tasks_en.json (structure + English text), overlays the selected language's
/// tasks_{lang}.json and applies the game mode's overrides.
/// </summary>
public sealed class QuestRepository
{
    public static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        // enums as "Pvp"/"Pve" (numbers are still accepted when reading)
        Converters = { new JsonStringEnumConverter() },
    };

    public IReadOnlyList<Quest> Quests { get; private set; } = [];
    public IReadOnlyDictionary<string, Quest> ById { get; private set; } = new Dictionary<string, Quest>();
    /// <summary>Languages that have a tasks_{lang}.json file ("en" always first).</summary>
    public IReadOnlyList<string> Languages { get; private set; } = ["en"];
    public string Language { get; private set; } = "en";
    public GameMode Mode { get; private set; }

    public static QuestRepository Load(string dataDir, string language, GameMode mode, bool includePastEvents = false)
    {
        var repo = new QuestRepository { Mode = mode };
        var all = Read<List<Quest>>(Path.Combine(dataDir, "tasks_en.json")) ?? [];
        var modeKey = mode == GameMode.Pve ? "pve" : "pvp";

        var translations = new Dictionary<string, Dictionary<string, QuestTranslation>>();
        foreach (var file in Directory.GetFiles(dataDir, "tasks_*.json"))
        {
            var lang = Path.GetFileNameWithoutExtension(file)["tasks_".Length..];
            if (lang == "en") continue;
            var list = Read<List<QuestTranslation>>(file) ?? [];
            translations[lang] = list.Where(t => t.Id != "").ToDictionary(t => t.Id);
        }
        repo.Languages = ["en", .. translations.Keys.Order()];
        repo.Language = translations.ContainsKey(language) ? language : "en";
        translations.TryGetValue(repo.Language, out var current);

        var quests = all.Where(q => q.GameModes.Count == 0 || q.GameModes.Contains(modeKey))
                        .Where(q => includePastEvents || !q.IsPastEvent)
                        .ToList();
        var ids = quests.Select(q => q.Id).ToHashSet();
        foreach (var q in quests)
        {
            ApplyMode(q, modeKey);
            q.Prerequisites = q.Prerequisites.Where(ids.Contains).Distinct().ToList();

            q.AllNames = [q.Name];
            foreach (var t in translations.Values)
                if (t.TryGetValue(q.Id, out var tr) && !string.IsNullOrWhiteSpace(tr.Name))
                    q.AllNames.Add(tr.Name);

            QuestTranslation? cur = null;
            current?.TryGetValue(q.Id, out cur);
            q.DisplayName = string.IsNullOrWhiteSpace(cur?.Name) ? q.Name : cur.Name;
            q.DisplayNotes = string.IsNullOrWhiteSpace(cur?.Notes) ? q.Notes : cur.Notes;
            q.DisplayGuide = string.IsNullOrWhiteSpace(cur?.Guide) ? q.Guide : cur.Guide;
            var objTr = cur?.Objectives.ToDictionary(o => o.Id) ?? [];
            foreach (var o in q.Objectives)
            {
                objTr.TryGetValue(o.Id, out var tr);
                o.DisplayText = string.IsNullOrWhiteSpace(tr?.Text) ? o.Text : tr.Text;
                o.DisplayDetails = o.Details
                    .Select((d, i) => tr != null && i < tr.Details.Count && tr.Details[i] != "" ? tr.Details[i] : d)
                    .ToList();
            }
        }

        repo.Quests = quests;
        repo.ById = quests.ToDictionary(q => q.Id);
        return repo;
    }

    static void ApplyMode(Quest q, string modeKey)
    {
        q.ModeWarnings = [];
        if (modeKey != "pve") return;
        if (q.ModeNotes.TryGetValue("pve", out var notes)) q.ModeWarnings.AddRange(notes);
        if (!q.ModeOverrides.TryGetValue("pve", out var ov)) return;
        if (ov.MinLevel is { } lvl) q.MinLevel = lvl;
        if (ov.Prerequisites is { } pre) q.Prerequisites = pre;
        if (ov.KappaRequired is { } k) q.KappaRequired = k;
        if (ov.ObjectivesTarkovDev is { Count: > 0 } objs)
            q.ModeWarnings.Add("PvE objectives (tarkov.dev): " + string.Join(" · ", objs));
    }

    static T? Read<T>(string path) =>
        File.Exists(path) ? JsonSerializer.Deserialize<T>(File.ReadAllText(path), Json) : default;

    /// <summary>Finds the data folder next to the exe, or walks up from it (dev runs).</summary>
    public static string FindDataDir(string startDir)
    {
        for (var dir = new DirectoryInfo(startDir); dir != null; dir = dir.Parent)
        {
            var candidate = Path.Combine(dir.FullName, "data");
            if (File.Exists(Path.Combine(candidate, "tasks_en.json"))) return candidate;
        }
        throw new FileNotFoundException("data/tasks_en.json bulunamadı. Önce tools/scraper/scrape_wiki.py çalıştırın.");
    }
}
