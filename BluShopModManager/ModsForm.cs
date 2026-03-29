using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices;


namespace BluShopModManager
{
    public class ModItem
    {
        public string? id { get; set; }
        public string? title { get; set; }
        public string? description { get; set; }
        public string? category { get; set; }
        public string? type { get; set; }
        public string? file_url { get; set; }
        public string? download_link { get; set; }
        public string? image_url { get; set; }
        public int download_count { get; set; }
        public string? version { get; set; }
        public string? uploader_username { get; set; }
    }

    public class ModsList
    {
        public List<ModItem>? mods { get; set; }
    }

    public class ModsForm : Form
    {
        [DllImport("shell32.dll")]
        private static extern void SetCurrentProcessExplicitAppUserModelID(string AppID);

        private static readonly HttpClient http = new HttpClient();

        private string GetModsJsonUrl()
        {
            return $"https://raw.githubusercontent.com/blutgts2/tempmodsload/refs/heads/main/mods.json?t={DateTime.Now.Ticks}";
        }
        private const int CARDS_PER_PAGE = 30;

        private Panel? leftPanel;
        private Panel? rightPanel;
        private FlowLayoutPanel? cardsPanel;
        private Label? statusLabel;
        private ListBox? logListBox;
        private Label? loadingLabel;
        private UpdateChecker? updateChecker;
        private SettingsData currentSettings;
        private List<ModItem> allMods = new List<ModItem>();
        private List<ModItem> currentFilteredMods = new List<ModItem>();
        private List<ModItem> searchedFilteredMods = new List<ModItem>();
        private ComboBox? categoryCombo;
        private TextBox? searchBox;
        private Button? clearSearchBtn;
        private Button? searchBtn;
        private Panel? imagePreviewPanel;
        private PictureBox? imagePreviewBox;
        private Label? imagePreviewTitle;
        private Button? toggleSidebarButton;
        private Label? versionLabel;
        private bool sidebarVisible = true;
        private int leftPanelWidth = 280;
        private bool modsLoaded = false;
        private string currentApplicationPath = "";
        private string currentSearchText = "";
        private bool updateChecked = false;
        private Dictionary<string, string> installedDates = new Dictionary<string, string>();

        private Button? btnPrevPage;
        private Button? btnNextPage;
        private Label? lblPageInfo;
        private int currentPage = 1;
        private int totalPages = 1;
        private Form? logViewerForm;
        private string currentCategory = "all";
        private Panel? cardsContainer;

        private static readonly string appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "BluModManager");
        private static readonly string settingsPath = Path.Combine(appDataPath, "settings.json");

        public ModsForm()
        {
            SetCurrentProcessExplicitAppUserModelID("BluShopModManager");

            try
            {
                string iconPath = Path.Combine(Application.StartupPath, "blumodmanager.ico");
                if (File.Exists(iconPath))
                {
                    this.Icon = new Icon(iconPath);
                }
                else
                {
                    this.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
                }
            }
            catch { }

            currentApplicationPath = Application.ExecutablePath;

            this.Text = "BluShop Mod Manager";
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MinimizeBox = true;
            this.Size = new Size(1200, 700);
            this.BackColor = Color.FromArgb(32, 32, 32);

            CheckSettingsFile();
            LoadInstalledDates();

            currentSettings = SettingsForm.GetSettings();
            SetupLeftPanel();
            SetupRightPanel();

            AddLog("BluShop Mod Manager Started");
            AddLog($"Settings file: {settingsPath}");

            CreateModsContent();
            allMods.Clear();
            modsLoaded = false;
            _ = LoadModsFromGitHub();

            updateChecker = new UpdateChecker(AddLog, (hasUpdate, version) =>
            {
                if (versionLabel != null && hasUpdate && version != null && !updateChecked)
                {
                    updateChecked = true;
                    versionLabel.Text = $"v{UpdateChecker.CURRENT_VERSION} → Update Available!";
                    versionLabel.ForeColor = Color.FromArgb(255, 200, 100);
                    versionLabel.Cursor = Cursors.Hand;
                    versionLabel.Click -= (s, e) => ShowUpdatePopup(version);
                    versionLabel.Click += (s, e) => ShowUpdatePopup(version);
                }
                else if (versionLabel != null && !hasUpdate)
                {
                    versionLabel.Text = $"v{UpdateChecker.CURRENT_VERSION}";
                    versionLabel.ForeColor = Color.FromArgb(150, 150, 150);
                }
            });

            if (currentSettings.AutoCheckUpdates)
            {
                updateChecker.CheckForUpdates(this, false);
            }
            else
            {
                updateChecker.CheckForUpdates(this, false);
                if (versionLabel != null)
                {
                    versionLabel.Text = $"v{UpdateChecker.CURRENT_VERSION}";
                    versionLabel.ForeColor = Color.FromArgb(150, 150, 150);
                }
            }
        }

        private void LoadInstalledDates()
        {
            try
            {
                if (File.Exists(settingsPath))
                {
                    string json = File.ReadAllText(settingsPath);
                    var settings = JsonSerializer.Deserialize<SettingsData>(json);
                    if (settings != null && !string.IsNullOrEmpty(settings.InstalledModsDates))
                    {
                        installedDates = JsonSerializer.Deserialize<Dictionary<string, string>>(settings.InstalledModsDates) ?? new Dictionary<string, string>();
                    }
                }
            }
            catch (Exception ex)
            {
                AddLog($"Error loading installed dates: {ex.Message}");
            }
        }

        private void SaveInstalledDate(string modId)
        {
            installedDates[modId] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            try
            {
                if (File.Exists(settingsPath))
                {
                    string json = File.ReadAllText(settingsPath);
                    var settings = JsonSerializer.Deserialize<SettingsData>(json);
                    if (settings != null)
                    {
                        settings.InstalledModsDates = JsonSerializer.Serialize(installedDates);
                        string updatedJson = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
                        File.WriteAllText(settingsPath, updatedJson);
                    }
                }
            }
            catch (Exception ex)
            {
                AddLog($"Error saving installed date: {ex.Message}");
            }
        }

        private async void ShowUpdatePopup(string latestVersion)
        {
            var result = MessageBox.Show(
                $"A new version is available!\n\n" +
                $"Current: v{UpdateChecker.CURRENT_VERSION}\n" +
                $"Latest: v{latestVersion}\n\n" +
                "Would you like to download the update?\n\n" +
                "The download will open in your browser, and this application will close.",
                "Update Available",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                string downloadUrl = "https://github.com/blutgts2/tempmodsload/releases/download/tempmod/BluShopModManager.exe";

                try
                {
                    AddLog($"Opening download in browser: {downloadUrl}");
                    Process.Start(new ProcessStartInfo(downloadUrl) { UseShellExecute = true });
                    Form countdownForm = new Form
                    {
                        Text = "Update Download Started",
                        Size = new Size(400, 180),
                        StartPosition = FormStartPosition.CenterParent,
                        FormBorderStyle = FormBorderStyle.FixedDialog,
                        MaximizeBox = false,
                        MinimizeBox = false,
                        BackColor = Color.FromArgb(32, 32, 32)
                    };

                    Label messageLabel = new Label
                    {
                        Text = $"Download started in your browser!\n\n" +
                               $"Downloading: BluShopModManager_v{latestVersion}.exe\n\n" +
                               $"This application will close in 5 seconds.",
                        Font = new Font("Segoe UI", 10),
                        ForeColor = Color.White,
                        Location = new Point(20, 30),
                        Size = new Size(360, 80),
                        TextAlign = ContentAlignment.MiddleCenter
                    };
                    countdownForm.Controls.Add(messageLabel);

                    Label countdownLabel = new Label
                    {
                        Text = "5",
                        Font = new Font("Segoe UI", 18, FontStyle.Bold),
                        ForeColor = Color.FromArgb(0, 120, 215),
                        Location = new Point(180, 110),
                        AutoSize = true
                    };
                    countdownForm.Controls.Add(countdownLabel);

                    countdownForm.Show();

                    for (int i = 5; i > 0; i--)
                    {
                        countdownLabel.Text = i.ToString();
                        await Task.Delay(1000);
                    }

                    AddLog("Closing application for update...");
                    Application.Exit();
                }
                catch (Exception ex)
                {
                    AddLog($"Failed to open download: {ex.Message}");
                    MessageBox.Show($"Failed to open download link:\n\n{ex.Message}", "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
        private async Task DownloadAndInstallUpdate(string latestVersion)
        {
            string downloadUrl = $"https://github.com/blutgts2/BluShopModManager/releases/latest/download/BluShopModManager.exe";

            AddLog($"Downloading update v{latestVersion}...");

            using var saveDialog = new SaveFileDialog
            {
                Title = "Save Update",
                Filter = "Executable|*.exe",
                FileName = $"BluShopModManager_v{latestVersion}.exe",
                DefaultExt = "exe"
            };

            if (saveDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    http.DefaultRequestHeaders.Add("User-Agent", "BluShopModManager/1.0");
                    byte[] fileData = await http.GetByteArrayAsync(downloadUrl);
                    await File.WriteAllBytesAsync(saveDialog.FileName, fileData);

                    AddLog($"✅ Update downloaded to: {saveDialog.FileName}");

                    var result = MessageBox.Show(
                        $"Update downloaded successfully!\n\n" +
                        $"Saved to: {saveDialog.FileName}\n\n" +
                        "Would you like to run the installer now?\n\n" +
                        "The current application will close.",
                        "Download Complete",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question);

                    if (result == DialogResult.Yes)
                    {
                        AddLog("Launching installer and closing application...");
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = saveDialog.FileName,
                            UseShellExecute = true
                        });
                        Application.Exit();
                    }
                }
                catch (Exception ex)
                {
                    AddLog($"❌ Download failed: {ex.Message}");
                    MessageBox.Show($"Failed to download update:\n\n{ex.Message}", "Download Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                AddLog("User cancelled download");
            }
        }

        private void CheckSettingsFile()
        {
            try
            {
                bool fileExists = File.Exists(settingsPath);

                if (!fileExists)
                {
                    AddLog($"Settings file not found. Creating new at: {settingsPath}");
                    Directory.CreateDirectory(appDataPath);
                    var defaultSettings = new SettingsData();
                    string json = JsonSerializer.Serialize(defaultSettings, new JsonSerializerOptions { WriteIndented = true });
                    File.WriteAllText(settingsPath, json);
                    AddLog("Default settings created successfully");
                }
                else
                {
                    AddLog($"Settings file found: {settingsPath}");
                    string json = File.ReadAllText(settingsPath);
                    var settings = JsonSerializer.Deserialize<SettingsData>(json);
                    if (settings != null)
                    {
                        AddLog($"Settings loaded: AutoCheckUpdates={settings.AutoCheckUpdates}, ConfirmDownloads={settings.ConfirmDownloads}");
                        if (!string.IsNullOrEmpty(settings.GorillaTagPath))
                        {
                            AddLog($"Gorilla Tag path: {settings.GorillaTagPath}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                AddLog($"Error loading settings: {ex.Message}");
            }
        }

        private void AddLog(string message)
        {
            if (logListBox != null)
            {
                if (logListBox.InvokeRequired)
                {
                    logListBox.Invoke(new Action(() => AddLog(message)));
                }
                else
                {
                    string timestamp = DateTime.Now.ToString("HH:mm:ss");
                    string logEntry = $"[{timestamp}] {message}";
                    logListBox.Items.Insert(0, logEntry);

                    while (logListBox.Items.Count > 500)
                    {
                        logListBox.Items.RemoveAt(logListBox.Items.Count - 1);
                    }
                }
            }
        }

        private void ClearLogs()
        {
            if (logListBox != null)
            {
                if (logListBox.InvokeRequired)
                {
                    logListBox.Invoke(new Action(() => ClearLogs()));
                }
                else
                {
                    logListBox.Items.Clear();
                    AddLog("Log cleared");
                }
            }
        }

        private void CopyFullLogToClipboard()
        {
            if (logListBox == null) return;

            try
            {
                var logLines = new List<string>();
                foreach (var item in logListBox.Items)
                {
                    if (item != null)
                    {
                        logLines.Add(item.ToString() ?? string.Empty);
                    }
                }
                logLines.Reverse();
                string fullLog = string.Join(Environment.NewLine, logLines);
                Clipboard.SetText(fullLog);
                AddLog("Full log copied to clipboard");
                MessageBox.Show("Full log has been copied to clipboard!", "Copied",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                AddLog($"Failed to copy log: {ex.Message}");
                MessageBox.Show($"Failed to copy log: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ShowFullLog()
        {
            if (logViewerForm == null || logViewerForm.IsDisposed)
            {
                logViewerForm = new Form
                {
                    Text = "Full Log Viewer",
                    Size = new Size(800, 600),
                    StartPosition = FormStartPosition.CenterParent,
                    BackColor = Color.FromArgb(28, 28, 28)
                };

                var fullLogBox = new ListBox
                {
                    Dock = DockStyle.Fill,
                    Font = new Font("Consolas", 10),
                    BackColor = Color.FromArgb(20, 20, 20),
                    ForeColor = Color.FromArgb(180, 180, 180),
                    DrawMode = DrawMode.OwnerDrawFixed,
                    ItemHeight = 26,
                    IntegralHeight = false
                };

                fullLogBox.DrawItem += (s, e) =>
                {
                    e.DrawBackground();
                    if (e.Index >= 0 && fullLogBox.Items[e.Index] is string item)
                    {
                        Color textColor = Color.FromArgb(180, 180, 180);
                        if (item.Contains("✅")) textColor = Color.FromArgb(100, 200, 100);
                        else if (item.Contains("❌")) textColor = Color.FromArgb(255, 100, 100);
                        else if (item.Contains("⚠️")) textColor = Color.FromArgb(255, 200, 100);
                        else if (item.Contains("📥")) textColor = Color.FromArgb(100, 200, 255);
                        else if (item.Contains("🔧") || item.Contains("⚙️")) textColor = Color.FromArgb(200, 150, 100);
                        else if (item.Contains("🔄")) textColor = Color.FromArgb(100, 200, 200);

                        using (var brush = new SolidBrush(textColor))
                        {
                            e.Graphics.DrawString(item, fullLogBox.Font, brush, e.Bounds.X, e.Bounds.Y);
                        }
                    }
                    e.DrawFocusRectangle();
                };

                if (logListBox != null)
                {
                    foreach (var item in logListBox.Items)
                    {
                        if (item != null)
                        {
                            fullLogBox.Items.Add(item);
                        }
                    }
                }

                logViewerForm.Controls.Add(fullLogBox);
                logViewerForm.Show(this);
            }
            else
            {
                logViewerForm.BringToFront();
            }
        }

        private async void ShowImagePreview(string imageUrl, string title)
        {
            if (string.IsNullOrEmpty(imageUrl))
            {
                AddLog($"No image available for {title}");
                return;
            }

            if (imagePreviewPanel == null || imagePreviewBox == null) return;

            if (imagePreviewTitle != null)
            {
                imagePreviewTitle.Text = title;
            }

            imagePreviewPanel.Visible = true;
            imagePreviewPanel.BringToFront();
            imagePreviewPanel.Dock = DockStyle.Fill;

            try
            {
                using var webClient = new HttpClient();
                webClient.DefaultRequestHeaders.Add("User-Agent", "BluShopModManager/1.0");
                byte[] imageData = await webClient.GetByteArrayAsync(imageUrl);
                using var ms = new MemoryStream(imageData);
                imagePreviewBox.Image = Image.FromStream(ms);
                imagePreviewBox.SizeMode = PictureBoxSizeMode.Zoom;
            }
            catch (Exception ex)
            {
                AddLog($"Failed to load image: {ex.Message}");
                if (imagePreviewBox != null)
                {
                    imagePreviewBox.Image = null;
                }
            }
        }

        private void CloseImagePreview()
        {
            if (imagePreviewPanel != null)
            {
                imagePreviewPanel.Visible = false;
                if (imagePreviewBox != null && imagePreviewBox.Image != null)
                {
                    imagePreviewBox.Image.Dispose();
                    imagePreviewBox.Image = null;
                }
            }
        }

        private void ToggleSidebar()
        {
            sidebarVisible = !sidebarVisible;
            if (leftPanel != null)
            {
                leftPanel.Width = sidebarVisible ? leftPanelWidth : 0;
                if (toggleSidebarButton != null)
                {
                    toggleSidebarButton.Text = sidebarVisible ? "◀" : "▶";
                    toggleSidebarButton.Location = new Point(sidebarVisible ? leftPanelWidth + 5 : 5, 10);
                }
            }
        }

        private void OpenModsFolder()
        {
            string? gtFolder = DirectoryGuard.EnsureGTDirectory(this, currentSettings);
            if (gtFolder == null) return;

            string pluginsFolder = Path.Combine(gtFolder, "BepInEx", "plugins");

            if (!Directory.Exists(pluginsFolder))
            {
                var result = MessageBox.Show(
                    "BepInEx plugins folder not found!\n\n" +
                    "You need to install BepInEx first before installing mods.\n\n" +
                    "Would you like to install BepInEx now?",
                    "BepInEx Required",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (result == DialogResult.Yes)
                {
                    ShowBepInExForm();
                }
                return;
            }

            try
            {
                Process.Start(new ProcessStartInfo(pluginsFolder) { UseShellExecute = true });
                AddLog($"Opened plugins folder: {pluginsFolder}");
            }
            catch (Exception ex)
            {
                AddLog($"Failed to open folder: {ex.Message}");
                MessageBox.Show($"Failed to open folder:\n\n{ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ShowInstalledMods()
        {
            string? gtFolder = DirectoryGuard.EnsureGTDirectory(this, currentSettings);
            if (gtFolder == null) return;

            string pluginsFolder = Path.Combine(gtFolder, "BepInEx", "plugins");

            if (!Directory.Exists(pluginsFolder))
            {
                MessageBox.Show("BepInEx plugins folder not found!\n\nPlease install BepInEx first.",
                    "Not Found", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string[] files = Directory.GetFiles(pluginsFolder, "*.dll");

            if (files.Length == 0)
            {
                MessageBox.Show("No installed mods found in the plugins folder.",
                    "No Mods", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            Form installedModsForm = new Form
            {
                Text = "Installed Mods",
                Size = new Size(550, 450),
                StartPosition = FormStartPosition.CenterParent,
                BackColor = Color.FromArgb(28, 28, 28),
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false
            };

            var listBox = new ListBox
            {
                Dock = DockStyle.Top,
                Height = 350,
                Font = new Font("Consolas", 10),
                BackColor = Color.FromArgb(40, 40, 40),
                ForeColor = Color.White
            };

            foreach (string file in files)
            {
                listBox.Items.Add(Path.GetFileName(file));
            }

            var deleteButton = new Button
            {
                Text = "Delete Selected",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                BackColor = Color.FromArgb(180, 60, 60),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(150, 40),
                Location = new Point(20, 370),
                Cursor = Cursors.Hand
            };
            deleteButton.FlatAppearance.BorderSize = 0;
            deleteButton.Click += (s, e) =>
            {
                if (listBox.SelectedItem != null)
                {
                    string? selectedFile = listBox.SelectedItem.ToString();
                    if (selectedFile != null)
                    {
                        string filePath = Path.Combine(pluginsFolder, selectedFile);

                        var confirm = MessageBox.Show($"Are you sure you want to delete {selectedFile}?",
                            "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

                        if (confirm == DialogResult.Yes)
                        {
                            try
                            {
                                File.Delete(filePath);
                                listBox.Items.Remove(selectedFile);
                                AddLog($"Deleted mod: {selectedFile}");
                                MessageBox.Show($"{selectedFile} deleted successfully!", "Deleted",
                                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                            }
                            catch (Exception ex)
                            {
                                AddLog($"Failed to delete {selectedFile}: {ex.Message}");
                                MessageBox.Show($"Failed to delete file:\n\n{ex.Message}", "Error",
                                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                        }
                    }
                }
                else
                {
                    MessageBox.Show("Please select a mod to delete.", "No Selection",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            };

            var closeButton = new Button
            {
                Text = "Close",
                Font = new Font("Segoe UI", 10),
                BackColor = Color.FromArgb(60, 60, 60),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(100, 40),
                Location = new Point(180, 370),
                Cursor = Cursors.Hand
            };
            closeButton.FlatAppearance.BorderSize = 0;
            closeButton.Click += (s, e) => installedModsForm.Close();

            installedModsForm.Controls.Add(listBox);
            installedModsForm.Controls.Add(deleteButton);
            installedModsForm.Controls.Add(closeButton);
            installedModsForm.ShowDialog(this);
        }

        private void ShowAnnouncementForm()
        {
            Form announcementForm = new Form
            {
                Text = "Announcements",
                Size = new Size(650, 400),
                StartPosition = FormStartPosition.CenterParent,
                BackColor = Color.FromArgb(32, 32, 32),
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false
            };

            int y = 20;

            var titleLabel = new Label
            {
                Text = "Announcements",
                Font = new Font("Segoe UI", 18, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 120, 215),
                Location = new Point(20, y),
                AutoSize = true
            };
            announcementForm.Controls.Add(titleLabel);
            y += 50;

            var separator = new Panel
            {
                Height = 2,
                BackColor = Color.FromArgb(0, 120, 215),
                Width = announcementForm.Width - 40,
                Location = new Point(20, y)
            };
            announcementForm.Controls.Add(separator);
            y += 30;

            var announcementText = new Label
            {
                Text = "This is v1.0.0 of the BluShop Mod Manager.\n\n" +
                       "The layout will be improved over time with new updates and features.\n\n" +
                       "To automatically get the newest version, make sure the setting is enabled in your settings page (enabled by default).\n\n" +
                       "Stay up to date in our community discord:",
                Font = new Font("Segoe UI", 11),
                ForeColor = Color.FromArgb(200, 200, 200),
                Location = new Point(20, y),
                Size = new Size(announcementForm.Width - 60, 180),
                AutoSize = false
            };
            announcementForm.Controls.Add(announcementText);
            y += 200;

            var discordBtn = new Button
            {
                Text = "Join Discord",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                BackColor = Color.FromArgb(88, 101, 242),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(120, 40),
                Location = new Point(20, y),
                Cursor = Cursors.Hand
            };
            discordBtn.FlatAppearance.BorderSize = 0;
            discordBtn.Click += (s, e) =>
            {
                Process.Start(new ProcessStartInfo("https://discord.gg/blugt") { UseShellExecute = true });
            };
            announcementForm.Controls.Add(discordBtn);

            var closeBtn = new Button
            {
                Text = "Close",
                Font = new Font("Segoe UI", 10),
                BackColor = Color.FromArgb(60, 60, 60),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(100, 40),
                Location = new Point(150, y),
                Cursor = Cursors.Hand
            };
            closeBtn.FlatAppearance.BorderSize = 0;
            closeBtn.Click += (s, e) => announcementForm.Close();
            announcementForm.Controls.Add(closeBtn);

            announcementForm.ShowDialog(this);
        }

        private void ShowSettingsForm()
        {
            Form settingsForm = new Form
            {
                Text = "Settings",
                Size = new Size(600, 500),
                StartPosition = FormStartPosition.CenterParent,
                BackColor = Color.FromArgb(32, 32, 32),
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false
            };

            int y = 20;

            var titleLabel = new Label
            {
                Text = "Settings",
                Font = new Font("Segoe UI", 18, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 120, 215),
                Location = new Point(20, y),
                AutoSize = true
            };
            settingsForm.Controls.Add(titleLabel);
            y += 50;

            var separator = new Panel
            {
                Height = 2,
                BackColor = Color.FromArgb(0, 120, 215),
                Width = settingsForm.Width - 40,
                Location = new Point(20, y)
            };
            settingsForm.Controls.Add(separator);
            y += 30;

            var chkAutoUpdates = new CheckBox
            {
                Text = "Automatically check for updates on startup (enabled by default)",
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.White,
                Location = new Point(30, y),
                AutoSize = true,
                Checked = currentSettings.AutoCheckUpdates
            };
            settingsForm.Controls.Add(chkAutoUpdates);
            y += 35;

            var chkConfirmDownloads = new CheckBox
            {
                Text = "Show confirmation before downloading mods",
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.White,
                Location = new Point(30, y),
                AutoSize = true,
                Checked = currentSettings.ConfirmDownloads
            };
            settingsForm.Controls.Add(chkConfirmDownloads);
            y += 50;

            var pathLabel = new Label
            {
                Text = "Gorilla Tag Executable:",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.FromArgb(200, 200, 200),
                Location = new Point(30, y),
                AutoSize = true
            };
            settingsForm.Controls.Add(pathLabel);
            y += 25;

            var txtGorillaTagPath = new TextBox
            {
                Font = new Font("Segoe UI", 9),
                BackColor = Color.FromArgb(60, 60, 60),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Width = 400,
                Location = new Point(30, y),
                Text = currentSettings.GorillaTagPath ?? "",
                ReadOnly = true
            };
            settingsForm.Controls.Add(txtGorillaTagPath);

            var btnBrowseExe = new Button
            {
                Text = "Browse",
                Font = new Font("Segoe UI", 10),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(80, 30),
                Location = new Point(440, y)
            };
            btnBrowseExe.FlatAppearance.BorderSize = 0;
            btnBrowseExe.Click += (s, e) =>
            {
                using var dlg = new OpenFileDialog
                {
                    Title = "Select Gorilla Tag.exe",
                    Filter = "Gorilla Tag|Gorilla Tag.exe|All Executables|*.exe",
                    CheckFileExists = true
                };

                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    string selected = dlg.FileName;
                    if (!Path.GetFileName(selected).Equals("Gorilla Tag.exe", StringComparison.OrdinalIgnoreCase))
                    {
                        MessageBox.Show("Please select the Gorilla Tag.exe file.", "Invalid File",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    txtGorillaTagPath.Text = selected;
                }
            };
            settingsForm.Controls.Add(btnBrowseExe);
            y += 45;

            var btnOpenSettingsFolder = new Button
            {
                Text = "📁 Open Settings Folder",
                Font = new Font("Segoe UI", 10),
                BackColor = Color.FromArgb(50, 50, 50),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(150, 35),
                Location = new Point(30, y),
                Cursor = Cursors.Hand
            };
            btnOpenSettingsFolder.FlatAppearance.BorderSize = 0;
            btnOpenSettingsFolder.Click += (s, e) =>
            {
                try
                {
                    Process.Start(new ProcessStartInfo(appDataPath) { UseShellExecute = true });
                    AddLog($"Opened settings folder: {appDataPath}");
                }
                catch (Exception ex)
                {
                    AddLog($"Failed to open settings folder: {ex.Message}");
                    MessageBox.Show($"Failed to open folder:\n\n{ex.Message}", "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };
            settingsForm.Controls.Add(btnOpenSettingsFolder);
            y += 45;

            var btnSave = new Button
            {
                Text = "Save Settings",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(120, 40),
                Location = new Point(30, y)
            };
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.Click += (s, e) =>
            {
                currentSettings.AutoCheckUpdates = chkAutoUpdates.Checked;
                currentSettings.ConfirmDownloads = chkConfirmDownloads.Checked;
                currentSettings.GorillaTagPath = txtGorillaTagPath.Text;
                SettingsForm.SaveSettingsStatic(currentSettings);
                AddLog("Settings saved");
                MessageBox.Show("Settings saved!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                settingsForm.Close();
            };
            settingsForm.Controls.Add(btnSave);

            var btnCancel = new Button
            {
                Text = "Cancel",
                Font = new Font("Segoe UI", 10),
                BackColor = Color.FromArgb(80, 80, 80),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(100, 40),
                Location = new Point(160, y)
            };
            btnCancel.FlatAppearance.BorderSize = 0;
            btnCancel.Click += (s, e) => settingsForm.Close();
            settingsForm.Controls.Add(btnCancel);

            y += 70;

            var dividerLine = new Panel
            {
                Height = 1,
                BackColor = Color.FromArgb(80, 80, 80),
                Width = settingsForm.Width - 60,
                Location = new Point(30, y)
            };
            settingsForm.Controls.Add(dividerLine);
            y += 20;

            var comingSoonLabel = new Label
            {
                Text = "More settings coming very soon!",
                Font = new Font("Segoe UI", 10, FontStyle.Italic),
                ForeColor = Color.FromArgb(150, 150, 150),
                Location = new Point(30, y),
                AutoSize = true
            };
            settingsForm.Controls.Add(comingSoonLabel);

            settingsForm.ShowDialog(this);
        }

        private void ShowBepInExForm()
        {
            new BepInExForm(currentSettings).ShowDialog(this);
        }

        private void ShowWebsiteListing(string modId, string modTitle)
        {
            var result = MessageBox.Show(
                $"This will open the direct listing for \"{modTitle}\" from our website.\n\n" +
                "You can view more information, descriptions, videos and more there.\n\n" +
                "Continue?",
                "View on Website",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                string url = $"https://blushop.base44.app/FileDetail?id={modId}";
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
                AddLog($"Opened website listing for: {modTitle}");
            }
        }

        private void UninstallBepInEx()
        {
            string? gtFolder = DirectoryGuard.EnsureGTDirectory(this, currentSettings);
            if (gtFolder == null) return;

            string bepinexFolder = Path.Combine(gtFolder, "BepInEx");
            string winhttpPath = Path.Combine(gtFolder, "winhttp.dll");
            string doorstopPath = Path.Combine(gtFolder, "doorstop_config.ini");

            bool hasBepInExFolder = Directory.Exists(bepinexFolder);
            bool hasWinhttp = File.Exists(winhttpPath);
            bool hasDoorstop = File.Exists(doorstopPath);

            if (!hasBepInExFolder && !hasWinhttp && !hasDoorstop)
            {
                MessageBox.Show("BepInEx is not installed in your Gorilla Tag folder.", "Not Installed",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var result = MessageBox.Show(
                "Are you sure you want to uninstall BepInEx?\n\n" +
                "This will remove:\n" +
                (hasBepInExFolder ? "• BepInEx folder\n" : "") +
                (hasWinhttp ? "• winhttp.dll\n" : "") +
                (hasDoorstop ? "• doorstop_config.ini\n" : ""),
                "Confirm Uninstall",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (result != DialogResult.Yes) return;

            try
            {
                AddLog("Uninstalling BepInEx...");

                if (hasBepInExFolder)
                {
                    Directory.Delete(bepinexFolder, true);
                    AddLog("Removed BepInEx folder");
                }

                if (hasWinhttp)
                {
                    File.Delete(winhttpPath);
                    AddLog("Removed winhttp.dll");
                }

                if (hasDoorstop)
                {
                    File.Delete(doorstopPath);
                    AddLog("Removed doorstop_config.ini");
                }

                AddLog("BepInEx uninstalled successfully");
                MessageBox.Show("BepInEx has been uninstalled successfully!", "Uninstall Complete",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                AddLog($"Failed to uninstall BepInEx: {ex.Message}");
                MessageBox.Show($"Failed to uninstall BepInEx:\n\n{ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ReinstallBepInEx()
        {
            string? gtFolder = DirectoryGuard.EnsureGTDirectory(this, currentSettings);
            if (gtFolder == null) return;

            string bepinexFolder = Path.Combine(gtFolder, "BepInEx");
            string winhttpPath = Path.Combine(gtFolder, "winhttp.dll");
            string doorstopPath = Path.Combine(gtFolder, "doorstop_config.ini");

            bool hasBepInExFolder = Directory.Exists(bepinexFolder);
            bool hasWinhttp = File.Exists(winhttpPath);
            bool hasDoorstop = File.Exists(doorstopPath);

            if (hasBepInExFolder || hasWinhttp || hasDoorstop)
            {
                var result = MessageBox.Show(
                    "BepInEx is already installed.\n\n" +
                    "Reinstalling will remove existing files and install fresh.\n\n" +
                    "Continue?",
                    "Confirm Reinstall",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (result != DialogResult.Yes) return;

                try
                {
                    AddLog("Removing existing BepInEx files...");

                    if (hasBepInExFolder)
                    {
                        Directory.Delete(bepinexFolder, true);
                    }
                    if (hasWinhttp) File.Delete(winhttpPath);
                    if (hasDoorstop) File.Delete(doorstopPath);

                    AddLog("Existing files removed");
                }
                catch (Exception ex)
                {
                    AddLog($"Failed to remove existing files: {ex.Message}");
                    MessageBox.Show($"Failed to remove existing BepInEx files:\n\n{ex.Message}", "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }

            AddLog("Installing BepInEx...");
            new BepInExForm(currentSettings).ShowDialog(this);
        }

        private void ReloadApplication()
        {
            var result = MessageBox.Show(
                "Reloading will close and reopen the application.\n\n" +
                "This will refresh all mod listings and settings.\n\n" +
                "Continue?",
                "Confirm Reload",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                AddLog("Restarting application...");
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = currentApplicationPath,
                        UseShellExecute = true
                    });
                    Application.Exit();
                }
                catch (Exception ex)
                {
                    AddLog($"Failed to restart: {ex.Message}");
                    MessageBox.Show($"Failed to restart application:\n\n{ex.Message}", "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void PerformSearch()
        {
            currentSearchText = searchBox?.Text ?? "";
            FilterBySearch();
        }

        private void FilterBySearch()
        {
            if (string.IsNullOrWhiteSpace(currentSearchText))
            {
                searchedFilteredMods = new List<ModItem>(currentFilteredMods);
            }
            else
            {
                searchedFilteredMods = currentFilteredMods
                    .Where(m => m.title?.ToLower().Contains(currentSearchText.ToLower()) == true)
                    .ToList();
            }

            int tempTotalPages = (int)Math.Ceiling((double)searchedFilteredMods.Count / CARDS_PER_PAGE);
            if (tempTotalPages == 0) tempTotalPages = 1;

            if (currentPage > tempTotalPages) currentPage = tempTotalPages;
            UpdatePaginationControlsForSearch(searchedFilteredMods.Count);
            RenderSearchedPage();
        }

        private void ClearSearch()
        {
            if (searchBox != null)
            {
                searchBox.Text = "";
                currentSearchText = "";
                FilterBySearch();
            }
        }

        private void CreateModsContent()
        {
            if (rightPanel == null) return;

            if (cardsContainer != null && cardsContainer.Parent == rightPanel)
            {
                if (modsLoaded && allMods.Count > 0)
                {
                    FilterMods();
                }
                return;
            }
            rightPanel.Controls.Clear();

            int xOffset = 35;
            int y = 20;

            var titleLabel = new Label
            {
                Text = "Mods",
                Font = new Font("Segoe UI", 18, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 120, 215),
                Location = new Point(xOffset + 20, y),
                AutoSize = true
            };
            rightPanel.Controls.Add(titleLabel);
            y += 50;

            var topSeparator = new Panel
            {
                Height = 2,
                BackColor = Color.FromArgb(0, 120, 215),
                Width = rightPanel.Width - (xOffset + 40),
                Location = new Point(xOffset + 20, y)
            };
            rightPanel.Controls.Add(topSeparator);
            y += 20;

            var buttonPanel = new Panel
            {
                Location = new Point(xOffset + 20, y),
                Width = rightPanel.Width - (xOffset + 40),
                Height = 40,
                BackColor = Color.Transparent
            };

            var openFolderBtn = new Button
            {
                Text = "📁 Open Mods Folder",
                Font = new Font("Segoe UI", 10),
                BackColor = Color.FromArgb(50, 50, 50),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(140, 35),
                Location = new Point(0, 0),
                Cursor = Cursors.Hand
            };
            openFolderBtn.FlatAppearance.BorderSize = 0;
            openFolderBtn.Click += (s, e) => OpenModsFolder();
            buttonPanel.Controls.Add(openFolderBtn);

            var installedModsBtn = new Button
            {
                Text = "📦 Installed Mods",
                Font = new Font("Segoe UI", 10),
                BackColor = Color.FromArgb(50, 50, 50),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(130, 35),
                Location = new Point(150, 0),
                Cursor = Cursors.Hand
            };
            installedModsBtn.FlatAppearance.BorderSize = 0;
            installedModsBtn.Click += (s, e) => ShowInstalledMods();
            buttonPanel.Controls.Add(installedModsBtn);

            rightPanel.Controls.Add(buttonPanel);
            y += 45;

            var categorySearchPanel = new Panel
            {
                Location = new Point(xOffset + 20, y),
                Width = rightPanel.Width - (xOffset + 40),
                Height = 45,
                BackColor = Color.FromArgb(40, 40, 40),
                BorderStyle = BorderStyle.FixedSingle
            };

            var categoryLabel = new Label
            {
                Text = "Category:",
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.FromArgb(180, 180, 180),
                Location = new Point(15, 12),
                AutoSize = true
            };
            categorySearchPanel.Controls.Add(categoryLabel);

            categoryCombo = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 10),
                BackColor = Color.FromArgb(50, 50, 50),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Width = 130,
                Location = new Point(95, 9),
                Items = { "All", "Menus", "Addon Mods", "BepInEx" },
                SelectedIndex = 0
            };
            categoryCombo.SelectedIndexChanged += (s, e) =>
            {
                if (categoryCombo.SelectedIndex == 0)
                    currentCategory = "all";
                else if (categoryCombo.SelectedIndex == 1)
                    currentCategory = "menus";
                else if (categoryCombo.SelectedIndex == 2)
                    currentCategory = "addon_mods";
                else if (categoryCombo.SelectedIndex == 3)
                    currentCategory = "bepinex";
                AddLog($"Category changed to: {categoryCombo.Text}");
                if (modsLoaded)
                {
                    FilterMods();
                }
            };
            categorySearchPanel.Controls.Add(categoryCombo);

            var searchLabel = new Label
            {
                Text = "Search:",
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.FromArgb(180, 180, 180),
                Location = new Point(240, 12),
                AutoSize = true
            };
            categorySearchPanel.Controls.Add(searchLabel);

            searchBox = new TextBox
            {
                Font = new Font("Segoe UI", 10),
                BackColor = Color.FromArgb(50, 50, 50),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Width = 120,
                Location = new Point(295, 9)
            };
            searchBox.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    PerformSearch();
                }
            };
            categorySearchPanel.Controls.Add(searchBox);

            searchBtn = new Button
            {
                Text = "Search",
                Font = new Font("Segoe UI", 8, FontStyle.Bold),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(55, 25),
                Location = new Point(420, 9),
                Cursor = Cursors.Hand
            };
            searchBtn.FlatAppearance.BorderSize = 0;
            searchBtn.Click += (s, e) => PerformSearch();
            categorySearchPanel.Controls.Add(searchBtn);

            clearSearchBtn = new Button
            {
                Text = "Clear",
                Font = new Font("Segoe UI", 8),
                BackColor = Color.FromArgb(60, 60, 60),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(50, 25),
                Location = new Point(480, 9),
                Cursor = Cursors.Hand
            };
            clearSearchBtn.FlatAppearance.BorderSize = 0;
            clearSearchBtn.Click += (s, e) => ClearSearch();
            categorySearchPanel.Controls.Add(clearSearchBtn);

            rightPanel.Controls.Add(categorySearchPanel);
            y += 55;

            cardsContainer = new Panel
            {
                Location = new Point(xOffset + 20, y),
                Width = rightPanel.Width - (xOffset + 40),
                Height = rightPanel.Height - y - 5,
                BackColor = Color.FromArgb(32, 32, 32)
            };

            loadingLabel = new Label
            {
                Text = "Loading Mod Listings...\nMay Lag Until Its Done Loading.",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 120, 215),
                BackColor = Color.FromArgb(32, 32, 32),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill,
                Visible = true
            };
            cardsContainer.Controls.Add(loadingLabel);

            cardsPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(32, 32, 32),
                AutoScroll = true,
                WrapContents = true,
                Padding = new Padding(10),
                Visible = false
            };
            cardsContainer.Controls.Add(cardsPanel);

            var paginationPanel = new Panel
            {
                Height = 50,
                Dock = DockStyle.Bottom,
                BackColor = Color.FromArgb(28, 28, 28)
            };

            btnPrevPage = new Button
            {
                Text = "◀ Previous",
                Font = new Font("Segoe UI", 10),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(105, 34),
                Location = new Point(16, 8),
                Visible = false
            };
            btnPrevPage.FlatAppearance.BorderSize = 0;
            btnPrevPage.Click += (s, e) => GoToPreviousPage();

            lblPageInfo = new Label
            {
                Text = "Page 1 of 1",
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.FromArgb(180, 180, 180),
                AutoSize = true,
                Location = new Point((paginationPanel.Width / 2) - 50, 14)
            };

            btnNextPage = new Button
            {
                Text = "Next ▶",
                Font = new Font("Segoe UI", 10),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(105, 34),
                Location = new Point(paginationPanel.Width - 120, 8),
                Visible = false
            };
            btnNextPage.FlatAppearance.BorderSize = 0;
            btnNextPage.Click += (s, e) => GoToNextPage();

            paginationPanel.Resize += (s, e) =>
            {
                if (lblPageInfo != null)
                    lblPageInfo.Location = new Point((paginationPanel.Width / 2) - (lblPageInfo.Width / 2), 14);
                if (btnNextPage != null)
                    btnNextPage.Location = new Point(paginationPanel.Width - 120, 8);
            };

            paginationPanel.Controls.Add(btnPrevPage);
            paginationPanel.Controls.Add(lblPageInfo);
            paginationPanel.Controls.Add(btnNextPage);

            cardsContainer.Controls.Add(paginationPanel);
            rightPanel.Controls.Add(cardsContainer);
        }

        private void ShowModsContent()
        {
            if (rightPanel != null)
            {
                rightPanel.Visible = true;
                if (modsLoaded && allMods.Count > 0)
                {
                    FilterMods();
                }
            }
        }

        private void ReloadMods()
        {
            ReloadApplication();
        }

        private void ShowLoading(string message)
        {
            if (loadingLabel != null)
            {
                if (loadingLabel.InvokeRequired)
                {
                    loadingLabel.Invoke(new Action(() => ShowLoading(message)));
                }
                else
                {
                    loadingLabel.Text = message;
                    loadingLabel.Visible = true;
                }
            }

            if (cardsPanel != null && cardsPanel.InvokeRequired)
            {
                cardsPanel.Invoke(new Action(() => cardsPanel.Visible = false));
            }
            else if (cardsPanel != null)
            {
                cardsPanel.Visible = false;
            }
        }

        private void HideLoading()
        {
            if (loadingLabel != null)
            {
                if (loadingLabel.InvokeRequired)
                {
                    loadingLabel.Invoke(new Action(() => HideLoading()));
                }
                else
                {
                    loadingLabel.Visible = false;
                }
            }

            if (cardsPanel != null && cardsPanel.InvokeRequired)
            {
                cardsPanel.Invoke(new Action(() => cardsPanel.Visible = true));
            }
            else if (cardsPanel != null)
            {
                cardsPanel.Visible = true;
            }
        }

        private async Task LoadModsFromGitHub()
        {
            ShowLoading("Loading Mod Listings...\nMay Lag Until Its Done Loading.");
            try
            {
                http.DefaultRequestHeaders.Add("User-Agent", "BluShopModManager/1.0");

                string url = GetModsJsonUrl();
                var response = await http.GetAsync(url);

                AddLog($"Response status: {response.StatusCode}");

                if (!response.IsSuccessStatusCode)
                {
                    AddLog($"Failed to fetch mods: HTTP {(int)response.StatusCode}");
                    HideLoading();
                    return;
                }

                string json = await response.Content.ReadAsStringAsync();
                AddLog($"JSON received (first 200 chars): {json.Substring(0, Math.Min(200, json.Length))}");

                var modsData = JsonSerializer.Deserialize<ModsList>(json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (modsData?.mods == null || modsData.mods.Count == 0)
                {
                    AddLog("No mods found in JSON");
                    HideLoading();
                    return;
                }

                allMods = modsData.mods;

                allMods = allMods.OrderBy(m =>
                {
                    int idNum;
                    return int.TryParse(m.id, out idNum) ? idNum : int.MaxValue;
                }).ToList();

                AddLog($"Loaded {allMods.Count} mods from GitHub");

                int menuCount = 0;
                int addonCount = 0;
                foreach (var mod in allMods)
                {
                    string cat = (mod.type ?? mod.category ?? "").ToLower();
                    AddLog($"ID: {mod.id} - {mod.title} - Category: '{cat}' - Downloads: {mod.download_count}");
                    if (cat == "menus") menuCount++;
                    else if (cat == "addon_mods") addonCount++;
                }
                AddLog($"Menus: {menuCount}, Addon Mods: {addonCount}");

                modsLoaded = true;

                this.Invoke((Action)(() =>
                {
                    SetStatus($"Loaded {allMods.Count} mods");
                    FilterMods();
                    HideLoading();
                }));
            }
            catch (Exception ex)
            {
                AddLog($"Error loading mods: {ex.Message}");
                HideLoading();
            }
        }

        private void FilterMods()
        {
            currentFilteredMods.Clear();
            AddLog($"Filtering: {currentCategory}");

            foreach (var mod in allMods)
            {
                string cat = (mod.type ?? mod.category ?? "").ToLower();

                if (currentCategory == "all")
                {
                    currentFilteredMods.Add(mod);
                }
                else if (currentCategory == "menus" && cat == "menus")
                {
                    currentFilteredMods.Add(mod);
                }
                else if (currentCategory == "addon_mods" && cat == "addon_mods")
                {
                    currentFilteredMods.Add(mod);
                }
                else if (currentCategory == "bepinex" && cat == "bepinex")
                {
                    currentFilteredMods.Add(mod);
                }
            }

            FilterBySearch();
            UpdatePaginationVisibility();
        }
        private void UpdatePaginationVisibility()
        {
            bool showPagination = searchedFilteredMods.Count > CARDS_PER_PAGE;
            if (btnPrevPage != null) btnPrevPage.Visible = showPagination;
            if (btnNextPage != null) btnNextPage.Visible = showPagination;
            if (lblPageInfo != null) lblPageInfo.Visible = showPagination || searchedFilteredMods.Count > 0;
        }

        private void UpdatePaginationControlsForSearch(int itemCount)
        {
            totalPages = (int)Math.Ceiling((double)itemCount / CARDS_PER_PAGE);
            if (totalPages == 0) totalPages = 1;

            if (lblPageInfo != null) lblPageInfo.Text = $"Page {currentPage} of {totalPages}";
            if (btnPrevPage != null) btnPrevPage.Enabled = currentPage > 1;
            if (btnNextPage != null) btnNextPage.Enabled = currentPage < totalPages;
        }

        private void RenderSearchedPage()
        {
            if (cardsPanel == null) return;

            if (cardsPanel.InvokeRequired)
            {
                cardsPanel.Invoke(new Action(() => RenderSearchedPage()));
                return;
            }

            cardsPanel.Controls.Clear();

            if (searchedFilteredMods.Count == 0)
            {
                cardsPanel.Controls.Add(new Label
                {
                    Text = "No mods available.",
                    ForeColor = Color.FromArgb(150, 150, 150),
                    Font = new Font("Segoe UI", 12),
                    AutoSize = true,
                    Margin = new Padding(20)
                });
                return;
            }

            int startIndex = (currentPage - 1) * CARDS_PER_PAGE;
            int endIndex = Math.Min(startIndex + CARDS_PER_PAGE, searchedFilteredMods.Count);

            for (int i = startIndex; i < endIndex; i++)
            {
                var card = CreateModCard(searchedFilteredMods[i]);
                cardsPanel.Controls.Add(card);
            }
        }

        private void RenderCurrentPage()
        {
            RenderSearchedPage();
        }

        private void UpdatePaginationControls()
        {
            UpdatePaginationControlsForSearch(searchedFilteredMods.Count);
        }

        private void GoToPreviousPage()
        {
            if (currentPage > 1) { currentPage--; RenderSearchedPage(); UpdatePaginationControls(); }
        }

        private void GoToNextPage()
        {
            if (currentPage < totalPages) { currentPage++; RenderSearchedPage(); UpdatePaginationControls(); }
        }

        private void SetStatus(string msg)
        {
            if (statusLabel != null) statusLabel.Text = msg;
        }

        private Panel CreateModCard(ModItem mod)
        {
            var card = new Panel
            {
                Width = 230,
                Height = 280,
                BackColor = Color.FromArgb(45, 45, 45),
                Margin = new Padding(8),
                Cursor = Cursors.Default
            };

            string cat = (mod.type ?? mod.category ?? "").ToLower();
            bool isAddon = cat == "addon_mods";
            bool isBepInEx = cat == "bepinex";

            string badgeText = isBepInEx ? "BEPINEX" : (isAddon ? "ADDON MOD" : "MENU");
            Color badgeColor = isBepInEx ? Color.FromArgb(245, 158, 11) : (isAddon ? Color.FromArgb(0, 200, 120) : Color.FromArgb(0, 120, 215));
            Color badgeBg = isBepInEx ? Color.FromArgb(60, 45, 10) : (isAddon ? Color.FromArgb(0, 40, 20) : Color.FromArgb(0, 20, 50));

            Color titleColor = isBepInEx ? Color.FromArgb(245, 158, 11) : (isAddon ? Color.FromArgb(0, 200, 120) : Color.FromArgb(0, 120, 215));

            Color installBtnColor = isBepInEx ? Color.FromArgb(200, 120, 0) : (isAddon ? Color.FromArgb(0, 140, 80) : Color.FromArgb(0, 120, 215));

            var imageBox = new PictureBox
            {
                Size = new Size(55, 55),
                Location = new Point(10, 10),
                BackColor = Color.FromArgb(35, 35, 35),
                SizeMode = PictureBoxSizeMode.Zoom,
                Cursor = Cursors.Hand
            };

            if (!string.IsNullOrEmpty(mod.image_url))
            {
                try
                {
                    using var webClient = new HttpClient();
                    webClient.DefaultRequestHeaders.Add("User-Agent", "BluShopModManager/1.0");
                    byte[] imageData = webClient.GetByteArrayAsync(mod.image_url).Result;
                    using var ms = new MemoryStream(imageData);
                    imageBox.Image = Image.FromStream(ms);
                }
                catch
                {
                    imageBox.BackColor = Color.FromArgb(35, 35, 35);
                }
            }
            imageBox.Click += (s, e) => ShowImagePreview(mod.image_url ?? "", mod.title ?? "Mod Image");
            card.Controls.Add(imageBox);

            string titleText = mod.title ?? "Unknown Mod";
            if (titleText.Length > 20)
            {
                titleText = titleText.Substring(0, 17) + "...";
            }

            var nameLabel = new Label
            {
                Text = titleText,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = titleColor,
                Location = new Point(70, 10),
                Size = new Size(150, 25)
            };
            card.Controls.Add(nameLabel);

            var uploaderLabel = new Label
            {
                Text = $"by @{mod.uploader_username ?? "unknown"}",
                Font = new Font("Segoe UI", 8),
                ForeColor = Color.FromArgb(120, 120, 120),
                Location = new Point(70, 35),
                AutoSize = true
            };
            card.Controls.Add(uploaderLabel);

            var downloadCountLabel = new Label
            {
                Text = mod.download_count > 0 ? $"↓ {mod.download_count} downloads" : "",
                Font = new Font("Segoe UI", 8),
                ForeColor = Color.FromArgb(100, 160, 100),
                Location = new Point(10, 75),
                AutoSize = true
            };
            card.Controls.Add(downloadCountLabel);

            if (!string.IsNullOrEmpty(mod.version))
            {
                var versionLabelCard = new Label
                {
                    Text = $"v{mod.version}",
                    Font = new Font("Segoe UI", 8),
                    ForeColor = Color.FromArgb(100, 100, 100),
                    Location = new Point(10, 95),
                    AutoSize = true
                };
                card.Controls.Add(versionLabelCard);
            }

            var categoryBadge = new Label
            {
                Text = badgeText,
                Font = new Font("Segoe UI", 7, FontStyle.Bold),
                ForeColor = badgeColor,
                BackColor = badgeBg,
                Location = new Point(10, 125),
                AutoSize = true,
                Padding = new Padding(3, 1, 3, 1)
            };
            card.Controls.Add(categoryBadge);

            if (installedDates.ContainsKey(mod.id ?? ""))
            {
                var installedLabel = new Label
                {
                    Text = $"Installed: {installedDates[mod.id ?? ""].Substring(5)}",
                    Font = new Font("Segoe UI", 7),
                    ForeColor = Color.FromArgb(100, 200, 100),
                    Location = new Point(10, 155),
                    AutoSize = true
                };
                card.Controls.Add(installedLabel);
            }

            var buttonPanelCard = new Panel
            {
                Location = new Point(10, 185),
                Width = 210,
                Height = 70,
                BackColor = Color.Transparent
            };

            var viewBtn = new Button
            {
                Text = "View",
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                BackColor = Color.FromArgb(60, 60, 60),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(70, 30),
                Location = new Point(0, 0),
                Cursor = Cursors.Hand
            };
            viewBtn.FlatAppearance.BorderSize = 0;
            viewBtn.Click += (s, e) => ShowWebsiteListing(mod.id ?? "", mod.title ?? "");
            buttonPanelCard.Controls.Add(viewBtn);

            var installBtn = new Button
            {
                Text = "Install",
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                BackColor = installBtnColor,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(100, 30),
                Location = new Point(80, 0),
                Cursor = Cursors.Hand
            };
            installBtn.FlatAppearance.BorderSize = 0;
            installBtn.Click += (s, e) => HandleInstall(mod);
            buttonPanelCard.Controls.Add(installBtn);

            card.Controls.Add(buttonPanelCard);

            card.MouseEnter += (s, e) => card.BackColor = Color.FromArgb(58, 58, 58);
            card.MouseLeave += (s, e) => card.BackColor = Color.FromArgb(45, 45, 45);

            return card;
        }
        private async void HandleInstall(ModItem mod)
        {
            string? gtFolder = DirectoryGuard.EnsureGTDirectory(this, currentSettings);
            if (gtFolder == null) return;

            string url = !string.IsNullOrEmpty(mod.download_link)
                ? mod.download_link
                : mod.file_url ?? "";

            if (string.IsNullOrEmpty(url))
            {
                AddLog($"No download URL for {mod.title}");
                MessageBox.Show("No download URL available for this mod.",
                    "No Download", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string pluginsFolder = Path.Combine(gtFolder, "BepInEx", "plugins");

            if (!Directory.Exists(pluginsFolder))
            {
                var result = MessageBox.Show(
                    "BepInEx plugins folder not found!\n\n" +
                    "You need to install BepInEx first before installing mods.\n\n" +
                    "Would you like to install BepInEx now?",
                    "BepInEx Required",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (result == DialogResult.Yes)
                {
                    AddLog("Opening BepInEx installer...");
                    ShowBepInExForm();
                }
                return;
            }

            if (currentSettings.ConfirmDownloads)
            {
                var result = MessageBox.Show(
                    $"Install \"{mod.title}\"?\n\nThis will download and install directly to:\n{pluginsFolder}",
                    "Confirm Install",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);
                if (result != DialogResult.Yes) return;
            }

            try
            {
                AddLog($"Downloading {mod.title} from: {url}");
                SetStatus($"Downloading {mod.title}...");

                using var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Add("User-Agent", "BluShopModManager/1.0");

                byte[] fileData = await httpClient.GetByteArrayAsync(url);

                string fileName = Path.GetFileName(new Uri(url).LocalPath);

                if (!fileName.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) &&
                    !fileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                {
                    fileName = $"{mod.title?.Replace(" ", "_")}.dll";
                }

                string destPath = Path.Combine(pluginsFolder, fileName);

                AddLog($"Saving to: {destPath}");
                await File.WriteAllBytesAsync(destPath, fileData);

                AddLog($"Successfully installed {mod.title}");
                SetStatus($"Installed {mod.title}");

                SaveInstalledDate(mod.id ?? "");

                MessageBox.Show(
                    $"{mod.title} installed successfully!\n\n" +
                    $"Installed to:\n{destPath}\n\n" +
                    "Launch Gorilla Tag to use the mod.",
                    "Install Complete",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                FilterMods();
            }
            catch (HttpRequestException ex)
            {
                AddLog($"Download failed: {ex.Message}");
                MessageBox.Show($"Failed to download mod:\n\n{ex.Message}\n\n" +
                    "Check your internet connection and try again.",
                    "Download Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                AddLog($"Installation failed: {ex.Message}");
                MessageBox.Show($"Failed to install mod:\n\n{ex.Message}",
                    "Install Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SetupLeftPanel()
        {
            leftPanel = new Panel
            {
                Dock = DockStyle.Left,
                Width = leftPanelWidth,
                BackColor = Color.FromArgb(28, 28, 28)
            };

            int y = 20;

            var topPanel = new Panel
            {
                Location = new Point(0, y),
                Width = leftPanel.Width,
                Height = 50,
                BackColor = Color.Transparent
            };

            var bluShopLabel = new Label
            {
                Text = "BluShop",
                Font = new Font("Segoe UI", 24, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 120, 215),
                Location = new Point(20, 0),
                AutoSize = true
            };
            topPanel.Controls.Add(bluShopLabel);

            var bellButton = new Button
            {
                Text = "🔔",
                Font = new Font("Segoe UI", 14),
                BackColor = Color.FromArgb(38, 38, 38),
                ForeColor = Color.FromArgb(200, 200, 200),
                FlatStyle = FlatStyle.Flat,
                Size = new Size(40, 40),
                Location = new Point(topPanel.Width - 100, 5),
                Cursor = Cursors.Hand
            };
            bellButton.FlatAppearance.BorderSize = 0;
            bellButton.Click += (s, e) => ShowAnnouncementForm();
            topPanel.Controls.Add(bellButton);

            leftPanel.Controls.Add(topPanel);
            y += 55;

            var modsButton = CreateStyledButton("Mods", 20, y);
            modsButton.Click += (s, e) =>
            {
                AddLog("Switching to Mods view");
                CreateModsContent();
                ShowModsContent();
            };
            leftPanel.Controls.Add(modsButton);
            y += 50;

            var bepinexButton = CreateStyledButton("BepInEx", 20, y);
            bepinexButton.Click += (s, e) =>
            {
                AddLog("Opening BepInEx installer...");
                ShowBepInExForm();
            };
            leftPanel.Controls.Add(bepinexButton);
            y += 50;

            var discordButton = CreateStyledButton("Discord", 20, y);
            discordButton.Click += (s, e) =>
            {
                AddLog("Opening Discord invite link...");
                Process.Start(new ProcessStartInfo("https://discord.gg/blugt") { UseShellExecute = true });
            };
            leftPanel.Controls.Add(discordButton);
            y += 50;

            var settingsButton = CreateStyledButton("Settings", 20, y);
            settingsButton.Click += (s, e) =>
            {
                AddLog("Opening settings...");
                ShowSettingsForm();
            };
            leftPanel.Controls.Add(settingsButton);
            y += 50;

            var reloadButton = CreateStyledButton("Reload Mods", 20, y);
            reloadButton.Click += (s, e) =>
            {
                AddLog("Manually reloading mods...");
                ReloadMods();
            };
            leftPanel.Controls.Add(reloadButton);
            y += 60;

            var logLabel = new Label
            {
                Text = "LOG",
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Color.FromArgb(120, 120, 120),
                Location = new Point(20, y),
                AutoSize = true
            };
            leftPanel.Controls.Add(logLabel);
            y += 25;

            var logButtonPanel = new Panel
            {
                Location = new Point(20, y),
                Width = leftPanel.Width - 40,
                Height = 25,
                BackColor = Color.Transparent
            };

            var clearLogButton = new Button
            {
                Text = "Clear",
                Font = new Font("Segoe UI", 8),
                BackColor = Color.FromArgb(50, 50, 50),
                ForeColor = Color.FromArgb(180, 180, 180),
                FlatStyle = FlatStyle.Flat,
                Size = new Size(50, 22),
                Location = new Point(0, 0),
                Cursor = Cursors.Hand
            };
            clearLogButton.FlatAppearance.BorderSize = 0;
            clearLogButton.Click += (s, e) => ClearLogs();

            var copyLogButton = new Button
            {
                Text = "Copy",
                Font = new Font("Segoe UI", 8),
                BackColor = Color.FromArgb(50, 50, 50),
                ForeColor = Color.FromArgb(180, 180, 180),
                FlatStyle = FlatStyle.Flat,
                Size = new Size(50, 22),
                Location = new Point(55, 0),
                Cursor = Cursors.Hand
            };
            copyLogButton.FlatAppearance.BorderSize = 0;
            copyLogButton.Click += (s, e) => CopyFullLogToClipboard();

            var fullLogButton = new Button
            {
                Text = "View Full",
                Font = new Font("Segoe UI", 8),
                BackColor = Color.FromArgb(50, 50, 50),
                ForeColor = Color.FromArgb(180, 180, 180),
                FlatStyle = FlatStyle.Flat,
                Size = new Size(70, 22),
                Location = new Point(110, 0),
                Cursor = Cursors.Hand
            };
            fullLogButton.FlatAppearance.BorderSize = 0;
            fullLogButton.Click += (s, e) => ShowFullLog();

            logButtonPanel.Controls.Add(clearLogButton);
            logButtonPanel.Controls.Add(copyLogButton);
            logButtonPanel.Controls.Add(fullLogButton);
            leftPanel.Controls.Add(logButtonPanel);
            y += 30;

            logListBox = new ListBox
            {
                Font = new Font("Consolas", 9),
                BackColor = Color.FromArgb(20, 20, 20),
                ForeColor = Color.FromArgb(180, 180, 180),
                BorderStyle = BorderStyle.FixedSingle,
                Width = leftPanel.Width - 40,
                Height = 150,
                Location = new Point(20, y),
                DrawMode = DrawMode.OwnerDrawFixed,
                IntegralHeight = false,
                ItemHeight = 26
            };

            logListBox.DrawItem += (s, e) =>
            {
                e.DrawBackground();
                if (e.Index >= 0 && logListBox.Items[e.Index] is string item)
                {
                    Color textColor = Color.FromArgb(180, 180, 180);
                    if (item.Contains("✅")) textColor = Color.FromArgb(100, 200, 100);
                    else if (item.Contains("❌")) textColor = Color.FromArgb(255, 100, 100);
                    else if (item.Contains("⚠️")) textColor = Color.FromArgb(255, 200, 100);
                    else if (item.Contains("📥")) textColor = Color.FromArgb(100, 200, 255);
                    else if (item.Contains("🔧") || item.Contains("⚙️")) textColor = Color.FromArgb(200, 150, 100);
                    else if (item.Contains("🔄")) textColor = Color.FromArgb(100, 200, 200);

                    using (var brush = new SolidBrush(textColor))
                    {
                        e.Graphics.DrawString(item, logListBox.Font, brush, e.Bounds.X, e.Bounds.Y);
                    }
                }
                e.DrawFocusRectangle();
            };

            leftPanel.Controls.Add(logListBox);
            y += 160;

            var versionPanel = new Panel
            {
                Location = new Point(20, y),
                Width = leftPanel.Width - 40,
                Height = 30,
                BackColor = Color.Transparent
            };
            versionLabel = new Label
            {
                Text = $"v{UpdateChecker.CURRENT_VERSION}",
                Font = new Font("Segoe UI", 8),
                ForeColor = Color.FromArgb(120, 120, 120),
                Location = new Point(0, 5),
                AutoSize = true,
                Cursor = Cursors.Hand
            };
            versionLabel.Click += (s, e) =>
            {
                if (updateChecker != null && updateChecker.UpdateAvailable && updateChecker.LatestVersion != null)
                {
                    ShowUpdatePopup(updateChecker.LatestVersion);
                }
            };
            versionPanel.Controls.Add(versionLabel);
            leftPanel.Controls.Add(versionPanel);
            y += 35;

            statusLabel = new Label
            {
                Text = "Ready",
                Font = new Font("Segoe UI", 8),
                ForeColor = Color.FromArgb(120, 120, 120),
                Location = new Point(20, y),
                Size = new Size(leftPanel.Width - 40, 30),
                TextAlign = ContentAlignment.MiddleLeft
            };
            leftPanel.Controls.Add(statusLabel);

            this.Controls.Add(leftPanel);

            toggleSidebarButton = new Button
            {
                Text = "◀",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                BackColor = Color.FromArgb(38, 38, 38),
                ForeColor = Color.FromArgb(200, 200, 200),
                FlatStyle = FlatStyle.Flat,
                Size = new Size(30, 30),
                Location = new Point(leftPanelWidth + 5, 10),
                Cursor = Cursors.Hand,
                Visible = true
            };
            toggleSidebarButton.FlatAppearance.BorderSize = 0;
            toggleSidebarButton.Click += (s, e) => ToggleSidebar();
            this.Controls.Add(toggleSidebarButton);
        }

        private Button CreateStyledButton(string text, int x, int y)
        {
            var btn = new Button
            {
                Text = text,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                BackColor = Color.FromArgb(38, 38, 38),
                ForeColor = Color.FromArgb(200, 200, 200),
                FlatStyle = FlatStyle.Flat,
                Width = leftPanel!.Width - 40,
                Height = 40,
                Location = new Point(x, y),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(15, 0, 0, 0),
                Visible = true,
                FlatAppearance = { BorderSize = 1, BorderColor = Color.FromArgb(0, 120, 215) }
            };

            btn.Paint += (s, e) =>
            {
                var button = s as Button;
                if (button != null)
                {
                    var rect = new Rectangle(0, 0, button.Width - 1, button.Height - 1);
                    using (var pen = new Pen(Color.FromArgb(0, 120, 215), 1))
                    {
                        e.Graphics.DrawRectangle(pen, rect);
                    }
                }
            };

            return btn;
        }

        private void SetupRightPanel()
        {
            rightPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(32, 32, 32),
                Padding = new Padding(20),
                AutoScroll = true
            };

            this.Controls.Add(rightPanel);

            imagePreviewPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(0, 0, 0, 200),
                Visible = false
            };

            var previewContainer = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(40, 40, 40)
            };

            imagePreviewTitle = new Label
            {
                Text = "",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(10, 10),
                AutoSize = true
            };
            previewContainer.Controls.Add(imagePreviewTitle);

            var closePreviewBtn = new Button
            {
                Text = "✕",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                BackColor = Color.FromArgb(60, 60, 60),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(30, 30),
                Location = new Point(previewContainer.Width - 40, 10),
                Cursor = Cursors.Hand
            };
            closePreviewBtn.FlatAppearance.BorderSize = 0;
            closePreviewBtn.Click += (s, e) => CloseImagePreview();
            previewContainer.Controls.Add(closePreviewBtn);

            imagePreviewBox = new PictureBox
            {
                Dock = DockStyle.Fill,
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.FromArgb(30, 30, 30)
            };
            previewContainer.Controls.Add(imagePreviewBox);

            imagePreviewPanel.Controls.Add(previewContainer);
            rightPanel.Controls.Add(imagePreviewPanel);
            imagePreviewPanel.BringToFront();

            CreateModsContent();
        }
    }
}