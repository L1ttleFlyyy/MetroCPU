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
        public float CurrentValue { get; private set; }
        private Timer tm;
        private ConcurrentQueue<TimeDataPair> q = new ConcurrentQueue<TimeDataPair>();
        private readonly Func<float> DataHandler;
        private int taskCount = 0;
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

        public Sensor(Func<float> dataHandler, double interval = 500, int datacount = 60)
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

        public async void GetData()
        {
            await Task.Run(new Action(() =>
            {
                if (!disposing)
                {
                    taskCount++;
                    CurrentValue = DataHandler();
                    MaxValue = (CurrentValue > MaxValue) ? CurrentValue : MaxValue;
                    MinValue = (CurrentValue < MinValue) ? CurrentValue : MinValue;
                    DateTime tmptime = DateTime.Now;
                    q.Enqueue(new TimeDataPair(tmptime, CurrentValue));
                    while (q.Count > MaxCapacity)
                    {
                        var tmp = new TimeDataPair();
                        q.TryDequeue(out tmp);
                    }
                    taskCount--;
                }
            }));
            if (tm.Interval != CurrentInterval)
                tm.Interval = CurrentInterval;
            NewDataAvailable?.Invoke();
        }

        public void Dispose()
        {
            if (!disposing)
            {
                disposing = true;
                while (taskCount > 0)
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
