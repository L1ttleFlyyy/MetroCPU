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
        public event Action UnsupportedEvent;
        public UnderVoltor(Ols ols)
        {
            _ols = ols;
            Support = new bool[6];
            for (int i = 0; i < 6; i++)
            {
                SetVolta((Peripheral)i, 5);
                GetVolta((Peripheral)i, out int tmp);
                if (tmp == 5)
                {
                    Support[i] = true;
                    SetVolta((Peripheral)i, 0);
                }
                else
                    Support[i] = false;
            }
        }

        public void SetVolta(Peripheral p, int Volta)
        {
            uint eax = (uint)Math.Round(Volta * 1.024) << 21;
            uint edx = WRBase + (uint)p * 0x100;
            if (_ols.Wrmsr(0x150, eax, edx) == 0)
            {
                Support[(int)p] = false;
                UnsupportedEvent?.Invoke();
            }
        }

        public void GetVolta(Peripheral p, out int Volta)
        {
            uint eax = 0;
            uint edx = RDBase + (uint)p * 0x100;
            if (_ols.Wrmsr(0x150, eax, edx) > 0)
            {
                _ols.Rdmsr(0x150, ref eax, ref edx);
                Volta = (int)Math.Round((((int)eax >> 21) / 1.024));
            }
            else
            {
                UnsupportedEvent?.Invoke();
                Volta = 0;
                Support[(int)p] = false;
            }
        }
    }
}
