using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Threading.Tasks;
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

        public float MaxValue { get; private set; }
        public float MinValue { get; private set; }
        public TimeDataPair CurrentValue { get; private set; }
        private Timer tm;
        private ConcurrentQueue<TimeDataPair> q = new ConcurrentQueue<TimeDataPair>();
        private readonly Func<float> DataHandler;
        private bool TaskRunning = false;
        private bool disposing = false;
        public TimeDataPair[] TimeDatas
        {
            get => q.ToArray();
        }
        public double CurrentInterval
        {
            get;
            set;
        }

        public Sensor(Func<float> dataHandler, double interval = 500, int datacount = 20)
        {
            MaxValue = 0;
            MinValue = float.MaxValue;
            MaxCapacity = datacount;
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
                MaxValue = (tmpdata > MaxValue) ? tmpdata : MaxValue;
                MinValue = (tmpdata < MinValue) ? tmpdata : MinValue;
                DateTime tmptime = DateTime.Now;
                CurrentValue = new TimeDataPair(tmptime, tmpdata);
                q.Enqueue(CurrentValue);
                while (q.Count > MaxCapacity)
                {
                    var tmp = new TimeDataPair();
                    q.TryDequeue(out tmp);
                }
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

        private int maxCapacity;

        public int MaxCapacity
        {
            set
            {
                maxCapacity = value;
                while (q.Count > value)
                { q.TryDequeue(out TimeDataPair tdpair); }
            }
            get => maxCapacity;
        }

        public int AvailableDataCount { get => q.Count; }
    }
}
