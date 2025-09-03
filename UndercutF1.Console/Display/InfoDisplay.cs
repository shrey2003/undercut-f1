using System.Runtime.InteropServices;
using Microsoft.Extensions.Options;
using Spectre.Console;
using Spectre.Console.Rendering;
using UndercutF1.Console.ExternalPlayerSync;
using UndercutF1.Data;

namespace UndercutF1.Console;

public sealed class InfoDisplay(
    TerminalInfoProvider terminalInfo,
    WebSocketSynchroniser synchroniser,
    Formula1Account accountService,
    IOptions<Options> options
) : IDisplay
{
    public Screen Screen => Screen.Info;

    public Task<IRenderable> GetContentAsync()
    {
        var authStatus = accountService.IsAuthenticated.Value;
        var payload = accountService.Payload.Value;
        var content = $"""
            [bold]Configuration[/]
            [bold]Data Directory:[/]        {options.Value.DataDirectory}
            [bold]Log Directory:[/]         {options.Value.LogDirectory}
            [bold]Audible Notifications:[/] {options.Value.Notify}
            [bold]Verbose Mode:[/]          {options.Value.Verbose}
            [bold]Forced Protocol:[/]       {options.Value.ForceGraphicsProtocol?.ToString() ?? "None"}
            [bold]F1 TV Account:[/]         {(payload is null ? "None" : authStatus)}
            [bold]Config Override File:[/]  {File.Exists(
                Options.ConfigFilePath
            )} ({Options.ConfigFilePath})
            See https://github.com/JustAman62/undercut-f1#configuration for information on how to configure these options.
            [dim]{payload}[/]

            [bold]Terminal Diagnostics[/]
            [bold]TERM_PROGRAM:[/]        {Environment.GetEnvironmentVariable("TERM_PROGRAM")}
            [bold]Window Size W/H:[/]     {terminalInfo.TerminalSize.Value.Width}/{terminalInfo.TerminalSize.Value.Height} ({terminalInfo.TerminalSize.Value.Height / Terminal.Size.Height})
            [bold]Kitty Graphics:[/]      {terminalInfo.IsKittyProtocolSupported.Value}
            [bold]iTerm2 Graphics:[/]     {terminalInfo.IsITerm2ProtocolSupported.Value}
            [bold]Sixel Graphics:[/]      {terminalInfo.IsSixelSupported.Value}
            [bold]Synchronized Output:[/] {terminalInfo.IsSynchronizedOutputSupported.Value}
            [bold]Version:[/]             {ThisAssembly.AssemblyInformationalVersion}
            [bold]Runtime Identifier:[/]  {RuntimeInformation.RuntimeIdentifier}
            
            [bold]OS:[/] {RuntimeInformation.OSDescription}

            [bold]External Service Sync[/]
            [bold]Service Status:[/]      {synchroniser.State} / {synchroniser.ExecuteTask?.Status}
            [bold]Enabled:[/]             {options.Value.ExternalPlayerSync?.Enabled ?? false}
            [bold]Service Type:[/]        {options.Value.ExternalPlayerSync?.ServiceType}
            [bold]Url:[/]                 {options.Value.ExternalPlayerSync?.Url}
            [bold]Connect Interval:[/]    {options.Value.ExternalPlayerSync?.WebSocketConnectInterval}
            """;

        return Task.FromResult<IRenderable>(new Panel(new Markup(content)).Expand());
    }
}
