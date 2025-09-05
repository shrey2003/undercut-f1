using UndercutF1.Data;

namespace UndercutF1.Console;

public class DelayInputHandler(IDateTimeProvider dateTimeProvider) : IInputHandler
{
    public bool IsEnabled => !dateTimeProvider.IsPaused;

    public Screen[] ApplicableScreens =>
        [
            Screen.ManageSession,
            Screen.RaceControl,
            Screen.DriverTracker,
            Screen.TimingTower,
            Screen.TimingHistory,
            Screen.TyreStints,
            Screen.DebugData,
        ];

    public ConsoleKey[] DisplayKeys => [ConsoleKey.N, ConsoleKey.M];

    // PrintScreen keycode = 44 (actually comma)
    // Delete keycode = 46 (actually period)
    public ConsoleKey[] Keys =>
        [ConsoleKey.N, ConsoleKey.M, ConsoleKey.PrintScreen, ConsoleKey.Delete];

    public string Description => "Delay";

    public int Sort => 22;

    public Task ExecuteAsync(
        ConsoleKeyInfo consoleKeyInfo,
        CancellationToken cancellationToken = default
    )
    {
        switch (consoleKeyInfo.Key)
        {
            case ConsoleKey.N:
                UpdateDelay(-1, consoleKeyInfo.Modifiers);
                break;
            case ConsoleKey.M:
                UpdateDelay(1, consoleKeyInfo.Modifiers);
                break;
            case ConsoleKey.PrintScreen:
                UpdateDelay(-1, ConsoleModifiers.Control);
                break;
            case ConsoleKey.Delete:
                UpdateDelay(1, ConsoleModifiers.Control);
                break;
        }

        return Task.CompletedTask;
    }

    private void UpdateDelay(int direction, ConsoleModifiers modifiers)
    {
        var amount = modifiers switch
        {
            ConsoleModifiers.Shift => 30 * direction,
            ConsoleModifiers.Control => 1 * direction,
            _ => 5 * direction,
        };

        dateTimeProvider.Delay += TimeSpan.FromSeconds(amount);

        if (dateTimeProvider.Delay < TimeSpan.Zero)
            dateTimeProvider.Delay = TimeSpan.Zero;
    }
}
