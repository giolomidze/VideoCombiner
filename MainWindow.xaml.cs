using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace VideoCombinerGUI
{
    public partial class MainWindow : Window
    {
        private readonly VideoCombiner _videoCombiner;
        private static readonly string[] ValidVideoExtensions = { ".mp4", ".avi", ".mkv", ".webm", ".mov" };

        public MainWindow()
        {
            InitializeComponent();

            var ffmpegPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "external", "ffmpeg.exe");
            _videoCombiner = new VideoCombiner(ffmpegPath);
            _videoCombiner.ProcessingProgressChanged += OnProcessingProgressChanged;

            VideoListBox.AllowDrop = true;
            VideoListBox.Drop += VideoListBox_Drop;
            CombineButton.Click += CombineButton_Click;
        }

        private async void CombineButton_Click(object sender, RoutedEventArgs e)
        {
            if (VideoListBox.Items.Count == 0)
            {
                MessageBox.Show("No videos selected for combining.", "Warning", MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            var saveFileDialog = new SaveFileDialog
            {
                Filter = "MP4 files (*.mp4)|*.mp4|All files (*.*)|*.*",
                Title = "Save Combined Video As",
                FileName = "combined_video.mp4"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                await CombineVideosAsync(saveFileDialog.FileName);
            }
        }

        private async Task CombineVideosAsync(string outputFileName)
        {
            try
            {
                CombineButton.IsEnabled = false;
                List<string> videoFiles = VideoListBox.Items.Cast<string>().ToList();

                await _videoCombiner.CombineVideosAsync(videoFiles, outputFileName);

                MessageBox.Show("Videos combined successfully!", "Success", MessageBoxButton.OK,
                    MessageBoxImage.Information);
                VideoListBox.Items.Clear();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                CombineButton.IsEnabled = true;
                ResetProgressBar();
            }
        }

        private void VideoListBox_Drop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;

            var files = e.Data.GetData(DataFormats.FileDrop) as string[];
            if (files == null) return;

            foreach (var file in files.Where(IsValidVideoFile))
            {
                VideoListBox.Items.Add(file);
            }
        }

        private static bool IsValidVideoFile(string file)
        {
            return ValidVideoExtensions.Contains(Path.GetExtension(file).ToLower());
        }

        private void OnProcessingProgressChanged(double progress)
        {
            Dispatcher.Invoke(() =>
            {
                LoadingProgressBar.Value = progress;
                ProcessingText.Text = $"Processing: {progress:F2}%";
            });
        }

        private void ResetProgressBar()
        {
            LoadingProgressBar.Value = 0;
            ProcessingText.Text = "Processing: 0%";
        }
    }
}