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
        private string projectPath = @"D:\workspace"; // change with your folder
        private string currentFilePath;

        public MainWindow()
        {
            InitializeComponent();
            PopulateFileViewer();
            LoadCustomHighlighting();

            //// Apply custom colors
            //var highlighting = CodeEditor.SyntaxHighlighting as IHighlightingDefinition;
            //if (highlighting != null)
            //{
            //    highlighting.GetNamedColor("Keyword").Foreground = new SimpleHighlightingBrush(Colors.Blue);
            //    highlighting.GetNamedColor("Comment").Foreground = new SimpleHighlightingBrush(Colors.Green);
            //    // Set more colors as needed
            //}

            // Command binding for F5 key
            CommandBinding runBinding = new CommandBinding(ApplicationCommands.New);
            runBinding.Executed += RunCode_Click;
            this.CommandBindings.Add(runBinding);
        }

        private void PopulateFileViewer()
        {
            var rootItem = new TreeViewItem { Header = "Project", Tag = "Root" };
            PopulateTreeView(rootItem, projectPath);
            FileViewer.Items.Add(rootItem);
        }

        private void PopulateTreeView(TreeViewItem parentItem, string path)
        {
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

                ConsoleOutput.Text = "Output:\n" + output;
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
                MessageBox.Show($"파일 로딩 오류: {ex.Message}");
                Console.WriteLine($"파일 로딩 중 오류 발생: {ex}");
            }
        }

        //private void UpdateFileInfo(string filePath)
        //{
        //    var fileInfo = new FileInfo(filePath);
        //    FileInfo.Text = $"파일: {fileInfo.Name}\n크기: {fileInfo.Length} 바이트\n최종 수정: {fileInfo.LastWriteTime}";
        //}
    }
}
