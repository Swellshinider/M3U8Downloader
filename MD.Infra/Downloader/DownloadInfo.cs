namespace MD.Infra.Downloader;

internal sealed class DownloadInfo
{
    public DownloadInfo(string url)
    {
        Url = url;
        Status = DownloadStatus.Pending;
    }

    public string Url { get; init; }
    public DownloadStatus Status { get; set; }
}