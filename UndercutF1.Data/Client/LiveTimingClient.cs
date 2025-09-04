using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace UndercutF1.Data;

public sealed class LiveTimingClient(
    ITimingService timingService,
    Formula1Account formula1Account,
    ILoggerProvider loggerProvider,
    IOptions<LiveTimingOptions> options,
    ILogger<LiveTimingClient> logger
) : ILiveTimingClient, IDisposable
{
    private bool _disposedValue;
    private string _sessionKey = "UnknownSession";

    private readonly JsonSerializerOptions _prettyJsonOptions = new(JsonSerializerOptions.Default)
    {
        WriteIndented = true,
    };

    private static readonly string[] _topics =
    [
        "Heartbeat",
        "ExtrapolatedClock",
        "TimingStats",
        "TimingAppData",
        "WeatherData",
        "TrackStatus",
        "DriverList",
        "RaceControlMessages",
        "SessionInfo",
        "SessionData",
        "LapCount",
        "TimingData",
        "TeamRadio",
        // Only available with subscription?
        "CarData.z",
        "Position.z",
        "ChampionshipPrediction",
        // Not sure if these work now?
        "PitLaneTimeCollection",
        "PitStopSeries",
        "PitStop",
    ];

    public HubConnection? Connection { get; private set; }

    public async Task StartAsync()
    {
        logger.LogInformation("Starting Live Timing client");

        if (Connection is not null)
            logger.LogWarning("Live timing connection already exists, restarting it");

        await DisposeConnectionAsync();

        Connection = new HubConnectionBuilder()
            .WithUrl(
                "wss://livetiming.formula1.com/signalrcore",
                configure =>
                {
                    configure.AccessTokenProvider = () =>
                        Task.FromResult(formula1Account.AccessToken.Value);
                }
            )
            .WithAutomaticReconnect()
            .ConfigureLogging(configure =>
                configure.AddProvider(loggerProvider).SetMinimumLevel(LogLevel.Information)
            )
            .AddJsonProtocol()
            .Build();

        Connection.On<string, JsonNode, DateTimeOffset>("feed", HandleData);
        Connection.Closed += HandleClosedAsync;

        await Connection.StartAsync();

        logger.LogInformation("Subscribing");
        var res = await Connection.InvokeAsync<JsonObject>("Subscribe", _topics);
        HandleSubscriptionResponse(res);

        logger.LogInformation("Started Live Timing client");
    }

    private void HandleSubscriptionResponse(JsonObject obj)
    {
        var sessionInfo = obj?["SessionInfo"];
        var location = sessionInfo?["Meeting"]?["Location"] ?? "UnknownLocation";
        var sessionName = sessionInfo?["Name"] ?? "UnknownName";
        var year = sessionInfo?["Path"]?.ToString().Split('/')[0] ?? DateTime.Now.Year.ToString();
        _sessionKey = $"{year}_{location}_{sessionName}".Replace(' ', '_');

        logger.LogInformation(
            "Found session key from subscription data: {SessionKey}",
            _sessionKey
        );

        var res = obj!.ToJsonString(_prettyJsonOptions);

        var filePath = Path.Join(options.Value.DataDirectory, $"{_sessionKey}/subscribe.json");
        if (!File.Exists(filePath))
        {
            var path = $"{options.Value.DataDirectory}/{_sessionKey}";
            Directory.CreateDirectory(path);
            logger.LogInformation("Writing subscription response to {Path}", path);
            File.WriteAllText(filePath, obj!.ToJsonString(_prettyJsonOptions));
        }
        else
        {
            logger.LogWarning(
                "Data Subscription file at {Path} already exists, will not create a new one",
                filePath
            );
        }

        timingService.ProcessSubscriptionData(res);
    }

    private void HandleData(string type, JsonNode json, DateTimeOffset dateTime)
    {
        var raw = new RawTimingDataPoint(type, json, dateTime);
        try
        {
            File.AppendAllText(
                Path.Join(options.Value.DataDirectory, $"{_sessionKey}/live.jsonl"),
                JsonSerializer.Serialize(raw) + Environment.NewLine
            );

            // TODO: converting `json` to a string shouldn't be needed here, we need to change the signature in TimingService
            timingService.EnqueueAsync(type, json.ToString(), dateTime);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to handle live timing data: {Res}", raw);
        }
    }

    private async Task HandleClosedAsync(Exception? cause)
    {
        logger.LogWarning(cause, "Live timing client connection closed, attempting to reconnect");
        try
        {
            await DisposeConnectionAsync();
            await StartAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to reconnect");
        }
    }

    private async Task DisposeConnectionAsync()
    {
        if (Connection is not null)
        {
            await Connection.DisposeAsync();
        }
        Connection = null;
    }

    public void Dispose()
    {
        if (!_disposedValue)
        {
            _ = DisposeConnectionAsync();
            _disposedValue = true;
        }
        GC.SuppressFinalize(this);
    }
}
