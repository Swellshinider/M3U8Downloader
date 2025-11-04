using MD.Infra.Downloader;

namespace MD.Infra.Commands;

public static class CommandHandler
{
    private static readonly DownloadManager _downloadManager = DownloadManager.Instance;
    private static readonly List<Command> _commands =
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
        new(CommandType.Add, AddUrl, ArgumentCantBeNull)
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
        new(CommandType.Output, SetOutputDirectory, ArgumentCantBeNull)
        {
            Description = "Sets the output directory for downloads.",
            Usage = "output <path> | Sets the output directory where downloads will be saved."
        }
    ];

    static CommandHandler()
    {
        Console.CancelKeyPress += async (s, e) =>
        {
            e.Cancel = true;
            await Exit(null);
        };
    }

    /// <summary>
    /// Executes a command.
    /// </summary>
    internal static async Task<ExecutionResult> Execute(string input)
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

        return await command.Execute(arguments);
    }

    private static async Task<ExecutionResult> DisplayHelp(string? arg)
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

    private static async Task<ExecutionResult> AddUrl(string? arg)
    {
        try
        {
            _downloadManager.AddToQueue(arg!);
            return new ExecutionResult(true, $"Added '{arg}' to the download queue.");
        }
        catch (Exception e)
        {
            return new ExecutionResult(false, e.Message);
        }
    }

    private static async Task<ExecutionResult> RemoveUrl(string? arg)
    {
        try
        {
            var removed = _downloadManager.RemoveFromQueue();
            return new ExecutionResult(true, $"Removed '{removed}' from the download queue.");
        }
        catch (Exception e)
        {
            return new ExecutionResult(false, e.Message);
        }
    }

    private static async Task<ExecutionResult> Clear(string? arg)
    {
        Console.Clear();
        return ExecutionResult.SuccessNoMessage();
    }

    private static async Task<ExecutionResult> ShowStatus(string? arg)
    {
        try
        {
            await _downloadManager.DisplayStatus();
            return new ExecutionResult(true, "Status displayed.");
        }
        catch (Exception e)
        {
            return new ExecutionResult(false, e.Message);
        }
    }
    
    private static async Task<ExecutionResult> SetOutputDirectory(string? arg)
    {
        try
        {
            _downloadManager.SetOutputDirectory(arg!);
            return new ExecutionResult(true, $"Output directory set to '{arg}'.");
        }
        catch (Exception e)
        {
            return new ExecutionResult(false, e.Message);
        }
    }

    private static async Task<ExecutionResult> StartDownload(string? arg)
    {
        try
        {
            await _downloadManager.Start();
            return new ExecutionResult(true, "Download started. You can monitor the progress by typing 'status'.");
        }
        catch (Exception e)
        {
            return new ExecutionResult(false, e.Message);
        }
    }

    private static async Task<ExecutionResult> StopDownload(string? arg)
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
            return new ExecutionResult(true, "DOwnload stopped successfully.");
        }
        catch (Exception e)
        {
            return new ExecutionResult(false, e.Message);
        }
    }

    private static async Task<ExecutionResult> Exit(string? arg)
    {
        var closeResult = _downloadManager.IsDownloading ? await StopDownload(arg) : ExecutionResult.SuccessNoMessage();

        if (!closeResult.Success)
        {
            $"Unable to stop download. {closeResult.Message}".WriteLine(ConsoleColor.Red);
            if (!Util.ConfirmUserAction("Do you want to exit anyway?"))
                return await Exit(arg);
        }

        return ExecutionResult.Exit();
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
}