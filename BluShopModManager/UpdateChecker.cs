using System;
using System.Drawing;
using System.Net.Http;
using System.Windows.Forms;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace BluShopModManager
{
    public class UpdateChecker
    {
        private const string GITHUB_RELEASE_URL = "https://github.com/blutgts2/tempmodsload/releases/latest";
        private const string DOWNLOAD_URL = "https://github.com/blutgts2/tempmodsload/releases/download/tempmod/BluShopModManager.exe";
        public const string CURRENT_VERSION = "1.0.2";
        public bool UpdateAvailable { get; private set; } = false;
        public string? LatestVersion { get; private set; }

        private static readonly HttpClient httpClient = new HttpClient();
        private Action<string>? logCallback;
        private Action<bool, string?>? updateCallback;

        public UpdateChecker(Action<string>? logCallback = null, Action<bool, string?>? updateCallback = null)
        {
            this.logCallback = logCallback;
            this.updateCallback = updateCallback;
        }

        private string GetVersionUrl()
        {
            return $"https://raw.githubusercontent.com/blutgts2/tempmodsload/refs/heads/main/bmmversion?t={DateTime.Now.Ticks}";
        }

        private void AddLog(string message)
        {
            logCallback?.Invoke(message);
            Debug.WriteLine($"[UpdateChecker] {message}");
        }

        public async void CheckForUpdates(Form parentForm, bool showPopup = true)
        {
            AddLog($"Checking for updates... Current version: v{CURRENT_VERSION}");

            try
            {
                string versionUrl = GetVersionUrl();
                AddLog($"Fetching latest version from: {versionUrl}");

                httpClient.DefaultRequestHeaders.Add("User-Agent", "BluShopModManager/1.0");
                httpClient.DefaultRequestHeaders.Add("Cache-Control", "no-cache, no-store, must-revalidate");
                httpClient.DefaultRequestHeaders.Add("Pragma", "no-cache");

                string latestVersion = await httpClient.GetStringAsync(versionUrl);
                latestVersion = latestVersion.Trim();

                AddLog($"Latest version from GitHub: v{latestVersion}");
                LatestVersion = latestVersion;

                if (IsNewerVersion(latestVersion, CURRENT_VERSION))
                {
                    UpdateAvailable = true;
                    AddLog($"✅ New version available! v{CURRENT_VERSION} -> v{latestVersion}");
                    updateCallback?.Invoke(true, latestVersion);

                    if (showPopup)
                    {
                        ShowUpdateDialog(parentForm, latestVersion);
                    }
                }
                else
                {
                    UpdateAvailable = false;
                    AddLog($"✅ You are running the latest version (v{CURRENT_VERSION})");
                    updateCallback?.Invoke(false, null);
                }
            }
            catch (HttpRequestException ex)
            {
                AddLog($"❌ Failed to check for updates: Network error - {ex.Message}");
                updateCallback?.Invoke(false, null);
            }
            catch (Exception ex)
            {
                AddLog($"❌ Failed to check for updates: {ex.Message}");
                updateCallback?.Invoke(false, null);
            }
        }

        private bool IsNewerVersion(string latest, string current)
        {
            try
            {
                var latestVersion = new Version(latest);
                var currentVersion = new Version(current);
                return latestVersion > currentVersion;
            }
            catch
            {
                return false;
            }
        }

        private void OpenDownloadInBrowser()
        {
            try
            {
                AddLog($"Opening download in browser: {DOWNLOAD_URL}");
                Process.Start(new ProcessStartInfo(DOWNLOAD_URL) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                AddLog($"❌ Failed to open browser: {ex.Message}");
                MessageBox.Show($"Failed to open download link:\n\n{ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ShowUpdateDialog(Form parentForm, string latestVersion)
        {
            if (parentForm.InvokeRequired)
            {
                parentForm.Invoke(new Action(() => ShowUpdateDialog(parentForm, latestVersion)));
                return;
            }

            AddLog($"Showing update dialog for version v{latestVersion}");

            Form dialog = new Form
            {
                Text = "Update Available",
                Size = new Size(550, 280),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = Color.FromArgb(28, 28, 28)
            };

            Label titleLabel = new Label
            {
                Text = "New Version Available!",
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 120, 215),
                Location = new Point(20, 20),
                AutoSize = true
            };

            Label versionLabel = new Label
            {
                Text = $"Current: v{CURRENT_VERSION}   →   Latest: v{latestVersion}",
                Font = new Font("Segoe UI", 11),
                ForeColor = Color.White,
                Location = new Point(20, 68),
                AutoSize = true
            };

            Label infoLabel = new Label
            {
                Text = "The download will open in your browser.",
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.FromArgb(180, 180, 180),
                Location = new Point(20, 110),
                AutoSize = true
            };

            Button downloadBtn = new Button
            {
                Text = "Download Update",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(150, 38),
                Location = new Point(20, 160),
                Cursor = Cursors.Hand
            };
            downloadBtn.FlatAppearance.BorderSize = 0;
            downloadBtn.Click += (s, e) =>
            {
                OpenDownloadInBrowser();
                dialog.Close();
            };

            Button laterBtn = new Button
            {
                Text = "Later",
                Font = new Font("Segoe UI", 10),
                BackColor = Color.FromArgb(80, 80, 80),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(100, 38),
                Location = new Point(180, 160),
                Cursor = Cursors.Hand
            };
            laterBtn.FlatAppearance.BorderSize = 0;
            laterBtn.Click += (s, e) =>
            {
                AddLog("User postponed update");
                dialog.Close();
            };

            dialog.Controls.AddRange(new Control[] { titleLabel, versionLabel, infoLabel, downloadBtn, laterBtn });
            dialog.ShowDialog(parentForm);
        }
    }
}