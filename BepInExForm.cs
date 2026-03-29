using System;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BluShopModManager
{
    public class BepInExForm : Form
    {
        private SettingsData currentSettings;
        private Label? statusLabel;
        private Button? installButton;
        private Button? cancelButton;
        private bool isReinstall = false;
        public BepInExForm(SettingsData settings)
        {
            currentSettings = settings;
            this.Text = "BepInEx Installation";
            this.Size = new Size(500, 280);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.FromArgb(28, 28, 28);
            SetupUI();
        }
        private void SetupUI()
        {
            Label titleLabel = new Label
            {
                Text = "Install BepInEx",
                Font = new Font("Segoe UI", 13, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 120, 215),
                Location = new Point(30, 20),
                AutoSize = true
            };

            Label infoLabel = new Label
            {
                Text = "BepInEx is required for most Gorilla Tag mods to work.\n\n" +
                       "This will install BepInEx directly into your Gorilla Tag folder.",
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.FromArgb(180, 180, 180),
                Location = new Point(30, 55),
                Size = new Size(440, 60),
                AutoSize = false
            };

            statusLabel = new Label
            {
                Text = "",
                Font = new Font("Segoe UI", 8),
                ForeColor = Color.FromArgb(120, 120, 120),
                Location = new Point(30, 130),
                Size = new Size(440, 25),
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleLeft
            };

            installButton = new Button
            {
                Text = "Install BepInEx",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(150, 38),
                Location = new Point(30, 170)
            };
            installButton.FlatAppearance.BorderSize = 0;
            installButton.Click += async (s, e) => await CheckAndInstall();

            cancelButton = new Button
            {
                Text = "Cancel",
                Font = new Font("Segoe UI", 10),
                BackColor = Color.FromArgb(55, 55, 55),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(100, 38),
                Location = new Point(195, 170)
            };
            cancelButton.FlatAppearance.BorderSize = 0;
            cancelButton.Click += (s, e) => this.Close();

            this.Controls.AddRange(new Control[] { titleLabel, infoLabel, statusLabel, installButton, cancelButton });
        }
        private async Task CheckAndInstall()
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
                    "BepInEx files or folders were found in your Gorilla Tag directory.\n\n" +
                    "• BepInEx folder: " + (hasBepInExFolder ? "✅ Found" : "❌ Not found") + "\n" +
                    "• winhttp.dll: " + (hasWinhttp ? "✅ Found" : "❌ Not found") + "\n" +
                    "• doorstop_config.ini: " + (hasDoorstop ? "✅ Found" : "❌ Not found") + "\n\n" +
                    "Would you like to reinstall BepInEx?\n" +
                    "(This will delete existing BepInEx files and install fresh)",
                    "BepInEx Already Detected",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (result != DialogResult.Yes)
                {
                    this.Close();
                    return;
                }
                isReinstall = true;
                if (statusLabel != null) statusLabel.Text = "Removing existing BepInEx files...";
                try
                {
                    if (hasBepInExFolder)
                    {
                        await Task.Run(() => Directory.Delete(bepinexFolder, true));
                        if (statusLabel != null) statusLabel.Text = "Removed BepInEx folder...";
                    }
                    if (hasWinhttp)
                    {
                        File.Delete(winhttpPath);
                        if (statusLabel != null) statusLabel.Text = "Removed winhttp.dll...";
                    }
                    if (hasDoorstop)
                    {
                        File.Delete(doorstopPath);
                        if (statusLabel != null) statusLabel.Text = "Removed doorstop_config.ini...";
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"Could not delete existing BepInEx files:\n\n{ex.Message}\n\n" +
                        "Please make sure Gorilla Tag is not running and try again.",
                        "Cannot Remove Files",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    if (installButton != null) installButton.Enabled = true;
                    if (cancelButton != null) cancelButton.Enabled = true;
                    return;
                }
            }
            await ExtractEmbeddedBepInEx(gtFolder);
        }
        private async Task ExtractEmbeddedBepInEx(string gtFolder)
        {
            if (installButton != null) installButton.Enabled = false;
            if (cancelButton != null) cancelButton.Enabled = false;

            if (statusLabel != null)
            {
                if (isReinstall)
                    statusLabel.Text = "Reinstalling BepInEx...";
                else
                    statusLabel.Text = "Installing BepInEx...";
            }
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                var zipResource = assembly.GetManifestResourceNames()
                    .FirstOrDefault(r => r.Contains("BepInExFiles.zip"));
                if (zipResource == null)
                {
                    MessageBox.Show(
                        "BepInEx installation files not found in the application.\n\n" +
                        "Please make sure BepInExFiles.zip is embedded in the application.",
                        "Installation Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);

                    if (installButton != null) installButton.Enabled = true;
                    if (cancelButton != null) cancelButton.Enabled = true;
                    return;
                }
                string tempZip = Path.Combine(Path.GetTempPath(), "BepInExFiles.zip");
                string tempExtract = Path.Combine(Path.GetTempPath(), "BepInExExtract");
                if (statusLabel != null) statusLabel.Text = "Extracting files...";
                using (var stream = assembly.GetManifestResourceStream(zipResource))
                using (var fileStream = File.Create(tempZip))
                {
                    if (stream != null)
                    {
                        await stream.CopyToAsync(fileStream);
                    }
                }
                if (Directory.Exists(tempExtract))
                {
                    Directory.Delete(tempExtract, true);
                }
                Directory.CreateDirectory(tempExtract);
                ZipFile.ExtractToDirectory(tempZip, tempExtract, true);
                var extractedItems = Directory.GetFileSystemEntries(tempExtract);
                if (extractedItems.Length == 1 && Directory.Exists(extractedItems[0]))
                {
                    string innerFolder = extractedItems[0];
                    if (statusLabel != null) statusLabel.Text = "Installing to Gorilla Tag folder...";

                    foreach (var item in Directory.GetFileSystemEntries(innerFolder))
                    {
                        string destName = Path.Combine(gtFolder, Path.GetFileName(item));
                        if (Directory.Exists(item))
                        {
                            CopyDirectory(item, destName);
                        }
                        else
                        {
                            File.Copy(item, destName, true);
                        }
                    }
                }
                else
                {
                    if (statusLabel != null) statusLabel.Text = "Installing to Gorilla Tag folder...";

                    foreach (var item in extractedItems)
                    {
                        string destName = Path.Combine(gtFolder, Path.GetFileName(item));
                        if (Directory.Exists(item))
                        {
                            CopyDirectory(item, destName);
                        }
                        else
                        {
                            File.Copy(item, destName, true);
                        }
                    }
                }
                File.Delete(tempZip);
                Directory.Delete(tempExtract, true);
                if (statusLabel != null) statusLabel.Text = "Installation complete!";
                string message = isReinstall
                    ? "✅ BepInEx has been successfully reinstalled!\n\n" +
                      "Files installed to:\n" + gtFolder + "\n\n" +
                      "Launch Gorilla Tag once to complete the setup."
                    : "✅ BepInEx installed successfully!\n\n" +
                      "Files installed to:\n" + gtFolder + "\n\n" +
                      "Launch Gorilla Tag once to complete the setup.";

                MessageBox.Show(
                    message,
                    "Installation Complete",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                this.Close();
            }
            catch (Exception ex)
            {
                if (statusLabel != null) statusLabel.Text = "Installation failed";
                MessageBox.Show(
                    $"Failed to install BepInEx:\n\n{ex.Message}",
                    "Installation Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);

                if (installButton != null) installButton.Enabled = true;
                if (cancelButton != null) cancelButton.Enabled = true;
            }
        }
        private static void CopyDirectory(string sourceDir, string destDir)
        {
            Directory.CreateDirectory(destDir);
            foreach (var file in Directory.GetFiles(sourceDir))
            {
                string destFile = Path.Combine(destDir, Path.GetFileName(file));
                File.Copy(file, destFile, true);
            }
            foreach (var dir in Directory.GetDirectories(sourceDir))
            {
                string destSubDir = Path.Combine(destDir, Path.GetFileName(dir));
                CopyDirectory(dir, destSubDir);
            }
        }
    }
}