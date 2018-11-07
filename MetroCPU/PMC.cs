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
        private readonly uint PMC_num_msr;
        protected readonly Ols _ols;
        protected readonly UIntPtr pthread;
        public string ErrorMessage { get; private set; }
        public bool GetPMCTSC(out ulong pmc_count, out ulong tsc_count)
        {
            uint eax_pmc = 0, edx_pmc = 0;
            uint eax_tsc = 0, edx_tsc = 0;
            try
            {
                if ((_ols.RdmsrTx(PMC_num_msr, ref eax_pmc, ref edx_pmc, pthread) > 0)
                    && (_ols.RdtscTx(ref eax_tsc, ref edx_tsc, pthread) > 0))
                {
                    pmc_count = ((ulong)edx_pmc << 32) + eax_tsc;
                    tsc_count = ((ulong)edx_tsc << 32) + eax_tsc;
                    return true;
                }
                else
                    pmc_count = 0;
                    tsc_count = 0;
                    return false;
            }
            catch(Exception e)
            {
                ErrorMessage = e.Message;
                pmc_count = 0;
                tsc_count = 0;
                return false;
            }
        }
        public ulong EventCounts
        {
            get
            {
                uint eax = 0, edx = 0;
                try
                {
                    _ols.RdmsrTx(PMC_num_msr, ref eax, ref edx, pthread);

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
                        pthread = (UIntPtr)(1UL << Thread);
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
                                    eax = eventSelect + uMask * 256U + 0x43 * 256 * 256;
                                    if (_ols.WrmsrTx(PMC_num + 0x186, eax, edx, (UIntPtr)(1UL << Thread)) == 0)
                                    {
                                        ErrorMessage = "Wrmsr failed";
                                        Dispose();
                                        return ;
                                    }
                                    PMC_num_msr = 0x0c1+PMC_num;
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
