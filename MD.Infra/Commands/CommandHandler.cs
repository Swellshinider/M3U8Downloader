using MD.Infra.Downloader;

namespace MD.Infra.Commands;

/// <summary>
/// Represents a command handler.
/// </summary>
public static class CommandHandler
{
    private static readonly List<Command> _commands;
    private static readonly DownloadManager _downloadManager = DownloadManager.Instance;

    static CommandHandler()
    {
        Console.CancelKeyPress += (s, e) =>
        {
            e.Cancel = true;
            Exit(null);
        };

        _commands =
        [
            new(CommandType.Help, DisplayHelp)
            {
                Description = "Displays this help message.",
                Usage = "help <command> | You can use with a command to get more details about it."
            },
            new(CommandType.Exit, Exit)
            {
                Description = "Exits the application.",
                Usage = "Exits the application."
            },
            new (CommandType.Clear, Clear)
            {
                Description = "Clears the console.",
                Usage = "clear | Clears the console."
            },
            new(CommandType.Add, AddUrl, [ArgumentCantBeNull])
            {
                Description = "Add a new url to the download queue.",
                Usage = "add <url/path> | Adds a new url or local path to the download queue."
            },
            new(CommandType.Remove, RemoveUrl)
            {
                Description = "Removes the last added url from the download queue.",
                Usage = "remove | Removes the last added url from the download queue."
            },
            new(CommandType.Stop, StopDownload)
            {
                Description = "Stops the current download.",
                Usage = "stop | Stops the current download in progress."
            },
            new(CommandType.Start, StartDownload)
            {
                Description = "Starts the download process.",
                Usage = "start | Starts downloading the queued items."
            },
            new(CommandType.Status, ShowStatus)
            {
                Description = "Displays the current download status.",
                Usage = "status | Displays whether a download is in progress or not."
            },
            new(CommandType.Output, SetOutputDirectory, [ArgumentCantBeNull])
            {
                Description = "Sets the output directory for downloads.",
                Usage = "output <path> | Sets the output directory where downloads will be saved."
            }
        ];
    }

    private static ExecutionResult SetOutputDirectory(string? arg)
    {
        throw new NotImplementedException();
    }

    private static ExecutionResult ShowStatus(string? arg)
    {
        throw new NotImplementedException();
    }

    private static ExecutionResult RemoveUrl(string? arg)
    {
        throw new NotImplementedException();
    }

    private static ExecutionResult StartDownload(string? arg)
    {
        throw new NotImplementedException();
    }

    private static ExecutionResult AddUrl(string? arg)
    {
        throw new NotImplementedException();
    }

    private static ExecutionResult DisplayHelp(string? arg)
    {
        if (arg.IsNull())
        {
            "Available commands:".WriteLine();

            foreach (var cmd in _commands)
                $"{cmd.Type,10}: {cmd.Description,-20}".WriteLine(ConsoleColor.Green);

            "\nYou can type".Write();
            " help <command> ".Write(ConsoleColor.Green);
            "to get more details about a command.\n".WriteLine();
            return ExecutionResult.SuccessNoMessage();
        }

        var command = _commands.FirstOrDefault(c => c.Type.ToString() == arg);

        if (command is null)
            return ExecutionResult.FailValidation($"Unknown command: {arg}");

        "Help ".Write();
        $"{arg}:".WriteLine(ConsoleColor.Green);

        $"    {command.Usage}".WriteLine();
        "".WriteLine();

        return ExecutionResult.SuccessNoMessage();
    }

    private static ExecutionResult Exit(string? arg)
    {
        var closeResult = _downloadManager.IsDownloading ? StopDownload(arg) : ExecutionResult.SuccessNoMessage();

        if (!closeResult.Success)
        {
            $"Unable to stop download. {closeResult.Message}".WriteLine(ConsoleColor.Red);
            if (!Util.ConfirmUserAction("Do you want to exit anyway?"))
                return Exit(arg);
        }

        return ExecutionResult.Exit();
    }

    private static ExecutionResult Clear(string? arg)
    {
        Console.Clear();
        return ExecutionResult.SuccessNoMessage();
    }

    private static ExecutionResult StopDownload(string? arg)
    {
        try
        {
            if (!_downloadManager.IsDownloading) // user has unsaved changes
            {
                "No download in progress.".WriteLine();
                return ExecutionResult.SuccessNoMessage();
            }

            "Stopping download...".WriteLine();
            _downloadManager.Stop();
            return new ExecutionResult(true, "Vault closed successfully.");
        }
        catch (Exception e)
        {
            return new ExecutionResult(false, e.Message);
        }
    }

    #region [ Validation Methods ]
    private static bool ArgumentCantBeNull(string? input, out string message)
    {
        if (input.IsNull())
        {
            message = "Argument cannot be null.";
            return false;
        }

        message = "";
        return true;
    }
    #endregion

    /// <summary>
    /// Executes a command.
    /// </summary>
    internal static ExecutionResult Execute(string input)
    {
        var parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var inputCommand = parts[0];
        var arguments = parts.Length <= 1
                            ? null
                            : string.Join(' ', parts.Skip(1));

        if (!Enum.TryParse<CommandType>(inputCommand, true, out var commandType))
            return ExecutionResult.FailValidation($"Unknown command: '{inputCommand}'");

        var command = _commands.FirstOrDefault(c => c.Type == commandType);

        if (command is null)
            return ExecutionResult.FailValidation($"Command not found: '{inputCommand}'");

        return command.Execute(arguments);
    }
}