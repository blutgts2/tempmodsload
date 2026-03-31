using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using System.Drawing;
using System.IO;

namespace BluShopModManager
{
    static class Program
    {
        [DllImport("user32.dll")]
        private static extern IntPtr SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("shell32.dll", SetLastError = true)]
        private static extern void SetCurrentProcessExplicitAppUserModelID([MarshalAs(UnmanagedType.LPWStr)] string AppID);

        private const int SW_RESTORE = 9;

        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            SetCurrentProcessExplicitAppUserModelID("BluShopModManager");

            string mutexName = "BluShopModManager_Mutex";
            bool createdNew;

            using (Mutex mutex = new Mutex(true, mutexName, out createdNew))
            {
                if (!createdNew)
                {
                    DialogResult result = MessageBox.Show(
                        "BluShop Mod Manager is already running.\n\n" +
                        "Would you like to switch to the existing window?\n\n" +
                        "• Yes - Switch to the running instance\n" +
                        "• No - Keep the existing instance running and close this one",
                        "Already Running",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question);

                    if (result == DialogResult.Yes)
                        SwitchToExistingInstance();
                    return;
                }

                RunApplication();
            }
        }

        private static void RunApplication()
        {
            Application.Run(new AppContext());
        }

        class AppContext : ApplicationContext
        {
            private SplashForm splash;
            private ModsForm? modsForm;
            private System.Windows.Forms.Timer timer;

            public AppContext()
            {
                splash = new SplashForm();

                try
                {
                    string iconPath = Path.Combine(Application.StartupPath, "blumodmanager.ico");
                    if (File.Exists(iconPath))
                        splash.Icon = new Icon(iconPath);
                    else
                        splash.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
                }
                catch { }

                splash.Show();
                splash.Refresh();

                timer = new System.Windows.Forms.Timer { Interval = 3000 };
                timer.Tick += Timer_Tick;
                timer.Start();
            }

            private void Timer_Tick(object? sender, EventArgs e)
            {
                timer.Stop();

                splash.SetStatus("Launching...");
                splash.Refresh();

                modsForm = new ModsForm();

                try
                {
                    string iconPath = Path.Combine(Application.StartupPath, "blumodmanager.ico");
                    if (File.Exists(iconPath))
                        modsForm.Icon = new Icon(iconPath);
                    else
                        modsForm.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
                }
                catch { }

                modsForm.FormClosed += (s, args) => ExitThread();

                modsForm.Show();
                splash.Close();
            }
        }

        private static void SwitchToExistingInstance()
        {
            try
            {
                Process currentProcess = Process.GetCurrentProcess();
                Process[] processes = Process.GetProcessesByName("BluShopModManager");

                foreach (Process process in processes)
                {
                    if (process.Id != currentProcess.Id)
                    {
                        IntPtr mainWindowHandle = process.MainWindowHandle;
                        if (mainWindowHandle != IntPtr.Zero)
                        {
                            ShowWindow(mainWindowHandle, SW_RESTORE);
                            SetForegroundWindow(mainWindowHandle);
                        }
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error switching to existing instance: {ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}