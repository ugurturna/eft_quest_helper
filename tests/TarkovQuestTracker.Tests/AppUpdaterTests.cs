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

    [Fact]
    public void Apply_NeverTouchesAnyOfThePlayersFiles()
    {
        var root = Path.Combine(Path.GetTempPath(), "tqt-update-" + Guid.NewGuid().ToString("N"));
        var install = Path.Combine(root, "install");
        var release = Path.Combine(root, "release");
        string[] mine =
        [
            @"data\profiles\Me\settings.json", @"data\profiles\Me\progress_pve.json", @"data\app.json",
            @"data\settings.json", @"data\progress_pvp.json", @"data\progress_pve.json",   // pre-profile layout
            @"data\backups\20260101-000000\data\app.json", @"data\flea\regular\meta.json", "portable.txt",
        ];
        try
        {
            Write(install, "TarkovQuestTracker.exe", "old exe");
            Write(release, "TarkovQuestTracker.exe", "new exe");
            foreach (var f in mine)
            {
                Write(install, f, "mine");
                Write(release, f, "from the release");
            }

            var updater = new AppUpdater(install);
            updater.Apply(release);
            updater.CleanupOldFiles();

            foreach (var f in mine) Assert.Equal("mine", Read(install, f));
            Assert.Equal("new exe", Read(install, "TarkovQuestTracker.exe"));
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }

    [Fact]
    public void BackupUserData_CopiesProfilesAndPrunesOldBackups()
    {
        var install = Path.Combine(Path.GetTempPath(), "tqt-backup-" + Guid.NewGuid().ToString("N"));
        try
        {
            Write(install, @"data\profiles\Me\progress_pvp.json", "my progress");
            Write(install, @"data\app.json", "state");
            Write(install, @"data\flea\regular\items.json", "cache");
            var updater = new AppUpdater(install);

            var start = new DateTime(2026, 11, 7, 12, 0, 0);
            string? latest = null;
            for (var i = 0; i < AppUpdater.BackupsKept + 2; i++) latest = updater.BackupUserData(start.AddMinutes(i));

            Assert.Equal("my progress", Read(latest!, @"data\profiles\Me\progress_pvp.json"));
            Assert.Equal("state", Read(latest!, @"data\app.json"));
            Assert.False(Directory.Exists(Path.Combine(latest!, "data", "flea")));  // re-downloadable, not backed up
            var backups = Directory.GetDirectories(Path.Combine(install, "data", "backups"));
            Assert.Equal(AppUpdater.BackupsKept, backups.Length);
            Assert.DoesNotContain(backups, b => b.EndsWith("20261107-120000"));     // the oldest one was pruned
            Assert.Equal("my progress", Read(install, @"data\profiles\Me\progress_pvp.json"));
        }
        finally
        {
            if (Directory.Exists(install)) Directory.Delete(install, true);
        }
    }

    [Fact]
    public void BackupUserData_NothingToBackUp_ReturnsNull()
    {
        var install = Path.Combine(Path.GetTempPath(), "tqt-backup-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(install);
        try { Assert.Null(new AppUpdater(install).BackupUserData()); }
        finally { Directory.Delete(install, true); }
    }

    static void Write(string dir, string relative, string text)
    {
        var path = Path.Combine(dir, relative);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, text);
    }

    static string Read(string dir, string relative) => File.ReadAllText(Path.Combine(dir, relative));
}
