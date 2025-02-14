using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Web;
using System.Windows;
using System.Windows.Controls;
using Octokit;
using Ookii.Dialogs.Wpf;

namespace GitHubDownloader
{
    public partial class MainWindow : Window
    {
        private static readonly HttpClient httpClient = new HttpClient();
        private string downloadPath = "Downloads";

        public MainWindow()
        {
            InitializeComponent();
            txtDownloadPath.Text = downloadPath;
            cmbMode.SelectionChanged += CmbMode_SelectionChanged;
        }

        /// <summary>
        /// Parses the GitHub folder URL and extracts repository owner, name, branch, and folder path.
        /// </summary>
        private void btnParseUrl_Click(object sender, RoutedEventArgs e)
        {
            string url = txtGitHubUrl.Text.Trim();
            if (string.IsNullOrWhiteSpace(url))
            {
                MessageBox.Show("Please enter a valid GitHub folder URL.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // Decode the URL to handle spaces and special characters
                string decodedUrl = HttpUtility.UrlDecode(url);
                var uri = new Uri(url);

                // Decode the path again after extracting it from the URI
                string decodedPath = HttpUtility.UrlDecode(uri.AbsolutePath.Trim('/'));
                var segments = decodedPath.Split('/');

                if (segments.Length < 5 || segments[2] != "tree")
                {
                    MessageBox.Show("Invalid URL format. Expected: https://github.com/user/repo/tree/branch/path/to/folder",
                        "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Automatically fill in the extracted values into the UI
                txtRepoOwner.Text = segments[0];
                txtRepoName.Text = segments[1];
                txtBranch.Text = segments[3];
                txtFolderPath.Text = string.Join("/", segments[4..]);

                MessageBox.Show("URL parsed successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error parsing the URL: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Handles mode selection and hides/shows appropriate input fields.
        /// </summary>
        private void CmbMode_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            bool isUrlMode = (cmbMode.SelectedIndex == 1);
            bool isApiMode = (cmbMode.SelectedIndex == 0);

            // Show/hide UI elements based on selected mode
            SetVisibility(isUrlMode ? Visibility.Collapsed : Visibility.Visible,
                txtRepoOwner, txtRepoOwnerLabel,
                txtRepoName, txtRepoNameLabel,
                txtFolderPath, txtFolderPathLabel,
                txtBranch, txtBranchLabel);

            // API Mode: Show/Hide GitHub Personal Access Token (PAT)
            SetVisibility(isApiMode ? Visibility.Visible : Visibility.Collapsed, txtPAT, txtPATLabel);
        }

        /// <summary>
        /// Opens a folder selection dialog for choosing the download location.
        /// </summary>
        private void btnBrowse_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new VistaFolderBrowserDialog();
            if (dialog.ShowDialog() == true)
            {
                downloadPath = dialog.SelectedPath;
                txtDownloadPath.Text = downloadPath;
            }
        }

        /// <summary>
        /// Initiates the download process based on the selected mode.
        /// </summary>
        private async void btnDownload_Click(object sender, RoutedEventArgs e)
        {
            string repoOwner = txtRepoOwner.Text.Trim();
            string repoName = txtRepoName.Text.Trim();
            string folderPath = txtFolderPath.Text.Trim();
            string branch = txtBranch.Text.Trim();
            bool useAPI = cmbMode.SelectedIndex == 0;

            if (string.IsNullOrWhiteSpace(repoOwner) || string.IsNullOrWhiteSpace(repoName) ||
                string.IsNullOrWhiteSpace(folderPath) || string.IsNullOrWhiteSpace(branch))
            {
                MessageBox.Show("Please fill all fields.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            SetVisibility(Visibility.Visible, treeViewFilesLabel, treeViewFiles, lstStatusLabel, lstStatus);

            if (useAPI)
            {
                string token = txtPAT.Password.Trim();
                if (string.IsNullOrWhiteSpace(token))
                {
                    MessageBox.Show("Please enter a GitHub PAT (Token)!", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var client = new GitHubClient(new ProductHeaderValue("MyApp"))
                {
                    Credentials = new Credentials(token)
                };

                lstStatus.Items.Add("Download with API has started...");
                treeViewFiles.Items.Clear();

                await ProcessFolderRecursively(repoOwner, repoName, folderPath, branch, client, null!);

                lstStatus.Items.Add("Download finished!");
            }
            else
            {
                lstStatus.Items.Add("Download with No API has started (git sparse-checkout)...");
                await Task.Run(() => CloneWithGit(repoOwner, repoName, folderPath, branch));
                lstStatus.Items.Add("Download finished!");
            }

            ResetWindow();

            MessageBox.Show("Download successfully completed!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void CloneWithGit(string repoOwner, string repoName, string folderPath, string branch)
        {
            try
            {
                string repoUrl = $"https://github.com/{repoOwner}/{repoName}.git";
                string repoPath = Path.Combine(downloadPath, repoName);

                if (!Directory.Exists(repoPath))
                {
                    lstStatus.Dispatcher.Invoke(() => lstStatus.Items.Add($"Cloning Git repository: {repoUrl}"));
                    RunGitCommand($"clone --no-checkout {repoUrl} \"{repoPath}\"");
                }

                Directory.SetCurrentDirectory(repoPath);

                lstStatus.Dispatcher.Invoke(() => lstStatus.Items.Add($"Checking out branch: {branch}"));
                RunGitCommand($"checkout {branch}"); // Ensure the branch is checked out first

                lstStatus.Dispatcher.Invoke(() => lstStatus.Items.Add("Enabling sparse-checkout mode..."));
                RunGitCommand("config core.sparseCheckout true"); // Enable advanced sparse-checkout behavior

                lstStatus.Dispatcher.Invoke(() => lstStatus.Items.Add("Clearing sparse-checkout patterns..."));
                RunGitCommand("sparse-checkout set --no-cone \"/*\""); // First, exclude all files

                lstStatus.Dispatcher.Invoke(() => lstStatus.Items.Add($"Setting sparse-checkout path: {folderPath}"));
                RunGitCommand($"sparse-checkout set \"{folderPath}/*\""); // Explicitly include only the selected folder

                lstStatus.Dispatcher.Invoke(() => lstStatus.Items.Add("Git sparse-checkout completed."));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error during Git clone: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Runs a Git command in the system process.
        /// </summary>
        private void RunGitCommand(string arguments)
        {
            try
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

                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();
                    process.WaitForExit();

                    if (!string.IsNullOrEmpty(output))
                        lstStatus.Dispatcher.Invoke(() => lstStatus.Items.Add($"Git Output: {output.Trim()}"));

                    if (!string.IsNullOrEmpty(error))
                        lstStatus.Dispatcher.Invoke(() => lstStatus.Items.Add($"Git Error: {error.Trim()}"));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error running Git command: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        /// <summary>
        /// Recursively processes a folder and downloads its contents using the GitHub API.
        /// </summary>
        private async Task ProcessFolderRecursively(string repoOwner, string repoName, string folderPath, string branch, GitHubClient client, TreeViewItem parentNode)
        {
            try
            {
                // Get all contents of the specified folder
                var contents = await client.Repository.Content.GetAllContentsByRef(repoOwner, repoName, folderPath, branch);
                string basePath = Path.Combine(downloadPath, folderPath);
                Directory.CreateDirectory(basePath);

                // Create a TreeView node for the folder
                TreeViewItem folderNode = new TreeViewItem { Header = folderPath };
                if (parentNode == null)
                {
                    treeViewFiles.Items.Add(folderNode);
                }
                else
                {
                    parentNode.Items.Add(folderNode);
                }

                // Process each item in the folder (files and subdirectories)
                await Parallel.ForEachAsync(contents, async (content, _) =>
                {
                    if (content.Type == ContentType.File)
                    {
                        // Construct file path and download the file
                        string filePath = Path.Combine(basePath, content.Name);
                        using var fileStream = new FileStream(filePath, System.IO.FileMode.Create, FileAccess.Write, FileShare.None);
                        using var stream = await httpClient.GetStreamAsync(content.DownloadUrl);
                        await stream.CopyToAsync(fileStream);

                        // Update UI with downloaded file information
                        lstStatus.Dispatcher.Invoke(() => lstStatus.Items.Add($"Downloaded: {content.Name}"));
                        folderNode.Dispatcher.Invoke(() => folderNode.Items.Add(new TreeViewItem { Header = content.Name }));
                    }
                    else if (content.Type == ContentType.Dir)
                    {
                        // Recursively process subdirectories
                        lstStatus.Dispatcher.Invoke(() => lstStatus.Items.Add($"Processing folder: {content.Name}"));
                        await ProcessFolderRecursively(repoOwner, repoName, content.Path, branch, client, folderNode);
                    }
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error processing folder '{folderPath}': {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Sets visibility for multiple UI elements at once.
        /// </summary>
        /// <summary>
        /// Sets visibility for multiple UI elements at once, avoiding null references.
        /// </summary>
        private void SetVisibility(Visibility visibility, params UIElement[] elements)
        {
            foreach (var element in elements)
            {
                if (element != null)  // Ensure the element is not null before modifying visibility
                {
                    element.Visibility = visibility;
                }
                else
                {
                    Debug.WriteLine("Warning: Attempted to set visibility on a null UI element.");
                }
            }
        }

        /// <summary>
        /// Resets the UI to its default state.
        /// </summary>
        private void ResetWindow()
        {
            // Reset text fields
            txtRepoOwner.Text = "";
            txtRepoName.Text = "";
            txtFolderPath.Text = "";
            txtBranch.Text = "master"; // Default branch
            txtDownloadPath.Text = "Downloads";
            txtPAT.Password = "";

            // Reset combo box
            //cmbMode.SelectedIndex = 0;

            // Hide dynamic UI elements
            SetVisibility(Visibility.Collapsed, treeViewFilesLabel, treeViewFiles, lstStatusLabel, lstStatus);

            // Clear status list
            lstStatus.Items.Clear();

            // Clear TreeView files
            treeViewFiles.Items.Clear();

            // Reset window size to fit new content
            this.SizeToContent = SizeToContent.WidthAndHeight;
        }
    }
}


