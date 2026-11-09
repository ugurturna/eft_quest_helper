using TarkovQuestTracker.Core.Services;

namespace TarkovQuestTracker.Tests;

/// <summary>The position the game writes into a screenshot's file name.</summary>
public sealed class ScreenshotPositionTests
{
    [Fact]
    public void Reads_position_and_time()
    {
        Assert.True(ScreenshotPosition.TryParse(
            "2024-07-11[14-22]_-212.33, 3.12, -156.75_0.00004, -0.86414, 0.00006, 0.50327_10.75 (0).png", out var p));
        Assert.Equal(-212.33, p.X);
        Assert.Equal(3.12, p.Y);
        Assert.Equal(-156.75, p.Z);
        Assert.Equal(new DateTime(2024, 7, 11, 14, 22, 0), p.Taken);
    }

    [Theory]
    [InlineData(@"C:\Users\x\Documents\Escape from Tarkov\Screenshots\2025-01-02[09-05]_12.5, -0.4, 300.25_0.0, 0.0, 0.0, 1.0_1.2 (1).png")]
    [InlineData("2025-01-02[09-05]12.5, -0.4, 300.25_0.0, 0.0, 0.0, 1.0_1.2 (0).png")]  // older names: no "_" after the time
    [InlineData("2025-01-02[09-05]_12, 0, 300_0, 0, 0, 1_1 (2).jpg")]
    public void Accepts_name_variants(string name) => Assert.True(ScreenshotPosition.TryParse(name, out _));

    [Theory]
    [InlineData("Screenshot 2025-01-02 090500.png")]
    [InlineData("2025-01-02[09-05] (0).png")]  // menu screenshot: no position
    [InlineData("2025-01-02[09-05]_12.5, -0.4_0.0, 0.0 (0).png")]
    public void Rejects_other_pictures(string name) => Assert.False(ScreenshotPosition.TryParse(name, out _));

    [Theory]
    [InlineData(0, 0, 0, 1, 0)]                                   // identity
    [InlineData(0, 0.7071068, 0, 0.7071068, 90)]                 // a quarter turn around the vertical axis
    [InlineData(0, 1, 0, 0, 180)]
    [InlineData(0, -0.7071068, 0, 0.7071068, -90)]
    public void Yaw_turns_around_the_vertical_axis(double x, double y, double z, double w, double degrees) =>
        Assert.Equal(degrees, ScreenshotPosition.YawOf(x, y, z, w), 3);
}
