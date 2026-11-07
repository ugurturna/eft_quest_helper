using TarkovQuestTracker.Core.Services;

namespace TarkovQuestTracker.Tests;

/// <summary>Finding the scraper scripts and copying their output (the scripts themselves are not run).</summary>
public sealed class WikiUpdaterTests
{
    [Fact]
    public void Finds_the_scripts_from_the_build_output()
    {
        var updater = WikiUpdater.Find(AppContext.BaseDirectory);
        Assert.NotNull(updater);
        Assert.True(File.Exists(Path.Combine(updater.ScraperDir, "scrape_wiki.py")));
        Assert.True(File.Exists(Path.Combine(updater.SourceDataDir, "tasks_en.json")));
    }

    [Fact]
    public void Is_unavailable_without_the_scripts()
    {
        var dir = Directory.CreateTempSubdirectory("tqt-noscripts-");
        try
        {
            Assert.Null(WikiUpdater.Find(dir.FullName));
        }
        finally
        {
            dir.Delete(recursive: true);
        }
    }

    [Fact]
    public void Copies_changed_data_files_into_the_app_folder_only()
    {
        var updater = WikiUpdater.Find(AppContext.BaseDirectory)!;
        var target = Directory.CreateTempSubdirectory("tqt-appdata-");
        try
        {
            var first = updater.CopyInto(target.FullName);
            Assert.True(first > 2);
            Assert.True(File.Exists(Path.Combine(target.FullName, "tasks_en.json")));
            Assert.False(File.Exists(Path.Combine(target.FullName, "diff_report.md")));
            // nothing changed since: nothing to copy
            Assert.Equal(0, updater.CopyInto(target.FullName));
            // the scripts write straight into the app's folder when it is the project's data folder
            Assert.Equal(0, updater.CopyInto(updater.SourceDataDir));
        }
        finally
        {
            target.Delete(recursive: true);
        }
    }
}
