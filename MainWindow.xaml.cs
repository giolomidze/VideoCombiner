using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace VideoCombinerGUI
{
    public partial class MainWindow : Window
    {
        private readonly VideoCombiner _videoCombiner;
        private static readonly string[] ValidVideoExtensions = { ".mp4", ".avi", ".mkv", ".webm", ".mov" };
        private readonly ObservableCollection<string> _videoFiles;

        public MainWindow()
        {
            InitializeComponent();

            var ffmpegPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "external", "ffmpeg.exe");
            _videoCombiner = new VideoCombiner(ffmpegPath);
            _videoCombiner.ProcessingProgressChanged += OnProcessingProgressChanged;

            _videoFiles = new ObservableCollection<string>();
            VideoListBox.ItemsSource = _videoFiles;
            _videoFiles.CollectionChanged += (s, e) => UpdateButtonStates();

            VideoListBox.AllowDrop = true;
            VideoListBox.Drop += VideoListBox_Drop;
            VideoListBox.SelectionChanged += VideoListBox_SelectionChanged;
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
                List<string> videoFiles = _videoFiles.ToList();

                await _videoCombiner.CombineVideosAsync(videoFiles, outputFileName);

                MessageBox.Show("Videos combined successfully!", "Success", MessageBoxButton.OK,
                    MessageBoxImage.Information);
                _videoFiles.Clear();
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
            // Handle file drops (external files)
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = e.Data.GetData(DataFormats.FileDrop) as string[];
                if (files == null) return;

                foreach (var file in files.Where(IsValidVideoFile))
                {
                    if (!_videoFiles.Contains(file))
                    {
                        _videoFiles.Add(file);
                    }
                }
                UpdateButtonStates();
                e.Handled = true;
            }
        }

        private void VideoListBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Don't start drag if clicking on a button
            if (FindAncestor<Button>((DependencyObject)e.OriginalSource) != null)
                return;

            if (sender is ListBox listBox)
            {
                var item = FindAncestor<ListBoxItem>((DependencyObject)e.OriginalSource);
                if (item != null && item.Content is string)
                {
                    listBox.SelectedItem = item.Content;
                    
                    // Start drag operation
                    var dragData = new DataObject(typeof(string), item.Content as string);
                    DragDrop.DoDragDrop(listBox, dragData, DragDropEffects.Move);
                }
            }
        }

        private void VideoListBox_DragOver(object sender, DragEventArgs e)
        {
            // Allow drop if it's a file drop or if we're dragging a string internally
            if (e.Data.GetDataPresent(DataFormats.FileDrop) || e.Data.GetDataPresent(typeof(string)))
            {
                e.Effects = DragDropEffects.Move;
                e.Handled = true;
            }
        }

        private void VideoListBox_DropReorder(object sender, DragEventArgs e)
        {
            // Handle file drops first (external files)
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                VideoListBox_Drop(sender, e);
                return;
            }

            // Handle internal reordering
            if (e.Data.GetDataPresent(typeof(string)) && sender is ListBox listBox)
            {
                var draggedContent = e.Data.GetData(typeof(string)) as string;
                if (draggedContent == null) return;

                var targetItem = FindAncestor<ListBoxItem>((DependencyObject)e.OriginalSource);
                if (targetItem == null)
                {
                    // Dropped on empty space, do nothing
                    return;
                }

                var targetContent = targetItem.Content as string;
                if (targetContent == null || targetContent == draggedContent)
                {
                    return;
                }

                var draggedIndex = _videoFiles.IndexOf(draggedContent);
                var targetIndex = _videoFiles.IndexOf(targetContent);

                if (draggedIndex >= 0 && targetIndex >= 0 && draggedIndex != targetIndex)
                {
                    _videoFiles.RemoveAt(draggedIndex);
                    // Adjust target index if item was removed before it
                    if (draggedIndex < targetIndex)
                        targetIndex--;
                    _videoFiles.Insert(targetIndex, draggedContent);
                    VideoListBox.SelectedItem = draggedContent;
                }

                e.Handled = true;
            }
        }

        private static T? FindAncestor<T>(DependencyObject current) where T : DependencyObject
        {
            while (current != null)
            {
                if (current is T ancestor)
                    return ancestor;
                current = System.Windows.Media.VisualTreeHelper.GetParent(current);
            }
            return null;
        }

        private void MoveUpButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string filePath)
            {
                var index = _videoFiles.IndexOf(filePath);
                if (index > 0)
                {
                    _videoFiles.RemoveAt(index);
                    _videoFiles.Insert(index - 1, filePath);
                    VideoListBox.SelectedItem = filePath;
                    UpdateButtonStates();
                }
            }
        }

        private void MoveDownButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string filePath)
            {
                var index = _videoFiles.IndexOf(filePath);
                if (index >= 0 && index < _videoFiles.Count - 1)
                {
                    _videoFiles.RemoveAt(index);
                    _videoFiles.Insert(index + 1, filePath);
                    VideoListBox.SelectedItem = filePath;
                    UpdateButtonStates();
                }
            }
        }

        private void RemoveButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string filePath)
            {
                _videoFiles.Remove(filePath);
                UpdateButtonStates();
            }
        }

        private void VideoListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateButtonStates();
        }

        private void UpdateButtonStates()
        {
            // Update button states for all items
            Dispatcher.BeginInvoke(new Action(() =>
            {
                for (int i = 0; i < VideoListBox.Items.Count; i++)
                {
                    var container = VideoListBox.ItemContainerGenerator.ContainerFromIndex(i) as ListBoxItem;
                    if (container != null)
                    {
                        var grid = System.Windows.Media.VisualTreeHelper.GetChild(container, 0) as Grid;
                        if (grid != null && grid.Children.Count >= 3)
                        {
                            var upButton = grid.Children[0] as Button;
                            var downButton = grid.Children[1] as Button;

                            if (upButton != null)
                                upButton.IsEnabled = i > 0;
                            if (downButton != null)
                                downButton.IsEnabled = i < VideoListBox.Items.Count - 1;
                        }
                    }
                }
            }), System.Windows.Threading.DispatcherPriority.Loaded);
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