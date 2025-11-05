using System.Collections.Concurrent;
using MD.Infra.Commands;
using MD.Infra.Converter;
using Xabe.FFmpeg.Downloader;

namespace MD.Infra;

public static class Application
{
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
        new(CommandType.Add, AddUrl)
        {
            Description = "Initialize addition wizard.",
            Usage = "add | Initialize addition wizard."
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
    private static readonly M3U8Converter _converter = new(true, 4);

    private static readonly string PROMPT = "> ";
    private static readonly Repl.Repl _repl = new();
    private static readonly ConcurrentQueue<DownloadData> _downloadDatas = [];

    private static string? _outputDir;
    private static CancellationTokenSource? _tokenSource;

    public static async Task Run()
    {
        "Welcome to M3U8Downloader CLI!\nType ".Write();
        "help ".Write(ConsoleColor.Green);
        "to see available commands.\n".WriteLine();
        "You can press Ctrl + C anytime to exit.\n".WriteLine();

        var commandHandler = new CommandHandler(_commands);

        Console.CancelKeyPress += async (s, e) =>
        {
            e.Cancel = true;
            _tokenSource?.Cancel();
            await Exit(null);
        };

        while (true)
        {
            var input = _repl.Run(PROMPT);
            var executionResult = await commandHandler.Execute(input);

            $"{executionResult.Message}".WriteLine(executionResult.Success
                                                                ? ConsoleColor.Green
                                                                : ConsoleColor.Red);
            if (executionResult.ShouldExit)
            {
                "Exiting...\n".WriteLine();
                break;
            }
        }
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
            "Please, provide url: ".WriteLine(ConsoleColor.Cyan);
            var url = _repl.AskUntilNotEmpty(PROMPT);
            "Please, provide title: ".WriteLine(ConsoleColor.Cyan);
            var title = _repl.AskUntilNotEmpty(PROMPT);
            var downloadData = new DownloadData(title, url);
            _downloadDatas.Enqueue(downloadData);
            return new ExecutionResult(true, $"Added '{title}' to the download queue.");
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
            if (_downloadDatas.IsEmpty)
                return new ExecutionResult(false, "Download queue is already empty");

            if (!_downloadDatas.TryDequeue(out var removed))
                return new ExecutionResult(false, "Unable to remove from the download queue");

            return new ExecutionResult(true, $"Removed '{removed.Title}' from the download queue.");
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
            foreach (var data in _downloadDatas)
                data.Print();

            return ExecutionResult.SuccessNoMessage();
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
            _outputDir = arg;
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
            if (_outputDir.IsNull())
                return new ExecutionResult(false, "Please set output directory before start. Use output command.");

            if (_tokenSource is null || _tokenSource.IsCancellationRequested)
                _tokenSource = new CancellationTokenSource();

            var availableDownloadData = _downloadDatas
                .Where(p => p.Status != DownloadStatus.InProgress && p.Status != DownloadStatus.Completed);
            _ = _converter.ConvertAsync([.. availableDownloadData], _outputDir!, _tokenSource.Token);

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
            if (!_converter.IsRunning)
            {
                "No download in progress.".WriteLine();
                return ExecutionResult.SuccessNoMessage();
            }

            "Stopping download...".WriteLine();
            await _tokenSource!.CancelAsync();
            return new ExecutionResult(true, "Download stopped successfully.");
        }
        catch (Exception e)
        {
            return new ExecutionResult(false, e.Message);
        }
    }

    private static async Task<ExecutionResult> Exit(string? arg)
    {
        if (_converter.IsRunning && !Util.ConfirmUserAction("You still have downloads running, are you sure you want to quit?"))
            return ExecutionResult.SuccessNoMessage();
        
        var closeResult = _converter.IsRunning ? await StopDownload(arg) : ExecutionResult.SuccessNoMessage();

        if (!closeResult.Success)
        {
            $"Unable to stop download. {closeResult.Message}".WriteLine(ConsoleColor.Red);
            if (!Util.ConfirmUserAction("Do you want to exit anyway?"))
                return await Exit(arg);
        }

        return ExecutionResult.Exit();
    }

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
}