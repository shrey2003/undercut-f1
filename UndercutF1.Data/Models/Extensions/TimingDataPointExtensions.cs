using System.Globalization;

namespace UndercutF1.Data;

public static class TimingDataPointExtensions
{
    public static Dictionary<string, TimingDataPoint.Driver> GetOrderedLines(
        this TimingDataPoint data
    ) => data.Lines.OrderBy(x => x.Value.Line).ToDictionary(x => x.Key, x => x.Value);

    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Style",
        "IDE0046:Convert to conditional expression",
        Justification = "Harder to read"
    )]
    public static decimal? GapToLeaderSeconds(this TimingDataPoint.Driver driver)
    {
        if (driver.GapToLeader?.Contains("LAP") ?? false)
            return 0;

        return decimal.TryParse(driver.GapToLeader, out var seconds) ? seconds : null;
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Style",
        "IDE0046:Convert to conditional expression",
        Justification = "Harder to read"
    )]
    public static decimal? IntervalSeconds(this TimingDataPoint.Driver.Interval interval)
    {
        if (interval.Value?.Contains("LAP") ?? false)
            return 0;

        return decimal.TryParse(interval?.Value, out var seconds) ? seconds : null;
    }

    public static bool TryParseTimeSpan(this string? str, out TimeSpan result) =>
        TimeSpan.TryParseExact(
            str,
            ["hh\\:mm\\:ss", "m\\:ss\\.fff", "ss\\.fff"],
            CultureInfo.InvariantCulture,
            out result
        );

    public static decimal? SmartGapToLeaderSeconds(
        this Dictionary<string, TimingDataPoint.Driver> lines,
        string driverNumber
    )
    {
        var line = lines.GetValueOrDefault(driverNumber);
        if (line is null)
        {
            return null;
        }

        // If this driver is not lapped, and has a valid gap to leader, then use it directly
        if (
            line.GapToLeader is not null
            && !line.GapToLeader.Contains(" L", StringComparison.InvariantCultureIgnoreCase)
        )
        {
            return line.GapToLeaderSeconds();
        }

        // If the driver is lapped, they won't have a GapToLeader,
        // so determine it manually by adding up all the intervals from higher placed drivers
        if (string.IsNullOrWhiteSpace(line.IntervalToPositionAhead?.Value))
        {
            return null;
        }

        // Take the lap line which gap a gap to leader, and use that as a basis
        var lastUnlappedDriver = lines
            .LastOrDefault(x => x.Value.GapToLeaderSeconds().HasValue, lines.First())
            .Value;

        var summedGapsOfPriorDrivers = lines
            .Where(x => x.Value.Line > lastUnlappedDriver.Line && x.Value.Line <= line.Line)
            .Sum(x => x.Value.IntervalToPositionAhead?.IntervalSeconds() ?? 0);

        return lastUnlappedDriver.GapToLeaderSeconds() + summedGapsOfPriorDrivers;
    }

    public static TimeSpan? ToTimeSpan(this TimingDataPoint.Driver.BestLap lap) =>
        lap.Value.TryParseTimeSpan(out var result) ? result : null;

    public static TimeSpan? ToTimeSpan(this TimingDataPoint.Driver.LapSectorTime lap) =>
        lap.Value.TryParseTimeSpan(out var result) ? result : null;
}
