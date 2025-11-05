using Xabe.FFmpeg.Events;

namespace MD.Infra.Converter;

internal sealed class DownloadData
{
    internal DownloadData(string title, string url)
    {
        Title = title;
        Url = url;
        Status = DownloadStatus.Pending;
    }

    public string Title { get; set; }
    public string Url { get; set; }
    public string? ErrorMessage { get; set; }
    public ConversionProgressEventArgs? ConversionProgress { get; set; }
    public TimeSpan TimeElapsed { get; set; }
    internal DownloadStatus Status { get; set; }

    public void Print()
    {
        Title.Write(ConsoleColor.Cyan);

        switch (Status)
        {
            case DownloadStatus.Pending:
                $" {Status}".WriteLine(ConsoleColor.Yellow);
                break;
            case DownloadStatus.InProgress:
                var progress = ConversionProgress!;
                var percent = progress.Percent;
                $" {progress.Duration} of {progress.TotalLength}".Write();
                $" ({percent})".WriteLine(percent < 25 ? ConsoleColor.Red : percent < 80 ? ConsoleColor.Yellow : ConsoleColor.Blue);
                break;
            case DownloadStatus.Completed:
                $" {Status}".WriteLine(ConsoleColor.Green);
                break;
            case DownloadStatus.Cancelled:
                $" {Status}".WriteLine(ConsoleColor.DarkMagenta);
                break;
            case DownloadStatus.Failed:
                $" {Status}. Error: ".Write(ConsoleColor.Red);
                ErrorMessage.WriteLine(ConsoleColor.DarkRed);
                break;
        }
    }
}