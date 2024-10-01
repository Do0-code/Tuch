using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

namespace Tuch
{
    public partial class AnimationEditorWindow : Window
    {
        public class AnimationFrame
        {
            public string Text { get; set; }
            public int Duration { get; set; }
            public double PixelWidth => Duration * 20; // 프레임당 20픽셀
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