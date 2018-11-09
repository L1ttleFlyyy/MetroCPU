using System;
using System.Windows;
using System.Windows.Controls;
using MahApps.Metro.Controls;
using InteractiveDataDisplay.WPF;
using OpenLibSys;
using System.Windows.Data;
using System.Collections.Generic;
using TestMySpline;

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
            x_actual = new List<float>(s.MaxCapacity);
            y_actual = new List<float>(s.MaxCapacity);
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
            x_actual = new List<float>(s.MaxCapacity);
            y_actual = new List<float>(s.MaxCapacity);
            sensor.NewDataAvailable += new Action(RefreshData);
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
            List<float> arrayList = new List<float>(1 + (N - 1) * Multiplier);
            CubicSpline spline = new CubicSpline();
            for (int i = 0; i < 1 + (N - 1) * Multiplier; i++) { arrayList.Add(x_actual[0] + stepsize * i); }
            float[] xs = arrayList.ToArray();
            float[] ys = spline.FitAndEval(x_actual.ToArray(), y_actual.ToArray(), xs);

            //if (PlotBuffer == null) { PlotBuffer = new Queue<Point>(); }
            //for (int i = (N - 2) * Multiplier+1; i < (N - 1) * Multiplier + 1; i++)
            //{
            //    PlotBuffer.Enqueue(new Point(xs[i], ys[i]));
            //}
            CurrentValue = y_actual[N - 1].ToString(DataFormat);
            maxY = (maxY > y_actual[N - 1]) ? maxY : y_actual[N - 1];
            minY = (minY < y_actual[N - 1]) ? minY : y_actual[N - 1];
            lineGraph.Dispatcher.Invoke(() =>
            {
                lineGraph.Plot(xs, ys);
                lineGraph.PlotOriginY = Origin_Y;
                lineGraph.PlotHeight = Height_Y;
                lineGraph.PlotOriginX = x_actual[0];
                lineGraph.PlotWidth = x_actual[N - 1] - x_actual[0];
                if (IsTextBox)
                {
                    TB1.Text = CurrentValue;
                    TB2.Text = maxY.ToString(DataFormat);
                    TB3.Text = minY.ToString(DataFormat);
                }
                lineGraph.Description = $"{CurrentValue} GHz";
            });

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
