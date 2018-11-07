using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace OpenLibSys
{
    class FreqPMC : PMC
    {
        public FreqPMC(Ols ols, ManufacturerName manufacturer, int thread, byte uMask, byte eventSelect) : base(ols, manufacturer, thread, uMask, eventSelect)
        {
        }

        public double GetCurrentFrequency()
        {
            ulong mask = 1UL << Thread;
            ulong mcnt_start, acnt_start, mcnt_stop, acnt_stop;
            uint eax_m = 0, edx_m = 0, eax_a = 0, edx_a = 0;
            ThreadAffinity.Set(mask);
            while (_ols.RdmsrTx(0xe8, ref eax_a, ref edx_a, pthread) == 0 || _ols.RdmsrTx(0xe7, ref eax_m, ref edx_m, pthread) == 0)
            { }
            mcnt_start = ((ulong)edx_m << 32) + eax_m;
            acnt_start = ((ulong)edx_a << 32) + eax_a;
            System.Threading.Thread.Sleep(30);
            while (_ols.RdmsrTx(0xe8, ref eax_a, ref edx_a, pthread) == 0 || _ols.RdmsrTx(0xe7, ref eax_m, ref edx_m, pthread) == 0)
            { }
            mcnt_stop = ((ulong)edx_m << 32) + eax_m;
            acnt_stop = ((ulong)edx_a << 32) + eax_a;
            ThreadAffinity.Set(mask);
            return 2.7 * (acnt_stop - acnt_start) / (mcnt_stop - mcnt_start);
        }
        /*
        public double GetCurrentFrequency()
        {
            ulong mask = 1UL << Thread;
            ulong start_tsc, stop_tsc;
            ulong start_pmc, stop_pmc;
            ThreadAffinity.Set(mask);
            if (GetPMCTSC(out start_pmc, out start_tsc))
            {
                System.Threading.Thread.Sleep(30);
                ThreadAffinity.Set(mask);
                if (GetPMCTSC(out stop_pmc, out stop_tsc))
                {
                    return (double)(stop_pmc - start_pmc) / (stop_tsc - start_tsc);
                }
            }
            ThreadAffinity.Set(mask);
            return 0;
        }
        */
        public double Frequency()
        {
            ulong mask = 1UL << Thread;
            ThreadAffinity.Set(mask);
            EstimatePerformanceMonitoringCounterFrequency(out double outFreq, out double outError);
            ThreadAffinity.Set(mask);
            return outFreq;
        }

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
            countBegin = EventCounts;
            long afterBegin = Stopwatch.GetTimestamp();

            while (Stopwatch.GetTimestamp() < timeEnd) { }
            countEnd = EventCounts;
            long afterEnd = Stopwatch.GetTimestamp();

            double delta = (timeEnd - timeBegin);
            frequency = 1e-6 *
              (((double)(countEnd - countBegin)) * Stopwatch.Frequency) / delta;

            double beginError = (afterBegin - timeBegin) / delta;
            double endError = (afterEnd - timeEnd) / delta;
            error = beginError + endError;
        }


    }
}
