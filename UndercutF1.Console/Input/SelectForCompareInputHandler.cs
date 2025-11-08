using UndercutF1.Data;

namespace UndercutF1.Console;

public sealed class SelectForCompareInputHandler(
    State state,
    TimingDataProcessor timingData,
    DriverListProcessor driverList,
    SessionInfoProcessor sessionInfo
) : IInputHandler
{
    public bool IsEnabled =>
        sessionInfo.Latest.IsRace()
        && (
            state.CursorOffset > 0
            // Allow reset action at any cursor offset
            || (state.CompareDrivers is (not null, not null))
        );

    public Screen[] ApplicableScreens => [Screen.TimingTower];

    public ConsoleKey[] Keys => [ConsoleKey.C];

    public string Description =>
        state.CompareDrivers switch
        {
            (null, null) => "Compare",
            (var first, null) => $"[olive]Compare with {GetDriverMarkup(first)}[/]",
            (_, _) => "[olive]Reset Compare[/]",
        };

    public int Sort => 40;

    public Task ExecuteAsync(
        ConsoleKeyInfo consoleKeyInfo,
        CancellationToken cancellationToken = default
    )
    {
        var selectedDriverNumber = timingData
            .Latest.Lines.FirstOrDefault(x => x.Value.Line == state.CursorOffset)
            .Key;

        state.CompareDrivers = (selectedDriverNumber, state.CompareDrivers) switch
        {
            (null, _) => (null, null),
            (var driver, (null, null)) => (driver, null),
            (var driver, (var first, null)) => (first, driver),
            _ => (null, null),
        };

        return Task.CompletedTask;
    }

    private string GetDriverMarkup(string driverNumber)
    {
        var driver = driverList.Latest.GetValueOrDefault(driverNumber);
        return driver is null ? driverNumber : DisplayUtils.MarkedUpDriverNumber(driver);
    }
}
