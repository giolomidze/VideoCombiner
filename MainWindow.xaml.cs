using System.IO;
using System.Windows;

namespace VideoCombinerGUI
{
    public partial class MainWindow
    {
        private readonly VideoCombiner _videoCombiner;

        public MainWindow()
        {
            InitializeComponent();

            var ffmpegPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "external", "ffmpeg.exe");
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
                    // Get the list of video files in the order they are listed in the ListBox
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
            }
            else
            {
                MessageBox.Show("Save operation was canceled.", "Canceled", MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
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

        private static bool IsValidVideoFile(string file)
        {
            string[] validExtensions = { ".mp4", ".avi", ".mkv", ".webm", ".mov" };
            var extension = Path.GetExtension(file).ToLower();
            return validExtensions.Contains(extension);
        }

        private void OnFinalizingProgressChanged(double progress)
        {
            Dispatcher.Invoke(() =>
            {
                FinalizingProgressBar.Value = progress;
                FinalizingText.Text = $"Finalizing: {progress:F2}%";
            });
        }

        private void OnProcessingProgressReceived(string data)
        {
            // This method is kept for completeness, but it won't display any speed-related information.
            // If not needed, the event ProcessingProgressReceived can be removed from VideoCombiner.
        }
    }
}