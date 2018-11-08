﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OpenLibSys
{

    class LogicalProcessor
    {
        private Ols _ols;
        public int Thread { get; }
        public ulong ThreadAffinityMask { get; }
        public UIntPtr PThread { get; }

        public LogicalProcessor(Ols ols, int thread)
        {
            _ols = ols;
            Thread = thread;
            ThreadAffinityMask = 1UL << Thread;
            PThread = new UIntPtr(ThreadAffinityMask);
        }

        public float GetCurrentFrequencyRatio()
        {
            ulong mcnt_start, acnt_start, mcnt_stop, acnt_stop;
            do
            {
                uint eax_m = 0, edx_m = 0, eax_a = 0, edx_a = 0;
                int cnt = 0;
                ThreadAffinity.Set(ThreadAffinityMask);
                while (_ols.RdmsrTx(0xe8, ref eax_a, ref edx_a, PThread) == 0 || _ols.RdmsrTx(0xe7, ref eax_m, ref edx_m, PThread) == 0)
                {
                    if (cnt < 4)
                        cnt++;
                    else
                        return 0;
                }
                mcnt_start = ((ulong)edx_m << 32) + eax_m;
                acnt_start = ((ulong)edx_a << 32) + eax_a;
                cnt = 0;
                System.Threading.Thread.Sleep(30);
                while (_ols.RdmsrTx(0xe8, ref eax_a, ref edx_a, PThread) == 0 || _ols.RdmsrTx(0xe7, ref eax_m, ref edx_m, PThread) == 0)
                {
                    if (cnt < 4)
                        cnt++;
                    else
                        return 0;
                }
                mcnt_stop = ((ulong)edx_m << 32) + eax_m;
                acnt_stop = ((ulong)edx_a << 32) + eax_a;
                ThreadAffinity.Set(ThreadAffinityMask);
            } while (acnt_stop <= acnt_start || mcnt_stop <= mcnt_start);
            return (float)(acnt_stop - acnt_start) / (mcnt_stop - mcnt_start);
        }
    }

    class PackageMonitor
    {
        public bool RAPL_supported { get; private set; }
        private Ols _ols;
        public readonly float PU;
        public readonly float ESU;
        public readonly float TU;
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
