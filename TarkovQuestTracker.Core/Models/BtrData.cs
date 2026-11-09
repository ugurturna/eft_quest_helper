namespace TarkovQuestTracker.Core.Models;

/// <summary>
/// One map of data/btr.json: where the BTR stops, where and when (raid time left) it spawns, and the routes it can drive.
/// Positions are game [x, z]; times are seconds of raid time left (counting down).
/// </summary>
public sealed class BtrMap
{
    /// <summary>The map name the quests use; set by the repository from the file's key.</summary>
    public string Name { get; set; } = "";
    /// <summary>The stop spots and timings are guesses, not yet checked in raids.</summary>
    public bool Estimated { get; set; } = true;
    /// <summary>How long the BTR waits at a stop.</summary>
    public double StopSeconds { get; set; } = 120;
    /// <summary>Game units (metres) per second along the legs.</summary>
    public double Speed { get; set; } = 5.5;
    /// <summary>How far off (either way) a prediction may be: the spawn time varies by about a minute.</summary>
    public double VarianceSeconds { get; set; } = 60;
    public Dictionary<string, BtrStop> Stops { get; set; } = [];
    public Dictionary<string, BtrSpawn> Spawns { get; set; } = [];
    /// <summary>"from>to" (stop or spawn ids) → the [x, z] bends between them; also used backwards. None = a straight leg.</summary>
    public Dictionary<string, double[][]> Roads { get; set; } = [];
    public List<BtrRoute> Routes { get; set; } = [];
}

public sealed class BtrStop
{
    public string Name { get; set; } = "";
    /// <summary>[x, z]</summary>
    public double[] Position { get; set; } = [0, 0];
}

public sealed class BtrSpawn
{
    /// <summary>Raid time left (seconds) when the BTR drives off from here.</summary>
    public double TimeLeft { get; set; }
    /// <summary>[x, z]</summary>
    public double[] Position { get; set; } = [0, 0];
}

/// <summary>One way the BTR drives a raid: from a spawn through the stops in order, then round again.</summary>
public sealed class BtrRoute
{
    public string Id { get; set; } = "";
    public string Spawn { get; set; } = "";
    public List<string> Stops { get; set; } = [];
    /// <summary>After the last stop it drives back to the first and goes round again.</summary>
    public bool Loop { get; set; } = true;
    /// <summary>Arrival times (time left) seen in raids, by position in <see cref="Stops"/> on the first round; null = calculated.</summary>
    public List<double?>? Observed { get; set; }
}
