using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.OpenApi.Models;
using UndercutF1.Console.Api;
using UndercutF1.Console.Audio;
using UndercutF1.Console.ExternalPlayerSync;
using UndercutF1.Console.Graphics;
using UndercutF1.Data;

namespace UndercutF1.Console;

public static partial class CommandHandler
{
    public static async Task Root(
        bool? isApiEnabled,
        DirectoryInfo? dataDirectory,
        DirectoryInfo? logDirectory,
        bool? isVerbose,
        bool? notifyEnabled,
        bool? preferFfmpeg,
        GraphicsProtocol? forceGraphicsProtocol
    )
    {
        var builder = GetBuilder(
            isApiEnabled: isApiEnabled,
            dataDirectory: dataDirectory,
            logDirectory: logDirectory,
            isVerbose: isVerbose,
            notifyEnabled: notifyEnabled,
            preferFfmpeg: preferFfmpeg,
            forceGraphicsProtocol: forceGraphicsProtocol
        );

        builder
            .Services.AddSingleton<ConsoleLoop>()
            .AddSingleton<State>()
            .AddInputHandlers()
            .AddDisplays()
            .AddSingleton<INotifyHandler, NotifyHandler>()
            .AddSingleton<TerminalInfoProvider>()
            .AddSingleton<AudioPlayer>()
            .AddSingleton<WebSocketSynchroniser>()
            .AddHostedService(sp => sp.GetRequiredService<ConsoleLoop>())
            .AddHostedService(sp => sp.GetRequiredService<WebSocketSynchroniser>());

        var options = builder.Configuration.Get<Options>() ?? new();

        if (options.ApiEnabled)
        {
            builder.WebHost.UseKestrel(opt => opt.ListenAnyIP(0xF1F1)); // listens on 61937

            builder
                .Services.AddRouting()
                .AddEndpointsApiExplorer()
                .AddSwaggerGen(c =>
                {
                    c.CustomSchemaIds(type =>
                        type.FullName!.Replace("UndercutF1.Data.", string.Empty)
                            .Replace("UndercutF1.Console.Api.", string.Empty)
                            .Replace("+", string.Empty)
                    );
                    c.SwaggerDoc(
                        "v1",
                        new OpenApiInfo { Title = "Undercut F1 API", Version = "v1" }
                    );
                });
        }

        builder.Services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(x =>
        {
            x.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
            x.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        });

        // The Swagger UI only respects the Mvc JsonOptions, so set both even though we only need the Http.Json one for minimal APIs
        builder.Services.Configure<Microsoft.AspNetCore.Mvc.JsonOptions>(x =>
        {
            x.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            x.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        });

        var app = builder.Build();

        if (options.ApiEnabled)
        {
            app.UseSwagger().UseSwaggerUI();

            app.MapSwagger();

            app.MapControlEndpoints().MapTimingEndpoints();
        }

        app.Logger.LogInformation("{Options}", options);

        Whisper.net.Logger.LogProvider.AddLogger(
            (level, msg) =>
            {
                switch (level)
                {
                    case Whisper.net.Logger.WhisperLogLevel.Error:
                        app.Logger.LogError("Whisper: {Message}", msg?.Trim('\n'));
                        break;
                    case Whisper.net.Logger.WhisperLogLevel.Warning:
                        app.Logger.LogDebug("Whisper: {Message}", msg?.Trim('\n'));
                        break;
                    case Whisper.net.Logger.WhisperLogLevel.Debug:
                        app.Logger.LogDebug("Whisper: {Message}", msg?.Trim('\n'));
                        break;
                    default:
                        app.Logger.LogDebug("Whisper {Level}: {Message}", level, msg?.Trim('\n'));
                        break;
                }
            }
        );

        await EnsureConfigFileExistsAsync(app.Logger);

        await app.RunAsync();
    }

    private static async Task EnsureConfigFileExistsAsync(ILogger logger)
    {
        try
        {
            if (File.Exists(Options.ConfigFilePath))
            {
                return;
            }
            var schemaLocation =
                "https://raw.githubusercontent.com/JustAman62/undercut-f1/refs/heads/master/config.schema.json";
            var defaultConfigFile = $$"""
                {
                    "$schema": "{{schemaLocation}}"
                }
                """;

            logger.LogInformation(
                "Writing default configuration file to {Path}",
                Options.ConfigFilePath
            );
            await File.WriteAllTextAsync(Options.ConfigFilePath, defaultConfigFile);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to write default configuration file to {Path}",
                Options.ConfigFilePath
            );
        }
    }
}
