using System.Globalization;
using System.Text;
using TarkovQuestTracker.Core.Models;

namespace TarkovQuestTracker.Core.Services;

/// <summary>
/// Searches quest names in every loaded language. Turkish/diacritic-insensitive,
/// ignores filler words ("the", "part", "bölüm"), tolerates small typos ("punişır 3").
/// </summary>
public sealed class SearchService(IEnumerable<Quest> quests)
{
    static readonly HashSet<string> Filler = ["the", "a", "part", "bolum", "kisim", "parca", "of"];

    readonly List<(Quest Quest, List<string[]> Names)> _index =
        quests.Select(q => (q, q.AllNames.Select(Tokens).ToList())).ToList();

    public IReadOnlyList<Quest> Search(string query, int limit = 50)
    {
        var qt = Tokens(query);
        if (qt.Length == 0) return [];
        return _index
            .Select(e => (e.Quest, Score: e.Names.Max(n => Score(qt, n))))
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.Quest.Name.Length)
            .Take(limit)
            .Select(x => x.Quest)
            .ToList();
    }

    static double Score(string[] query, string[] name)
    {
        if (name.Length == 0) return 0;
        if (query.SequenceEqual(name)) return 100;
        double total = 0;
        foreach (var q in query)
        {
            var best = name.Max(n => TokenScore(q, n));
            if (best == 0) return 0;  // every query word must match something
            total += best;
        }
        var bonus = string.Join(" ", name).StartsWith(string.Join(" ", query), StringComparison.Ordinal) ? 20 : 0;
        return 40 + 40 * total / query.Length + bonus;
    }

    static double TokenScore(string q, string n)
    {
        if (q == n) return 1;
        if (char.IsDigit(q[0]) || char.IsDigit(n[0])) return 0;  // "part 3" must not match "part 4"
        if (n.StartsWith(q, StringComparison.Ordinal)) return 0.9;
        if (q.Length >= 3 && n.Contains(q, StringComparison.Ordinal)) return 0.7;
        var tolerance = q.Length >= 6 ? 2 : q.Length >= 4 ? 1 : 0;
        if (tolerance > 0)
        {
            var prefix = n.Length > q.Length ? n[..Math.Min(n.Length, q.Length + 1)] : n;
            if (Math.Min(Levenshtein(q, n), Levenshtein(q, prefix)) <= tolerance) return 0.5;
        }
        return 0;
    }

    public static string[] Tokens(string text) =>
        Normalize(text).Split(' ', StringSplitOptions.RemoveEmptyEntries)
                       .Where(t => !Filler.Contains(t)).ToArray();

    /// <summary>Lowercase ASCII-ish form: "Punişır - Bölüm 3" -> "punisir bolum 3", "sh" -> "s".</summary>
    public static string Normalize(string text)
    {
        var sb = new StringBuilder(text.Length);
        foreach (var c in text.Replace('ı', 'i').Replace('İ', 'i').Normalize(NormalizationForm.FormD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark) continue;
            sb.Append(char.IsLetterOrDigit(c) ? char.ToLowerInvariant(c) : ' ');
        }
        return sb.ToString().Replace("sh", "s").Replace("ch", "c");
    }

    static int Levenshtein(string a, string b)
    {
        var prev = new int[b.Length + 1];
        var cur = new int[b.Length + 1];
        for (var j = 0; j <= b.Length; j++) prev[j] = j;
        for (var i = 1; i <= a.Length; i++)
        {
            cur[0] = i;
            for (var j = 1; j <= b.Length; j++)
                cur[j] = Math.Min(Math.Min(cur[j - 1] + 1, prev[j] + 1), prev[j - 1] + (a[i - 1] == b[j - 1] ? 0 : 1));
            (prev, cur) = (cur, prev);
        }
        return prev[b.Length];
    }
}
