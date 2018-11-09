using System;
using System.Windows;
using System.Windows.Controls;
using MahApps.Metro.Controls;
using InteractiveDataDisplay.WPF;
using OpenLibSys;
using System.Windows.Data;
using System.Collections.Generic;
using TestMySpline;
using System.Windows.Threading;

namespace MetroCPU
{
    class Sensor2LineGraph
    {
        public string CurrentValue { get; private set; }
        private double Origin_Y;
        private double Height_Y;
        private Sensor sensor;
        private LineGraph lineGraph;
        private TransitionText TB1;
        private TextBlock TB2, TB3;
        private string DataFormat;
        private readonly bool IsTextBox;
        private List<float> x_actual;
        private List<float> y_actual;
        private List<float> Point_x;
        private List<float> Point_y;
        private float[] xs;
        private float[] ys;
        private const int Multiplier = 3;
        private int WindowWidth, data_capacity;
        private float maxY = 0, minY = float.MaxValue;
        private bool data_busy = false;
        private int pointer = 0;

        public Sensor2LineGraph(Sensor s, LineGraph l, string dataFormat, double o, double h, TransitionText currentTB, TextBlock maxTB, TextBlock minTb)
        {
            Origin_Y = o;
            Height_Y = h;
            DataFormat = dataFormat;
            IsTextBox = true;
            sensor = s;
            lineGraph = l;
            data_capacity = s.MaxCapacity;
            WindowWidth = (data_capacity - 1) * Multiplier + 1;
            Point_x = new List<float>(data_capacity * Multiplier + 1);
            Point_y = new List<float>(data_capacity * Multiplier + 1);
            x_actual = new List<float>(data_capacity + 1);
            y_actual = new List<float>(data_capacity + 1);
            sensor.NewDataAvailable += new Action(RefreshData);
            TB1 = currentTB;
            TB2 = maxTB;
            TB3 = minTb;
        }

        public Sensor2LineGraph(Sensor s, LineGraph l, string dataFormat, double o, double h)
        {
            Origin_Y = o;
            Height_Y = h;
            DataFormat = dataFormat;
            IsTextBox = false;
            sensor = s;
            lineGraph = l;
            data_capacity = s.MaxCapacity;
            WindowWidth = (data_capacity - 1) * Multiplier + 1;
            Point_x = new List<float>(data_capacity * Multiplier + 1);
            Point_y = new List<float>(data_capacity * Multiplier + 1);
            x_actual = new List<float>(s.MaxCapacity + 1);
            y_actual = new List<float>(s.MaxCapacity + 1);
            sensor.NewDataAvailable += new Action(RefreshData);
        }

        public void RefreshUI()
        {
            if (!data_busy)
            {
                //lineGraph.Plot(xs, ys);
                //lineGraph.PlotOriginY = Origin_Y;
                //lineGraph.PlotHeight = Height_Y;
                //lineGraph.PlotOriginX = x_actual[0];
                //lineGraph.PlotWidth = x_actual[x_actual.Count - 1] - x_actual[0];
                if (IsTextBox)
                {
                    TB1.Text = CurrentValue;
                    TB2.Text = maxY.ToString(DataFormat);
                    TB3.Text = minY.ToString(DataFormat);
                }
                lineGraph.Description = $"{CurrentValue} GHz";
                int N = Point_x.Count;
                if (pointer < N)
                {
                    if (N <= WindowWidth)
                    {
                        lineGraph.Plot(Point_x.GetRange(0, pointer + 1), Point_y.GetRange(0, pointer + 1));
                        lineGraph.PlotWidth = Point_x[pointer] - Point_x[0];
                    }
                    else
                    {
                        lineGraph.Plot(Point_x.GetRange(pointer - WindowWidth + 1, WindowWidth), Point_y.GetRange(pointer - WindowWidth + 1, WindowWidth));

                        lineGraph.PlotWidth = Point_x[pointer] - Point_x[pointer - WindowWidth + 1];
                    }
                    lineGraph.PlotOriginY = Origin_Y;
                    lineGraph.PlotHeight = Height_Y;
                    lineGraph.PlotOriginX = Point_x[0];
                    pointer++;
                }
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
            float[] arrayList = new float[1 + (N - 1) * Multiplier];
            CubicSpline spline = new CubicSpline();
            for (int i = 0; i < 1 + (N - 1) * Multiplier; i++) { arrayList[i] = x_actual[0] + stepsize * i; }
            data_busy = true;
            xs = arrayList;
            ys = spline.FitAndEval(x_actual.ToArray(), y_actual.ToArray(), xs);

            if (Point_x.Count < WindowWidth)
            {
                Point_x.Clear();
                Point_x.AddRange(xs);
                Point_y.Clear();
                Point_y.AddRange(ys);
            }
            else
            {
                if (Point_x.Count > WindowWidth)pointer -= Multiplier;
                var tmp = Point_x.GetRange(Multiplier, Multiplier).ToArray();
                Point_x.Clear();
                Point_x.AddRange(tmp);
                Point_x.AddRange(xs);
                tmp = Point_y.GetRange(Multiplier, Multiplier).ToArray();
                Point_y.Clear();
                Point_y.AddRange(tmp);
                Point_y.AddRange(ys);
            }
            CurrentValue = y_actual[N - 1].ToString(DataFormat);
            maxY = (maxY > y_actual[N - 1]) ? maxY : y_actual[N - 1];
            minY = (minY < y_actual[N - 1]) ? minY : y_actual[N - 1];
            data_busy = false;
        }

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
        private DispatcherTimer UITimer;
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
                if (Text != value)
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
