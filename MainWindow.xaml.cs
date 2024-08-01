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
        private VideoCombiner _videoCombiner;
        private FFmpegProgressParser _progressParser;

        public MainWindow()
        {
            InitializeComponent();

            // Initialize VideoCombiner with the path to ffmpeg.exe
            string ffmpegPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "external", "ffmpeg.exe");
            _videoCombiner = new VideoCombiner(ffmpegPath);
        }

        private async void CombineButton_Click(object sender, RoutedEventArgs e)
        {
            if (VideoListBox.Items.Count == 0)
            {
                MessageBox.Show("No videos selected for combining.", "Warning", MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            // Get the selected output file path from SaveFileDialog
            Microsoft.Win32.SaveFileDialog saveFileDialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "MP4 files (*.mp4)|*.mp4|All files (*.*)|*.*",
                Title = "Save Combined Video As",
                FileName = "combined_video.mp4" // Default file name
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                string outputFileName = saveFileDialog.FileName;

                // Calculate the total duration (dummy value or calculate from files)
                double totalDuration = 100; // Placeholder; replace with actual total duration calculation

                // Initialize the progress parser with the total duration
                _progressParser = new FFmpegProgressParser(totalDuration);
                _progressParser.ProgressChanged += UpdateProgress;

                try
                {
                    // Start FFmpeg and report progress
                    ProgressBar.IsIndeterminate = false;
                    await _videoCombiner.CombineVideosAsync(VideoListBox.Items.Cast<string>().ToList(), outputFileName,
                        _progressParser.Parse);

                    // Indeterminate progress for finalization phase
                    ProgressBar.IsIndeterminate = true;
                    ProgressText.Text = "Finalizing...";

                    // Wait a bit to simulate finalization (replace with actual wait if needed)
                    await Task.Delay(2000); // This is a placeholder; actual time may vary

                    // Clear progress indicators and file list
                    ProgressBar.IsIndeterminate = false;
                    ProgressBar.Value = 0;
                    ProgressText.Text = "Progress: 0%";
                    VideoListBox.Items.Clear();

                    MessageBox.Show("Videos combined successfully!", "Success", MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("Save operation was canceled.", "Canceled", MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        private void UpdateProgress(double progress)
        {
            Dispatcher.Invoke(() =>
            {
                ProgressBar.Value = progress;
                ProgressText.Text = $"Progress: {progress:F2}%";
            });
        }


        private void VideoListBox_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var droppedData = e.Data.GetData(DataFormats.FileDrop);
                if (droppedData is string[] files)
                {
                    foreach (var file in files)
                    {
                        if (IsValidVideoFile(file))
                        {
                            VideoListBox.Items.Add(file);
                        }
                    }
                }
            }
        }

        private bool IsValidVideoFile(string file)
        {
            string[] validExtensions = { ".mp4", ".avi", ".mkv", ".webm", ".mov" };
            string extension = Path.GetExtension(file).ToLower();
            return validExtensions.Contains(extension);
        }
    }
}