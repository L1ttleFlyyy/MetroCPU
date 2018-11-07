using System;
using System.Collections.Generic;
using System.Text;

namespace OpenLibSys
{
    enum ManufacturerName { GenuineIntel, AuthenticAMD, Unknown };

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

        private ulong AperfCounts
        {
            get
            {
                uint eax_a = 0, edx_a = 0;
                _ols.RdmsrTx(0xe8, ref eax_a, ref edx_a, PThread);
                return ((ulong)edx_a << 32) + eax_a;
            }
        }

        public double GetCurrentFrequencyRatio()
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
            return (double)(acnt_stop - acnt_start) / (mcnt_stop - mcnt_start);
        }
    }

    class CPUinfo : IDisposable
    {
        private readonly WMICPUinfo wmi;
        private const int MaxIndDefined = 0x1f;
        private Ols _ols;
        private readonly List<LogicalProcessor> logicalProcessors;
        private readonly List<Sensor> frequencySensors;
        public int test;
        public bool LoadSucceeded { get; }
        public bool SST_support { get; }
        public uint[,] CPUID { get; private set; }
        public int MaxCPUIDind { get; private set; }
        public uint[,] CPUID_ex { get; private set; }
        public int MaxCPUIDexind { get; private set; }
        public bool IsCpuid { get; }
        public int MaxClockSpeed { get => (int)wmi.MaxClockSpeed; }
        public string Model { get => wmi.Name; }
        public string Manufacturer { get => wmi.Manufacturer; }
        public int ThreadCount { get => (int)wmi.NumberOfLogicalProcessors; }
        public int CoreCount { get => (int)wmi.NumberOfCores; }
        public readonly string ErrorMessage = "No error";
        public bool IsHyperThreading { get=>ThreadCount>CoreCount; }
        public double[] Freq
        {
            get
            {
                List<double> tmp = new List<double>(ThreadCount);
                foreach (Sensor s in frequencySensors)
                {
                    tmp.Add(wmi.MaxClockSpeed * s.CurrentData);
                }
                return tmp.ToArray();
            }
        }
        public bool SST_enabled
        {
            get
            {
                if (SST_support)
                {
                    uint eax = 0, edx = 0;
                    if (_ols.Rdmsr(0x770, ref eax, ref edx) > 0)
                        return eax > 0 ? true : false;
                    else
                        return false;
                }
                else
                    return false;
            }
            set
            {
                if (value)
                    test = _ols.Wrmsr(0x770, 1, 0);
                else
                    test = _ols.Wrmsr(0x770, 0, 0);
            }
        }
        public CPUinfo()
        {
            _ols = new Ols();
            uint res = _ols.GetStatus();
            if (res != 0)
            {
                ErrorMessage = ((Ols.Status)res).ToString() + "\n" + ((Ols.OlsDllStatus)_ols.GetDllStatus()).ToString();
                LoadSucceeded = false;
            }
            else
            {
                wmi = new WMICPUinfo();
                LoadSucceeded = true;
                IsCpuid = _ols.IsCpuid() > 0;
                _getCPUID();
                _getCPUIDex();
                SST_support = BitsSlicer(CPUID[6, 0], 7, 7) > 0;
                logicalProcessors = new List<LogicalProcessor>(CoreCount);
                frequencySensors = new List<Sensor>(CoreCount);
                int times = IsHyperThreading?2:1;
                for (int i = 0; i < CoreCount; i++)
                {
                    logicalProcessors.Add(new LogicalProcessor(_ols, i* times));
                    frequencySensors.Add(new Sensor(logicalProcessors[i].GetCurrentFrequencyRatio));
                }

            }
        }
        
        private void _getCPUID()
        {
            CPUID = new uint[MaxIndDefined + 1, 4];
            CPUID[0, 0] = 0;
            CPUID[0, 1] = 0;
            CPUID[0, 2] = 0;
            CPUID[0, 3] = 0;
            if (IsCpuid)
            {
                _ols.Cpuid(0, ref CPUID[0, 0], ref CPUID[0, 1], ref CPUID[0, 2], ref CPUID[0, 3]);
            }
            MaxCPUIDind = Math.Min((int)CPUID[0, 0], MaxIndDefined);
            if (MaxCPUIDind > 0)
            {
                for (uint tmp = 1; tmp <= MaxCPUIDind; tmp++)
                {
                    _ols.Cpuid(tmp, ref CPUID[tmp, 0], ref CPUID[tmp, 1], ref CPUID[tmp, 2], ref CPUID[tmp, 3]);
                }
            }
        }

        private void _getCPUIDex()
        {
            CPUID_ex = new uint[MaxIndDefined + 1, 4];
            CPUID_ex[0, 0] = 0;
            CPUID_ex[0, 1] = 0;
            CPUID_ex[0, 2] = 0;
            CPUID_ex[0, 3] = 0;
            if (IsCpuid)
            {
                _ols.Cpuid(0x80000000, ref CPUID_ex[0, 0], ref CPUID_ex[0, 1], ref CPUID_ex[0, 2], ref CPUID_ex[0, 3]);
            }
            MaxCPUIDexind = Math.Min((int)(CPUID_ex[0, 0] - 0x80000000), MaxIndDefined);

            if (MaxCPUIDexind > 0)
            {
                for (uint tmp = 1; tmp <= MaxCPUIDexind; tmp++)
                {
                    _ols.Cpuid(tmp + 0x80000000, ref CPUID_ex[tmp, 0], ref CPUID_ex[tmp, 1], ref CPUID_ex[tmp, 2], ref CPUID_ex[tmp, 3]);
                }
            }
        }

        public static uint BitsSlicer(uint exx, int Highest, int Lowest)
        {
            if (Highest < Lowest || Highest > 31 || Highest < 0 || Lowest > 31 || Lowest < 0)
            {
                throw new ArgumentOutOfRangeException();
            }
            uint head, tail;
            tail = exx / (uint)Math.Pow(2, Lowest);
            head = (uint)(exx / (ulong)Math.Pow(2, Highest + 1) * (ulong)Math.Pow(2, Highest + 1) / (uint)Math.Pow(2, Lowest));
            return tail - head;
        }

        public static string ToBinary(uint data, int bits)
        {
            if (bits > 31 || bits < 0)
            {
                throw new ArgumentOutOfRangeException();
            }
            return Convert.ToString(data, 2).PadLeft(bits, '0');
        }

        private static string ExToString(uint i)
        {
            byte[] bytes = new byte[4];
            for (int j = 0; j < 4; j++)
            {
                bytes[j] = (byte)(i / Math.Pow(256, j));
            }
            return Encoding.ASCII.GetString(bytes);
        }

        #region IDisposable Support
        private bool disposedValue = false; // 要检测冗余调用

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    this.CPUID = null;
                    this.CPUID_ex = null;
                }
                foreach (Sensor s in frequencySensors)
                {
                    s.Dispose();
                }
                _ols.Dispose();

                disposedValue = true;
            }
        }
        public void Dispose()
        {
            Dispose(true);
        }
        #endregion

    }
}
