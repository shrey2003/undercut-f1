namespace UndercutF1.Console.ExternalPlayerSync;

/// <summary>
/// Options to configure the external player synchronisation service
/// </summary>
public sealed record SyncOptions
{
    /// <summary>
    /// Whether the player sync is enabled or not.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// What the external service we're syncing against is.
    /// This determines what API calls undercutf1 will be making, and how it will make them.
    /// </summary>
    public ServiceType ServiceType { get; set; }

    /// <summary>
    /// The URL where the external players API can be found.
    /// </summary>
    public Uri? Url { get; set; }

    /// <summary>
    /// The amount of time, in milliseconds, to wait between attempts to connect to the external services
    /// WebSocket interface.
    /// </summary>
    public int WebSocketConnectInterval { get; set; } = 30000;
}
