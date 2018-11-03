using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using MahApps.Metro.Controls;
using OpenLibSys;

namespace MetroCPU
{
    public partial class MainWindow : MetroWindow
    {
        public MainWindow()
        {
            if (!IsAdmin())
            {
                RunAsRestart();
                Environment.Exit(0);
            }
            InitializeComponent();
            cpuinfo = new CPUinfo();
            if (!cpuinfo.LoadSucceeded)
            {
                MessageBox.Show(cpuinfo.ErrorMessage);
                Environment.Exit(1);
            }
            else
            {

                MessageBox.Show($"SST: {cpuinfo.SST_support}\n"
                    + $"Frequency: {cpuinfo.Freq}");
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i <= cpuinfo.MaxCPUIDind; i++)
                {
                    sb.Append($"{i.ToString("X2")}H :"
                        + $" {cpuinfo.cpuid[i, 0].ToString("X8")}H"
                        + $" {cpuinfo.cpuid[i, 1].ToString("X8")}H"
                        + $" {cpuinfo.cpuid[i, 2].ToString("X8")}H"
                        + $" {cpuinfo.cpuid[i, 3].ToString("X8")}H\n");
                }
                TextBox1.Text =sb.ToString();
                //Environment.Exit(0);
                if (!cpuinfo.SST_support)
                {
                    SST_TextBlock.Text = "Unavailable";
                    SST_Dock.IsEnabled = false;
                }
                else
                {
                    Toggle1.IsChecked = cpuinfo.SST_enabled;
                }
            }
        }

        private CPUinfo cpuinfo;

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

        private void Toggle1_Click(object sender, RoutedEventArgs e)
        {
            if (Toggle1.IsChecked.HasValue)
            {
                cpuinfo.SST_enabled = Toggle1.IsChecked.Value;
                Toggle1.IsChecked = cpuinfo.SST_enabled;
            }
        }
    }
}
