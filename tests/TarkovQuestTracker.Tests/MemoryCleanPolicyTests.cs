using TarkovQuestTracker.Core.Services;

namespace TarkovQuestTracker.Tests;

public sealed class MemoryCleanPolicyTests
{
    static readonly DateTime Now = new(2026, 11, 24, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Cleans_when_use_reaches_the_limit() => Assert.True(MemoryCleanPolicy.ShouldAutoClean(85, 85, Now, null));

    [Fact]
    public void Leaves_it_under_the_limit() => Assert.False(MemoryCleanPolicy.ShouldAutoClean(84, 85, Now, null));

    [Fact]
    public void Waits_out_the_cooldown()
    {
        Assert.False(MemoryCleanPolicy.ShouldAutoClean(95, 85, Now, Now - TimeSpan.FromMinutes(1)));
        Assert.True(MemoryCleanPolicy.ShouldAutoClean(95, 85, Now, Now - MemoryCleanPolicy.Cooldown));
    }

    [Fact]
    public void A_limit_out_of_range_is_clamped()
    {
        // a hand-edited 0 must not clean every few minutes at any RAM use
        Assert.False(MemoryCleanPolicy.ShouldAutoClean(30, 0, Now, null));
        Assert.True(MemoryCleanPolicy.ShouldAutoClean(98, 100, Now, null));
    }

    [Fact]
    public void Unknown_area_bits_are_dropped()
    {
        Assert.Equal(MemoryAreas.StandbyList, MemoryCleanPolicy.Sanitize((int)MemoryAreas.StandbyList | 1024));
        Assert.Equal(MemoryAreas.None, MemoryCleanPolicy.Sanitize(-1 & ~(int)MemoryAreas.All));
    }

    [Fact]
    public void The_default_leaves_the_game_alone() =>
        Assert.False(MemoryAreas.Default.HasFlag(MemoryAreas.WorkingSets));
}
