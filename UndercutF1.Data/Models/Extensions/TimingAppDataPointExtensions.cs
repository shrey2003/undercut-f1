namespace UndercutF1.Data;

public static class TimingAppDataPointExtensions
{
    public static Dictionary<string, TimingAppDataPoint.Driver> GetOrderedLines(
        this TimingAppDataPoint data
    ) => data.Lines.OrderBy(x => x.Value.Line).ToDictionary(x => x.Key, x => x.Value);

    /// <summary>
    /// Calculates how many laps the stint lasted
    /// </summary>
    public static int GetStintDuration(this TimingAppDataPoint.Driver.Stint stint) =>
        (stint.TotalLaps - stint.StartLaps) ?? 0;

    /// <summary>
    /// Gets the lap number that the selected stint started on
    /// </summary>
    public static int GetPitLapForStint(
        this Dictionary<string, TimingAppDataPoint.Driver.Stint> stints,
        string stintNumber
    ) =>
        stints
            .TakeWhile((kv) => int.Parse(kv.Key) < int.Parse(stintNumber))
            .Sum(x => x.Value.GetStintDuration()) + 1;
}
