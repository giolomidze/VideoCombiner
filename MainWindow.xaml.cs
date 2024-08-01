using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;

namespace VideoMerger
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void VideoListBox_Drop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;

            var droppedData = e.Data.GetData(DataFormats.FileDrop);

            if (droppedData is not string[] files) return;

            foreach (var file in files)
            {
                if (IsValidVideoFile(file))
                {
                    VideoListBox.Items.Add(file);
                }
            }
        }


        private bool IsValidVideoFile(string file)
        {
            string[] validExtensions = { ".mp4", ".avi", ".mkv", ".webm", ".mov" };
            var extension = Path.GetExtension(file).ToLower();
            return validExtensions.Contains(extension);
        }

        private void CombineButton_Click(object sender, RoutedEventArgs e)
        {
            if (VideoListBox.Items.Count == 0)
            {
                MessageBox.Show("No videos to combine.");
                return;
            }

            List<string> videoFiles = VideoListBox.Items.Cast<string>().ToList();
            CombineVideos(videoFiles);
        }

        private async void CombineVideos(List<string> videoFiles)
        {
            // Sort the video files by creation date
            var sortedFiles = videoFiles.Select(file => new FileInfo(file))
                .OrderBy(fileInfo => fileInfo.CreationTime)
                .Select(fileInfo => fileInfo.FullName)
                .ToList();

            // Show SaveFileDialog to select output file location
            Microsoft.Win32.SaveFileDialog saveFileDialog = new Microsoft.Win32.SaveFileDialog();
            saveFileDialog.Filter = "MP4 files (*.mp4)|*.mp4|All files (*.*)|*.*";
            saveFileDialog.Title = "Save Combined Video As";
            saveFileDialog.FileName = "combined_video.mp4"; // Default file name

            if (saveFileDialog.ShowDialog() == true)
            {
                string outputFileName = saveFileDialog.FileName;
                string ffmpegPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "external", "ffmpeg.exe");

                if (!File.Exists(ffmpegPath))
                {
                    MessageBox.Show(
                        $"FFmpeg executable not found at {ffmpegPath}. Please ensure it is correctly placed.", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

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
                    // Determine the total duration (dummy value or calculate from files)
                    double totalDuration = 100; // Placeholder; replace with actual total duration calculation

                    // Start FFmpeg process
                    Process ffmpeg = new Process();
                    ffmpeg.StartInfo.FileName = ffmpegPath;
                    ffmpeg.StartInfo.Arguments =
                        $"-f concat -safe 0 -i \"{tempFileList}\" -c copy \"{outputFileName}\"";
                    ffmpeg.StartInfo.UseShellExecute = false;
                    ffmpeg.StartInfo.RedirectStandardOutput = true;
                    ffmpeg.StartInfo.RedirectStandardError = true;
                    ffmpeg.ErrorDataReceived += (sender, e) => ParseFFmpegProgress(e.Data, totalDuration);
                    ffmpeg.Start();

                    // Begin reading output
                    ffmpeg.BeginErrorReadLine();

                    // Wait for the process to exit
                    await ffmpeg.WaitForExitAsync();

                    if (ffmpeg.ExitCode == 0)
                    {
                        MessageBox.Show("Videos combined successfully!", "Success", MessageBoxButton.OK,
                            MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show("FFmpeg encountered an error during processing.", "Error", MessageBoxButton.OK,
                            MessageBoxImage.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
                finally
                {
                    // Clean up
                    File.Delete(tempFileList);
                }
            }
            else
            {
                MessageBox.Show("Save operation was canceled.", "Canceled", MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        private void ParseFFmpegProgress(string data, double totalDuration)
        {
            if (string.IsNullOrWhiteSpace(data))
                return;

            // Sample progress output: frame=  220 fps=0.0 q=-1.0 Lsize=   13763kB time=00:00:09.00 bitrate=12522.5kbits/s speed=15.9x
            var timeMatch = Regex.Match(data, @"time=(\d{2}:\d{2}:\d{2}.\d{2})");
            if (timeMatch.Success)
            {
                var currentTime = TimeSpan.ParseExact(timeMatch.Groups[1].Value, @"hh\:mm\:ss\.ff",
                    CultureInfo.InvariantCulture);

                // Calculate the progress percentage
                double progress = (currentTime.TotalSeconds / totalDuration) * 100;
                progress = Math.Min(progress, 100); // Cap the progress at 100%

                // Update UI on the main thread
                Dispatcher.Invoke(() =>
                {
                    ProgressBar.Value = progress;
                    ProgressText.Text = $"Progress: {progress:F2}%";
                });
            }
        }
    }
}