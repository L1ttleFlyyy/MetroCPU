using System;
using System.Windows;
using System.Windows.Controls;
using MahApps.Metro.Controls;
using InteractiveDataDisplay.WPF;
using OpenLibSys;
using System.Windows.Data;
using System.Collections.Generic;
using TestMySpline;
using System.Globalization;
using System.Windows.Input;

namespace MetroCPU
{
    class Sensor2LineGraph
    {
        public string CurrentValue { get; private set; }
        private double Origin_Y;
        private double Height_Y;
        private double Origin_X;
        private double Width_X;
        private Sensor sensor;
        private LineGraph lineGraph;
        private TransitionText TB1;
        private TextBlock TB2, TB3;
        private string DataFormat;
        private bool IsBusy = false;
        private readonly bool IsTextBox;
        private Queue<float> x_actual;
        private Queue<float> y_actual;
        private const int Capacity = 10;
        private float[] xs, ys;
        private const int Multiplier = 60;
        private float maxY = 0, minY = float.MaxValue;

        public Sensor2LineGraph(Sensor s, LineGraph l, string dataFormat, double o, double h, TransitionText currentTB, TextBlock maxTB, TextBlock minTb)
        {
            Origin_Y = o;
            Height_Y = h;
            DataFormat = dataFormat;
            IsTextBox = true;
            sensor = s;
            lineGraph = l;
            x_actual = new Queue<float>(Capacity);
            y_actual = new Queue<float>(Capacity);
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
            x_actual = new Queue<float>(60);
            y_actual = new Queue<float>(60);
            sensor.NewDataAvailable += new Action(RefreshData);
        }

        public void RefreshUI()
        {
            if (xs != null && !IsBusy)
            {
                IsBusy = true;
                lineGraph.Dispatcher.Invoke(() =>
                {
                    lineGraph.Plot(xs, ys);
                    lineGraph.PlotOriginY = Origin_Y;
                    lineGraph.PlotHeight = Height_Y;
                    lineGraph.PlotOriginX = Origin_X;
                    lineGraph.PlotWidth = Width_X;
                    if (IsTextBox)
                    {
                        TB1.Text = CurrentValue;
                        TB2.Text = maxY.ToString(DataFormat);
                        TB3.Text = minY.ToString(DataFormat);
                    }
                    lineGraph.Description = $"{CurrentValue} GHz";
                });
                IsBusy = false;
            }
        }

        private void RefreshData()
        {
            TimeDataPair tdp = sensor.CurrentValue;
            if (x_actual.Count < Capacity)
            {
                x_actual.Enqueue(DateTime2MilliSecond(tdp.Time));
                y_actual.Enqueue(tdp.Data);
            }
            else
            {
                x_actual.Dequeue();
                y_actual.Dequeue();
                x_actual.Enqueue(DateTime2MilliSecond(tdp.Time));
                y_actual.Enqueue(tdp.Data);
            }
            int N = x_actual.Count;
            if (N < 3 || IsBusy) return;
            IsBusy = true;
            float start = x_actual.Peek();
            float stepsize = (DateTime2MilliSecond(tdp.Time) - start) / (N - 1) / Multiplier;
            float[] arrayList = new float[(N - 1) * Multiplier + 1];
            CubicSpline spline = new CubicSpline();
            for (int i = 0; i < 1 + (N - 1) * Multiplier; i++) { arrayList[i] = start + stepsize * i; }
            xs = arrayList;
            ys = spline.FitAndEval(x_actual.ToArray(), y_actual.ToArray(), xs);
            CurrentValue = tdp.Data.ToString(DataFormat);
            maxY = (maxY > tdp.Data) ? maxY : tdp.Data;
            minY = (minY < tdp.Data) ? minY : tdp.Data;
            Origin_X = start + stepsize * Multiplier;
            Width_X = stepsize * (N - 2) * Multiplier;
            IsBusy = false;
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
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return ((Visibility)value) == Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return ((bool)value) ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    public class ValueToTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return ((double)value).ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return double.Parse((string)value);
        }
    }

    public partial class MainWindow : MetroWindow
    {
        private System.Windows.Threading.DispatcherTimer UITimer;
        private UnderVoltor2Sliders UV2S;
    }

    class TransitionText
    {
        private TextBlock TB1;
        private TextBlock TB2;
        private TransitioningContentControl TCC;
        private double tmp0 = 0, tmp1;
        public bool selector { get; private set; }
        public string Text
        {
            get => selector ? TB1.Text : TB2.Text;
            set
            {
                if (selector)
                {
                    tmp1 = double.Parse(value);
                    if (tmp1 < tmp0)
                        TCC.Transition = TransitionType.Up;
                    else if (tmp1 > tmp0)
                        TCC.Transition = TransitionType.Down;
                    else
                        TCC.Transition = TransitionType.Default;
                    tmp0 = tmp1;
                    TB1.Text = value;
                    TCC.Content = TB1;
                    selector = false;
                }
                else
                {
                    tmp1 = double.Parse(value);
                    if (tmp1 < tmp0)
                        TCC.Transition = TransitionType.Up;
                    else if (tmp1 > tmp0)
                        TCC.Transition = TransitionType.Down;
                    else
                        TCC.Transition = TransitionType.Default;
                    tmp0 = tmp1;
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

    class UnderVoltor2Sliders
    {
        private Slider[] sliders;
        private UnderVoltor underVoltor;
        
        public int[] GetSettingFromSliders()
        {
            int[] tmp = new int[6];
            for (int i = 0; i < 6; i++)
            {
                tmp[i] = (int)sliders[i].Value;
            }
            return tmp;
        }

        public void SetToSliders(int[] tmpsettings)
        {
            int i = 0;
            foreach (Slider rs in sliders)
            {
                rs.Dispatcher.Invoke(() =>
                {
                    if (rs.IsEnabled = underVoltor.Support[i])
                    {
                        rs.Value = tmpsettings[i];
                    }
                });
                i++;
            }
        }

        public UnderVoltor2Sliders(UnderVoltor uv, Slider s0, Slider s1, Slider s2, Slider s3, Slider s4, Slider s5, Button setButton, Button saveButton, Button resetButton)
        {
            sliders = new Slider[6] { s0, s1, s2, s3, s4, s5 };
            underVoltor = uv;
            SetToSliders(underVoltor.AppliedSettings);
            bool enabled = s0.IsEnabled||s1.IsEnabled || s2.IsEnabled || s3.IsEnabled || s4.IsEnabled || s5.IsEnabled;
            if (enabled)
            {
                setButton.Click += (s, e) =>
                {
                    setButton.Cursor = Cursors.Wait;
                    uv.AppliedSettings = GetSettingFromSliders();
                    setButton.Cursor = Cursors.Arrow;
                };
                saveButton.Click += (s, e) =>
                {
                    saveButton.Cursor = Cursors.Wait;
                    uv.AppliedSettings = GetSettingFromSliders();
                    uv.SaveSettingsToFile();
                    saveButton.Cursor = Cursors.Arrow;
                };
                resetButton.Click += (s, e) =>
                {
                    setButton.Cursor = Cursors.Wait;
                    SetToSliders(uv.GetSettingsFromFile());
                    setButton.Cursor = Cursors.Arrow;
                };
            }
            else
            {
                saveButton.IsEnabled = false;
                setButton.IsEnabled = false;
                resetButton.IsEnabled = false;
            }
        }

    }

    class EPP2Sliders
    {
        private EnergyPerformancePreference epp;
        private ComboBox cbb;
        private RangeSlider rs;
        private Slider slider;
        public EPP2Sliders(EnergyPerformancePreference epp,ComboBox cbb,RangeSlider rs,Slider slider)
        {
            this.epp = epp;
            this.cbb = cbb;
            this.rs = rs;
            this.slider = slider;
        }
    }
}
