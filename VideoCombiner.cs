using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using File = System.IO.File;

namespace VideoCombinerGUI
{
    public class VideoCombiner : IDisposable
    {
        private readonly string _ffmpegPath;
        private Process _ffmpegProcess;
        private double _totalDuration;

        public event Action<double> ProcessingProgressChanged;
        public event Action<string> ProcessingProgressReceived;

        public VideoCombiner(string ffmpegPath)
        {
            _ffmpegPath = ffmpegPath ?? throw new ArgumentNullException(nameof(ffmpegPath));
        }

        public async Task CombineVideosAsync(IEnumerable<string> videoFiles, string outputFileName)
        {
            if (videoFiles == null) throw new ArgumentNullException(nameof(videoFiles));
            if (string.IsNullOrWhiteSpace(outputFileName))
                throw new ArgumentException("Output file name cannot be empty.", nameof(outputFileName));

            _totalDuration = await CalculateTotalDurationAsync(videoFiles);

            var tempFileList = await CreateTempFileListAsync(videoFiles);

            try
            {
                await RunFfmpegProcessAsync(tempFileList, outputFileName);
                await MonitorProcessingProgressAsync(outputFileName);
            }
            finally
            {
                File.Delete(tempFileList);
            }
        }

        private async Task<double> CalculateTotalDurationAsync(IEnumerable<string> videoFiles)
        {
            double totalDuration = 0;
            foreach (var file in videoFiles)
            {
                try
                {
                    using var tagFile = TagLib.File.Create(file);
                    totalDuration += tagFile.Properties.Duration.TotalSeconds;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error reading file {file}: {ex.Message}");
                }
            }

            return totalDuration;
        }

        private async Task<string> CreateTempFileListAsync(IEnumerable<string> videoFiles)
        {
            var tempFileList = Path.GetTempFileName();
            await using var writer = new StreamWriter(tempFileList);
            foreach (var file in videoFiles)
            {
                await writer.WriteLineAsync($"file '{file}'");
            }

            return tempFileList;
        }

        private async Task RunFfmpegProcessAsync(string tempFileList, string outputFileName)
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

            _ffmpegProcess.OutputDataReceived += (_, e) => OnProcessingProgressReceived(e.Data);
            _ffmpegProcess.ErrorDataReceived += (_, e) => OnProcessingProgressReceived(e.Data);

            _ffmpegProcess.Start();
            _ffmpegProcess.BeginOutputReadLine();
            _ffmpegProcess.BeginErrorReadLine();

            await _ffmpegProcess.WaitForExitAsync();

            if (_ffmpegProcess.ExitCode != 0)
            {
                throw new Exception("FFmpeg encountered an error during processing.");
            }
        }

        private void OnProcessingProgressReceived(string data)
        {
            if (string.IsNullOrEmpty(data)) return;

            ProcessingProgressReceived?.Invoke(data);

            var timeMatch = Regex.Match(data, @"time=(\d+:\d+:\d+.\d+)");
            if (timeMatch.Success)
            {
                var time = TimeSpan.Parse(timeMatch.Groups[1].Value);
                var progress = (_totalDuration > 0) ? (time.TotalSeconds / _totalDuration) * 100 : 0;
                ProcessingProgressChanged?.Invoke(progress);
            }
        }

        private async Task MonitorProcessingProgressAsync(string outputFileName)
        {
            const int refreshInterval = 500; // milliseconds
            var fileInfo = new FileInfo(outputFileName);
            long previousSize = 0;

            while (!_ffmpegProcess.HasExited || previousSize != fileInfo.Length)
            {
                await Task.Delay(refreshInterval);
                fileInfo.Refresh();

                if (previousSize != fileInfo.Length)
                {
                    double progress = (double)fileInfo.Length / (fileInfo.Length + 1) * 100;
                    previousSize = fileInfo.Length;
                    ProcessingProgressChanged?.Invoke(Math.Min(progress, 100));
                }
            }

            ProcessingProgressChanged?.Invoke(100);
        }

        public void Dispose()
        {
            _ffmpegProcess?.Dispose();
        }
    }
}