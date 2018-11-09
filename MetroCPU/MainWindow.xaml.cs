using System;
using System.Diagnostics;
using System.Security.Principal;
using System.Text;
using System.Windows;
using System.Windows.Documents;
using MahApps.Metro.Controls;
using OpenLibSys;
using InteractiveDataDisplay.WPF;
using System.Windows.Media;
using System.Collections.Generic;

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
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i <= cpuinfo.MaxCPUIDind; i++)
                {
                    sb.Append($"{i.ToString("X2")}H :"
                        + $" {cpuinfo.CPUID[i, 0].ToString("X8")}H"
                        + $" {cpuinfo.CPUID[i, 1].ToString("X8")}H"
                        + $" {cpuinfo.CPUID[i, 2].ToString("X8")}H"
                        + $" {cpuinfo.CPUID[i, 3].ToString("X8")}H\n");
                }
                sb.Append(cpuinfo.Manufacturer + "\n");
                for (int i = 0; i <= cpuinfo.MaxCPUIDexind; i++)
                {
                    sb.Append($"{(0x80000000 + i).ToString("X8")}H :"
                        + $" {cpuinfo.CPUID_ex[i, 0].ToString("X8")}H"
                        + $" {cpuinfo.CPUID_ex[i, 1].ToString("X8")}H"
                        + $" {cpuinfo.CPUID_ex[i, 2].ToString("X8")}H"
                        + $" {cpuinfo.CPUID_ex[i, 3].ToString("X8")}H\n");
                }

                TextBox1.Text = sb.ToString();
                TextBox2.Text = string.Empty;
                if (!cpuinfo.SST_support)
                {
                    SST_TextBlock.Text = "Unavailable";
                    SST_Dock.IsEnabled = false;
                }
                else
                {
                    Toggle1.IsChecked = cpuinfo.SST_enabled;
                }

                UITimer = new System.Windows.Threading.DispatcherTimer() { Interval = TimeSpan.FromMilliseconds(20) };
                if (cpuinfo.Manufacturer == "GenuineIntel")
                {
                    Sensor2LineGraph s2l1 = new Sensor2LineGraph(cpuinfo.CoreVoltageSensor, CoreVoltagePlotter, "G4", -0.1, 1.9, new TransitionText(VoltaCurrent), VoltaMax, VoltaMin);
                    Sensor2LineGraph s2l2 = new Sensor2LineGraph(cpuinfo.PackagePowerSensor, PackagePowerPlotter, "G3", -1, cpuinfo.PPM.TDP+1, new TransitionText(PowerCurrent), PowerMax, PowerMin);
                    Sensor2LineGraph s2l3 = new Sensor2LineGraph(cpuinfo.PackageTemperatureSensor, PackageTemperaturePlotter, "F2", -2, 101, new TransitionText(TempCurrent), TempMax, TempMin);
                    UITimer.Tick += new EventHandler((sender,e)=> {
                        s2l1.RefreshUI();
                        s2l2.RefreshUI();
                        s2l3.RefreshUI();
                    });
                }
                int tmp = 0;
                List<Sensor2LineGraph> S2LGs = new List<Sensor2LineGraph>(cpuinfo.CoreCount);
                foreach (Sensor s in cpuinfo.frequencyRatioSensors)
                {
                    var lg = new LineGraph();
                    lines.Children.Add(lg);
                    lg.Stroke = new SolidColorBrush(Color.FromArgb(255, 0, 128, (byte)(255 * Math.Pow(2, -tmp))));
                    lg.Description = "0 Ghz";
                    var s2lg = new Sensor2LineGraph(s, lg, "F2", -0.2, cpuinfo.MaxClockSpeed / 1000.0 + 2);
                    UITimer.Tick += new EventHandler((sender,e)=>s2lg.RefreshUI());
                    S2LGs.Add(s2lg);
                    tmp++;
                }
                UITimer.Start();
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

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            StringBuilder sb = new StringBuilder();
            int i = 0;
            foreach (Sensor s in cpuinfo.frequencyRatioSensors)
            {
                sb.AppendLine($"Core{i}: {s.CurrentValue.Data * cpuinfo.MaxClockSpeed} Mhz");
                i++;
            }
            if (cpuinfo.Manufacturer == "GenuineIntel")
            {
                sb.AppendLine($"PackageTemp: {cpuinfo.PackageTemperatureSensor.CurrentValue.Data} °C");
                sb.AppendLine($"PlatformPower: {cpuinfo.PackagePowerSensor.CurrentValue.Data} W");
                sb.AppendLine($"Core Voltage: {cpuinfo.CoreVoltageSensor.CurrentValue.Data * 1000} mV");
            }
            TextBox2.Text = sb.ToString();
        }
    }
}
