using System.Windows;
using System;
using System.Windows.Controls;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace Tuch
{
    public partial class HomeScreen : UserControl
    {
        public event EventHandler CreateNewProjectRequested;
        public event EventHandler<string> OpenRecentProjectRequested;

        private ObservableCollection<RecentProject> recentProjects;

        public HomeScreen()
        {
            InitializeComponent();
            LoadRecentProjects();
        }

        private void CreateNewProject_Click(object sender, RoutedEventArgs e)
        {
            CreateNewProjectRequested?.Invoke(this, EventArgs.Empty);
        }

        private void LoadRecentProjects()
        {
            recentProjects = new ObservableCollection<RecentProject>();
            string recentProjectsFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Tuch", "RecentProjects.txt");

            if (File.Exists(recentProjectsFile))
            {
                foreach (string line in File.ReadAllLines(recentProjectsFile))
                {
                    string[] parts = line.Split('|');
                    if (parts.Length == 2)
                    {
                        recentProjects.Add(new RecentProject { Name = parts[0], Path = parts[1] });
                    }
                }
            }

            RecentProjectsListView.ItemsSource = recentProjects;
        }

        private void RecentProjectsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (RecentProjectsListView.SelectedItem is RecentProject selectedProject)
            {
                OpenRecentProjectRequested?.Invoke(this, selectedProject.Path);
            }
        }

        public void AddRecentProject(string name, string path)
        {
            var existingProject = recentProjects.FirstOrDefault(p => p.Path == path);
            if (existingProject != null)
            {
                // 이미 존재하는 프로젝트라면 제거
                recentProjects.Remove(existingProject);
            }

            // 새 프로젝트를 맨 앞에 추가
            recentProjects.Insert(0, new RecentProject { Name = name, Path = path });

            // 최근 프로젝트 목록을 10개로 유지
            while (recentProjects.Count > 10)
            {
                recentProjects.RemoveAt(recentProjects.Count - 1);
            }

            SaveRecentProjects();
        }

        private void SaveRecentProjects()
        {
            string recentProjectsFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Tuch", "RecentProjects.txt");
            Directory.CreateDirectory(Path.GetDirectoryName(recentProjectsFile));

            using (StreamWriter writer = new StreamWriter(recentProjectsFile))
            {
                foreach (var project in recentProjects)
                {
                    writer.WriteLine($"{project.Name}|{project.Path}");
                }
            }
        }
    }

    public class RecentProject
    {
        public string Name { get; set; }
        public string Path { get; set; }
    }
}