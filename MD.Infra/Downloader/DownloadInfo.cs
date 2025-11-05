namespace MD.Infra.Downloader;

internal sealed class DownloadInfo
{
    public DownloadInfo(string url, string fileName)
    {
        Url = url;
        FileName = fileName;
        Status = DownloadStatus.Pending;
    }

    public string Url { get; init; }
    public string FileName { get; init; }
    
    public DownloadStatus Status { get; set; }
    public string Message { get; set; } = string.Empty;
    public int Percent { get; set; } = 0;
    public string Duration { get; set; } = "00:00:00";
    public string TotalLength { get; set; } = "00:00:00";
}