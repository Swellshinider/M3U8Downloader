using System.Diagnostics;
using Xabe.FFmpeg;

namespace MD.Infra.Converter;

internal sealed class M3U8Converter
{
    private readonly bool _useGpu;
    private readonly int _maximumThreads;

    internal M3U8Converter(bool useGpu, int maximumThreads)
    {
        _useGpu = useGpu;
        _maximumThreads = maximumThreads;
    }

    internal bool IsRunning { get; private set; }

    internal async Task ConvertAsync(List<DownloadData> dataList, string outputDirectory, CancellationToken token)
    {
        using var semaphore = new SemaphoreSlim(_maximumThreads);
        IsRunning = true;

        var tasks = dataList.Where(d => d.Status == DownloadStatus.Pending).Select(async data =>
        {
            await semaphore.WaitAsync(token);

            try
            {
                await ProcessData(data, outputDirectory, token);
            }
            finally
            {
                semaphore.Release();
            }

        }).ToList();

        await Task.WhenAll(tasks);
        IsRunning = false;
    }

    private async Task ProcessData(DownloadData data, string outputDirectory, CancellationToken token)
    {

        try
        {
            var stopWatch = Stopwatch.StartNew();
            stopWatch.Start();
            data.Status = DownloadStatus.InProgress;

            var filePath = Path.Combine(outputDirectory, $"{data.Title}.mp4");
            var conversion = FFmpeg.Conversions.New()
                .AddParameter($"-i \"{data.Url}\"", ParameterPosition.PreInput)
                .AddParameter(_useGpu ? "-c:v h264_nvenc" : "-c:v libx264");
            conversion.SetPriority(ProcessPriorityClass.AboveNormal);

            conversion.OnProgress += (s, eargs) =>
            {
                data.ConversionProgress = eargs;
                data.TimeElapsed = stopWatch.Elapsed;
            };

            await conversion.Start(token);

            stopWatch.Stop();
            data.Status = DownloadStatus.Completed;
        }
        catch (OperationCanceledException)
        {
            data.Status = DownloadStatus.Cancelled;
        }
        catch (Exception ex)
        {
            data.Status = DownloadStatus.Failed;
            data.ErrorMessage = ex.Message;
        }
    }
}