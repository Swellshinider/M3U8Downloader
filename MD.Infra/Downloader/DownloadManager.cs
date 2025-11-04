using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace MD.Infra.Downloader;

internal sealed class DownloadManager
{
    private static readonly Lazy<DownloadManager> _instance = new(() => new DownloadManager());

    public static DownloadManager Instance => _instance.Value;

    private string? _outputDirectory;
    private readonly ConcurrentQueue<DownloadInfo> _downloadQueue;

    private DownloadManager()
    {
        _downloadQueue = new ConcurrentQueue<DownloadInfo>();
    }

    public bool IsDownloading { get; private set; }

    internal async Task Start()
    {
        if (IsDownloading)
            throw new InvalidOperationException("A download is already in progress.");

        IsDownloading = true;
        // Download logic goes here
    }

    internal void Stop()
    {
        if (!IsDownloading)
            throw new InvalidOperationException("No download is currently in progress.");

        if (_downloadQueue.IsEmpty)
            throw new InvalidOperationException("The download queue is empty.");

        // Logic to stop the download goes here
        IsDownloading = false;        
    }

    internal void AddToQueue(string url)
    {
        if (url.IsNull())
            throw new ArgumentNullException(nameof(url), "URL cannot be null or empty.");

        if (!Uri.IsWellFormedUriString(url, UriKind.Absolute) && !File.Exists(url))
            throw new ArgumentException("The provided URL or path is not valid.", nameof(url));

        _downloadQueue.Enqueue(new DownloadInfo(url));
    }

    internal string RemoveFromQueue()
    {
        if (!_downloadQueue.TryDequeue(out var downloadInfo))
            throw new InvalidOperationException("The download queue is empty.");

        return downloadInfo.Url;
    }

    internal void SetOutputDirectory(string outputPath)
    {
        if (_downloadQueue.IsEmpty)
            throw new InvalidOperationException("The download queue is empty.");

        if (_outputDirectory.IsNull())
            throw new InvalidOperationException("Output directory is not set.");

        if (!Directory.Exists(_outputDirectory))
            Directory.CreateDirectory(_outputDirectory!);

        if (IsDownloading)
            throw new InvalidOperationException("Cannot change output directory while downloading.");

        _outputDirectory = outputPath;
    }
    
    internal async Task DisplayStatus()
    {
        if (_downloadQueue.IsEmpty)
        {
            "No downloads in the queue.".WriteLine();
            return;
        }

        "Current Download Queue:".WriteLine();
        foreach (var downloadInfo in _downloadQueue)
        {
            $"- {downloadInfo.Url}".WriteLine();
        }
    }
}