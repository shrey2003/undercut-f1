using System.Text.Json.Nodes;
using SharpWebview;
using SharpWebview.Content;
using Spectre.Console;
using UndercutF1.Data;

namespace UndercutF1.Console;

public static partial class CommandHandler
{
    public static async Task LoginToFormula1Account(bool? isVerbose)
    {
        var builder = GetBuilder(
            isVerbose: isVerbose,
            useConsoleLogging: isVerbose.GetValueOrDefault()
        );

        var app = builder.Build();

        var accountService = app.Services.GetRequiredService<Formula1Account>();
        var existingPayload = accountService.Payload.Value;

        // Allow a relogin on the day of expiry but not before
        if (existingPayload is not null && existingPayload.Expiry.Date != DateTime.Today)
        {
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine(
                $"""
                An access token is already configured in [bold]{Options.ConfigFilePath}[/].
                [dim]{existingPayload}[/]
                This token will expire on [bold]{existingPayload.Expiry:yyyy-MM-dd}[/], at which point you'll need to login again.
                If you'd like to log in again, please first logout using [bold]undercutf1 logout[/].
                """
            );
            return;
        }

        var preamble = $"""
            Login to your Formula 1 Account (which has any level of F1 TV subscription) to access all the Live Timing feeds and unlock all features of undercut-f1.

            An account is [bold]NOT[/] needed for undercut-f1 to function, it only unlocks the following features:
            - Driver Tracker (live position of cars on track)
            - Pit Stop times
            - Championship tables with live prediction
            - DRS active indicator on Timing Screen

            Additionally, logging in is [bold]NOT[/] required if you import data for already completed sessions, as all data is always available after a session is complete.

            Once logged in, your access token will be stored in [bold]{Options.ConfigFilePath}[/]. Your actual account credentials will not be stored anywhere.
            Simply remove the token entry from the file, or run [bold]undercutf1 logout[/] to prevent undercut-f1 from using your token.
            """;

        AnsiConsole.MarkupLine(preamble);
        AnsiConsole.WriteLine();

        if (!AnsiConsole.Confirm("Proceed to login?", defaultValue: false))
        {
            return;
        }

        AnsiConsole.MarkupLine(
            "Opening a browser window for you to login to your Formula 1 account. Once logged in, close the browser window and return here."
        );

        var token = LoginWithWebView();

        if (token is null)
            return;

        var authResult = accountService.CheckToken(token, out var payload);

        if (authResult != Formula1Account.AuthenticationResult.Success)
        {
            AnsiConsole.MarkupLine(
                $"""
                [red]Invalid token received from login. Please try again.
                Ensure the account you are logging in with has an active F1 TV subscription.
                Auth Result: [bold]{authResult}[/][/]

                [dim]{payload}[/]
                """
            );
            return;
        }

        await EnsureConfigFileExistsAsync(app.Logger);

        // Read in the existing config file, then write out the file including the access token
        // We read the file rather than just save the config to try and avoid changing other contents in the file
        // e.g. we might have config set by environment variables that shouldn't end up in the file
        // or the file might have keys in it that we don't read in to config, but we shouldn't remove from the file.
        var configFileJson = await ReadConfigFileAsync();
        configFileJson[nameof(Options.Formula1AccessToken)] = token;
        await File.WriteAllTextAsync(
            Options.ConfigFilePath,
            configFileJson.ToJsonString(Constants.JsonSerializerOptions)
        );

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine(
            $"""
            [green]Login Successful.[/] Your access token has been saved in [bold]{Options.ConfigFilePath}[/].
            This token will expire on [bold]{payload?.Expiry:yyyy-MM-dd}[/], at which point you'll need to login again.
            """
        );
    }

    public static async Task LogoutOfFormula1Account()
    {
        AnsiConsole.MarkupLine(
            $"""
            Logging out will remove your access token stored in [bold]{Options.ConfigFilePath}[/].
            To log back in again in the future, simply run [bold]undercut-f1 login[/].
            """
        );
        AnsiConsole.WriteLine();

        var configFileJson = await ReadConfigFileAsync();
        configFileJson.Remove(nameof(Options.Formula1AccessToken));

        await File.WriteAllTextAsync(
            Options.ConfigFilePath,
            configFileJson.ToJsonString(Constants.JsonSerializerOptions)
        );

        AnsiConsole.MarkupLine(
            $"""
            [green]Logout successful.[/]
            """
        );
    }

    private static string? LoginWithWebView()
    {
        using var webView = new Webview(debug: false, interceptExternalLinks: false);

        var cookie = default(string);

        webView
            .SetTitle("Login to Formula 1")
            .SetSize(1024, 768, WebviewHint.None)
            .SetSize(400, 400, WebviewHint.Min)
            .Bind(
                "sendLoginCookie",
                (id, req) =>
                {
                    // The params are sent as an array of strings
                    // We know theres only one element, so strip the array start and end chars to get the element.
                    cookie = req[2..^2];

                    AnsiConsole.WriteLine();
                    AnsiConsole.MarkupLine(
                        """
                        [bold green]Received login cookie, you may now close the browser.[/]
                        """
                    );
                    AnsiConsole.WriteLine();
                }
            )
            .InitScript(
                """
                function getCookie(name) {
                    return (name = (document.cookie + ';').match(new RegExp(name + '=.*;'))) && name[0].split(/=|;/)[1];
                }

                var previousCookie = "";
                setInterval(() => {
                    let cookie = getCookie('login-session');
                    if (cookie && previousCookie !== cookie) {
                        sendLoginCookie(cookie);
                        previousCookie = cookie;
                        document.body.insertAdjacentText('afterbegin', 'Login Complete, you may now close the browser');
                    }
                }, 1000);
                """
            )
            .Navigate(new UrlContent("https://account.formula1.com/#/en/login"))
            // Run() blocks until the WebView is closed.
            .Run();

        if (cookie is null)
        {
            AnsiConsole.MarkupLine(
                "[red]Failed to retrieve login session cookie, please try again[/]"
            );
            return null;
        }

        return cookie;
    }

    private static async Task<JsonObject> ReadConfigFileAsync()
    {
        var configFileContents = await File.ReadAllTextAsync(Options.ConfigFilePath);
        return JsonNode
                .Parse(configFileContents, new() { PropertyNameCaseInsensitive = true })
                ?.AsObject() ?? throw new InvalidOperationException("Unable to parse config JSON");
    }
}
