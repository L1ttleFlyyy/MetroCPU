using System;

namespace OpenLibSys
{
    class PMC : IDisposable
    {
        public bool Disposed { get; private set; } = false;
        public int Thread { get; }
        public int UMask { get; }
        public int EventSelect { get; }
        private readonly uint PMC_num;
        private readonly Ols _ols;

        public string ErrorMessage { get; }
        public ulong EventCounts
        {
            get
            {
                uint eax = 0, edx = 0;
                try
                {
                    _ols.RdmsrTx(0x0c1 + PMC_num, ref eax, ref edx, (UIntPtr)(1UL << Thread));

                    return ((ulong)edx << 32) + eax;
                }
                catch(Exception e)
                {
                    return 0;
                }
            }
        }

        public PMC(Ols ols, ManufacturerName Manufacturer, int thread, byte uMask, byte eventSelect)
        {
            _ols = ols;
            if (_ols.IsMsr() > 0)
            {
                switch (Manufacturer)
                {
                    case ManufacturerName.GenuineIntel:
                        Thread = thread;
                        UMask = uMask;
                        EventSelect = eventSelect;
                        uint eax = 0, edx = 0;
                        for (uint i = 0; i < 8; i++)
                        {
                            if (_ols.RdmsrTx(i + 0x186, ref eax, ref edx, (UIntPtr)(1UL << Thread)) > 0)
                            {
                                if (CPUinfo.BitsSlicer(eax, 22, 22) == 0)
                                {
                                    PMC_num = i;
                                    edx = 0;
                                    eax = eventSelect + uMask * 256U + 0x41 * 256 * 256;
                                    if (_ols.WrmsrTx(PMC_num + 0x186, eax, edx, (UIntPtr)(1UL << Thread)) == 0)
                                    {
                                        ErrorMessage = "Wrmsr failed";
                                        Dispose();
                                    }
                                    break;
                                }
                            }
                            else
                            {
                                ErrorMessage = "No available PMC for this logical processor";
                                Dispose();
                            }
                        }
                        break;
                    case ManufacturerName.AuthenticAMD:
                        ErrorMessage = "Unsupported cpu vendor";
                        Dispose();
                        break;
                    default:
                        ErrorMessage = "Unsupported cpu vendor";
                        Dispose();
                        break;
                }
            }
        }

        protected virtual void Dispose(bool disposing)
        {

            if (!Disposed)
            {
                if (disposing)
                {
                    // TODO: 释放托管状态(托管对象)。
                }

                _ols.WrmsrTx(PMC_num + 0x186, 0, 0, (UIntPtr)(1UL << Thread));
                Disposed = true;
            }
        }
        public void Dispose()
        {
            Dispose(true);
        }

    }
}
