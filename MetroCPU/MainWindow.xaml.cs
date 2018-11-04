using System;
using System.Diagnostics;
using System.Security.Principal;
using System.Text;
using System.Windows;
using System.Windows.Documents;
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
                cpuinfo.Dispose();
                Environment.Exit(1);
            }
            else
            {
                StringBuilder freqsb = new StringBuilder();
                foreach(double ii in cpuinfo.Freq_List)
                    freqsb.AppendLine(ii.ToString());
                
                MessageBox.Show($"SST: {cpuinfo.SST_support}\n"
                    + $"Frequency: {freqsb}\n"
                    + $"Physical corecount: {cpuinfo.CoreCount}\n"
                    + $"Logical corecount: {cpuinfo.ThreadCount}");
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i <= cpuinfo.MaxCPUIDind; i++)
                {
                    sb.Append($"{i.ToString("X2")}H :"
                        + $" {cpuinfo.cpuid[i, 0].ToString("X8")}H"
                        + $" {cpuinfo.cpuid[i, 1].ToString("X8")}H"
                        + $" {cpuinfo.cpuid[i, 2].ToString("X8")}H"
                        + $" {cpuinfo.cpuid[i, 3].ToString("X8")}H\n");
                }

                sb.Append(cpuinfo.Manufacturer+"\n");
                for (int i = 0; i <= cpuinfo.MaxCPUIDexind; i++)
                {
                    sb.Append($"{(0x80000000+i).ToString("X8")}H :"
                        + $" {cpuinfo.cpuid_ex[i, 0].ToString("X8")}H"
                        + $" {cpuinfo.cpuid_ex[i, 1].ToString("X8")}H"
                        + $" {cpuinfo.cpuid_ex[i, 2].ToString("X8")}H"
                        + $" {cpuinfo.cpuid_ex[i, 3].ToString("X8")}H\n");
                }

                TextBox1.Text = sb.ToString();
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
                if (Toggle1.IsChecked != cpuinfo.SST_enabled)
                {
                    Toggle1.IsChecked = cpuinfo.SST_enabled;
                    SST_Group.IsEnabled = false;
                    MessageBox.Show("SST status won't change unless reboot");
                }
            }
        }

        private void Hyperlink_Click(object sender, RoutedEventArgs e)
        {
            Hyperlink hl = (Hyperlink)sender;
            string navigateUri = hl.NavigateUri.ToString();
            Process.Start(new ProcessStartInfo(navigateUri));
            e.Handled = true;
        }

        private void MetroWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            cpuinfo.Dispose();
        }
    }
}
