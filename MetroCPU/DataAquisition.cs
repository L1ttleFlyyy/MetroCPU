using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OpenLibSys
{

    public class LogicalProcessor
    {
        private Ols _ols;
        public int Thread { get; }
        public ulong ThreadAffinityMask { get; }
        public UIntPtr PThread { get; }
        public readonly int MaxFrequency;

        public LogicalProcessor(Ols ols, int thread, int maxFreq)
        {
            MaxFrequency = maxFreq;
            _ols = ols;
            Thread = thread;
            ThreadAffinityMask = 1UL << Thread;
            PThread = new UIntPtr(ThreadAffinityMask);
        }

        public float GetCurrentFrequency()
        {
            float err,ratio;
            do {
                ratio = GetCurrentFrequencyRatio(out err);
            }
            while (err > 1e-3);
            return ratio * MaxFrequency / 1000;
        }

        public float GetCurrentFrequencyRatio(out float error)
        {
            ulong mcnt_start, acnt_start, mcnt_stop, acnt_stop;
            ulong err_start, err_stop;
            do
            {
                uint eax_m = 0, edx_m = 0, eax_a = 0, edx_a = 0, eax_e = 0, edx_e = 0;
                ThreadAffinity.Set(ThreadAffinityMask);
                _ols.RdmsrTx(0xe8, ref eax_a, ref edx_a, PThread);
                _ols.RdmsrTx(0xe7, ref eax_m, ref edx_m, PThread);
                _ols.RdmsrTx(0xe8, ref eax_e, ref edx_e, PThread);
                mcnt_start = ((ulong)edx_m << 32) + eax_m;
                acnt_start = ((ulong)edx_a << 32) + eax_a;
                err_start = ((ulong)edx_a << 32) + eax_e;
                System.Threading.Thread.Sleep(20);
                _ols.RdmsrTx(0xe8, ref eax_a, ref edx_a, PThread);
                _ols.RdmsrTx(0xe7, ref eax_m, ref edx_m, PThread);
                _ols.RdmsrTx(0xe8, ref eax_e, ref edx_e, PThread);
                mcnt_stop = ((ulong)edx_m << 32) + eax_m;
                acnt_stop = ((ulong)edx_a << 32) + eax_a;
                err_stop = ((ulong)edx_a << 32) + eax_e;
                ThreadAffinity.Set(ThreadAffinityMask);
            } while (acnt_stop <= acnt_start || mcnt_stop <= mcnt_start);
            float res = (float)(acnt_stop - acnt_start) / (mcnt_stop - mcnt_start);
            error = Math.Abs(res-(float)(err_stop - err_start) / (mcnt_stop - mcnt_start))/res;
            return res;
        }
    }

    public class PackageMonitor
    {
        public bool RAPL_supported { get; private set; }
        private Ols _ols;
        public readonly float PU;
        public readonly float ESU;
        public readonly float TU;
        public float TDP
        {
            get
            {
                uint eax = 0, edx = 0;
                _ols.Rdmsr(0x614, ref eax, ref edx);
                return CPUinfo.BitsSlicer(eax, 14, 0) * PU;
            }
        }
        private string Manufacturer;

        public PackageMonitor(Ols ols, string manufacturer)
        {
            Manufacturer = manufacturer;
            if (Manufacturer == "GenuineIntel")
            {
                _ols = ols;
                uint edx = 0, eax = 0;
                if (_ols.Rdmsr(0x606, ref eax, ref edx) > 0)
                {
                    PU = 1F / (float)Math.Pow(2, CPUinfo.BitsSlicer(eax, 3, 0));
                    ESU = 1F / (float)Math.Pow(2, CPUinfo.BitsSlicer(eax, 12, 8));
                    TU = 1F / (float)Math.Pow(2, CPUinfo.BitsSlicer(eax, 19, 16));
                    RAPL_supported = true;
                }
                RAPL_supported = true;
            }
            else
                RAPL_supported = false;
        }

        public float GetPackagePower()
        {
            uint eax_start = 0, eax_stop = 0, edx = 0;
            long t_start, t_stop, f;
            do
            {
                _ols.Rdmsr(0x611, ref eax_start, ref edx);
                t_start = Stopwatch.GetTimestamp();
                f = Stopwatch.Frequency;
                Thread.Sleep(30);
                _ols.Rdmsr(0x611, ref eax_stop, ref edx);
                t_stop = Stopwatch.GetTimestamp();
            } while (eax_start > eax_stop);
            return ESU * (eax_stop - eax_start) / (t_stop - t_start) * f;
        }

        public float GetCurrentVoltage()
        {
            if (Manufacturer == "GenuineIntel")
            {
                uint eax = 0, edx = 0;
                if (_ols.Rdmsr(0x198, ref eax, ref edx) > 0)
                {
                    return (ushort)edx / 8192F;
                }
                else
                    return 0;
            }
            else if (Manufacturer == "AuthenticAMD")
                return 0;
            else
                return 0;
        }

        public float GetCurrentTemprature()
        {
            if (Manufacturer == "GenuineIntel")
            {
                uint tjmax, t_tmp;
                uint eax = 0, edx = 0;
                tjmax = (_ols.Rdmsr(0x1a2, ref eax, ref edx) > 0) ? CPUinfo.BitsSlicer(eax, 22, 16) : 100;
                t_tmp = (_ols.Rdmsr(0x1b1, ref eax, ref edx) > 0) ? CPUinfo.BitsSlicer(eax, 22, 16) : 0;
                return tjmax - t_tmp;
            }
            else if (Manufacturer == "AuthenticAMD")
                return 0;
            else
                return 0;
        }

    }
}
