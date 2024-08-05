using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using File = System.IO.File;

namespace VideoCombinerGUI;

public class VideoCombiner(string ffmpegPath)
{
    private Process _ffmpegProcess = null!;
    private double _totalDuration;

    public event Action<double> ProcessingProgressChanged = delegate { };
    public event Action<string> ProcessingProgressReceived = delegate { };

    private bool IsRunning => !_ffmpegProcess.HasExited;

    public double TotalDuration => _totalDuration;

    public async Task CombineVideosAsync(List<string> videoFiles, string outputFileName)
    {
        // Calculate total duration
        _totalDuration = CalculateTotalDuration(videoFiles);

        // Create a temporary file to store the list of video files
        var tempFileList = Path.GetTempFileName();
        await using (StreamWriter writer = new StreamWriter(tempFileList))
        {
            foreach (string file in videoFiles)
            {
                writer.WriteLine($"file '{file}'");
            }
        }

        try
        {
            if (!File.Exists(ffmpegPath))
            {
                throw new FileNotFoundException("FFmpeg executable not found.", ffmpegPath);
            }

            _ffmpegProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = ffmpegPath,
                    Arguments =
                        $"-f concat -safe 0 -i \"{tempFileList}\" -c copy \"{outputFileName}\" -progress pipe:1 -loglevel error",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true
                }
            };

            _ffmpegProcess.OutputDataReceived += (_, e) =>
            {
                if (e.Data != null) OnProcessingProgressReceived(e.Data);
            };
            _ffmpegProcess.ErrorDataReceived += (_, e) =>
            {
                if (e.Data != null) OnProcessingProgressReceived(e.Data);
            };
            _ffmpegProcess.Start();
            _ffmpegProcess.BeginOutputReadLine();
            _ffmpegProcess.BeginErrorReadLine();

            await _ffmpegProcess.WaitForExitAsync();

            if (_ffmpegProcess.ExitCode != 0)
            {
                throw new Exception("FFmpeg encountered an error during processing.");
            }

            await MonitorProcessingProgress(outputFileName);
        }
        finally
        {
            if (File.Exists(tempFileList))
            {
                File.Delete(tempFileList);
            }
        }
    }

    private double CalculateTotalDuration(List<string> videoFiles)
    {
        double totalDuration = 0;
        foreach (var file in videoFiles)
        {
            try
            {
                var fileInfo = TagLib.File.Create(file);
                totalDuration += fileInfo.Properties.Duration.TotalSeconds;
            }
            catch (Exception ex)
            {
                // Log exception or handle error (e.g., file not found, unable to read duration)
                Console.WriteLine($"Error reading file {file}: {ex.Message}");
            }
        }

        return totalDuration;
    }

    private void OnProcessingProgressReceived(string data)
    {
        if (string.IsNullOrEmpty(data)) return;

        // Invoke the event to notify subscribers about new data
        ProcessingProgressReceived(data);

        // Parse data for 'time=' and other progress indicators
        var timeMatch = Regex.Match(data, @"time=(\d+:\d+:\d+.\d+)");

        if (!timeMatch.Success) return;
        var time = TimeSpan.Parse(timeMatch.Groups[1].Value);
        // Notify listeners with time (e.g., update progress bar)
        var progress = (_totalDuration > 0) ? (time.TotalSeconds / _totalDuration) * 100 : 0;
        ProcessingProgressChanged(progress);
    }

    private async Task MonitorProcessingProgress(string outputFileName)
    {
        const int refreshInterval = 500; // milliseconds
        FileInfo fileInfo = new FileInfo(outputFileName);
        long previousSize = 0;
        long currentSize = 0;

        while (IsRunning || previousSize != currentSize)
        {
            await Task.Delay(refreshInterval);
            fileInfo.Refresh();
            currentSize = fileInfo.Length;

            if (previousSize != currentSize)
            {
                double progress = (double)currentSize / (currentSize + 1) * 100;
                previousSize = currentSize;
                ProcessingProgressChanged?.Invoke(Math.Min(progress, 100));
            }
            else if (!IsRunning)
            {
                break;
            }
        }

        ProcessingProgressChanged?.Invoke(100);
    }
}