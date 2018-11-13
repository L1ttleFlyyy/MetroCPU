using System;
using System.Diagnostics;
using System.Security.Principal;
using System.Windows;
using System.Windows.Input;
using OpenLibSys;

namespace MetroCPU
{
    /// <summary>
    /// TrayIcon.xaml 的交互逻辑
    /// </summary>
    public partial class TrayIcon : Window
    {
        private MainWindow mainWindow;
        private CPUinfo cpuinfo;
        private bool EPP_Enabled = false;
        private bool PSM_Enabled { get => cpuinfo.PSM.IsEnabled; set => cpuinfo.PSM.IsEnabled = value; }
        public TrayIcon()
        {
            if (!IsAdmin())
            {
                RunAsRestart();
                Environment.Exit(0);
            }
            InitializeComponent();
            Visibility = Visibility.Hidden;

            cpuinfo = new CPUinfo();
            if (!cpuinfo.LoadSucceeded)
            {
                MessageBox.Show(cpuinfo.ErrorMessage);
                cpuinfo.Dispose();
                Close();
                Environment.Exit(0);
            }
        }

        public void LaunchMainWindow()
        {
            if (mainWindow == null)
            {
                mainWindow = new MainWindow(cpuinfo);
                mainWindow.Closing += new System.ComponentModel.CancelEventHandler(
                    (s, e) => mainWindow.DisposeSensors());
                mainWindow.Closed += new EventHandler(
                    (s, e) => mainWindow = null);
                mainWindow.Show();
            }

            if (mainWindow.IsVisible)
            {
                if (mainWindow.WindowState == WindowState.Minimized)
                    mainWindow.WindowState = WindowState.Normal;
                mainWindow.Activate();
            }
            else
                mainWindow.Show();
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            mainWindow?.Close();
            taskbarIcon.Dispose();
            Application.Current.Shutdown();
        }

        private void MenuItem_Click_1(object sender, RoutedEventArgs e)
        {
            Cursor = Cursors.Wait;
            LaunchMainWindow();
            Cursor = Cursors.Arrow;
        }

        private static bool IsAdmin()
        {
            WindowsIdentity id = WindowsIdentity.GetCurrent();
            WindowsPrincipal principal = new WindowsPrincipal(id);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        private static bool RunAsRestart()
        {
            Process p = Process.GetCurrentProcess();

            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                UseShellExecute = true,
                WorkingDirectory = Environment.CurrentDirectory,
                FileName = p.MainModule.FileName,
                Verb = "runas"
            };

            try
            {
                Process.Start(startInfo);
                p.Close();
                Environment.Exit(0);
            }
            catch
            {
                return false;
            }
            return true;
        }

    }
}
