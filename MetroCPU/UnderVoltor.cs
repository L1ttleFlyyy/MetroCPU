using System;

namespace OpenLibSys
{
    enum Peripheral { CPUCore, IntelGPU, CPUCache, UnCore, AnalogIO, DigitalIO };
    class UnderVoltor
    {
        private Ols _ols;
        private const uint RDBase = 0x80000010;
        private const uint WRBase = 0x80000011;
        public bool[] Support { get; private set; }
        public int[] Settings { get; set; }
        public UnderVoltor(Ols ols)
        {
            _ols = ols;
            Support = new bool[6];
            Settings = new int[6];
            for (int i = 0; i < 6; i++)
            {
                
                if (GetVolta((Peripheral)i, out int origin))
                {
                    SetVolta((Peripheral)i, origin+5);
                    GetVolta((Peripheral)i, out int tmp);
                    if (tmp == origin+5)
                    {
                        Settings[i] = origin;
                        Support[i] = true;
                        SetVolta((Peripheral)i, origin);
                    }
                    else
                        Support[i] = false;
                }
                else
                    Support[i] = false;
            }
        }

        public void SetSettings(int[] settings)
        {
            Settings = settings;
            for (int i = 0; i < 6; i++)
            {
                if (Support[i])
                {
                    SetVolta((Peripheral)i, Settings[i]);
                }
            }
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
}
