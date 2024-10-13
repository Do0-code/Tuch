using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using Microsoft.Win32;


namespace Tuch
{
    public partial class AnimationEditorWindow : Window
    {
        public class AnimationFrame
        {
            public string Text { get; set; }
            public int Duration { get; set; }
            public double PixelWidth => Duration * 50; // 프레임당 50픽셀
        }

        private ObservableCollection<AnimationFrame> frames;
        private int currentFrameIndex = -1;
        private DispatcherTimer animationTimer;
        private int currentPlaybackFrame = 0;
        private int currentFrameTime = 0;

        public AnimationEditorWindow()
        {
            InitializeComponent();
            frames = new ObservableCollection<AnimationFrame>();
            TimelineControl.ItemsSource = frames;

            animationTimer = new DispatcherTimer();
            animationTimer.Interval = TimeSpan.FromMilliseconds(16.67); // 약 60fps
            animationTimer.Tick += AnimationTimer_Tick;
        }

        private void AnimationTimer_Tick(object sender, EventArgs e)
        {
            if (frames.Count == 0) return;

            currentFrameTime++;
            if (currentFrameTime >= frames[currentPlaybackFrame].Duration)
            {
                currentFrameTime = 0;
                currentPlaybackFrame++;
                if (currentPlaybackFrame >= frames.Count)
                {
                    currentPlaybackFrame = 0;
                }
                PreviewText.Text = frames[currentPlaybackFrame].Text;
            }
        }

        private void AddFrame_Click(object sender, RoutedEventArgs e)
        {
            frames.Add(new AnimationFrame { Text = "New Frame", Duration = 3 });
            currentFrameIndex = frames.Count - 1;
            UpdateEditorWithCurrentFrame();
        }

        private void DeleteFrame_Click(object sender, RoutedEventArgs e)
        {
            if (currentFrameIndex >= 0 && currentFrameIndex < frames.Count)
            {
                frames.RemoveAt(currentFrameIndex);
                if (currentFrameIndex >= frames.Count)
                {
                    currentFrameIndex = frames.Count - 1;
                }
                UpdateEditorWithCurrentFrame();
            }
        }

        private void UpdateFrame_Click(object sender, RoutedEventArgs e)
        {
            if (currentFrameIndex >= 0 && currentFrameIndex < frames.Count)
            {
                frames[currentFrameIndex].Text = FrameTextEditor.Text;
                if (int.TryParse(FrameDurationBox.Text, out int duration))
                {
                    frames[currentFrameIndex].Duration = duration;
                }
                TimelineControl.Items.Refresh();
            }
        }

        private void PlayAnimation_Click(object sender, RoutedEventArgs e)
        {
            if (frames.Count == 0) return;

            currentPlaybackFrame = 0;
            currentFrameTime = 0;
            PreviewText.Text = frames[currentPlaybackFrame].Text;
            animationTimer.Start();
        }

        private void StopAnimation_Click(object sender, RoutedEventArgs e)
        {
            animationTimer.Stop();
            PreviewText.Text = "";
        }

        private void SaveAnimation_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "Animation files (*.anim)|*.anim",
                DefaultExt = "anim"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                SaveAnimationToFile(saveFileDialog.FileName);
            }
        }

        private void SaveAnimationToFile(string filePath)
        {
            using (StreamWriter writer = new StreamWriter(filePath))
            {
                List<AnimationFrame> currentFrames = new List<AnimationFrame>();

                foreach (var frame in frames)
                {
                    if (currentFrames.Count > 0 && currentFrames[0].Text != frame.Text)
                    {
                        WriteFramesToFile(writer, currentFrames);
                        currentFrames.Clear();
                    }
                    currentFrames.Add(frame);
                }

                if (currentFrames.Count > 0)
                {
                    WriteFramesToFile(writer, currentFrames);
                }
            }

            MessageBox.Show("Animation saved successfully!", "Save Animation", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void WriteFramesToFile(StreamWriter writer, List<AnimationFrame> frames)
        {
            if (frames.Count == 1)
            {
                writer.WriteLine($"$for {frames[0].Duration} {frames[0].Text}");
            }
            else
            {
                writer.WriteLine($"$frames {string.Join(" ", frames.Select(f => f.Text))}");
            }
        }

        private void UpdateEditorWithCurrentFrame()
        {
            if (currentFrameIndex >= 0 && currentFrameIndex < frames.Count)
            {
                FrameTextEditor.Text = frames[currentFrameIndex].Text;
                FrameDurationBox.Text = frames[currentFrameIndex].Duration.ToString();
            }
            else
            {
                FrameTextEditor.Text = string.Empty;
                FrameDurationBox.Text = string.Empty;
            }
        }
    }
}