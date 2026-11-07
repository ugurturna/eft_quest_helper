using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using TarkovQuestTracker.Core.Models;

namespace TarkovQuestTracker.Core.Services;

/// <summary>
/// Loads tasks_en.json (structure + English text), overlays the selected language's
/// tasks_{lang}.json and applies the game mode's overrides.
/// </summary>
public sealed partial class QuestRepository
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

    /// <summary>Past (historical) events are never loaded: they can't be done any more.</summary>
    public static QuestRepository Load(string dataDir, string language, GameMode mode)
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
                        .Where(q => !q.IsPastEvent)
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

        LoadItemNeeds(dataDir, quests, modeKey, repo.Language);
        repo.Quests = quests;
        repo.ById = quests.ToDictionary(q => q.Id);
        return repo;
    }

    /// <summary>The text itself says how many: "Hand over 5 items", "Hand over the 2 LEDX", "Find 3 ... in raid".</summary>
    [GeneratedRegex(@"^\s*(hand over|find|stash|plant|hide|place|install|mark|obtain)\s+(the\s+|any\s+)?\d[\d,]*\s", RegexOptions.IgnoreCase)]
    private static partial Regex StatedAmount();

    static void LoadItemNeeds(string dataDir, List<Quest> quests, string modeKey, string language)
    {
        var data = Read<QuestItemData>(Path.Combine(dataDir, "quest_items.json"));
        if (data == null || !data.Modes.TryGetValue(modeKey, out var byQuest)) return;
        var iconDir = Path.Combine(dataDir, "icons");
        string Pick(Dictionary<string, string>? names, string id) =>
            names?.GetValueOrDefault(language) ?? names?.GetValueOrDefault("en") ?? id;

        ItemNeed Need(ItemObjective o)
        {
            var id = o.Items[0];
            var info = data.Items.GetValueOrDefault(id);
            var icon = Path.Combine(iconDir, id + ".png");
            return new ItemNeed(id, Pick(info?.Name, id), Pick(info?.Short, id), o.Count, o.Fir,
                o.Items.Skip(1).Select(a => Pick(data.Items.GetValueOrDefault(a)?.Name, a)).ToList(),
                File.Exists(icon) ? icon : null);
        }

        foreach (var q in quests)
        {
            if (!byQuest.TryGetValue(q.Id, out var objectives)) continue;
            var needs = objectives.Where(o => o.Items.Count > 0).Select(o => (o, need: Need(o))).ToList();

            var byId = q.Objectives.ToDictionary(o => o.Id);
            foreach (var (o, need) in needs)
                if (o.Objective != null && byId.TryGetValue(o.Objective, out var target))
                {
                    target.Item ??= need;
                    // the scraper takes the first number of the text, often part of an item name
                    // ("42 Signature Blend English Tea", "6-STEN-140-M"); tarkov.dev knows the amount,
                    // unless the text states it itself ("Hand over the 2 LEDX")
                    if (!StatedAmount().IsMatch(target.Text)) target.Count = Math.Max(1, o.Count);
                }
            // "Find X in raid" + "Hand over the items" are about the same item, whichever side was matched
            for (var i = 1; i < q.Objectives.Count; i++)
            {
                var (find, give) = (q.Objectives[i - 1], q.Objectives[i]);
                if (!give.Text.StartsWith("Hand over", StringComparison.OrdinalIgnoreCase)) continue;
                if (give.Item == null) give.Item = find.Item;
                else if (find.Item == null && find.Text.StartsWith("Find", StringComparison.OrdinalIgnoreCase)) find.Item = give.Item;
            }

            q.ItemNeeds = needs.Where(x => x.o.Type != "findItem").Select(x => x.need)
                // the same item over several objectives (plant 3× one by one) becomes one entry
                .GroupBy(n => (n.ItemId, n.Fir, n.Alternatives.Count))
                .Select(g => g.First() with { Count = g.Sum(n => n.Count) })
                .ToList();
        }
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
        throw new FileNotFoundException("data/tasks_en.json bulunamadı. Programı yanındaki 'data' klasörüyle birlikte kullandığınızdan emin olun.");
    }
}
