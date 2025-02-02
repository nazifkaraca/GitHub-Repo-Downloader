using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Octokit;
using Ookii.Dialogs.Wpf;

namespace GitHubDownloader
{
    public partial class MainWindow : Window
    {
        private static readonly HttpClient httpClient = new HttpClient();
        private string downloadPath = "DownloadedFiles";

        public MainWindow()
        {
            InitializeComponent();
            txtDownloadPath.Text = downloadPath;
        }

        private void btnBrowse_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new VistaFolderBrowserDialog();
            if (dialog.ShowDialog() == true)
            {
                downloadPath = dialog.SelectedPath;
                txtDownloadPath.Text = downloadPath;
            }
        }

        private async void btnDownload_Click(object sender, RoutedEventArgs e)
        {
            string repoOwner = txtRepoOwner.Text.Trim();
            string repoName = txtRepoName.Text.Trim();
            string folderPath = txtFolderPath.Text.Trim();
            string branch = txtBranch.Text.Trim();
            bool useAPI = cmbMode.SelectedIndex == 0; // If API mode selected true 

            if (string.IsNullOrWhiteSpace(repoOwner) || string.IsNullOrWhiteSpace(repoName) ||
                string.IsNullOrWhiteSpace(folderPath) || string.IsNullOrWhiteSpace(branch))
            {
                MessageBox.Show("Please fill all fields.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (useAPI)
            {
                // Use API mode
                string token = txtPAT.Password.Trim();
                if (string.IsNullOrWhiteSpace(token))
                {
                    MessageBox.Show("Please enter GitHub PAT (Token)!", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var client = new GitHubClient(new ProductHeaderValue("MyApp"))
                {
                    Credentials = new Credentials(token)
                };

                progressBar.Visibility = Visibility.Visible;
                lstStatus.Items.Add("Download with API has started..");
                treeViewFiles.Items.Clear();

                await ProcessFolderRecursively(repoOwner, repoName, folderPath, branch, client, null);

                lstStatus.Items.Add("Download finished!");
                progressBar.Visibility = Visibility.Hidden;
            }
            else
            {
                // No API mode - use `git sparse-checkout`
                lstStatus.Items.Add("Download with No API has started (git sparse-checkout)...");
                await Task.Run(() => CloneWithGit(repoOwner, repoName, folderPath, branch));

                lstStatus.Items.Add("Download finished!");
            }
        }

        private async Task ProcessFolderRecursively(string repoOwner, string repoName, string folderPath, string branch, GitHubClient client, TreeViewItem parentNode)
        {
            var contents = await client.Repository.Content.GetAllContentsByRef(repoOwner, repoName, folderPath, branch);
            string basePath = Path.Combine(downloadPath, folderPath);
            Directory.CreateDirectory(basePath);

            TreeViewItem folderNode = new TreeViewItem { Header = folderPath };
            if (parentNode == null)
            {
                treeViewFiles.Items.Add(folderNode);
            }
            else
            {
                parentNode.Items.Add(folderNode);
            }

            int totalFiles = contents.Count;
            int processedFiles = 0;

            foreach (var content in contents)
            {
                processedFiles++;
                double progress = (double)processedFiles / totalFiles * 100;
                progressBar.Value = progress;

                if (content.Type == ContentType.File)
                {
                    string filePath = Path.Combine(basePath, content.Name);
                    var fileBytes = await httpClient.GetByteArrayAsync(content.DownloadUrl);
                    await File.WriteAllBytesAsync(filePath, fileBytes);

                    lstStatus.Items.Add($"Downloaded: {content.Name}");
                    folderNode.Items.Add(new TreeViewItem { Header = content.Name });
                }
                else if (content.Type == ContentType.Dir)
                {
                    lstStatus.Items.Add($"Folder is being processed: {content.Name}");
                    await ProcessFolderRecursively(repoOwner, repoName, content.Path, branch, client, folderNode);
                }
            }
        }

        private void CloneWithGit(string repoOwner, string repoName, string folderPath, string branch)
        {
            string repoUrl = $"https://github.com/{repoOwner}/{repoName}.git";
            string repoPath = Path.Combine(downloadPath, repoName);

            if (!Directory.Exists(repoPath))
            {
                lstStatus.Dispatcher.Invoke(() => lstStatus.Items.Add($"Git repo is cloning: {repoUrl}"));
                RunGitCommand($"clone --no-checkout {repoUrl} \"{repoPath}\"");
            }

            Directory.SetCurrentDirectory(repoPath);
            RunGitCommand("sparse-checkout init");
            RunGitCommand($"sparse-checkout set {folderPath}");
            RunGitCommand($"checkout {branch}");

            lstStatus.Dispatcher.Invoke(() => lstStatus.Items.Add("Git cloning completed."));
        }

        private void RunGitCommand(string arguments)
        {
            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (Process process = new Process { StartInfo = psi })
            {
                process.Start();
                process.WaitForExit();
            }
        }
    }
}
