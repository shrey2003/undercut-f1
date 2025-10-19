namespace UndercutF1.Data;

public static class SessionInfoDataPointExtensions
{
    public static DateTime? GetStartDateTimeUtc(this SessionInfoDataPoint data)
    {
        if (!data.StartDate.HasValue || string.IsNullOrWhiteSpace(data.GmtOffset))
        {
            return null;
        }

        var dateTimeOffset = new DateTimeOffset(
            data.StartDate.Value,
            TimeSpan.Parse(data.GmtOffset)
        );

        return dateTimeOffset.UtcDateTime;
    }

    public static bool IsRace(this SessionInfoDataPoint? sessionInfo) =>
        sessionInfo?.Type == "Race";

    public static bool IsQualifying(this SessionInfoDataPoint? sessionInfo) =>
        sessionInfo?.Type == "Qualifying";
}
