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
using System.Windows.Media.Imaging;
using System.Windows.Controls;
using System.Windows.Input;

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
                
                if (!cpuinfo.SST_support)
                {
                    SST_TextBlock.Text = "Unavailable";
                    SST_Group.IsEnabled = false;
                    EPP_Group.IsEnabled = false;
                }
                else
                {
                    Toggle1.IsChecked = cpuinfo.SST_enabled;
                }

                UITimer = new System.Windows.Threading.DispatcherTimer() { Interval = TimeSpan.FromMilliseconds(800) };
                if (cpuinfo.Manufacturer == "GenuineIntel")
                {
                    Sensor2LineGraph s2l1 = new Sensor2LineGraph(cpuinfo.CoreVoltageSensor, CoreVoltagePlotter, "G4", -0.1, 1.9, new TransitionText(VoltaCurrent), VoltaMax, VoltaMin);
                    Sensor2LineGraph s2l2 = new Sensor2LineGraph(cpuinfo.PackagePowerSensor, PackagePowerPlotter, "G3", -1, cpuinfo.PPM.TDP+1, new TransitionText(PowerCurrent), PowerMax, PowerMin);
                    Sensor2LineGraph s2l3 = new Sensor2LineGraph(cpuinfo.PackageTemperatureSensor, PackageTemperaturePlotter, "G2", -2, 101, new TransitionText(TempCurrent), TempMax, TempMin);
                    UITimer.Tick += new EventHandler((sender,e)=> {
                        s2l1.RefreshUI();
                        s2l2.RefreshUI();
                        s2l3.RefreshUI();
                    });
                }
                UV2S = new UnderVoltor2Sliders(cpuinfo.underVoltor, Slider0, Slider1, Slider2, Slider3, Slider4, Slider5,ApplyButton,SaveButton,ResetButton);

                int tmp = 0;
                List<Sensor2LineGraph> S2LGs = new List<Sensor2LineGraph>(cpuinfo.CoreCount);
                foreach (Sensor s in cpuinfo.frequencyRatioSensors)
                {
                    var lg = new LineGraph();
                    lines.Children.Add(lg);
                    lg.Stroke = new SolidColorBrush(Color.FromArgb(255, 0, 128, (byte)(255 * Math.Pow(2, -tmp))));
                    lg.Description = "0 Ghz";
                    Sensor2LineGraph s2lg;
                    if (tmp == 0)
                    {
                        s2lg = new Sensor2LineGraph(s, lg, "F2", -0.2, cpuinfo.MaxClockSpeed / 1000.0 + 2 , new TransitionText(CurrentFrequencyTransition),MaxFrequencyTextBox,MinFrequencyTextBox);
                    }
                    else
                    {
                        s2lg = new Sensor2LineGraph(s, lg, "F2", -0.2, cpuinfo.MaxClockSpeed / 1000.0 + 2);
                    }
                    UITimer.Tick += new EventHandler((sender, e) => s2lg.RefreshUI());
                    S2LGs.Add(s2lg);
                    tmp++;
                }
                CPUNameTextBox.Text = cpuinfo.wmi.Name;
                CoresTextBox.Text = cpuinfo.CoreCount.ToString();
                ThreadsTextBox.Text = cpuinfo.ThreadCount.ToString();
                ManufacturerTextBox.Text = cpuinfo.SimplifiedManufacturer;
                SocketTextBox.Text = cpuinfo.wmi.SocketDesignation;
                FamilyTextBox.Text = cpuinfo.wmi.Family;
                ModelTextBox.Text = cpuinfo.wmi.Model;
                SteppingTextBox.Text = cpuinfo.wmi.Stepping;
                L1TextBox.Text = cpuinfo.wmi.L1Cache;
                L2TextBox.Text = cpuinfo.wmi.L2Cache;
                L3TextBox.Text = cpuinfo.wmi.L3Cache;
                MaxClockSpeedTextBox.Text = (cpuinfo.MaxClockSpeed/1000F).ToString();
                CPUIcon.Source = new ImageSourceConverter().ConvertFromString(
                    "pack://application:,,,/MetroCPU;component/"+cpuinfo.wmi.CPUIcon) as ImageSource;
                UITimer.Start();
                psm = new PowerStatusMonitor(
                    ()=> MessageBox.Show($"{psm.GetPowerLineStatus()}") 
                ,()=> MessageBox.Show($"{psm.GetPowerLineStatus()}")
                ,true);
            }
        }
        private PowerStatusMonitor psm;
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

        private void Tabs_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if(Tabs.SelectedIndex == 0)
            {
                if (!Tab1.Children.Contains(MonitorGroup))
                {
                    Tab2.Children.Remove(MonitorGroup);
                    Tab1.Children.Add(MonitorGroup);
                }
            }
            else if(Tabs.SelectedIndex ==1)
            {
                if (!Tab2.Children.Contains(MonitorGroup))
                {
                    Tab1.Children.Remove(MonitorGroup);
                    Tab2.Children.Add(MonitorGroup);
                }
            }
        }

    }
}
