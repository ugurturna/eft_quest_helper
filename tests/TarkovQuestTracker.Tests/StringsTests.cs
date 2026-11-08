using System.Text.RegularExpressions;
using TarkovQuestTracker.Core.Services;

namespace TarkovQuestTracker.Tests;

/// <summary>The UI string dictionaries: a key defined twice crashes the app at start (XamlParseException).</summary>
public sealed class StringsTests
{
    static string StringsDir =>
        Path.Combine(Path.GetDirectoryName(QuestRepository.FindDataDir(AppContext.BaseDirectory))!, "TarkovQuestTracker", "Strings");

    static List<string> Keys(string lang) =>
        Regex.Matches(File.ReadAllText(Path.Combine(StringsDir, $"Strings.{lang}.xaml")), "x:Key=\"([^\"]+)\"")
             .Select(m => m.Groups[1].Value).ToList();

    [Theory]
    [InlineData("en")]
    [InlineData("tr")]
    public void No_key_is_defined_twice(string lang)
    {
        var keys = Keys(lang);
        Assert.Empty(keys.GroupBy(k => k).Where(g => g.Count() > 1).Select(g => g.Key));
    }

    [Fact]
    public void Both_languages_have_the_same_keys() => Assert.Equal(Keys("en").Order(), Keys("tr").Order());
}
