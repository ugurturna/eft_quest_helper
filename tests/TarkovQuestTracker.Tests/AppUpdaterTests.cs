using System.Text.Json;
using TarkovQuestTracker.Core.Services;

namespace TarkovQuestTracker.Tests;

public class AppUpdaterTests
{
    const string Release = """
        {"tag_name":"v0.2.0","html_url":"https://github.com/x/y/releases/tag/v0.2.0","body":"notes","draft":false,"prerelease":false,
         "assets":[{"name":"something.txt","size":1,"browser_download_url":"https://e/x.txt"},
                   {"name":"TarkovQuestTracker-v0.2.0-win-x64.zip","size":1234,"digest":"sha256:ABCDEF",
                    "browser_download_url":"https://e/TarkovQuestTracker-v0.2.0-win-x64.zip"}]}
        """;

    [Fact]
    public void ParseRelease_PicksTheWinX64Zip()
    {
        using var doc = JsonDocument.Parse(Release);
        var r = AppUpdater.ParseRelease(doc.RootElement)!;
        Assert.Equal(new Version(0, 2, 0), r.Version);
        Assert.Equal("https://e/TarkovQuestTracker-v0.2.0-win-x64.zip", r.AssetUrl);
        Assert.Equal(1234, r.AssetSize);
        Assert.Equal("abcdef", r.Sha256);
    }

    [Fact]
    public void ParseRelease_SkipsPreReleasesAndReleasesWithoutTheZip()
    {
        using var pre = JsonDocument.Parse(Release.Replace("\"prerelease\":false", "\"prerelease\":true"));
        Assert.Null(AppUpdater.ParseRelease(pre.RootElement));
        using var noZip = JsonDocument.Parse("""{"tag_name":"v0.2.0","assets":[]}""");
        Assert.Null(AppUpdater.ParseRelease(noZip.RootElement));
    }

    [Theory]
    [InlineData("0.2.0", "0.1.2.0", true)]
    [InlineData("0.1.2", "0.1.2.0", false)]  // assembly versions have a 4th part
    [InlineData("0.1.1", "0.1.2.0", false)]
    [InlineData("1.0.0", "0.9.9", true)]
    public void IsNewer_ComparesThreeParts(string candidate, string current, bool newer) =>
        Assert.Equal(newer, AppUpdater.IsNewer(Version.Parse(candidate), Version.Parse(current)));

    [Fact]
    public void Apply_ReplacesAppFilesButKeepsTheProfiles()
    {
        var root = Path.Combine(Path.GetTempPath(), "tqt-update-" + Guid.NewGuid().ToString("N"));
        var install = Path.Combine(root, "install");
        var release = Path.Combine(root, "release");
        try
        {
            Write(install, "TarkovQuestTracker.exe", "old exe");
            Write(install, @"data\tasks_en.json", "old quests");
            Write(install, @"data\profiles\Me\progress_pvp.json", "my progress");
            Write(release, "TarkovQuestTracker.exe", "new exe");
            Write(release, @"data\tasks_en.json", "new quests");
            Write(release, @"data\icons\new.png", "icon");
            Write(release, @"data\profiles\Me\progress_pvp.json", "must not overwrite");

            var updater = new AppUpdater(install);
            updater.Apply(release);

            Assert.Equal("new exe", Read(install, "TarkovQuestTracker.exe"));
            Assert.Equal("new quests", Read(install, @"data\tasks_en.json"));
            Assert.Equal("icon", Read(install, @"data\icons\new.png"));
            Assert.Equal("my progress", Read(install, @"data\profiles\Me\progress_pvp.json"));
            Assert.Equal("old exe", Read(install, "TarkovQuestTracker.exe.old"));

            updater.CleanupOldFiles();
            Assert.False(File.Exists(Path.Combine(install, "TarkovQuestTracker.exe.old")));
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }

    static void Write(string dir, string relative, string text)
    {
        var path = Path.Combine(dir, relative);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, text);
    }

    static string Read(string dir, string relative) => File.ReadAllText(Path.Combine(dir, relative));
}
