using System.Diagnostics;
using System.IO;

namespace VideoMerger;

public class VideoCombiner
{
    private readonly string _ffmpegPath;

    public VideoCombiner(string ffmpegPath)
    {
        _ffmpegPath = ffmpegPath;
    }

    public async Task CombineVideosAsync(List<string> videoFiles, string outputFileName,
        Action<string> onProgressDataReceived)
    {
        // Sort the video files by creation date
        var sortedFiles = videoFiles.Select(file => new FileInfo(file))
            .OrderBy(fileInfo => fileInfo.CreationTime)
            .Select(fileInfo => fileInfo.FullName)
            .ToList();

        // Create a temporary file list for FFmpeg
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

            // Start FFmpeg process
            Process ffmpeg = new Process();
            ffmpeg.StartInfo.FileName = _ffmpegPath;
            ffmpeg.StartInfo.Arguments = $"-f concat -safe 0 -i \"{tempFileList}\" -c copy \"{outputFileName}\"";
            ffmpeg.StartInfo.UseShellExecute = false; // Run without shell
            ffmpeg.StartInfo.CreateNoWindow = true; // Don't create a console window
            ffmpeg.StartInfo.RedirectStandardOutput = true;
            ffmpeg.StartInfo.RedirectStandardError = true;
            ffmpeg.ErrorDataReceived += (sender, e) => onProgressDataReceived?.Invoke(e.Data);
            ffmpeg.Start();

            // Begin reading output
            ffmpeg.BeginErrorReadLine();

            // Wait for the process to exit
            await ffmpeg.WaitForExitAsync();

            if (ffmpeg.ExitCode != 0)
            {
                throw new Exception("FFmpeg encountered an error during processing.");
            }
        }
        finally
        {
            // Clean up
            File.Delete(tempFileList);
        }
    }
}