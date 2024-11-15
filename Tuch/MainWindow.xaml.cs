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
            ShowHomeScreen();

            // Command binding for F5 key
            CommandBinding runBinding = new CommandBinding(ApplicationCommands.New);
            runBinding.Executed += RunCode_Click;
            this.CommandBindings.Add(runBinding);

            // Subscribe to HomeScreen events
            HomeScreenView.CreateNewProjectRequested += HomeScreen_CreateNewProjectRequested;
            HomeScreenView.OpenRecentProjectRequested += HomeScreen_OpenRecentProjectRequested;
        }

        private void ShowHomeScreen()
        {
            HomeScreenView.Visibility = Visibility.Visible;
            EditorView.Visibility = Visibility.Collapsed;
        }

        private void ShowEditorView()
        {
            HomeScreenView.Visibility = Visibility.Collapsed;
            EditorView.Visibility = Visibility.Visible;
        }

        private void OpenAnimationEditor_Click(object sender, RoutedEventArgs e)
        {
            var animationEditor = new AnimationEditorWindow();
            animationEditor.Show();
        }


        private void PopulateFileViewer()
        {
            FileViewer.Items.Clear();
            if (!string.IsNullOrEmpty(projectPath) && Directory.Exists(projectPath))
            {
                var rootItem = new TreeViewItem { Header = "Project", Tag = "Root" };
                PopulateTreeView(rootItem, projectPath);
                FileViewer.Items.Add(rootItem);
            }
        }

        private void PopulateTreeView(TreeViewItem parentItem, string path)
        {
            if (string.IsNullOrEmpty(path) || !Directory.Exists(path))
            {
                return;
            }

            foreach (string directory in Directory.GetDirectories(path))
            {
                var directoryItem = new TreeViewItem
                {
                    Header = new DirectoryInfo(directory).Name,
                    Tag = "Directory"
                };
                parentItem.Items.Add(directoryItem);
                PopulateTreeView(directoryItem, directory);
            }

            foreach (string file in Directory.GetFiles(path))
            {
                var fileItem = new TreeViewItem
                {
                    Header = Path.GetFileName(file),
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

        private void HomeScreen_OpenRecentProjectRequested(object sender, string projectPath)
        {
            OpenProject(projectPath);
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
                //디렉토리가 존재하지 않으면 생성
                if (!Directory.Exists(projectPath))
                {
                    Directory.CreateDirectory(projectPath);
                }
                //main.c 내용
                if (!File.Exists(mainFilePath))
                {
                    File.WriteAllText(mainFilePath, "// Your C code here\n");
                }
                // 파일 뷰어 갱신
                PopulateFileViewer();
                // main.c를 에디터에 로드
                ShowEditorView();
                LoadFileContent(mainFilePath);

                // Add to recent projects
                HomeScreenView.AddRecentProject(Path.GetFileName(projectPath), projectPath);
            }
        }

        private void OpenProject(string path)
        {
            if (Directory.Exists(path))
            {
                projectPath = path;
                PopulateFileViewer();
                ShowEditorView();

                // Try to load main.c if it exists
                string mainFilePath = Path.Combine(projectPath, "main.c");
                if (File.Exists(mainFilePath))
                {
                    LoadFileContent(mainFilePath);
                }

                // Add to recent projects
                HomeScreenView.AddRecentProject(Path.GetFileName(projectPath), projectPath);
            }
            else
            {
                MessageBox.Show("The selected project folder does not exist.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void FileItem_Selected(object sender, RoutedEventArgs e)
        {
            var fileItem = sender as TreeViewItem;
            if (fileItem != null && fileItem.Tag as string == "File")
            {
                string filePath = GetFilePath(fileItem);
                if (File.Exists(filePath))
                {
                    LoadFileContent(filePath);
                }
            }
        }

        private void OpenProject_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog();
            System.Windows.Forms.DialogResult result = dialog.ShowDialog();

            if (result == System.Windows.Forms.DialogResult.OK && !string.IsNullOrWhiteSpace(dialog.SelectedPath))
            {
                OpenProject(dialog.SelectedPath);
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
                MessageBox.Show("Please save/create the file before running.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Get the project directory
            string projectDirectory = Path.GetDirectoryName(currentFilePath);

            // Process the code with SplitBuild
            string processedCode = SplitBuild(CodeEditor.Text, projectDirectory);

            // Create a temporary source file with the processed code
            string tempSourcePath = Path.Combine(Path.GetTempPath(), "TuchCompiler");
            if (!Directory.Exists(tempSourcePath))
            {
                Directory.CreateDirectory(tempSourcePath);
            }

            string tempSourceFile = Path.Combine(tempSourcePath, Path.GetFileName(currentFilePath));
            File.WriteAllText(tempSourceFile, processedCode);

            // Use the same directory as the source file for output executable
            string outputExe = Path.Combine(projectDirectory, Path.GetFileNameWithoutExtension(currentFilePath) + ".exe");

            // Delete existing exe if it exists
            try
            {
                if (File.Exists(outputExe))
                {
                    File.Delete(outputExe);
                }
            }
            catch
            {
                // Ignore deletion errors
            }

            // Compile with elevated permissions
            ProcessStartInfo compileInfo = new ProcessStartInfo();
            compileInfo.FileName = "g++";
            compileInfo.Arguments = $"-o \"{outputExe}\" \"{tempSourceFile}\"";
            compileInfo.UseShellExecute = false;
            compileInfo.RedirectStandardError = true;
            compileInfo.CreateNoWindow = true;
            compileInfo.Verb = "runas"; // Request elevated permissions

            try
            {
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

                // Run the compiled program in a CMD window
                ProcessStartInfo run = new ProcessStartInfo();
                run.FileName = "cmd.exe";
                run.Arguments = $"/K \"{outputExe}\"";
                run.UseShellExecute = true;
                run.CreateNoWindow = false;
                run.WorkingDirectory = projectDirectory; // Use project directory as working directory

                Process.Start(run);
            }
            catch (Exception ex)
            {
                ConsoleOutput.Text = $"Error: {ex.Message}";
            }
            finally
            {
                // Clean up the temporary files after a short delay
                Task.Delay(500).ContinueWith(t =>
                {
                    try
                    {
                        if (File.Exists(outputExe))
                        {
                            File.Delete(outputExe);
                        }
                        if (File.Exists(tempSourceFile))
                        {
                            File.Delete(tempSourceFile);
                        }
                    }
                    catch
                    {
                        // Ignore deletion errors
                    }
                });
            }
        }

        private string SplitBuild(string sourceCode, string projectDirectory)
        {
            var lines = sourceCode.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            var resultCode = new StringBuilder();

            foreach (var line in lines)
            {
                if (line.Trim().StartsWith("///"))
                {
                    // Extract filename after ///
                    string filename = line.Trim().Substring(3).Trim();

                    // Check if the file has no extension, add .cppe
                    if (!Path.HasExtension(filename))
                    {
                        filename += ".cppe";
                    }

                    // Construct full path
                    string fullPath = Path.Combine(projectDirectory, filename);

                    // Check if file exists
                    if (File.Exists(fullPath))
                    {
                        try
                        {
                            // Read and append the content of the .cppe file
                            string cppeContent = File.ReadAllText(fullPath);
                            resultCode.AppendLine(cppeContent);
                        }
                        catch (Exception ex)
                        {
                            // If there's an error reading the file, keep the original line
                            resultCode.AppendLine($"// Error including {filename}: {ex.Message}");
                            resultCode.AppendLine(line);
                        }
                    }
                    else
                    {
                        // If file doesn't exist, keep the original line and add comment
                        resultCode.AppendLine($"// File not found: {filename}");
                        resultCode.AppendLine(line);
                    }
                }
                else
                {
                    // For normal lines, just append them
                    resultCode.AppendLine(line);
                }
            }

            return resultCode.ToString();
        }

        // Add this helper method to check if the process has elevated permissions
        private bool IsElevated()
        {
            using (var identity = System.Security.Principal.WindowsIdentity.GetCurrent())
            {
                var principal = new System.Security.Principal.WindowsPrincipal(identity);
                return principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
            }
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
                MessageBox.Show($"파일 로딩 오류: {ex.Message}");
            }
        }

        //private void UpdateFileInfo(string filePath)
        //{
        //    var fileInfo = new FileInfo(filePath);
        //    FileInfo.Text = $"파일: {fileInfo.Name}\n크기: {fileInfo.Length} 바이트\n최종 수정: {fileInfo.LastWriteTime}";
        //}
    }
}