using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace BluShopModManager
{
    public static class DirectoryGuard
    {
        public static string? EnsureGTDirectory(Form parent, SettingsData settings)
        {
            string folder = settings.GetGTFolder();

            if (!string.IsNullOrEmpty(folder) &&
                File.Exists(Path.Combine(folder, "Gorilla Tag.exe")))
            {
                return folder;
            }

            return ShowSetupDialog(parent, settings);
        }

        private static string? ShowSetupDialog(Form parent, SettingsData settings)
        {
            using var dialog = new Form
            {
                Text = "Gorilla Tag Directory Required",
                Size = new Size(520, 230),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = Color.FromArgb(22, 27, 34)
            };

            var iconLbl = new Label
            {
                Text = "📁",
                Font = new Font("Segoe UI", 24),
                Location = new Point(20, 18),
                AutoSize = true,
                BackColor = Color.Transparent
            };

            var title = new Label
            {
                Text = "Gorilla Tag directory not set!",
                Font = new Font("Segoe UI", 13, FontStyle.Bold),
                ForeColor = Color.FromArgb(59, 158, 255),
                Location = new Point(68, 20),
                AutoSize = true
            };

            var info = new Label
            {
                Text = "Select your Gorilla Tag.exe to install mods.",
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.FromArgb(180, 180, 180),
                Location = new Point(68, 52),
                AutoSize = true
            };

            var pathBox = new Label
            {
                Text = "No path selected",
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.FromArgb(110, 118, 129),
                Location = new Point(20, 95),
                Size = new Size(360, 24),
                BackColor = Color.FromArgb(13, 17, 23),
                BorderStyle = BorderStyle.FixedSingle,
                TextAlign = ContentAlignment.MiddleLeft
            };

            string? chosenExePath = null;

            var browseBtn = new Button
            {
                Text = "Browse...",
                Font = new Font("Segoe UI", 9),
                BackColor = Color.FromArgb(48, 54, 61),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(110, 26),
                Location = new Point(388, 94)
            };
            browseBtn.FlatAppearance.BorderSize = 0;
            browseBtn.Click += (s, e) =>
            {
                using var dlg = new OpenFileDialog
                {
                    Title = "Select Gorilla Tag.exe",
                    Filter = "Gorilla Tag|Gorilla Tag.exe|All Executables|*.exe",
                    CheckFileExists = true
                };
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    if (!Path.GetFileName(dlg.FileName).Equals("Gorilla Tag.exe", StringComparison.OrdinalIgnoreCase))
                    {
                        MessageBox.Show("Please select Gorilla Tag.exe specifically.",
                            "Wrong File", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    chosenExePath = dlg.FileName;
                    pathBox.Text = chosenExePath;
                    pathBox.ForeColor = Color.FromArgb(52, 211, 153);
                }
            };
            var confirmBtn = new Button
            {
                Text = "✓  Confirm & Install",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                BackColor = Color.FromArgb(29, 111, 184),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(190, 38),
                Location = new Point(20, 140)
            };
            confirmBtn.FlatAppearance.BorderSize = 0;
            confirmBtn.Click += (s, e) =>
            {
                if (string.IsNullOrEmpty(chosenExePath))
                {
                    MessageBox.Show("Please select your Gorilla Tag.exe first.",
                        "No Path", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                settings.GorillaTagPath = chosenExePath;
                SettingsForm.SaveSettingsStatic(settings);
                dialog.DialogResult = DialogResult.OK;
                dialog.Close();
            };
            var cancelBtn = new Button
            {
                Text = "Cancel",
                Font = new Font("Segoe UI", 10),
                BackColor = Color.FromArgb(48, 54, 61),
                ForeColor = Color.FromArgb(180, 180, 180),
                FlatStyle = FlatStyle.Flat,
                Size = new Size(110, 38),
                Location = new Point(220, 140)
            };
            cancelBtn.FlatAppearance.BorderSize = 0;
            cancelBtn.Click += (s, e) =>
            {
                dialog.DialogResult = DialogResult.Cancel;
                dialog.Close();
            };
            dialog.Controls.AddRange(new Control[] { iconLbl, title, info, pathBox, browseBtn, confirmBtn, cancelBtn });

            return dialog.ShowDialog(parent) == DialogResult.OK ? settings.GetGTFolder() : null;
        }
    }
}