using System.Globalization;
using System.Text.RegularExpressions;

namespace TarkovQuestTracker.Core.Services;

/// <summary>
/// Where the player stood when a screenshot was taken. The game writes it into the file name:
/// <c>2024-07-11[14-22]_-212.33, 3.12, -156.75_0.00004, -0.86414, 0.00006, 0.50327_10.75 (0).png</c>
/// = date[hour-minute]_position x, y (height), z_rotation quaternion x, y, z, w_… .
/// </summary>
public sealed record ScreenshotPosition(double X, double Y, double Z, double Yaw, DateTime Taken)
{
    const string Num = @"(-?\d+(?:\.\d+)?)";

    static readonly Regex Pattern = new(
        $@"^(\d{{4}}-\d{{2}}-\d{{2}})\[(\d{{2}})-(\d{{2}})\]_?{Num}, {Num}, {Num}_?{Num}, {Num}, {Num}, {Num}",
        RegexOptions.CultureInvariant);

    /// <summary>Reads a screenshot's file name (with or without its folder); false for any other picture.</summary>
    public static bool TryParse(string fileName, out ScreenshotPosition position)
    {
        position = null!;
        var m = Pattern.Match(Path.GetFileName(fileName));
        if (!m.Success) return false;
        double N(int group) => double.Parse(m.Groups[group].Value, CultureInfo.InvariantCulture);
        if (!DateTime.TryParseExact(m.Groups[1].Value, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                                    DateTimeStyles.None, out var day)) return false;
        var taken = day.AddHours(N(2)).AddMinutes(N(3));
        position = new ScreenshotPosition(N(4), N(5), N(6), YawOf(N(7), N(8), N(9), N(10)), taken);
        return true;
    }

    /// <summary>
    /// Facing direction in degrees around the vertical axis, the way TarkovMonitor turns the game's quaternion
    /// into the rotation tarkov.dev's player marker uses (Unity's y-up order: the y component is the vertical one).
    /// </summary>
    public static double YawOf(double x, double y, double z, double w)
    {
        var sinyCosp = 2 * (w * y + x * z);
        var cosyCosp = 1 - 2 * (z * z + y * y);
        return Math.Atan2(sinyCosp, cosyCosp) * 180 / Math.PI;
    }
}
