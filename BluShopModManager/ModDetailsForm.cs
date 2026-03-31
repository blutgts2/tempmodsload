using System;
using System.Diagnostics;
using System.Drawing;
using System.Net.Http;
using System.Windows.Forms;
using System.IO;
using System.Threading.Tasks;

namespace BluShopModManager
{
    public class ModDetailsForm : Form
    {
        private ModItem mod;
        private PictureBox? imageBox;
        private Label? titleLabel;
        private Label? uploaderLabel;
        private Label? versionLabel;
        private Label? downloadCountLabel;
        private Label? viewCountLabel;
        private Label? createdDateLabel;
        private Label? updatedDateLabel;
        private Label? descriptionLabel;
        private Button? installBtn;
        private Button? viewOnSiteBtn;
        private Button? closeBtn;
        private Panel? contentPanel;
        private FlowLayoutPanel? imagesPanel;
        private LinkLabel? youtubeLink;

        private static readonly Color DarkGrey = Color.FromArgb(24, 24, 28);
        private static readonly Color MediumGrey = Color.FromArgb(32, 32, 38);
        private static readonly Color LightGrey = Color.FromArgb(45, 45, 52);
        private static readonly Color PrimaryBlue = Color.FromArgb(30, 144, 255);
        private static readonly Color GlowBlue = Color.FromArgb(0, 191, 255);

        public ModDetailsForm(ModItem modItem)
        {
            mod = modItem;
            this.Text = mod.title ?? "Mod Details";
            this.Size = new Size(950, 850);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = DarkGrey;
            try
            {
                this.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            }
            catch { }

            contentPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = MediumGrey,
                AutoScroll = true,
                Padding = new Padding(25)
            };
            this.Controls.Add(contentPanel);

            this.Load += async (s, e) => await LoadModDetails();
        }

        private async Task LoadModDetails()
        {
            if (contentPanel == null) return;

            int y = 10;

            titleLabel = new Label
            {
                Text = mod.title ?? "Unknown Mod",
                Font = new Font("Segoe UI", 22, FontStyle.Bold),
                ForeColor = PrimaryBlue,
                Location = new Point(0, y),
                AutoSize = true
            };
            contentPanel.Controls.Add(titleLabel);
            y += 55;

            var separator = new Panel
            {
                Height = 2,
                BackColor = GlowBlue,
                Width = contentPanel.Width - 50,
                Location = new Point(0, y)
            };
            contentPanel.Controls.Add(separator);
            y += 25;

            imageBox = new PictureBox
            {
                Size = new Size(200, 200),
                Location = new Point(0, y),
                BackColor = LightGrey,
                SizeMode = PictureBoxSizeMode.Zoom,
                BorderStyle = BorderStyle.FixedSingle
            };
            contentPanel.Controls.Add(imageBox);

            var infoPanel = new Panel
            {
                Location = new Point(220, y),
                Size = new Size(contentPanel.Width - 270, 200),
                BackColor = Color.Transparent
            };
            contentPanel.Controls.Add(infoPanel);

            int infoY = 5;

            uploaderLabel = new Label
            {
                Text = $"Created by: @{mod.uploader_username ?? "unknown"}",
                Font = new Font("Segoe UI", 12),
                ForeColor = Color.FromArgb(200, 200, 200),
                Location = new Point(0, infoY),
                AutoSize = true
            };
            infoPanel.Controls.Add(uploaderLabel);
            infoY += 30;

            versionLabel = new Label
            {
                Text = !string.IsNullOrEmpty(mod.version) ? $"Version: {mod.version}" : "Version: Latest",
                Font = new Font("Segoe UI", 11),
                ForeColor = Color.FromArgb(180, 180, 180),
                Location = new Point(0, infoY),
                AutoSize = true
            };
            infoPanel.Controls.Add(versionLabel);
            infoY += 30;

            downloadCountLabel = new Label
            {
                Text = mod.download_count > 0 ? $"Downloads: {mod.download_count}" : "Downloads: Not tracked",
                Font = new Font("Segoe UI", 11),
                ForeColor = Color.FromArgb(120, 200, 120),
                Location = new Point(0, infoY),
                AutoSize = true
            };
            infoPanel.Controls.Add(downloadCountLabel);
            infoY += 30;

            if (mod.view_count > 0)
            {
                viewCountLabel = new Label
                {
                    Text = $"Views: {mod.view_count}",
                    Font = new Font("Segoe UI", 11),
                    ForeColor = Color.FromArgb(150, 150, 200),
                    Location = new Point(0, infoY),
                    AutoSize = true
                };
                infoPanel.Controls.Add(viewCountLabel);
                infoY += 30;
            }

            if (!string.IsNullOrEmpty(mod.created_date))
            {
                try
                {
                    DateTime created = DateTime.Parse(mod.created_date);
                    createdDateLabel = new Label
                    {
                        Text = $"Added: {created:yyyy-MM-dd}",
                        Font = new Font("Segoe UI", 10),
                        ForeColor = Color.FromArgb(140, 140, 150),
                        Location = new Point(0, infoY),
                        AutoSize = true
                    };
                    infoPanel.Controls.Add(createdDateLabel);
                    infoY += 25;
                }
                catch { }
            }

            if (!string.IsNullOrEmpty(mod.updated_date))
            {
                try
                {
                    DateTime updated = DateTime.Parse(mod.updated_date);
                    updatedDateLabel = new Label
                    {
                        Text = $"Updated: {updated:yyyy-MM-dd}",
                        Font = new Font("Segoe UI", 10),
                        ForeColor = Color.FromArgb(140, 140, 150),
                        Location = new Point(0, infoY),
                        AutoSize = true
                    };
                    infoPanel.Controls.Add(updatedDateLabel);
                    infoY += 30;
                }
                catch { }
            }

            if (!string.IsNullOrEmpty(mod.desc_youtube_url))
            {
                youtubeLink = new LinkLabel
                {
                    Text = "▶ Watch on YouTube",
                    Font = new Font("Segoe UI", 10),
                    LinkColor = Color.FromArgb(255, 80, 80),
                    Location = new Point(0, infoY),
                    AutoSize = true,
                    Cursor = Cursors.Hand
                };
                youtubeLink.LinkClicked += (s, e) =>
                {
                    Process.Start(new ProcessStartInfo(mod.desc_youtube_url) { UseShellExecute = true });
                };
                infoPanel.Controls.Add(youtubeLink);
                infoY += 30;
            }

            infoY += 15;

            installBtn = new Button
            {
                Text = "Install Mod",
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                BackColor = PrimaryBlue,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(140, 40),
                Location = new Point(0, infoY),
                Cursor = Cursors.Hand
            };
            installBtn.FlatAppearance.BorderSize = 0;
            installBtn.Click += (s, e) =>
            {
                this.DialogResult = DialogResult.OK;
                this.Close();
            };
            infoPanel.Controls.Add(installBtn);
            infoY += 55;

            viewOnSiteBtn = new Button
            {
                Text = "View on Website",
                Font = new Font("Segoe UI", 10),
                BackColor = LightGrey,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(140, 35),
                Location = new Point(0, infoY),
                Cursor = Cursors.Hand
            };
            viewOnSiteBtn.FlatAppearance.BorderSize = 0;
            viewOnSiteBtn.Click += (s, e) =>
            {
                string url = $"https://blushop.base44.app/FileDetail?id={mod.id}";
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            };
            infoPanel.Controls.Add(viewOnSiteBtn);

            y += 220;

            var descHeader = new Label
            {
                Text = "Description",
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = GlowBlue,
                Location = new Point(0, y),
                AutoSize = true
            };
            contentPanel.Controls.Add(descHeader);
            y += 30;
            descriptionLabel = new Label
            {
                Text = mod.description ?? "No description available.",
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.FromArgb(200, 200, 200),
                Location = new Point(0, y),
                Size = new Size(contentPanel.Width - 50, 200),  
                AutoSize = false
            };
            contentPanel.Controls.Add(descriptionLabel);
            y += 210;  

            if (mod.desc_images != null && mod.desc_images.Count > 0)
            {
                var imagesHeader = new Label
                {
                    Text = "Additional Images",
                    Font = new Font("Segoe UI", 12, FontStyle.Bold),
                    ForeColor = GlowBlue,
                    Location = new Point(0, y),
                    AutoSize = true
                };
                contentPanel.Controls.Add(imagesHeader);
                y += 30;

                imagesPanel = new FlowLayoutPanel
                {
                    Location = new Point(0, y),
                    Width = contentPanel.Width - 50,
                    Height = 150,
                    BackColor = Color.Transparent,
                    AutoScroll = true,
                    WrapContents = false
                };

                foreach (var imgUrl in mod.desc_images)
                {
                    var imgBox = new PictureBox
                    {
                        Size = new Size(120, 120),
                        BackColor = LightGrey,
                        SizeMode = PictureBoxSizeMode.Zoom,
                        Margin = new Padding(5),
                        Cursor = Cursors.Hand,
                        Tag = imgUrl
                    };
                    imgBox.Click += async (s, e) =>
                    {
                        var pb = s as PictureBox;
                        if (pb != null && pb.Tag != null)
                        {
                            await ShowLargeImage(pb.Tag.ToString());
                        }
                    };
                    imagesPanel.Controls.Add(imgBox);

                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            using var client = new HttpClient();
                            client.DefaultRequestHeaders.Add("User-Agent", "BluShopModManager/1.0");
                            byte[] imageData = await client.GetByteArrayAsync(imgUrl);
                            using var ms = new MemoryStream(imageData);
                            var img = Image.FromStream(ms);
                            imgBox.Invoke(new Action(() => imgBox.Image = img));
                        }
                        catch { }
                    });
                }

                contentPanel.Controls.Add(imagesPanel);
                y += 160;
            }

            if (!string.IsNullOrEmpty(mod.image_url))
            {
                try
                {
                    using var webClient = new HttpClient();
                    webClient.DefaultRequestHeaders.Add("User-Agent", "BluShopModManager/1.0");
                    byte[] imageData = await webClient.GetByteArrayAsync(mod.image_url);
                    using var ms = new MemoryStream(imageData);
                    imageBox.Image = Image.FromStream(ms);
                }
                catch
                {
                    imageBox.BackColor = LightGrey;
                }
            }

            closeBtn = new Button
            {
                Text = "Close",
                Font = new Font("Segoe UI", 10),
                BackColor = LightGrey,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(100, 38),
                Location = new Point(contentPanel.Width - 125, contentPanel.Height - 60),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
                Cursor = Cursors.Hand
            };
            closeBtn.FlatAppearance.BorderSize = 0;
            closeBtn.Click += (s, e) => this.Close();
            contentPanel.Controls.Add(closeBtn);
        }

        private async Task ShowLargeImage(string imageUrl)
        {
            if (string.IsNullOrEmpty(imageUrl)) return;

            Form imageForm = new Form
            {
                Text = "Image Preview",
                Size = new Size(800, 800),
                StartPosition = FormStartPosition.CenterParent,
                BackColor = DarkGrey,
                WindowState = FormWindowState.Maximized
            };

            var pictureBox = new PictureBox
            {
                Dock = DockStyle.Fill,
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = DarkGrey
            };

            try
            {
                using var client = new HttpClient();
                client.DefaultRequestHeaders.Add("User-Agent", "BluShopModManager/1.0");
                byte[] imageData = await client.GetByteArrayAsync(imageUrl);
                using var ms = new MemoryStream(imageData);
                pictureBox.Image = Image.FromStream(ms);
            }
            catch
            {
                pictureBox.BackColor = LightGrey;
            }

            var closeBtn = new Button
            {
                Text = "Close",
                Font = new Font("Segoe UI", 10),
                BackColor = LightGrey,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(100, 38),
                Location = new Point(imageForm.Width - 120, imageForm.Height - 60),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
                Cursor = Cursors.Hand
            };
            closeBtn.Click += (s, e) => imageForm.Close();

            imageForm.Controls.Add(pictureBox);
            imageForm.Controls.Add(closeBtn);
            imageForm.ShowDialog(this);
        }

        public bool ShouldInstall()
        {
            return this.DialogResult == DialogResult.OK;
        }
    }
}