using System;
using Microsoft.Win32;
using System.Windows.Forms;
using System.IO;

namespace OpenLibSys
{
    public class PowerStatusMonitor
    {
        public bool IsEnabled { get; private set; }
        public bool FileSetting
        {
            set
            {
                if (IsEnabled != value)
                {
                    IsEnabled = value;
                    EnableChanged?.Invoke(value);
                    SettingFile.Setting = value;
                }
            }
        }
        public event Action PowerResume;
        public event Action<bool> EnableChanged;
        public event Action<PowerLineStatus> PowerModeChanged;
        private AutoSetting SettingFile = new AutoSetting("AutoStart");
        public PowerStatusMonitor()
        {
            IsEnabled = SettingFile.Setting;

            SystemEvents.PowerModeChanged += new PowerModeChangedEventHandler((s, e) =>
            {
                if (IsEnabled)
                {
                    switch (e.Mode)
                    {
                        case PowerModes.Resume:
                            PowerResume?.Invoke();
                            ChangePowerMode();
                            break;
                        case PowerModes.StatusChange:
                            ChangePowerMode();
                            break;
                        default:
                            break;

                    }
                }
            });
        }

        private void ChangePowerMode()
        {
            PowerModeChanged?.Invoke(SystemInformation.PowerStatus.PowerLineStatus);
        }
    }

    public class AutoSetting
    {
        public string Name { get; private set; }
        private string filePath
        {
            get
            {
                string file = Directory.GetCurrentDirectory() + @"\" + Name;
                if (!File.Exists(file))
                {
                    using (StreamWriter sw = new StreamWriter(File.Create(file)))
                    {
                        sw.WriteLine(true.ToString());
                    }
                }
                return file;
            }
        }
        public bool Setting
        {
            get => LoadSettingsFromFile();
            set => SaveSettingsToFile(value);
        }
        private void SaveSettingsToFile(bool setting)
        {
            using (FileStream fs = new FileStream(filePath, FileMode.Create))
            using (StreamWriter sw = new StreamWriter(fs))
            {
                sw.WriteLine(setting.ToString());
            }
        }
        private bool LoadSettingsFromFile()
        {
            bool tmp;
            using (StreamReader sr = new StreamReader(filePath))
            {
                tmp = bool.Parse(sr.ReadLine());
            }
            return tmp;
        }

        public AutoSetting(string displayName)
        {
            Name = displayName;
        }
    }
}
