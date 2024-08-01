using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;

namespace VideoMerger
{
    public partial class MainWindow : Window
    {
        private VideoCombiner _videoCombiner;

        public MainWindow()
        {
            InitializeComponent();

            string ffmpegPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "external", "ffmpeg.exe");
            _videoCombiner = new VideoCombiner(ffmpegPath);
            _videoCombiner.FinalizingProgressChanged += OnFinalizingProgressChanged;
            _videoCombiner.ProcessingProgressReceived += OnProcessingProgressReceived;
        }

        private async void CombineButton_Click(object sender, RoutedEventArgs e)
        {
            if (VideoListBox.Items.Count == 0)
            {
                MessageBox.Show("No videos selected for combining.", "Warning", MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            var saveFileDialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "MP4 files (*.mp4)|*.mp4|All files (*.*)|*.*",
                Title = "Save Combined Video As",
                FileName = "combined_video.mp4"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                string outputFileName = saveFileDialog.FileName;
                try
                {
                    await _videoCombiner.CombineVideosAsync(VideoListBox.Items.Cast<string>().ToList(), outputFileName);
                    MessageBox.Show("Videos combined successfully!", "Success", MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    VideoListBox.Items.Clear();
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

        private void OnProcessingProgressReceived(string data)
        {
            if (string.IsNullOrEmpty(data)) return;

            // Parse data for 'size=', 'time=', 'bitrate=', 'speed='
            var sizeMatch = Regex.Match(data, @"size=\s*(\d+)kB");
            var timeMatch = Regex.Match(data, @"time=(\d+:\d+:\d+.\d+)");
            var speedMatch = Regex.Match(data, @"speed=\s*(\d+.\d+)x");

            if (sizeMatch.Success)
            {
                var size = int.Parse(sizeMatch.Groups[1].Value);
                // Update UI with file size (e.g., FinalizingProgressBar.Value = ...)
            }

            if (timeMatch.Success)
            {
                var time = TimeSpan.Parse(timeMatch.Groups[1].Value);
                // Update UI with time (e.g., ProcessingProgressBar.Value = ...)
            }

            if (speedMatch.Success)
            {
                var speed = double.Parse(speedMatch.Groups[1].Value);
                // Optionally use speed to estimate remaining time
            }
        }

        private void OnFinalizingProgressChanged(double progress)
        {
            Dispatcher.Invoke(() =>
            {
                FinalizingProgressBar.Value = progress;
                FinalizingText.Text = $"Finalizing: {progress:F2}%";
            });
        }
    }
}