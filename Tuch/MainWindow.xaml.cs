using System;
using System.Collections.Generic;
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
using System.Windows.Navigation;
//using System.Windows.Shapes;
using System.IO;
using Microsoft.Win32;
using System.Diagnostics;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using System.Xml;


namespace Tuch
{
    public partial class MainWindow : Window
    {
        private string projectPath;
        private string currentFilePath;

        public MainWindow()
        {
            InitializeComponent();
            PopulateFileViewer();
            LoadCustomHighlighting();

            // Command binding for F5 key
            CommandBinding runBinding = new CommandBinding(ApplicationCommands.New);
            runBinding.Executed += RunCode_Click;
            this.CommandBindings.Add(runBinding);
        }

        private void PopulateFileViewer()
        {
            FileViewer.Items.Clear();
            if (string.IsNullOrEmpty(projectPath) || !Directory.Exists(projectPath))
            {
                MessageBox.Show("유효한 프로젝트가 로드되지 않았습니다.", "알림", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            var rootItem = new TreeViewItem { Header = "Project", Tag = "Root" };
            PopulateTreeView(rootItem, projectPath);
            FileViewer.Items.Add(rootItem);
        }

        private void PopulateTreeView(TreeViewItem parentItem, string path)
        {
            if (string.IsNullOrEmpty(path) || !Directory.Exists(path))
            {
                MessageBox.Show("유효하지 않은 프로젝트 경로입니다.", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            foreach (string directory in Directory.GetDirectories(path))
            {
                var directoryItem = new TreeViewItem
                {
                    Header = new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        Children =
                {
                    //new Image { Source = (BitmapImage)FindResource("FolderIcon"), Width = 16, Height = 16 },
                    new TextBlock { Text = new DirectoryInfo(directory).Name, Margin = new Thickness(5,0,0,0) }
                }
                    },
                    Tag = "Directory"
                };
                parentItem.Items.Add(directoryItem);
                PopulateTreeView(directoryItem, directory);
            }

            foreach (string file in Directory.GetFiles(path))
            {
                var fileItem = new TreeViewItem
                {
                    Header = new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        Children =
                {
                    //new Image { Source = (BitmapImage)FindResource("FileIcon"), Width = 16, Height = 16 },
                    new TextBlock { Text = Path.GetFileName(file), Margin = new Thickness(5,0,0,0) }
                }
                    },
                    Tag = "File"
                };
                fileItem.Selected += FileItem_Selected;
                parentItem.Items.Add(fileItem);
            }
        }

        private void LoadCustomHighlighting()
        {
            using (Stream s = typeof(MainWindow).Assembly.GetManifestResourceStream("Tuch.CustomCHighlighting.xshd"))
            {
                if (s != null)
                {
                    using (XmlReader reader = new XmlTextReader(s))
                    {
                        CodeEditor.SyntaxHighlighting = HighlightingLoader.Load(reader, HighlightingManager.Instance);
                    }
                }
            }
        }

        private void HomeScreen_CreateNewProjectRequested(object sender, EventArgs e)
        {
            CreateNewProject();
        }

        private void NewProject_Click(object sender, RoutedEventArgs e)
        {
            CreateNewProject();
        }

        private void CreateNewProject()
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog();
            System.Windows.Forms.DialogResult result = dialog.ShowDialog();

            if (result == System.Windows.Forms.DialogResult.OK && !string.IsNullOrWhiteSpace(dialog.SelectedPath))
            {
                projectPath = dialog.SelectedPath;
                string mainFilePath = Path.Combine(projectPath, "main.c");

                // 디렉토리가 존재하지 않으면 생성
                if (!Directory.Exists(projectPath))
                {
                    Directory.CreateDirectory(projectPath);
                }

                // main.c 파일 생성
                File.WriteAllText(mainFilePath, "// Your C code here\n");

                // 파일 뷰어 갱신
                PopulateFileViewer();

                // 에디터 뷰로 전환
                HomeScreenView.Visibility = Visibility.Collapsed;
                EditorView.Visibility = Visibility.Visible;

                // main.c를 에디터에 로드
                LoadFileContent(mainFilePath);
            }
            else
            {
                MessageBox.Show("프로젝트 생성이 취소되었거나 유효하지 않은 경로가 선택되었습니다.", "알림", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void FileItem_Selected(object sender, RoutedEventArgs e)
        {
            var fileItem = sender as TreeViewItem;
            if (fileItem != null && fileItem.Tag as string != "Root")
            {
                string filePath = GetFilePath(fileItem);
                if (File.Exists(filePath))
                {
                    LoadFileContent(filePath);
                }
                else
                {
                    MessageBox.Show($"File not found: {filePath}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private string GetFilePath(TreeViewItem item)
        {
            List<string> pathParts = new List<string>();
            TreeViewItem parent = item.Parent as TreeViewItem;

            // Traverse up the tree, but stop before the root item
            while (parent != null && parent.Parent is TreeViewItem)
            {
                if (parent.Header is StackPanel sp)
                {
                    var textBlock = sp.Children.OfType<TextBlock>().FirstOrDefault();
                    if (textBlock != null)
                    {
                        pathParts.Insert(0, textBlock.Text);
                    }
                }
                else if (parent.Header is string headerText)
                {
                    pathParts.Insert(0, headerText);
                }
                parent = parent.Parent as TreeViewItem;
            }

            // Add the selected item's name
            if (item.Header is StackPanel itemSp)
            {
                var itemTextBlock = itemSp.Children.OfType<TextBlock>().FirstOrDefault();
                if (itemTextBlock != null)
                {
                    pathParts.Add(itemTextBlock.Text);
                }
            }
            else if (item.Header is string itemHeaderText)
            {
                pathParts.Add(itemHeaderText);
            }

            return Path.Combine(projectPath, Path.Combine(pathParts.ToArray()));
        }

        private void RunCode_Click(object sender, RoutedEventArgs e)
        {
            RunCode();
        }

        private void RunCode()
        {
            if (string.IsNullOrEmpty(currentFilePath))
            {
                MessageBox.Show("Please save the file before running.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Save current content to the file
            File.WriteAllText(currentFilePath, CodeEditor.Text);

            // Compile and run the C program
            string outputExe = Path.ChangeExtension(currentFilePath, ".exe");

            // Compile
            ProcessStartInfo compileInfo = new ProcessStartInfo();
            compileInfo.FileName = "gcc"; // Make sure GCC is in your PATH
            compileInfo.Arguments = $"-o \"{outputExe}\" \"{currentFilePath}\"";
            compileInfo.UseShellExecute = false;
            compileInfo.RedirectStandardError = true;
            compileInfo.CreateNoWindow = true;

            using (Process compileProcess = Process.Start(compileInfo))
            {
                string errors = compileProcess.StandardError.ReadToEnd();
                compileProcess.WaitForExit();

                if (compileProcess.ExitCode != 0)
                {
                    ConsoleOutput.Text = "Compilation Error:\n" + errors;
                    return;
                }
            }

            // Run
            ProcessStartInfo runInfo = new ProcessStartInfo();
            runInfo.FileName = outputExe;
            runInfo.UseShellExecute = false;
            runInfo.RedirectStandardOutput = true;
            runInfo.RedirectStandardError = true;
            runInfo.CreateNoWindow = true;

            using (Process runProcess = Process.Start(runInfo))
            {
                string output = runProcess.StandardOutput.ReadToEnd();
                string errors = runProcess.StandardError.ReadToEnd();

                ConsoleOutput.Text =output;
                if (!string.IsNullOrEmpty(errors))
                {
                    ConsoleOutput.Text += "\nErrors:\n" + errors;
                }
            }

            // Clean up the exe file
            File.Delete(outputExe);
        }

        private void NewFile_Click(object sender, RoutedEventArgs e)
        {
            CodeEditor.Text = string.Empty;
            currentFilePath = null;
        }

        private void OpenFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "C files (*.c)|*.c|All files (*.*)|*.*";
            if (openFileDialog.ShowDialog() == true)
            {
                currentFilePath = openFileDialog.FileName;
                CodeEditor.Text = File.ReadAllText(currentFilePath);
            }
        }

        private void SaveFile_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(currentFilePath))
            {
                SaveFileDialog saveFileDialog = new SaveFileDialog();
                saveFileDialog.Filter = "C files (*.c)|*.c|All files (*.*)|*.*";
                if (saveFileDialog.ShowDialog() == true)
                {
                    currentFilePath = saveFileDialog.FileName;
                }
                else
                {
                    return;
                }
            }
            File.WriteAllText(currentFilePath, CodeEditor.Text);
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void LoadFileContent(string filePath)
        {
            try
            {
                currentFilePath = filePath;
                CodeEditor.Text = File.ReadAllText(filePath);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"File loading error: {ex.Message}");
            }
        }

        //private void UpdateFileInfo(string filePath)
        //{
        //    var fileInfo = new FileInfo(filePath);
        //    FileInfo.Text = $"파일: {fileInfo.Name}\n크기: {fileInfo.Length} 바이트\n최종 수정: {fileInfo.LastWriteTime}";
        //}
    }
}
