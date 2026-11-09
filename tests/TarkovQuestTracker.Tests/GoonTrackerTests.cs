using TarkovQuestTracker.Core.Models;
using TarkovQuestTracker.Core.Services;

namespace TarkovQuestTracker.Tests;

public class GoonTrackerTests
{
    // the shape of tarkov-goon-tracker.com's Next.js page, with the PvP feed's bot rows
    const string Page = """
        <html><head><title>Goons</title></head><body><div id="__next">…</div>
        <script id="__NEXT_DATA__" type="application/json">{"props":{"pageProps":{"trackings":[
          {"currentDate":"Invalid Date","map":{"slug":"some-stream-title","name":"Watch my stream!"},"username":"bot-tp","id":"Invalid Date"},
          {"currentDate":"2026-01-02T10:00:00Z","map":{"slug":"woods","name":"Woods"},"username":" Scav Bunker ","id":"a"},
          {"currentDate":"2026-01-02T12:30:00Z","map":{"slug":"customs","name":"Customs"},"username":"bsg servers","id":"b"},
          {"currentDate":"2026-01-02T11:00:00Z","map":{"slug":"factory","name":"Factory"},"username":"x","id":"c"},
          {"currentDate":"2026-01-02T09:00:00Z","map":{"slug":"Lighthouse","name":"Lighthouse"},"id":"d"}
        ]}},"page":"/[locale]/pve"}</script></body></html>
        """;

    [Fact]
    public void Parse_KeepsGoonMapsWithRealDates_NewestFirst()
    {
        var rows = GoonTracker.Parse(Page);

        Assert.Equal(["customs", "woods", "lighthouse"], rows.Select(r => r.MapSlug));
        Assert.Equal("Customs", rows[0].MapName);
        Assert.Equal(new DateTime(2026, 1, 2, 12, 30, 0, DateTimeKind.Utc), rows[0].SeenAt);
        Assert.Equal(DateTimeKind.Utc, rows[0].SeenAt.Kind);
        Assert.Equal("Scav Bunker", rows[1].Reporter);
        Assert.Equal("", rows[2].Reporter);
    }

    [Theory]
    [InlineData("<html><body>no data</body></html>")]
    [InlineData("""<script id="__NEXT_DATA__" type="application/json">{"props":{"pageProps":{}}}</script>""")]
    public void Parse_PageWithoutReports_IsEmpty(string html) => Assert.Empty(GoonTracker.Parse(html));

    [Theory]
    [InlineData(GameMode.Pvp, "https://www.tarkov-goon-tracker.com/tr")]
    [InlineData(GameMode.Pve, "https://www.tarkov-goon-tracker.com/tr/pve")]
    public void PageUrl_FollowsGameMode(GameMode mode, string url) => Assert.Equal(url, GoonTracker.PageUrl(mode));
}
