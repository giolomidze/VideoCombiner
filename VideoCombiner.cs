using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TagLib;
using File = System.IO.File;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

public class VideoCombiner
{
    private readonly string _ffmpegPath;
    private Process _ffmpegProcess;

    public event Action<double> FinalizingProgressChanged;
    public event Action<string> ProcessingProgressReceived;

    public bool IsRunning => _ffmpegProcess != null && !_ffmpegProcess.HasExited;

    public VideoCombiner(string ffmpegPath)
    {
        _ffmpegPath = ffmpegPath;
    }

    public async Task CombineVideosAsync(List<string> videoFiles, string outputFileName)
    {
        var sortedFiles = SortFilesByCreationDate(videoFiles);

        string tempFileList = Path.GetTempFileName();
        using (StreamWriter writer = new StreamWriter(tempFileList))
        {
            foreach (string file in sortedFiles)
            {
                writer.WriteLine($"file '{file}'");
            }
        }

        try
        {
            if (!File.Exists(_ffmpegPath))
            {
                throw new FileNotFoundException("FFmpeg executable not found.", _ffmpegPath);
            }

            _ffmpegProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = _ffmpegPath,
                    Arguments =
                        $"-f concat -safe 0 -i \"{tempFileList}\" -c copy \"{outputFileName}\" -progress pipe:1 -loglevel error",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true
                }
            };

            _ffmpegProcess.OutputDataReceived += (sender, e) => ProcessingProgressReceived?.Invoke(e.Data);
            _ffmpegProcess.ErrorDataReceived += (sender, e) => ProcessingProgressReceived?.Invoke(e.Data);
            _ffmpegProcess.Start();
            _ffmpegProcess.BeginOutputReadLine();
            _ffmpegProcess.BeginErrorReadLine();

            await _ffmpegProcess.WaitForExitAsync();

            if (_ffmpegProcess.ExitCode != 0)
            {
                throw new Exception("FFmpeg encountered an error during processing.");
            }

            await MonitorFinalizingProgress(outputFileName);
        }
        finally
        {
            if (File.Exists(tempFileList))
            {
                File.Delete(tempFileList);
            }
        }
    }

    private List<string> SortFilesByCreationDate(List<string> files)
    {
        return files.OrderBy(file =>
        {
            try
            {
                // Use FileInfo to get the creation time
                var fileInfo = new FileInfo(file);
                return fileInfo.CreationTime;
            }
            catch
            {
                // In case of error, return a default DateTime
                return DateTime.MinValue;
            }
        }).ToList();
    }

    private async Task MonitorFinalizingProgress(string outputFileName)
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
                FinalizingProgressChanged?.Invoke(Math.Min(progress, 100));
            }
            else if (!IsRunning)
            {
                break;
            }
        }

        FinalizingProgressChanged?.Invoke(100);
    }
}