namespace UndercutF1.Data;

/// <summary>
/// Options to configure the behaviour of live timing.
/// </summary>
public record LiveTimingOptions
{
    /// <summary>
    /// The directory to read and store live timing data for simulations.
    /// When live sessions are being listened to, all data received will be recorded in this directory.
    /// This is also the directory that imported data is saved to.
    ///
    /// Defaults to <c>%LOCALAPPDATA%/undercut-f1/data</c> on Windows,
    /// <c>$XDG_DATA_HOME/undercut-f1/data</c> or <c>~/.local/share</c> on Mac/Linux.
    /// </summary>
    public string DataDirectory { get; set; } = GetDefaultDataDirectory();

    /// <summary>
    /// The directory where logs will be output to.
    /// Defaults to <c>~/undercut-f1/logs</c>
    /// </summary>
    public string LogDirectory { get; set; } = GetDefaultLogDirectory();

    /// <summary>
    /// Whether the app should expose an API at http://localhost:61937.
    /// Defaults to <c>false</c>.
    /// </summary>
    public bool ApiEnabled { get; set; } = false;

    /// <summary>
    /// Whether verbose logging should be enabled.
    /// Defaults to <c>false</c>.
    /// </summary>
    public bool Verbose { get; set; } = false;

    /// <summary>
    /// Whether notifications should be sent to the user when new Race Control messages are received.
    /// Blue flag related messages do not result in notifications, but all other messages do.
    /// UndercutF1.Console implements these notifications as <c>BEL</c>s sent to your terminal, resulting in an audible beep.
    /// </summary>
    public bool Notify { get; set; } = true;

    /// <summary>
    /// Try to conform to Windows/XDG directory standards by default.
    /// <see cref="Environment.SpecialFolder.LocalApplicationData"/> will return <c>%LOCALAPPDATA%</c> on Windows.
    /// On Linux/Mac, we will try to use <c>$XDG_DATA_HOME</c> or <c>~/.local/share</c>.
    /// </summary>
    private static string GetDefaultDataDirectory()
    {
        // NOTE: Environment.SpecialFolder.LocalApplicationData returns ~/Library/Application Support on Mac,
        // so using the environment variables directly on Mac/Linux to keep them both using XDG standards.
        if (OperatingSystem.IsWindows())
        {
            return Path.Join(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.LocalApplicationData,
                    Environment.SpecialFolderOption.Create
                ),
                "undercut-f1",
                "data"
            );
        }
        else
        {
            var xdgDataDirectory = Environment.GetEnvironmentVariable("XDG_DATA_HOME");
            if (string.IsNullOrWhiteSpace(xdgDataDirectory))
            {
                xdgDataDirectory = Path.Join(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    ".local",
                    "share"
                );
            }

            return Path.Join(xdgDataDirectory, "undercut-f1", "data");
        }
    }

    /// <summary>
    /// Try to conform to Windows/XDG directory standards by default.
    /// <see cref="Environment.SpecialFolder.LocalApplicationData"/> will return <c>%LOCALAPPDATA%</c> on Windows.
    /// On Linux/Mac, we will try to use <c>$XDG_STATE_HOME</c> or <c>~/.local/state</c>
    /// </summary>
    private static string GetDefaultLogDirectory()
    {
        if (OperatingSystem.IsWindows())
        {
            return Path.Join(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.LocalApplicationData,
                    Environment.SpecialFolderOption.Create
                ),
                "undercut-f1",
                "logs"
            );
        }
        else
        {
            var xdgStateDirectory = Environment.GetEnvironmentVariable("XDG_STATE_HOME");
            if (string.IsNullOrWhiteSpace(xdgStateDirectory))
            {
                xdgStateDirectory = Path.Join(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    ".local",
                    "state"
                );
            }

            return Path.Join(xdgStateDirectory, "undercut-f1", "logs");
        }
    }
}
