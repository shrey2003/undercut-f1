using InMemLogger;
using Serilog;
using Serilog.Events;
using TextCopy;
using UndercutF1.Console.ExternalPlayerSync;
using UndercutF1.Console.Graphics;
using UndercutF1.Data;

namespace UndercutF1.Console;

public static partial class CommandHandler
{
    private static WebApplicationBuilder GetBuilder(
        bool? isApiEnabled = false,
        DirectoryInfo? dataDirectory = null,
        DirectoryInfo? logDirectory = null,
        bool? isVerbose = false,
        bool? notifyEnabled = null,
        bool? preferFfmpeg = null,
        GraphicsProtocol? forceGraphicsProtocol = null,
        bool useConsoleLogging = false
    )
    {
        var builder = WebApplication.CreateEmptyBuilder(new() { ApplicationName = "undercutf1" });

        var commandLineOpts = new Dictionary<string, string?>();
        if (isVerbose is not null)
        {
            commandLineOpts.Add(nameof(Options.Verbose), isVerbose.ToString());
        }
        if (isApiEnabled is not null)
        {
            commandLineOpts.Add(nameof(Options.ApiEnabled), isApiEnabled.ToString());
        }
        if (dataDirectory is not null)
        {
            commandLineOpts.Add(nameof(Options.DataDirectory), dataDirectory?.FullName);
        }
        if (logDirectory is not null)
        {
            commandLineOpts.Add(nameof(Options.LogDirectory), logDirectory?.FullName);
        }
        if (notifyEnabled is not null)
        {
            commandLineOpts.Add(nameof(Options.Notify), notifyEnabled.ToString());
        }
        if (preferFfmpeg is not null)
        {
            commandLineOpts.Add(nameof(Options.PreferFfmpegPlayback), preferFfmpeg.ToString());
        }
        if (forceGraphicsProtocol is not null)
        {
            commandLineOpts.Add(
                nameof(Options.ForceGraphicsProtocol),
                forceGraphicsProtocol.ToString()
            );
        }

        _ = builder
            .Configuration.AddJsonFile(Options.ConfigFilePath, optional: true)
            .AddEnvironmentVariables("UNDERCUTF1_")
            .AddInMemoryCollection(commandLineOpts);

        var options = builder.Configuration.Get<Options>() ?? new();

        var (inMemoryLogLevel, fileLogLevel) = options.Verbose
            ? (LogLevel.Trace, LogEventLevel.Verbose)
            : (LogLevel.Information, LogEventLevel.Information);

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Is(fileLogLevel)
            .WriteTo.File(
                path: Path.Join(options.LogDirectory, "/undercutf1.log"),
                rollOnFileSizeLimit: true,
                rollingInterval: RollingInterval.Hour,
                retainedFileCountLimit: 5
            )
            .CreateLogger();

        builder
            .Services.AddOptions()
            .AddLogging(configure =>
            {
                if (useConsoleLogging)
                {
                    configure
                        .ClearProviders()
                        .SetMinimumLevel(inMemoryLogLevel)
                        .AddSerilog()
                        .AddTerminal(opt =>
                        {
                            opt.SingleLine = true;
                            opt.UseColors = true;
                            opt.UseUtcTimestamp = true;
                        });
                }
                else
                {
                    configure
                        .ClearProviders()
                        .SetMinimumLevel(inMemoryLogLevel)
                        .AddInMemory()
                        .AddSerilog();
                }
            })
            .Configure<Options>(builder.Configuration)
            .AddLiveTiming(builder.Configuration)
            .AddSingleton<WebSocketSynchroniser>()
            .InjectClipboard();

        builder.WebHost.UseServer(new NullServer());

        return builder;
    }
}
