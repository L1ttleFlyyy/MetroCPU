using System;
using System.Collections.Generic;
using System.IO;

namespace OpenLibSys
{
    enum Peripheral { CPUCore, IntelGPU, CPUCache, UnCore, AnalogIO, DigitalIO };
    class UnderVoltor
    {
        private Ols _ols;
        private const uint RDBase = 0x80000010;
        private const uint WRBase = 0x80000011;
        public UnderVoltSettings SettingFile;
        public bool[] Support { get; private set; }
        public int[] CurrentSettings
        {
            get
            {
                int[] tmp = new int[6];
                for (int i = 0; i < 6; i++)
                {
                    if (Support[i])
                    {
                        GetVolta((Peripheral)i, out tmp[i]);
                    }
                }
                return tmp;
            }
            set
            {
                for (int i = 0; i < 6; i++)
                {
                    if (Support[i])
                    {
                        SetVolta((Peripheral)i, value[i]);
                    }
                }
            }
        }

        public void SaveSettingsToFile()
        {
            SettingFile.Settings = CurrentSettings;
        }

        public UnderVoltor(Ols ols)
        {
            _ols = ols;
            Support = new bool[6];
            CurrentSettings = new int[6];
            for (int i = 0; i < 6; i++)
            {

                if (GetVolta((Peripheral)i, out int origin))
                {
                    SetVolta((Peripheral)i, origin + 5);
                    GetVolta((Peripheral)i, out int tmp);
                    if (tmp == origin + 5)
                    {
                        CurrentSettings[i] = origin;
                        Support[i] = true;
                        SetVolta((Peripheral)i, origin);
                    }
                    else
                        Support[i] = false;
                }
                else
                    Support[i] = false;
            }
            SettingFile = new UnderVoltSettings("Untilization") { Settings = CurrentSettings };
        }

        public bool SetVolta(Peripheral p, int Volta)
        {
            uint eax = (uint)Math.Round(Volta * 1.024) << 21;
            uint edx = WRBase + (uint)p * 0x100;
            if (_ols.Wrmsr(0x150, eax, edx) == 0)
            {
                Support[(int)p] = false;
                return false;
            }
            else
                return true;
        }

        public bool GetVolta(Peripheral p, out int Volta)
        {
            uint eax = 0;
            uint edx = RDBase + (uint)p * 0x100;
            if (_ols.Wrmsr(0x150, eax, edx) > 0 && _ols.Rdmsr(0x150, ref eax, ref edx) > 0)
            {
                Volta = (int)Math.Round((((int)eax >> 21) / 1.024));
                return true;
            }
            else
            {
                Volta = 0;
                Support[(int)p] = false;
                return false;
            }
        }
    }

    class UnderVoltSettings
    {
        public string Name { get; private set; }
        private string fileDirectory
        {
            get
            {
                string tempDir = Directory.GetCurrentDirectory() + @"\UnderVolt";
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
                        for (int i = 0; i < 6; i++)
                            sw.WriteLine("0");
                    }
                }
                return file;
            }
        }
        public int[] Settings
        {
            get => LoadSettingsFromFile();
            set => SaveSettingsToFile(value);
        }
        private void SaveSettingsToFile(int[] settings)
        {
            using (FileStream fs = new FileStream(filePath, FileMode.Create))
            using (StreamWriter sw = new StreamWriter(fs))
            {
                foreach (int i in settings)
                    sw.WriteLine(i.ToString());
            }
        }
        private int[] LoadSettingsFromFile()
        {
            int[] temp = new int[6];
            using (StreamReader sr = new StreamReader(filePath))
            {
                foreach (int i in new int[] { 0, 1, 2, 3, 4, 5 })
                {
                    temp[i] = int.Parse(sr.ReadLine());
                }
            }
            return temp;
        }
        public UnderVoltSettings(string displayName)
        {
            Name = displayName;
            LoadSettingsFromFile();
        }
    }
}
