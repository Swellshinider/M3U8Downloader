namespace MD.Infra.Downloader;

internal sealed class DownloadManager
{
    private static readonly Lazy<DownloadManager> _instance = new(() => new DownloadManager());

    public static DownloadManager Instance => _instance.Value;

    private DownloadManager()
    {
        // Initialize download manager resources here
    }

    public bool IsDownloading { get; private set; }

    internal void Stop()
    {
        throw new NotImplementedException();
    }
}