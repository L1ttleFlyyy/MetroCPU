﻿using System;
using System.Diagnostics;
using System.Security.Principal;
using System.Text;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Controls;
using MahApps.Metro.Controls;
using InteractiveDataDisplay.WPF;
using OpenLibSys;
using System.Windows.Data;

namespace MetroCPU
{
    class Sensor2LineGraph
    {
        public string MaxValue { get; private set; }
        public string MinValue { get; private set; }
        public string CurrentValue { get; private set; }
        private Sensor sensor;
        private LineGraph lineGraph;
        private TextBlock TB1,TB2,TB3;
        private readonly bool IsTextBox;
        private float[] x, y;
        
        public Sensor2LineGraph(Sensor s, LineGraph l, TextBlock currentTB,TextBlock maxTB,TextBlock minTb)
        {
            IsTextBox = true;
            sensor = s;
            lineGraph = l;
            sensor.NewDataAvailable += new Action(Refresh);
            TB1 = currentTB;
            TB2 = maxTB;
            TB3 = minTb;
        }

        public Sensor2LineGraph(Sensor s, LineGraph l)
        {
            IsTextBox = false;
            sensor = s;
            lineGraph = l;
            sensor.NewDataAvailable += new Action(Refresh);
        }

        private void Refresh()
        {
            TimeDataPair[] tmppairs = new TimeDataPair[sensor.TimeDatas.Length];
            sensor.TimeDatas.CopyTo(tmppairs, 0);
            int datacounts = tmppairs.Length;
            x = new float[datacounts];
            y = new float[datacounts];
            long tick_begin = tmppairs[0].Time.Ticks;
            int i = 0;
            float max_x = 0, max_y = 0, min_y = float.MaxValue;
            foreach (TimeDataPair tdp in tmppairs)
            {
                x[i] = tdp.Time.Ticks - tick_begin;
                y[i] = tdp.Data;
                max_x = (x[i] > max_x) ? x[i] : max_x;
                max_y = (y[i] > max_y) ? y[i] : max_y;
                min_y = (y[i] < min_y) ? y[i] : min_y;
                i++;
            }
            CurrentValue = y[i - 1].ToString("G3");
            MaxValue = max_y.ToString("G3");
            MinValue = min_y.ToString("G3");
            float delta_y = max_y - min_y;
            for (i = 0; i < datacounts; i++)
            {
                x[i] = x[i] / max_x;
                y[i] = (y[i] - min_y) / delta_y;
            }
            lineGraph.Dispatcher.Invoke(() => {
                if (IsTextBox)
                {
                    TB1.Text = CurrentValue;
                    TB2.Text = MaxValue;
                    TB3.Text = MinValue;
                }
                lineGraph.Description = $"{CurrentValue} GHz";
                lineGraph.Plot(x, y);
            });
        }
    }

    public class VisibilityToCheckedConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return ((Visibility)value) == Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return ((bool)value) ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    public partial class MainWindow : MetroWindow
    {

    }
}
