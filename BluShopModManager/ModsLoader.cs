using System;
using System.Collections.Generic;
using System.Drawing;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BluShopModManager
{
    public class ModsLoader : UserControl
    {
        private Label loadingLabel;
        private ProgressBar progressBar;
        private Label statusLabel;
        private FlowLayoutPanel cardsPanel;
        private Action<List<ModItem>> onModsLoaded;
        private Action<string> onLogMessage;
        private bool isLoading = false;

        public ModsLoader(Action<List<ModItem>> onModsLoadedCallback, Action<string> onLogCallback)
        {
            onModsLoaded = onModsLoadedCallback;
            onLogMessage = onLogCallback;

            this.BackColor = Color.FromArgb(32, 32, 38);
            this.Dock = DockStyle.Fill;

            InitializeUI();
        }

        private void InitializeUI()
        {
            loadingLabel = new Label
            {
                Text = "Loading Mod Listings...",
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 144, 255),
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Top,
                Height = 40,
                Padding = new Padding(0, 10, 0, 0)
            };

            progressBar = new ProgressBar
            {
                Style = ProgressBarStyle.Marquee,
                MarqueeAnimationSpeed = 30,
                Height = 5,
                Dock = DockStyle.Top,
                ForeColor = Color.FromArgb(30, 144, 255),
                Visible = true
            };

            statusLabel = new Label
            {
                Text = "Initializing...",
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.FromArgb(150, 150, 150),
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Top,
                Height = 30,
                Padding = new Padding(0, 5, 0, 0)
            };

            cardsPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(32, 32, 38),
                AutoScroll = true,
                WrapContents = true,
                Padding = new Padding(10),
                Visible = false
            };

            this.Controls.Add(cardsPanel);
            this.Controls.Add(progressBar);
            this.Controls.Add(statusLabel);
            this.Controls.Add(loadingLabel);
        }

        public async Task StartLoading(string jsonUrl, HttpClient httpClient)
        {
            if (isLoading) return;
            isLoading = true;

            await Task.Run(async () =>
            {
                try
                {
                    UpdateStatusOnUI("Fetching mod listings...");

                    using var client = new HttpClient();
                    client.DefaultRequestHeaders.Add("User-Agent", "BluShopModManager/1.0");

                    UpdateStatusOnUI("Connecting to server...");

                    var response = await client.GetAsync(jsonUrl);

                    if (!response.IsSuccessStatusCode)
                    {
                        UpdateStatusOnUI($"Error: HTTP {(int)response.StatusCode}");
                        onLogMessage?.Invoke($"Failed to fetch mods: HTTP {(int)response.StatusCode}");
                        isLoading = false;
                        return;
                    }

                    UpdateStatusOnUI("Downloading mod data...");

                    using var stream = await response.Content.ReadAsStreamAsync();

                    UpdateStatusOnUI("Processing mod listings...");

                    var modsData = await JsonSerializer.DeserializeAsync<ModsList>(stream,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (modsData?.mods == null || modsData.mods.Count == 0)
                    {
                        UpdateStatusOnUI("No mods found");
                        onLogMessage?.Invoke("No mods found in JSON");
                        isLoading = false;
                        return;
                    }

                    UpdateStatusOnUI("Organizing mods...");

                    var sortedMods = modsData.mods.OrderBy(m =>
                    {
                        int idNum;
                        return int.TryParse(m.id, out idNum) ? idNum : int.MaxValue;
                    }).ToList();

                    int menuCount = sortedMods.Count(m => (m.type ?? m.category ?? "").ToLower() == "menus");
                    int addonCount = sortedMods.Count(m => (m.type ?? m.category ?? "").ToLower() == "addon_mods");
                    int bepinexCount = sortedMods.Count(m => (m.type ?? m.category ?? "").ToLower() == "bepinex");

                    onLogMessage?.Invoke($"Loaded {sortedMods.Count} mods from GitHub");
                    onLogMessage?.Invoke($"Menus: {menuCount}, Addon Mods: {addonCount}, BepInEx: {bepinexCount}");

                    UpdateStatusOnUI($"Loaded {sortedMods.Count} mods!");

                    this.Invoke(new Action(() =>
                    {
                        cardsPanel.Visible = true;
                        loadingLabel.Visible = false;
                        progressBar.Visible = false;
                        statusLabel.Visible = false;
                    }));

                    onModsLoaded?.Invoke(sortedMods);
                    isLoading = false;
                }
                catch (Exception ex)
                {
                    UpdateStatusOnUI($"Error: {ex.Message}");
                    onLogMessage?.Invoke($"Error loading mods: {ex.Message}");
                    isLoading = false;
                }
            });
        }

        private void UpdateStatusOnUI(string message)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => UpdateStatusOnUI(message)));
                return;
            }

            statusLabel.Text = message;
            loadingLabel.Text = "Loading Mod Listings...";
        }

        public void UpdateCards(List<ModItem> mods, Func<ModItem, Panel> createCardMethod)
        {
            if (cardsPanel.InvokeRequired)
            {
                cardsPanel.Invoke(new Action(() => UpdateCards(mods, createCardMethod)));
                return;
            }

            cardsPanel.Controls.Clear();

            if (mods == null || mods.Count == 0)
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

            foreach (var mod in mods)
            {
                var card = createCardMethod(mod);
                cardsPanel.Controls.Add(card);
            }
        }

        public void FilterMods(List<ModItem> mods, Func<ModItem, Panel> createCardMethod)
        {
            UpdateCards(mods, createCardMethod);
        }

        public void ShowLoading()
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(ShowLoading));
                return;
            }

            cardsPanel.Visible = false;
            loadingLabel.Visible = true;
            progressBar.Visible = true;
            statusLabel.Visible = true;
        }

        public void ShowCards()
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(ShowCards));
                return;
            }

            cardsPanel.Visible = true;
            loadingLabel.Visible = false;
            progressBar.Visible = false;
            statusLabel.Visible = false;
        }

        public FlowLayoutPanel GetCardsPanel()
        {
            return cardsPanel;
        }
    }
}