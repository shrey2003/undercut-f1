using System.CommandLine;
using UndercutF1.Console;
using UndercutF1.Console.Graphics;

var isVerboseOption = new Option<bool?>("--verbose", "-v")
{
    Description = "Whether verbose logging should be enabled",
};
var isApiEnabledOption = new Option<bool?>("--with-api")
{
    Description = "Whether the API endpoint should be exposed at http://localhost:61937",
};
var dataDirectoryOption = new Option<DirectoryInfo?>("--data-directory")
{
    Description = "The directory to which timing data will be read from and written to",
};
var logDirectoryOption = new Option<DirectoryInfo?>("--log-directory")
{
    Description = "The directory to which logs will be written to",
};
var notifyOption = new Option<bool?>("--notify")
{
    Description =
        "Whether audible BELs are sent to your terminal when new race control messages are received",
};
var preferFfmpegOption = new Option<bool?>("--prefer-ffmpeg")
{
    Description =
        "Prefer the usage of `ffplay` for playing Team Radio on Mac/Linux, instead of afplay/mpg123. `ffplay` is always used on Windows",
};
var forceGraphicsProtocol = new Option<GraphicsProtocol?>("--force-graphics-protocol")
{
    Description = "Forces the usage of a particular graphics protocol.",
};

var rootCommand = new RootCommand("undercutf1")
{
    isVerboseOption,
    isApiEnabledOption,
    dataDirectoryOption,
    logDirectoryOption,
    notifyOption,
    preferFfmpegOption,
    forceGraphicsProtocol,
};

rootCommand.SetAction(parseResult =>
    CommandHandler.Root(
        parseResult.GetValue(isApiEnabledOption),
        parseResult.GetValue(dataDirectoryOption),
        parseResult.GetValue(logDirectoryOption),
        parseResult.GetValue(isVerboseOption),
        parseResult.GetValue(notifyOption),
        parseResult.GetValue(preferFfmpegOption),
        parseResult.GetValue(forceGraphicsProtocol)
    )
);

var yearArgument = new Argument<int>("year") { Description = "The year the meeting took place." };
var meetingKeyOption = new Option<int?>("--meeting-key", "--meeting", "-m")
{
    Description =
        "The Meeting Key of the session to import. If not provided, all meetings in the year will be listed.",
};
var sessionKeyOption = new Option<int?>("--session-key", "--session", "-s")
{
    Description =
        "The Session Key of the session inside the selected meeting to import. If not provided, all sessions in the provided meeting will be listed.",
};

var importCommand = new Command(
    "import",
    """
    Imports data from the F1 Live Timing API, if you have missed recording a session live. 
    The data is imported in a format that can be replayed in real-time using undercutf1.
    """
)
{
    yearArgument,
    meetingKeyOption,
    sessionKeyOption,
    dataDirectoryOption,
    logDirectoryOption,
    isVerboseOption,
};

importCommand.SetAction(res =>
    CommandHandler.ImportSession(
        res.GetValue(yearArgument),
        res.GetValue(meetingKeyOption),
        res.GetValue(sessionKeyOption),
        res.GetValue(dataDirectoryOption),
        res.GetValue(logDirectoryOption),
        res.GetValue(isVerboseOption)
    )
);

rootCommand.Subcommands.Add(importCommand);

var infoCommand = new Command(
    "info",
    """
    Prints diagnostics about undercutf1, and the terminal in the command is run in.
    """
)
{
    dataDirectoryOption,
    logDirectoryOption,
    isVerboseOption,
    notifyOption,
    preferFfmpegOption,
    forceGraphicsProtocol,
};
infoCommand.SetAction(res =>
    CommandHandler.GetInfo(
        res.GetValue(dataDirectoryOption),
        res.GetValue(logDirectoryOption),
        res.GetValue(isVerboseOption),
        res.GetValue(notifyOption),
        res.GetValue(preferFfmpegOption),
        res.GetValue(forceGraphicsProtocol)
    )
);
rootCommand.Subcommands.Add(infoCommand);

var graphicsProtocolArgument = new Argument<GraphicsProtocol>("The graphics protocol to use");
var imageFilePathArgument = new Argument<FileInfo>("file");

var imageCommand = new Command(
    "image",
    """
    Displays the image from the provided filepath in the terminal, using the appropiate graphics protocol.
    """
)
{
    imageFilePathArgument,
    graphicsProtocolArgument,
    isVerboseOption,
};
imageCommand.SetAction(res =>
    CommandHandler.OutputImage(
        res.GetRequiredValue(imageFilePathArgument),
        res.GetValue(graphicsProtocolArgument),
        res.GetValue(isVerboseOption)
    )
);
rootCommand.Subcommands.Add(imageCommand);

var loginCommand = new Command(
    "login",
    """
    Login to your Formula 1 account to unlock all data feeds (like the driver tracker).
    """
)
{
    isVerboseOption,
};
loginCommand.SetAction(res => CommandHandler.LoginToFormula1Account(res.GetValue(isVerboseOption)));
rootCommand.Subcommands.Add(loginCommand);

var logoutCommand = new Command(
    "logout",
    """
    Logout of your Formula 1 account.
    """
)
{
    isVerboseOption,
};
logoutCommand.SetAction(res => CommandHandler.LogoutOfFormula1Account());
rootCommand.Subcommands.Add(logoutCommand);

await new CommandLineConfiguration(rootCommand).InvokeAsync(args);
