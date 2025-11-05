using MD.Infra.Commands;
using Xabe.FFmpeg.Downloader;

namespace MD.Infra;

public static class Application
{
    public static async Task Run()
    {
        "Checking for FFmpeg binaries...".WriteLine();
        await FFmpegDownloader.GetLatestVersion(FFmpegVersion.Official);
        "FFmpeg is ready.".WriteLine(ConsoleColor.Green);
        "\n".WriteLine();
        
        "Welcome to LealVault CLI!\nType ".Write();
        "help ".Write(ConsoleColor.Green);
        "to see available commands.\n".WriteLine();
        "You can press Ctrl + C anytime to exit.\n".WriteLine();

        var repl = new Repl.Repl();
        var prompt = "md> ";

        while (true)
        {
            var input = repl.Run(prompt);
            var executionResult = await CommandHandler.Execute(input);

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
}