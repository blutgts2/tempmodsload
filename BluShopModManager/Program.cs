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

        private const int SW_RESTORE = 9;

        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

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
                    {
                        SwitchToExistingInstance();
                    }
                    return;
                }

                RunApplication();
            }
        }

        private static void RunApplication()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            try
            {
                string iconPath = Path.Combine(Application.StartupPath, "blumodmanager.ico");
                if (File.Exists(iconPath))
                {
                    Application.Run(new ModsForm());
                }
                else
                {
                    Application.Run(new ModsForm());
                }
            }
            catch
            {
                Application.Run(new ModsForm());
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