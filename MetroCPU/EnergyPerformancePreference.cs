using System;
using System.IO;

namespace OpenLibSys
{
    public class EnergyPerformancePreference
    {
        public byte MaxLimit
        {
            get
            {
                if (IsEnabled)
                {
                    uint eax = 0, edx = 0;
                    _ols.Rdmsr(0x771, ref eax, ref edx);
                    return (byte)CPUinfo.BitsSlicer(eax, 7, 0);
                }
                else
                    return 255;
            }
        }
        public byte MinLimit
        {
            get
            {
                if (IsEnabled)
                {
                    uint eax = 0, edx = 0;
                    _ols.Rdmsr(0x771, ref eax, ref edx);
                    return (byte)CPUinfo.BitsSlicer(eax, 31, 24);
                }
                else
                    return 1;
            }
        }
        public bool IsEnabled
        {
            get
            {
                if (_cpu.SST_support)
                {
                    return _cpu.SST_enabled;
                }
                else
                    return false;
            }
            set
            {
                if (_cpu.SST_support)
                {
                    if (value != IsEnabled)
                    {
                        _cpu.SST_enabled = value;
                        EnableChanged?.Invoke();
                    }
                }
            }
        }
        public EPPSettings PowerSavingSettings;
        public EPPSettings HighPerformanceSettings;
        public event Action EnableChanged;
        public event Action NewSettingsApplied;
        private Ols _ols;
        private CPUinfo _cpu;
        public int SettingIndex { get; private set; }
        public EPPSettings CurrentSetting { get => SettingIndex == 0 ? HighPerformanceSettings : PowerSavingSettings; }

        public EnergyPerformancePreference(CPUinfo cpu)
        {
            _cpu = cpu;
            _ols = _cpu._ols;
            SettingIndex = 0;
            PowerSavingSettings = new EPPSettings("PowerSaving");
            HighPerformanceSettings = new EPPSettings("HighPerformance");
        }

        private void ApplySettings(byte[] settings)
        {
            if (_cpu.SST_support)
            {
                if (!_cpu.SST_enabled) { _ols.Wrmsr(0x770, 1, 0); }
                uint eax = EPPSettings.Settings2EAX(settings);
                uint edx = 0;
                for (int i = 0; i < _cpu.ThreadCount; i++)
                {
                    _ols.WrmsrTx(0x774, eax, edx, (UIntPtr)(1UL << i));
                }
                NewSettingsApplied?.Invoke();
            }
        }

        public void ApplySettings(EPPSettings settings)
        {
            if (settings.Name == "HighPerformance")
            {
                SettingIndex = 0;
            }
            else if (settings.Name == "PowerSaving")
            {
                SettingIndex = 1;
            }
            ApplySettings(settings.Settings);
        }

        public void SaveSettingsTo(byte[] tempsettings, EPPSettings settingFile)
        {
            settingFile.Settings = tempsettings;
        }

        public byte[] LoadSettingsFromFile(EPPSettings settingFile)
        {
            return settingFile.Settings;
        }

    }

    public class EPPSettings
    {
        public string Name { get; private set; }
        private string fileDirectory
        {
            get
            {
                string tempDir = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\MetroCPU";
                if (!Directory.Exists(tempDir))
                {
                    Directory.CreateDirectory(tempDir);
                }
                return tempDir;
            }
        }
        private string filePath
        {
            get
            {
                string file = fileDirectory + @"\" + Name;
                if (!File.Exists(file))
                {
                    using (StreamWriter sw = new StreamWriter(File.Create(file)))
                    {
                        sw.WriteLine("128");
                        sw.WriteLine("255");
                        sw.WriteLine("1");
                    }
                }
                return file;
            }
        }
        public byte[] Settings
        {
            get => LoadSettingsFromFile();
            set => SaveSettingsToFile(value);
        }
        private void SaveSettingsToFile(byte[] settings)
        {
            using (FileStream fs = new FileStream(filePath, FileMode.Create))
            using (StreamWriter sw = new StreamWriter(fs))
            {
                foreach (int i in settings)
                    sw.WriteLine(i.ToString());
            }
        }
        private byte[] LoadSettingsFromFile()
        {
            byte[] temp = new byte[3];
            using (StreamReader sr = new StreamReader(filePath))
            {
                foreach (int i in new int[] { 0, 1, 2 })
                {
                    temp[i] = byte.Parse(sr.ReadLine());
                }
            }
            return temp;
        }
        public EPPSettings(string displayName)
        {
            Name = displayName;
        }
        public static uint Settings2EAX(byte[] settings)
        {
            return settings[0] * 0x1000000U + settings[1] * 0x100U + settings[2];
        }
    }
}
