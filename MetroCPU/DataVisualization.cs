using System;
using System.Diagnostics;
using System.Security.Principal;
using System.Text;
using System.Windows;
using System.Windows.Documents;
using MahApps.Metro.Controls;
using InteractiveDataDisplay.WPF;
using OpenLibSys;

namespace MetroCPU
{
    class Sensor2LineGraph
    {
        private Sensor sensor;
        private LineGraph lineGraph;
        private float[] x,y;
        public Sensor2LineGraph(Sensor s, LineGraph l)
        {
            sensor = s;
            lineGraph = l;
            x = new float[sensor.MaxCapacity];
            y = new float[sensor.MaxCapacity];
            sensor.NewDataAvailable += new Action(Refresh);
        }

        private void Refresh()
        {
            TimeDataPair[] tmppairs = new TimeDataPair[sensor.TimeDatas.Length];
            sensor.TimeDatas.CopyTo(tmppairs,0);
            long tick_begin=tmppairs[0].Time.Ticks;
            int i = 0;
            foreach(TimeDataPair tdp in tmppairs)
            {
                x[i] = tdp.Time.Ticks-tick_begin;
                y[i] = tdp.Data;
                i++;
            }
            lineGraph.Dispatcher.Invoke(()=>lineGraph.Plot(x,y));
        }
    }

    public partial class MainWindow : MetroWindow
    {
        
    }
}
