namespace UndercutF1.Console;

public record State
{
    private Screen _screen = Screen.Main;

    public Screen PreviousScreen = Screen.Main;

    public Screen CurrentScreen
    {
        get => _screen;
        set
        {
            PreviousScreen = _screen;
            _screen = value;
        }
    }

    public int CursorOffset { get; set; } = 0;

    public (string? First, string? Second) CompareDrivers { get; set; } = (null, null);
}
