using System;
using System.Diagnostics;
using System.IO;
using System.Security.Principal;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
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
                taskbarIcon.ShowBalloonTip("Failed, Click To Exit", cpuinfo.ErrorMessage, Hardcodet.Wpf.TaskbarNotification.BalloonIcon.Error);
                taskbarIcon.ResetBalloonCloseTimer();
                taskbarIcon.TrayBalloonTipClicked += (s, e) =>
                    {
                        cpuinfo.Dispose();
                        Close();
                        Environment.Exit(0);
                    };
            }
            #region EPP binding
            cpuinfo.underVoltor.AppliedSettings = cpuinfo.underVoltor.GetSettingsFromFile();
            AutoStartMenu.IsChecked = Startup;
            if (cpuinfo.SST_support)
            {
                PowerPlanMenu.IsEnabled = true;
                cpuinfo.EPP.EnableChanged += () => PowerPlanMenu.IsEnabled = cpuinfo.EPP.IsEnabled;
                cpuinfo.PSM.PowerModeChanged += (status) => SetPowerPlan(status);
                cpuinfo.PSM.PowerResume += () => cpuinfo.underVoltor.AppliedSettings = cpuinfo.underVoltor.GetSettingsFromFile();
                cpuinfo.PSM.EnableChanged += (status) =>
                {
                    if (AutoSwitchMenu.IsChecked != cpuinfo.PSM.IsEnabled)
                        AutoSwitchMenu.IsChecked = status;
                };
                AutoSwitchMenu.Click += (s, e) =>
                {
                    if (AutoSwitchMenu.IsChecked != cpuinfo.PSM.IsEnabled)
                        cpuinfo.PSM.FileSetting = AutoSwitchMenu.IsChecked;
                };

                cpuinfo.EPP.NewSettingsApplied += () =>
                {
                    if (cpuinfo.EPP.SettingIndex == 0)
                    {
                        HPMenu.IsChecked = true;
                        PSMenu.IsChecked = false;
                        if (NotificationMenu.IsChecked)
                            taskbarIcon.ShowBalloonTip("Note", "High Performance Mode", Hardcodet.Wpf.TaskbarNotification.BalloonIcon.Info);
                        taskbarIcon.IconSource = (BitmapImage)Resources["HighPerformanceIcon"];

                    }
                    else
                    {
                        HPMenu.IsChecked = false;
                        PSMenu.IsChecked = true;
                        if (NotificationMenu.IsChecked)
                            taskbarIcon.ShowBalloonTip("Note", "Power Saving Mode", Hardcodet.Wpf.TaskbarNotification.BalloonIcon.Info);
                        taskbarIcon.IconSource = (BitmapImage)Resources["PowerSavingIcon"];

                    }
                };
                HPMenu.Click += (s, e) => SetPowerPlan(System.Windows.Forms.PowerLineStatus.Online);
                PSMenu.Click += (s, e) => SetPowerPlan(System.Windows.Forms.PowerLineStatus.Offline);
                taskbarIcon.ToolTipText = $"{cpuinfo.wmi.Name}\nIntel® Speed Shift Technology Available";
                AutoSwitchMenu.IsChecked = cpuinfo.PSM.IsEnabled;
                if (AutoStartMenu.IsChecked)
                    SetPowerPlan(cpuinfo.PSM.GetPowerLineStatus());

            }
            else
            {
                taskbarIcon.ToolTipText = $"{cpuinfo.wmi.Name}\n(Intel® Speed Shift Technology NOT supported)";
                PowerPlanMenu.IsEnabled = false;
                taskbarIcon.ShowBalloonTip("Warning", "Intel SST is not supported\nMost Functions wont work", Hardcodet.Wpf.TaskbarNotification.BalloonIcon.Warning);
            }
            #endregion
        }

        public void SetPowerPlan(System.Windows.Forms.PowerLineStatus status)
        {
            if (status == System.Windows.Forms.PowerLineStatus.Offline)
            {
                cpuinfo.EPP.ApplySettings(cpuinfo.EPP.PowerSavingSettings);
                if (NotificationMenu.IsChecked)
                    taskbarIcon.ShowBalloonTip("Note", "Power Saving Mode", Hardcodet.Wpf.TaskbarNotification.BalloonIcon.Info);
                HPMenu.IsChecked = false;
                PSMenu.IsChecked = true;
                taskbarIcon.IconSource = (BitmapImage)Resources["PowerSavingIcon"];

            }
            else
            {
                cpuinfo.EPP.ApplySettings(cpuinfo.EPP.HighPerformanceSettings);
                if (NotificationMenu.IsChecked)
                    taskbarIcon.ShowBalloonTip("Note", "High Performance Mode", Hardcodet.Wpf.TaskbarNotification.BalloonIcon.Info);
                HPMenu.IsChecked = true;
                PSMenu.IsChecked = false;
                taskbarIcon.IconSource = (BitmapImage)Resources["HighPerformanceIcon"];

            }
        }
        #region startup
        private bool Startup
        {
            get => GetRegister();
            set
            {
                RegisterInStartup(value);
            }
        }

        private bool GetRegister()
        {
            RegistryKey registryKey = Registry.CurrentUser.OpenSubKey
                    ("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", false);

            foreach (string name in registryKey.GetValueNames())
            {
                if (string.Equals(name, "MetroCPU"))
                {
                    return true;
                }
            }
            return false;
        }

        private void RegisterInStartup(bool isChecked)
        {
            RegistryKey registryKey = Registry.CurrentUser.OpenSubKey
                    ("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
            if (isChecked)
            {
                registryKey.SetValue("MetroCPU", System.Reflection.Assembly.GetEntryAssembly().Location);
            }
            else
            {
                registryKey.DeleteValue("MetroCPU");
            }
        }
        private void AutoStartMenu_Click(object sender, RoutedEventArgs e)
        {
            if (AutoStartMenu.IsChecked != Startup)
            {
                Startup = AutoStartMenu.IsChecked;
                AutoStartMenu.IsChecked = Startup;
            }
        }
        #endregion

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
