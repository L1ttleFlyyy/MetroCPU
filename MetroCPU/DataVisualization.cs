using System;
using System.Windows;
using System.Windows.Controls;
using MahApps.Metro.Controls;
using InteractiveDataDisplay.WPF;
using OpenLibSys;
using System.Windows.Data;
using System.Timers;
using System.Collections.Generic;
using System.Collections;
using TestMySpline;
using System.Collections.Concurrent;

namespace MetroCPU
{
    class Sensor2LineGraph
    {
        //public string MaxValue { get; private set; }
        //public string MinValue { get; private set; }
        public string CurrentValue { get; private set; }
        private double Origin_Y;
        private double Height_Y;
        private Sensor sensor;
        private LineGraph lineGraph;
        private TransitionText TB1;
        private TextBlock TB2, TB3;
        private string DataFormat;
        private bool refreshed = true;
        private readonly bool IsTextBox;
        //private List<double> x;
        //private List<double> y;
        private List<float> x_actual;
        private List<float> y_actual;
        //private Timer tm;
        //private Point? Point0, Point1;
        //private Queue<Point> PlotBuffer;
        private float[] xs, ys;
        private const int Multiplier = 40;
        private float maxY = 0, minY = float.MaxValue;

        public Sensor2LineGraph(Sensor s, LineGraph l, string dataFormat, double o, double h, TransitionText currentTB, TextBlock maxTB, TextBlock minTb)
        {
            Origin_Y = o;
            Height_Y = h;
            //PlotBuffer = new Queue<Point>();
            DataFormat = dataFormat;
            IsTextBox = true;
            sensor = s;
            lineGraph = l;
            //x = new List<double>(s.MaxCapacity * Multiplier);
            //y = new List<double>(s.MaxCapacity * Multiplier);
            x_actual = new List<float>(s.MaxCapacity);
            y_actual = new List<float>(s.MaxCapacity);
            //tm = new Timer(sensor.CurrentInterval / (Multiplier + 1)) { AutoReset = true };
            //tm.Elapsed += (sender, e) => RefreshGraph();
            sensor.NewDataAvailable += new Action(RefreshData);
            //tm.Start();
            TB1 = currentTB;
            TB2 = maxTB;
            TB3 = minTb;
        }

        public Sensor2LineGraph(Sensor s, LineGraph l, string dataFormat, double o, double h)
        {
            Origin_Y = o;
            Height_Y = h;
            //PlotBuffer = new Queue<Point>();
            DataFormat = dataFormat;
            IsTextBox = false;
            sensor = s;
            lineGraph = l;
            //x = new List<double>(s.MaxCapacity * Multiplier);
            //y = new List<double>(s.MaxCapacity * Multiplier);
            x_actual = new List<float>(s.MaxCapacity);
            y_actual = new List<float>(s.MaxCapacity);
            //tm = new Timer(sensor.CurrentInterval / (Multiplier + 1)) { AutoReset = true };
            //tm.Elapsed += (sender, e) => RefreshGraph();
            sensor.NewDataAvailable += new Action(RefreshData);
            //tm.Start();
        }

        //private void RefreshGraph()
        //{
        //    if (PlotBuffer?.Count > 0)
        //    {
        //        if (x.Count < x.Capacity)
        //        {
        //            x.Add(PlotBuffer.Peek().X);
        //            y.Add(PlotBuffer.Dequeue().Y);
        //        }
        //        else
        //        {
        //            x.RemoveAt(0);
        //            x.Add(PlotBuffer.Peek().X);
        //            y.RemoveAt(0);
        //            y.Add(PlotBuffer.Dequeue().Y);
        //        }
        //        lineGraph.Dispatcher.Invoke(() => {
        //            lineGraph.Plot(x, y);
        //            pointer++;
        //            });
        //    }
        //}

        public void RefreshUI()
        {
            if (xs != null&& !refreshed)
            {
                lineGraph.Dispatcher.Invoke(() =>
                {
                    lineGraph.Plot(xs, ys);
                    lineGraph.PlotOriginY = Origin_Y;
                    lineGraph.PlotHeight = Height_Y;
                    lineGraph.PlotOriginX = x_actual[1];
                    lineGraph.PlotWidth = x_actual[x_actual.Count - 2] - x_actual[1];
                    if (IsTextBox)
                    {
                        TB1.Text = CurrentValue;
                        TB2.Text = maxY.ToString(DataFormat);
                        TB3.Text = minY.ToString(DataFormat);
                    }
                    lineGraph.Description = $"{CurrentValue} GHz";
                });
                refreshed = true;
            }
        }

        private void RefreshData()
        {
            TimeDataPair tdp = sensor.CurrentValue;
            if (x_actual.Count < x_actual.Capacity)
            {
                x_actual.Add(DateTime2MilliSecond(tdp.Time));
                y_actual.Add(tdp.Data);
            }
            else
            {
                x_actual.RemoveAt(0);
                y_actual.RemoveAt(0);
                x_actual.Add(DateTime2MilliSecond(tdp.Time));
                y_actual.Add(tdp.Data);
            }
            int N = x_actual.Count;
            if (N < 3) return;

            float stepsize = (x_actual[N - 1] - x_actual[0]) / (N - 1) / Multiplier;
            List<float> arrayList = new List<float>((N - 1) * Multiplier + 1);
            CubicSpline spline = new CubicSpline();
            for (int i = 0; i < 1 + (N - 1) * Multiplier; i++) { arrayList.Add(x_actual[0] + stepsize * i); }
            xs = arrayList.ToArray();
            ys = spline.FitAndEval(x_actual.ToArray(), y_actual.ToArray(), xs);

            //if (PlotBuffer == null) { PlotBuffer = new Queue<Point>(); }
            //for (int i = (N - 2) * Multiplier+1; i < (N - 1) * Multiplier + 1; i++)
            //{
            //    PlotBuffer.Enqueue(new Point(xs[i], ys[i]));
            //}
            CurrentValue = y_actual[N - 1].ToString(DataFormat);
            maxY = (maxY > y_actual[N - 1]) ? maxY : y_actual[N - 1];
            minY = (minY < y_actual[N - 1]) ? minY : y_actual[N - 1];
            refreshed = false;
        }

        //private void Refresh()
        //{
        //    TimeDataPair[] tmppairs = new TimeDataPair[sensor.TimeDatas.Length];
        //    sensor.TimeDatas.CopyTo(tmppairs, 0);
        //    int datacounts = tmppairs.Length;
        //    x = new long[datacounts];
        //    y = new float[datacounts];
        //    int i = 0;
        //    float max_y = 0, min_y = float.MaxValue;
        //    foreach (TimeDataPair tdp in tmppairs)
        //    {
        //        x[i] = DateTime2MilliSecond(tdp.Time);
        //        y[i] = tdp.Data;
        //        if (IsTextBox)
        //        {
        //            max_y = (y[i] > max_y) ? y[i] : max_y;
        //            min_y = (y[i] < min_y) ? y[i] : min_y;
        //        }
        //        i++;
        //    }
        //    CurrentValue = y[i - 1].ToString(DataFormat);
        //    MaxValue = max_y.ToString(DataFormat);
        //    MinValue = min_y.ToString(DataFormat);
        //    lineGraph.Dispatcher.Invoke(() =>
        //    {
        //        if (IsTextBox)
        //        {
        //            TB1.Text = CurrentValue;
        //            TB2.Text = MaxValue;
        //            TB3.Text = MinValue;
        //        }
        //        lineGraph.Description = $"{CurrentValue} GHz";
        //        lineGraph.Plot(x, y);
        //    });
        //}

        public static long DateTime2MilliSecond(DateTime dt)
        {
            return (1000 * 24 * 3600 * dt.DayOfYear)
                + (1000 * 3600 * dt.Hour)
                + (1000 * 60 * dt.Minute)
                + (1000 * dt.Second)
                + dt.Millisecond;
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
        private System.Windows.Threading.DispatcherTimer UITimer;
    }

    class TransitionText
    {
        private TextBlock TB1;
        private TextBlock TB2;
        private TransitioningContentControl TCC;
        public bool selector { get; private set; }
        public string Text
        {
            get => selector ? TB1.Text : TB2.Text;
            set
            {
                if (selector)
                {
                    TB1.Text = value;
                    TCC.Content = TB1;
                    selector = false;
                }
                else
                {
                    TB2.Text = value;
                    TCC.Content = TB2;
                    selector = true;
                }
            }
        }
        public TransitionText(TransitioningContentControl tcc)
        {
            TCC = tcc;
            selector = false;
            TB1 = new TextBlock() { TextAlignment = TextAlignment.Center };
            TB2 = new TextBlock() { TextAlignment = TextAlignment.Center };
        }
    }

}
