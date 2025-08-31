using Spectre.Console;
using Spectre.Console.Rendering;
using UndercutF1.Console;
using UndercutF1.Data;

public class ManageSessionDisplay(
    ITimingService timingService,
    IDateTimeProvider dateTimeProvider,
    ILiveTimingClient liveTimingClient,
    SessionInfoProcessor sessionInfo
) : IDisplay
{
    public Screen Screen => Screen.ManageSession;

    public Task<IRenderable> GetContentAsync()
    {
        var table = new Table { Title = new TableTitle("Recently Processed Messages") };
        _ = table.AddColumns("Type", "Data", "Timestamp");
        table.Expand();

        var queueSnapshot = timingService.GetQueueSnapshot();
        queueSnapshot.Reverse();
        foreach (var (type, data, timestamp) in queueSnapshot)
        {
            _ = table.AddRow(
                type.EscapeMarkup(),
                data?.EscapeMarkup() ?? "",
                timestamp.ToString("s")
            );
        }

        var status = new Rows(
            new Text(
                $"Live Client Status: {liveTimingClient.Connection?.State.ToString() ?? "No Connection"}"
            ),
            new Text($"Simulation Time (UTC): {dateTimeProvider.Utc:s}"),
            new Text($"Delay: {dateTimeProvider.Delay}"),
            new Text($"Items in Queue: {timingService.GetRemainingWorkItems()}")
        );

        var session = new Rows(
            new Text(
                $"Location: {sessionInfo.Latest.Meeting?.Circuit?.ShortName ?? ""}"
            ).RightJustified(),
            new Text($"Type: {sessionInfo.Latest.Name ?? ""}").RightJustified(),
            new Text($"Start (UTC): {sessionInfo.Latest.GetStartDateTimeUtc():s}").RightJustified(),
            new Text(
                $"Key: {sessionInfo.Latest.Key}/{sessionInfo.Latest.Meeting?.Circuit?.Key}"
            ).RightJustified()
        );

        session.Collapse();

        var header = new Columns(status, session).Expand();

        var layout = new Layout().SplitRows(
            new Layout("Header", header).Size(5),
            new Layout("Data Queue", table)
        );

        return Task.FromResult<IRenderable>(layout);
    }
}
