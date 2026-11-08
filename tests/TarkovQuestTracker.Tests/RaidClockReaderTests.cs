using System.IO.Compression;
using TarkovQuestTracker.Core.Services;

namespace TarkovQuestTracker.Tests;

/// <summary>The raid clock in pictures cut from real screenshots (Fixtures: width, height, then raw BGRA, gzipped).</summary>
public sealed class RaidClockReaderTests
{
    static TimeSpan? Read(string fixture)
    {
        using var file = File.OpenRead(Path.Combine(AppContext.BaseDirectory, "Fixtures", fixture + ".bgra.gz"));
        using var gzip = new GZipStream(file, CompressionMode.Decompress);
        using var reader = new BinaryReader(gzip);
        int width = reader.ReadInt32(), height = reader.ReadInt32();
        var pixels = reader.ReadBytes(width * height * 4);
        return RaidClockReader.Read(pixels, width, height);
    }

    [Theory]
    [InlineData("clock_0_31_55", 0, 31, 55)]          // double "O", as the game drew it
    [InlineData("clock_0_31_55_small", 0, 31, 55)]    // a smaller screen
    [InlineData("clock_0_31_55_big", 0, 31, 55)]      // enlarged, as the detector reads it at 1080p
    [InlineData("clock_single_o_0_19_17", 0, 19, 17)] // single "O": just the clock (a 9 and a 7)
    public void Reads_the_clock(string fixture, int h, int m, int s) => Assert.Equal(new TimeSpan(h, m, s), Read(fixture));

    [Theory]
    [InlineData("clock_basic_fps_0_18", 0, 18)]  // the FPS counter's box darkens it, "RTT: 0" runs over the seconds
    [InlineData("clock_full_fps_0_19", 0, 19)]   // the long FPS counter
    [InlineData("clock_fps_covered", 0, 24)]     // double "O" with the FPS counter
    public void Under_an_fps_counter_reads_hours_and_minutes(string fixture, int h, int m)
    {
        var time = Read(fixture);
        Assert.NotNull(time);
        Assert.Equal((h, m), (time.Value.Hours, time.Value.Minutes));
    }

    [Fact]
    public void A_countdown_in_an_exfil_row_is_not_the_raid_clock() =>
        // the same clock moved down to where an exfil's countdown is: it once ended the raid a few seconds in
        Assert.Null(Read("clock_exfil_row"));

    [Fact]
    public void An_empty_picture_is_no_time() => Assert.Null(RaidClockReader.Read(new byte[40 * 30 * 4], 40, 30));
}
