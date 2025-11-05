using System.Collections.Concurrent;
using Xabe.FFmpeg;

namespace MD.Infra.Downloader;

internal sealed class DownloadManager
{
    private static readonly Lazy<DownloadManager> _instance = new(() => new DownloadManager());
    public static DownloadManager Instance => _instance.Value;

    private string? _outputDirectory;
    private string? _patternFileName;
    private readonly ConcurrentQueue<DownloadInfo> _downloadQueue;
    private readonly ConcurrentBag<DownloadInfo> _completedDownloads = new();
    private readonly ConcurrentDictionary<string, DownloadInfo> _inProgressDownloads;
    
    private readonly SemaphoreSlim _downloadSemaphore = new(4); 
    private CancellationTokenSource? _cts;

    private DownloadManager()
    {
        _downloadQueue = new ConcurrentQueue<DownloadInfo>();
        _inProgressDownloads = new ConcurrentDictionary<string, DownloadInfo>();
    }

    public bool IsDownloading { get; private set; }

    /// <summary>
    /// Starts the download process in a non-blocking background task.
    /// </summary>
    internal void Start()
    {
        if (IsDownloading)
            throw new InvalidOperationException("A download is already in progress.");

        if (_outputDirectory.IsNull())
            throw new InvalidOperationException("Output directory must be set before starting. Use the 'output' command.");
        
        if (_patternFileName.IsNull())
            throw new InvalidOperationException("Pattern file name must be set before starting. Use the 'pattern' command.");

        if (_downloadQueue.IsEmpty)
            throw new InvalidOperationException("Download queue is empty. Add URLs using the 'add' command.");

        IsDownloading = true;
        _cts = new CancellationTokenSource();
        _ = Task.Run(ProcessQueueAsync, _cts.Token);
    }
    
    /// <summary>
    /// Continuously processes the queue until it's empty or canceled.
    /// </summary>
    private async Task ProcessQueueAsync()
    {
        var token = _cts!.Token;
        
        while (IsDownloading && !token.IsCancellationRequested && _downloadQueue.TryDequeue(out var downloadInfo))
        {
            try
            {
                await _downloadSemaphore.WaitAsync(token);

                _inProgressDownloads.TryAdd(downloadInfo.Url, downloadInfo);

                _ = Task.Run(() => DownloadFileAsync(downloadInfo, token), token);
            }
            catch (OperationCanceledException)
            {
                "Download process was canceled.".WriteLine(ConsoleColor.Yellow);
                _inProgressDownloads.TryRemove(downloadInfo.Url, out _);
                break;
            }
            catch (Exception ex)
            {
                $"Error in queue processor: {ex.Message}".WriteLine(ConsoleColor.Red);
            }
        }

        IsDownloading = false;
    }

    /// <summary>
    /// Downloads a single M3U8 file using Xabe.FFmpeg.
    /// </summary>
    private async Task DownloadFileAsync(DownloadInfo downloadInfo, CancellationToken token)
    {
        try
        {
            downloadInfo.Status = DownloadStatus.InProgress;
            var uri = new Uri(downloadInfo.Url);
            var fileName = Path.Combine(Path.GetFileName(uri.AbsolutePath), downloadInfo.FileName + ".mp4");
            var outputPath = Path.Combine(_outputDirectory!, fileName);
            var conversion = await FFmpeg.Conversions.FromSnippet.SaveM3U8Stream(uri, outputPath);
            
            conversion.OnProgress += (sender, args) =>
            {
                downloadInfo.Percent = args.Percent;
                downloadInfo.Duration = args.Duration.ToString(@"hh\:mm\:ss");
                downloadInfo.TotalLength = args.TotalLength.ToString(@"hh\:mm\:ss");
            };

            await conversion.Start(token);

            downloadInfo.Status = DownloadStatus.Completed;
        }
        catch (OperationCanceledException)
        {
            downloadInfo.Status = DownloadStatus.Cancelled;
        }
        catch (Exception ex)
        {
            downloadInfo.Status = DownloadStatus.Failed;
            downloadInfo.Message = ex.Message;
        }
        finally
        {
            _completedDownloads.Add(downloadInfo);
            _inProgressDownloads.TryRemove(downloadInfo.Url, out _);
            _downloadSemaphore.Release();
        }
    }

    /// <summary>
    /// Signals the CancellationTokenSource to stop all in-progress downloads.
    /// </summary>
    internal void Stop()
    {
        if (!IsDownloading)
            throw new InvalidOperationException("No download is currently in progress.");

        _cts?.Cancel();
        IsDownloading = false;
        _downloadQueue.Clear();
    }

    internal void AddToQueue(string url)
    {
        if (url.IsNull())
            throw new ArgumentNullException(nameof(url), "URL cannot be null or empty.");

        if (_patternFileName.IsNull())
            throw new InvalidOperationException("Pattern file name must be set before adding URLs. Use the 'pattern' command.");

        if (!Uri.IsWellFormedUriString(url, UriKind.Absolute) && !File.Exists(url))
            throw new ArgumentException("The provided URL or path is not valid.", nameof(url));

        _downloadQueue.Enqueue(new DownloadInfo(url, $"{_patternFileName}_{_downloadQueue.Count + 1}"));
    }

    internal string RemoveFromQueue()
    {
        if (!_downloadQueue.TryDequeue(out var downloadInfo))
            throw new InvalidOperationException("The download queue is empty.");

        return downloadInfo.Url;
    }

    internal void SetPatternFileName(string pattern)
    {
        if (pattern.IsNull())
            throw new ArgumentNullException(nameof(pattern), "Pattern cannot be null or empty.");

        if (IsDownloading)
            throw new InvalidOperationException("Cannot change pattern file name while downloading.");

        _patternFileName = pattern;
    }

    internal void SetOutputDirectory(string outputPath)
    {
        if (outputPath.IsNull())
            throw new ArgumentNullException(nameof(outputPath), "Output path cannot be null or empty.");

        if (IsDownloading)
            throw new InvalidOperationException("Cannot change output directory while downloading.");

        // Create directory if it doesn't exist
        if (!Directory.Exists(outputPath))
        {
            try
            {
                Directory.CreateDirectory(outputPath!);
            }
            catch(Exception ex)
            {
                throw new IOException($"Failed to create output directory: {ex.Message}", ex);
            }
        }
        
        _outputDirectory = outputPath;
    }
    
    /// <summary>
    /// Displays the status of in-progress and queued downloads.
    /// </summary>
    internal async Task DisplayStatus()
    {
        if (!_downloadQueue.IsEmpty)
        {
            "In Queue:".WriteLine(ConsoleColor.Yellow);
            foreach (var downloadInfo in _downloadQueue)
                $"    | {downloadInfo.Url} ({downloadInfo.Status})".WriteLine();
            "".WriteLine();
        }

        if (!_inProgressDownloads.IsEmpty)
        {
            "In Progress:".WriteLine(ConsoleColor.Cyan);
            foreach (var (_, downloadInfo) in _inProgressDownloads)
            {
                $"    | {downloadInfo.FileName}, {downloadInfo.Status} ({downloadInfo.Percent}%)".WriteLine();
                $"    |-> {downloadInfo.Duration}/{downloadInfo.TotalLength}".WriteLine();
                $"=====================".WriteLine();
            }
            "".WriteLine();
        }

        if (!_completedDownloads.IsEmpty)
        {
            "Completed Downloads:".WriteLine(ConsoleColor.Green);
            foreach (var downloadInfo in _completedDownloads)
            {
                $"    | {downloadInfo.FileName}, {downloadInfo.Status}".WriteLine();
                if (downloadInfo.Status == DownloadStatus.Failed)
                    $"    |-> Error: {downloadInfo.Message}".WriteLine(ConsoleColor.Red);
                $"=====================".WriteLine();
            }
            "".WriteLine();
        }
    }
}