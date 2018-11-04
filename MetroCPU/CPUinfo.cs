using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace OpenLibSys
{

    class CPUinfo : IDisposable
    {

        private const int MaxIndDefined = 0x1f;
        private Ols _ols;
        private List<FreqPMC> PMC_List;
        public int test;
        public bool LoadSucceeded { get; }
        public bool SST_support { get; }
        public uint[,] cpuid = new uint[32, 4];
        public int MaxCPUIDind { get; }
        public uint[,] cpuid_ex = new uint[32, 4];
        public int MaxCPUIDexind { get; }
        public bool IsCpuid { get; }
        public string Vendor { get; }
        public string Manufacturer { get; }
        public int ThreadCount { get; }
        public int CoreCount { get; }
        public ArrayList Freq_List { get; }
        public readonly string ErrorMessage = "No error";
        public int LogicalCoreCounts { get; }
        public bool IsHyperThreading { get; }

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
                LoadSucceeded = true;
                IsCpuid = _ols.IsCpuid() > 0;
                _getCPUID();
                MaxCPUIDind = (int)Math.Min(cpuid[0, 0], MaxIndDefined);
                _getCPUIDex();
                MaxCPUIDexind = (int)Math.Min((cpuid_ex[0, 0] - 0x80000000), MaxIndDefined);
                Vendor = _getVendor();
                Manufacturer = _getManufacturer();
                SST_support = BitsSlicer(cpuid[6, 0], 7, 7) > 0;
                IsHyperThreading = BitsSlicer(cpuid[1, 3], 28, 28) > 0;
                ThreadCount = _getThreadCount();
                CoreCount = IsHyperThreading?ThreadCount>>1:ThreadCount;
                PMC_List = new List<FreqPMC>(ThreadCount);
                Freq_List = new ArrayList(ThreadCount);
                for(int i=0;i<ThreadCount;i++)
                {
                    PMC_List.Add( new FreqPMC(_ols, Manufacturer, i, 0, 0x3c));
                    Freq_List.Add( PMC_List[i].Frequency());
                }
                /*RdTSC();
                //ulong mask = ThreadAffinity.Set(1UL <<1);

                EstimatePerformanceMonitoringCounterFrequency(
                  out double estimatedPerformanceMonitoringCounterFrequency,
                  out double estimatedPerformanceMonitoringCounterFrequencyError);

                //ThreadAffinity.Set(mask);
                Freq = estimatedPerformanceMonitoringCounterFrequency;*/
                
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

        private int _getThreadCount()
        {
            uint eax = 0, ebx = 0, ecx = 0, edx = 0;
            int cnt = 0;
            while (_ols.CpuidTx(01, ref eax, ref ebx, ref ecx, ref edx, (UIntPtr)(1UL << cnt)) == 1)
            {
                cnt++;
            }
            return cnt;
        }

        private string _getManufacturer()
        {
            StringBuilder sb = new StringBuilder();
            uint[] ex = new uint[4] { 0, 0, 0, 0 };
            _ols.Cpuid(0, ref ex[0], ref ex[1], ref ex[2], ref ex[3]);
            foreach (uint i in new uint[] { ex[1], ex[3], ex[2] })
                sb.Append(ExToString(i));
            return sb.ToString();
        }

        private string _getVendor()
        {
            StringBuilder sb = new StringBuilder();
            uint[] ex = new uint[4] { 0, 0, 0, 0 };
            foreach (uint ind in new uint[] { 0x80000002, 0x80000003, 0x80000004 })
            {
                _ols.Cpuid(ind, ref ex[0], ref ex[1], ref ex[2], ref ex[3]);
                foreach (uint i in ex)
                    sb.Append(ExToString(i));
            }
            return sb.ToString();

        }

        private void _getCPUID()
        {
            cpuid[0, 0] = 0;
            cpuid[0, 1] = 0;
            cpuid[0, 2] = 0;
            cpuid[0, 3] = 0;
            if (IsCpuid)
            {
                _ols.Cpuid(0, ref cpuid[0, 0], ref cpuid[0, 1], ref cpuid[0, 2], ref cpuid[0, 3]);
            }
            int maxind = Math.Min((int)cpuid[0, 0], MaxIndDefined);
            if (maxind > 0)
            {
                for (uint tmp = 1; tmp <= maxind; tmp++)
                {
                    _ols.Cpuid(tmp, ref cpuid[tmp, 0], ref cpuid[tmp, 1], ref cpuid[tmp, 2], ref cpuid[tmp, 3]);
                }
            }
        }

        private void _getCPUIDex()
        {
            cpuid_ex[0, 0] = 0;
            cpuid_ex[0, 1] = 0;
            cpuid_ex[0, 2] = 0;
            cpuid_ex[0, 3] = 0;
            if (IsCpuid)
            {
                _ols.Cpuid(0x80000000, ref cpuid_ex[0, 0], ref cpuid_ex[0, 1], ref cpuid_ex[0, 2], ref cpuid_ex[0, 3]);
            }
            int maxind = Math.Min((int)(cpuid_ex[0, 0] - 0x80000000), MaxIndDefined);

            if (maxind > 0)
            {
                for (uint tmp = 1; tmp <= maxind; tmp++)
                {
                    _ols.Cpuid(tmp + 0x80000000, ref cpuid_ex[tmp, 0], ref cpuid_ex[tmp, 1], ref cpuid_ex[tmp, 2], ref cpuid_ex[tmp, 3]);
                }
            }
        }
        /*
        private void EstimatePerformanceMonitoringCounterFrequency(out double frequency, out double error)
        {

            // preload the function
            EstimatePerformanceMonitoringCounterFrequency(0, out double f, out double e);
            EstimatePerformanceMonitoringCounterFrequency(0, out f, out e);

            // estimate the frequency
            error = double.MaxValue;
            frequency = 0;
            for (int i = 0; i < 5; i++)
            {
                EstimatePerformanceMonitoringCounterFrequency(0.025, out f, out e);
                if (e < error)
                {
                    error = e;
                    frequency = f;
                }

                if (error < 1e-4)
                    break;
            }
        }

        private void EstimatePerformanceMonitoringCounterFrequency(double timeWindow, out double frequency, out double error)
        {
            long ticks = (long)(timeWindow * Stopwatch.Frequency);
            ulong countBegin, countEnd;

            long timeBegin = Stopwatch.GetTimestamp() +
              (long)Math.Ceiling(0.001 * ticks);
            long timeEnd = timeBegin + ticks;

            while (Stopwatch.GetTimestamp() < timeBegin) { }
            countBegin = RdTSC();
            long afterBegin = Stopwatch.GetTimestamp();

            while (Stopwatch.GetTimestamp() < timeEnd) { }
            countEnd = RdTSC();
            long afterEnd = Stopwatch.GetTimestamp();

            double delta = (timeEnd - timeBegin);
            frequency = 1e-6 *
              (((double)(countEnd - countBegin)) * Stopwatch.Frequency) / delta;

            double beginError = (afterBegin - timeBegin) / delta;
            double endError = (afterEnd - timeEnd) / delta;
            error = beginError + endError;
        }


        private ulong RdTSC()
        {
            //uint eax = 0, edx = 0;
            //int res = _ols.RdmsrTx(0x0c1, ref eax, ref edx, (UIntPtr)(1UL));
            //_ols.Rdtsc(ref eax, ref edx);
            //_ols.RdtscTx(ref eax, ref edx, (UIntPtr)(1UL<<5));
            //return ((ulong)edx << 32) + eax;
            return pMC.EventCounts;
        }*/

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
                    // TODO: 释放托管状态(托管对象)。
                }
                foreach(FreqPMC f in PMC_List)
                {
                    f.Dispose();
                }
                //pMC.Dispose();
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
