using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BluShopModManager
{
    public static class ModInstaller
    {
        private static readonly HttpClient Http = new HttpClient();
        public static async Task InstallMod(
            Form parent,
            SettingsData settings,
            string modTitle,
            string downloadUrl,
            string? relativeDestFolder = null
        )
        {
            var gtPath = DirectoryGuard.EnsureGTDirectory(parent, settings);
            if (gtPath == null)
            {
                return;
            }
            try
            {
                parent.Invoke(() => SetStatus(parent, $"Downloading {modTitle}..."));

                var bytes = await Http.GetByteArrayAsync(downloadUrl);

                string destFolder = string.IsNullOrEmpty(relativeDestFolder)
                    ? gtPath
                    : Path.Combine(gtPath, relativeDestFolder);

                Directory.CreateDirectory(destFolder);
                if (downloadUrl.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                {
                    var tempZip = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.zip");
                    File.WriteAllBytes(tempZip, bytes);
                    System.IO.Compression.ZipFile.ExtractToDirectory(tempZip, destFolder, overwriteFiles: true);
                    File.Delete(tempZip);
                }
                else
                {
                    var fileName = Path.GetFileName(new Uri(downloadUrl).LocalPath);
                    File.WriteAllBytes(Path.Combine(destFolder, fileName), bytes);
                }

                parent.Invoke(() =>
                {
                    MessageBox.Show(
                        $"✅ {modTitle} installed successfully!\n\nLocation: {destFolder}",
                        "Install Complete",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information
                    );
                });
            }
            catch (Exception ex)
            {
                parent.Invoke(() =>
                {
                    MessageBox.Show(
                        $"Failed to install {modTitle}:\n{ex.Message}",
                        "Install Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error
                    );
                });
            }
        }
        public static async Task InstallBepInEx(Form parent, SettingsData settings, string bepinexZipUrl)
        {
            var gtPath = DirectoryGuard.EnsureGTDirectory(parent, settings);
            if (gtPath == null) return;

            try
            {
                parent.Invoke(() => SetStatus(parent, "Downloading BepInEx..."));

                var bytes = await Http.GetByteArrayAsync(bepinexZipUrl);
                var tempZip = Path.Combine(Path.GetTempPath(), $"bepinex_{Guid.NewGuid()}.zip");
                File.WriteAllBytes(tempZip, bytes);

                System.IO.Compression.ZipFile.ExtractToDirectory(tempZip, gtPath, overwriteFiles: true);
                File.Delete(tempZip);

                parent.Invoke(() =>
                {
                    MessageBox.Show(
                        $"✅ BepInEx installed into:\n{gtPath}\n\nLaunch Gorilla Tag once to complete setup, then install mods!",
                        "BepInEx Installed",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information
                    );
                });
            }
            catch (Exception ex)
            {
                parent.Invoke(() =>
                {
                    MessageBox.Show(
                        $"Failed to install BepInEx:\n{ex.Message}",
                        "Install Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error
                    );
                });
            }
        }
        private static void SetStatus(Form form, string text)
        {
            foreach (Control c in form.Controls)
            {
                if (c is Label lbl && lbl.Name == "lblStatus")
                {
                    lbl.Text = text;
                    break;
                }
            }
        }
    }
}