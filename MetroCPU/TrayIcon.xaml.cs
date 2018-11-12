using System;
using System.Windows;
using System.Windows.Input;

namespace MetroCPU
{
    /// <summary>
    /// TrayIcon.xaml 的交互逻辑
    /// </summary>
    public partial class TrayIcon : Window
    {
        private MainWindow mainWindow;
        public TrayIcon()
        {
            InitializeComponent();
            Visibility = Visibility.Hidden;
            taskbarIcon.LeftClickCommand = new ShowMainWindowCommand();
            taskbarIcon.LeftClickCommandParameter = this;
        }

        public void LaunchMainWindow()
        {
            Cursor = Cursors.Wait;
            if (mainWindow == null)
            {
                mainWindow = new MainWindow();
                mainWindow.Show();
            }

            if (mainWindow.IsVisible)
            {
                if (mainWindow.WindowState == WindowState.Minimized)
                {
                    mainWindow.WindowState = WindowState.Normal;
                }
                mainWindow.Activate();
            }
            else
                mainWindow.Show();
            Cursor = Cursors.Arrow;
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            mainWindow.Close();
            taskbarIcon.Dispose();
            Application.Current.Shutdown();
        }
    }

    public class ShowMainWindowCommand : ICommand
    {
        public void Execute(object parameter)
        {
            ((TrayIcon)parameter).LaunchMainWindow();
        }

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public event EventHandler CanExecuteChanged;
    }
}
