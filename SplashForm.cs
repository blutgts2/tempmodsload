using System;
using System.Drawing;
using System.Windows.Forms;

namespace BluShopModManager
{
    public class SplashForm : Form
    {
        private Label? statusLabel;
        private ProgressBar? progressBar;
        private Button? minimizeBtn;
        private Button? closeBtn;
        private Point dragOffset;
        public SplashForm()
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Size = new Size(500, 280);
            this.BackColor = Color.FromArgb(28, 28, 28);
            this.TopMost = true;
            this.MouseDown += (s, e) => { dragOffset = e.Location; };
            this.MouseMove += (s, e) =>
            {
                if (e.Button == MouseButtons.Left)
                    this.Location = new Point(
                        this.Left + e.X - dragOffset.X,
                        this.Top + e.Y - dragOffset.Y);
            };
            minimizeBtn = new Button
            {
                Text = "−",
                Font = new Font("Segoe UI", 13),
                ForeColor = Color.FromArgb(180, 180, 180),
                BackColor = Color.Transparent,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(36, 30),
                Location = new Point(this.Width - 76, 2),
                TabStop = false,
                Cursor = Cursors.Hand
            };
            minimizeBtn.FlatAppearance.BorderSize = 0;
            minimizeBtn.FlatAppearance.MouseOverBackColor = Color.FromArgb(60, 60, 60);
            minimizeBtn.Click += (s, e) => this.WindowState = FormWindowState.Minimized;
            closeBtn = new Button
            {
                Text = "✕",
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.FromArgb(180, 180, 180),
                BackColor = Color.Transparent,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(36, 30),
                Location = new Point(this.Width - 38, 2),
                TabStop = false,
                Cursor = Cursors.Hand
            };
            closeBtn.FlatAppearance.BorderSize = 0;
            closeBtn.FlatAppearance.MouseOverBackColor = Color.FromArgb(200, 50, 50);
            closeBtn.Click += (s, e) => System.Environment.Exit(0);
            var topBar = new Panel
            {
                Height = 3,
                Width = this.Width,
                BackColor = Color.FromArgb(0, 120, 215),
                Location = new Point(0, 0),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            var bluLabel = new Label
            {
                Text = "Blu",
                Font = new Font("Segoe UI", 28, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 120, 215),
                AutoSize = true,
                BackColor = Color.Transparent,
                Location = new Point(40, 60)
            };

            var shopLabel = new Label
            {
                Text = "Shop",
                Font = new Font("Segoe UI", 28, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                BackColor = Color.Transparent,
                Location = new Point(40 + TextRenderer.MeasureText("Blu", new Font("Segoe UI", 28, FontStyle.Bold)).Width, 60)
            };

            var subtitleLabel = new Label
            {
                Text = "Mod Manager",
                Font = new Font("Segoe UI", 11),
                ForeColor = Color.FromArgb(120, 120, 120),
                AutoSize = true,
                BackColor = Color.Transparent,
                Location = new Point(40, 110)
            };

            statusLabel = new Label
            {
                Text = "Loading mods...",
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.FromArgb(140, 140, 140),
                AutoSize = true,
                BackColor = Color.Transparent,
                Location = new Point(40, 195)
            };

            progressBar = new ProgressBar
            {
                Style = ProgressBarStyle.Marquee,
                MarqueeAnimationSpeed = 30,
                Size = new Size(420, 6),
                Location = new Point(40, 220),
                BackColor = Color.FromArgb(50, 50, 50)
            };
            this.Controls.Add(topBar);
            this.Controls.Add(bluLabel);
            this.Controls.Add(shopLabel);
            this.Controls.Add(subtitleLabel);
            this.Controls.Add(statusLabel);
            this.Controls.Add(progressBar);
            this.Controls.Add(minimizeBtn);
            this.Controls.Add(closeBtn);
            minimizeBtn.BringToFront();
            closeBtn.BringToFront();
        }
        public void SetStatus(string msg)
        {
            if (statusLabel != null)
                statusLabel.Text = msg;
        }
    }
}