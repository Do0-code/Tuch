using System.Windows;
using System;
using System.Windows.Controls;

namespace Tuch
{
    public partial class HomeScreen : UserControl
    {
        public event EventHandler CreateNewProjectRequested;

        public HomeScreen()
        {
            InitializeComponent();
        }

        private void CreateNewProject_Click(object sender, RoutedEventArgs e)
        {
            CreateNewProjectRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}