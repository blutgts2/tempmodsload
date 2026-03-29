using System;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using System.Text.Json;

namespace BluShopModManager
{
    public class SettingsForm : Form
    {
        private SettingsData? settings;
        private static readonly string appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "BluModManager");
        private static readonly string settingsPath = Path.Combine(appDataPath, "settings.json");

        private CheckBox? chkAutoUpdates;
        private CheckBox? chkConfirmDownloads;
        private TextBox? txtGorillaTagPath;
        private Button? btnBrowseExe;
        private Button? btnSave;
        private Button? btnCancel;

        public SettingsForm()
        {
            Directory.CreateDirectory(appDataPath);

            this.Text = "Settings";
            this.Size = new Size(500, 360);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.FromArgb(45, 45, 45);

            settings = GetSettings();
            SetupUI();
        }

        public static SettingsData GetSettings()
        {
            try
            {
                if (File.Exists(settingsPath))
                {
                    string json = File.ReadAllText(settingsPath);
                    return JsonSerializer.Deserialize<SettingsData>(json) ?? new SettingsData();
                }
            }
            catch { }
            return new SettingsData();
        }

        public static void SaveSettingsStatic(SettingsData data)
        {
            try
            {
                Directory.CreateDirectory(appDataPath);
                string json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(settingsPath, json);
            }
            catch { }
        }

        private void SaveSettings()
        {
            if (settings != null)
                SaveSettingsStatic(settings);
        }

        private void SetupUI()
        {
            int y = 20;

            var titleLabel = new Label
            {
                Text = "Settings",
                Font = new Font("Segoe UI", 18, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 120, 215),
                Location = new Point(20, y),
                AutoSize = true
            };
            this.Controls.Add(titleLabel);
            y += 45;

            var sep = new Panel
            {
                Height = 2,
                BackColor = Color.FromArgb(0, 120, 215),
                Width = this.Width - 40,
                Location = new Point(20, y)
            };
            this.Controls.Add(sep);
            y += 20;

            chkAutoUpdates = new CheckBox
            {
                Text = "Automatically check for updates on startup",
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.White,
                Location = new Point(30, y),
                AutoSize = true,
                Checked = settings?.AutoCheckUpdates ?? true
            };
            this.Controls.Add(chkAutoUpdates);
            y += 35;

            chkConfirmDownloads = new CheckBox
            {
                Text = "Show confirmation before downloading mods",
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.White,
                Location = new Point(30, y),
                AutoSize = true,
                Checked = settings?.ConfirmDownloads ?? true
            };
            this.Controls.Add(chkConfirmDownloads);
            y += 45;

            var pathLabel = new Label
            {
                Text = "Gorilla Tag Executable (Gorilla Tag.exe):",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.FromArgb(200, 200, 200),
                Location = new Point(30, y),
                AutoSize = true
            };
            this.Controls.Add(pathLabel);
            y += 25;

            txtGorillaTagPath = new TextBox
            {
                Font = new Font("Segoe UI", 9),
                BackColor = Color.FromArgb(60, 60, 60),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Width = 300,
                Location = new Point(30, y),
                Text = settings?.GorillaTagPath ?? "",
                ReadOnly = true
            };
            this.Controls.Add(txtGorillaTagPath);

            btnBrowseExe = new Button
            {
                Text = "Browse",
                Font = new Font("Segoe UI", 10),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(80, 30),
                Location = new Point(340, y)
            };
            btnBrowseExe.FlatAppearance.BorderSize = 0;
            btnBrowseExe.Click += (s, e) => BrowseForExe();
            this.Controls.Add(btnBrowseExe);
            y += 55;

            btnSave = new Button
            {
                Text = "Save",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(100, 40),
                Location = new Point(150, y)
            };
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.Click += (s, e) => SaveAndClose();
            this.Controls.Add(btnSave);

            btnCancel = new Button
            {
                Text = "Cancel",
                Font = new Font("Segoe UI", 10),
                BackColor = Color.FromArgb(80, 80, 80),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(100, 40),
                Location = new Point(270, y)
            };
            btnCancel.FlatAppearance.BorderSize = 0;
            btnCancel.Click += (s, e) => this.Close();
            this.Controls.Add(btnCancel);
        }

        private void BrowseForExe()
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
                    MessageBox.Show("Please select the Gorilla Tag.exe file.",
                        "Invalid File", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (txtGorillaTagPath != null)
                    txtGorillaTagPath.Text = selected;
            }
        }

        private void SaveAndClose()
        {
            if (settings == null) return;

            settings.AutoCheckUpdates = chkAutoUpdates?.Checked ?? true;
            settings.ConfirmDownloads = chkConfirmDownloads?.Checked ?? true;
            settings.GorillaTagPath = txtGorillaTagPath?.Text ?? "";

            SaveSettings();

            MessageBox.Show("Settings saved!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            this.Close();
        }
    }
}