using TarkovQuestTracker.Core.Models;

namespace TarkovQuestTracker.Core.Services;

/// <summary>
/// Finds quest names in text read off the in-game Tasks screen (OCR). Only the English names are used:
/// the game must run in English. Strict on purpose: a missed quest is cheap, a wrong one is not.
/// </summary>
public sealed class QuestTextMatcher
{
    public sealed record Result(IReadOnlyList<Quest> Quests, int Unmatched);

    readonly List<(Quest Quest, string Text, string Digits)> _index;
    readonly int _maxWords;

    public QuestTextMatcher(IEnumerable<Quest> quests)
    {
        _index = quests.Select(q => (q, Text: Clean(q.Name)))
                       .Where(x => x.Text != "")
                       .Select(x => (x.q, x.Text, Digits(x.Text)))
                       .ToList();
        _maxWords = _index.Count == 0 ? 0 : _index.Max(e => e.Text.Count(c => c == ' ') + 1);
    }

    /// <summary>
    /// <paramref name="lines"/> are text pieces in reading order (one table cell or line each). A name may be
    /// split over two lines, or followed by other columns on the same line.
    /// </summary>
    public Result Match(IEnumerable<string> lines)
    {
        var cleaned = lines.Select(Clean).ToList();
        var found = new List<Quest>();
        var unmatched = 0;
        for (var i = 0; i < cleaned.Count; i++)
        {
            if (cleaned[i] == "") continue;
            // the longer reading wins: a name wrapped onto the next line beats its first half
            var joined = i + 1 < cleaned.Count && cleaned[i + 1] != "" ? Best(cleaned[i] + " " + cleaned[i + 1]) : null;
            var single = Best(cleaned[i]);
            if (joined != null && (single == null || joined.Value.Length > cleaned[i].Length))
            {
                Add(joined.Value.Quest);
                i++;
            }
            else if (single != null) Add(single.Value.Quest);
            else if (cleaned[i].Count(char.IsLetter) >= 3) unmatched++;
        }
        return new Result(found, unmatched);

        void Add(Quest q)
        {
            if (!found.Contains(q)) found.Add(q);
        }
    }

    /// <summary>Best quest whose name is the start of <paramref name="text"/> (other columns may follow it).</summary>
    (Quest Quest, int Length)? Best(string text)
    {
        var words = text.Split(' ');
        for (var n = Math.Min(words.Length, _maxWords); n >= 1; n--)
        {
            var prefix = string.Join(' ', words, 0, n);
            if (prefix.Length < 4) break;
            var digits = Digits(prefix);
            Quest? best = null;
            var bestDistance = int.MaxValue;
            foreach (var (quest, name, nameDigits) in _index)
            {
                var allowed = Allowed(name.Length);
                if (Math.Abs(name.Length - prefix.Length) > allowed || nameDigits != digits) continue;
                var d = allowed == 0 ? (name == prefix ? 0 : 1) : SearchService.Levenshtein(prefix, name);
                if (d <= allowed && d < bestDistance)
                {
                    best = quest;
                    bestDistance = d;
                }
            }
            if (best != null) return (best, prefix.Length);
        }
        return null;
    }

    /// <summary>Typos OCR may make: none in short names ("Debut" vs "Debts"), about 1 in 8 letters in long ones.</summary>
    static int Allowed(int length) => length <= 5 ? 0 : length <= 10 ? 1 : length / 8;

    /// <summary>Compares on letters and digits only; fixes the usual OCR slips in "Part 1" ("Part l", "Part I").</summary>
    public static string Clean(string text)
    {
        var words = SearchService.Normalize(text).Split(' ', StringSplitOptions.RemoveEmptyEntries);
        for (var i = 1; i < words.Length; i++)
            if (words[i - 1] == "part") words[i] = words[i].Replace('l', '1').Replace('i', '1').Replace('o', '0');
        return string.Join(' ', words);
    }

    static string Digits(string text) => new(text.Where(char.IsDigit).ToArray());
}
