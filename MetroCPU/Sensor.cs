using System;
using System.Collections.Concurrent;
using System.Timers;

namespace OpenLibSys
{
    struct TimeDataPair
    {
        public readonly DateTime Time;
        public readonly float Data;


        public TimeDataPair(DateTime time, float data)
        {
            Data = data;
            Time = time;
        }
    }

    class Sensor : IDisposable
    {
        public event Action NewDataAvailable;
        public TimeDataPair CurrentValue { get; private set; }
        private Timer tm;
        private readonly Func<float> DataHandler;
        private bool TaskRunning = false;
        private bool disposing = false;
        public double CurrentInterval
        {
            get;
            set;
        }

        public Sensor(Func<float> dataHandler, double interval = 800)
        {
            CurrentInterval = interval;
            tm = new Timer(CurrentInterval);
            DataHandler = dataHandler;
            tm.Elapsed += (sender, e) => GetData();
            tm.AutoReset = true;
            GetData();
            tm.Start();
        }

        public void GetData()
        {
            if (!disposing && !TaskRunning)
            {
                TaskRunning = true;
                float tmpdata;
                tmpdata = DataHandler();
                DateTime tmptime = DateTime.Now;
                CurrentValue = new TimeDataPair(tmptime, tmpdata);
                if (tm.Interval != CurrentInterval)
                    tm.Interval = CurrentInterval;
                TaskRunning = false;
                NewDataAvailable?.Invoke();
            }
        }

        public void Dispose()
        {
            if (!disposing)
            {
                disposing = true;
                while (TaskRunning)
                {
                    System.Threading.Thread.Sleep(10);
                }
                tm.Dispose();
            }
        }
    }
}
