using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using OxyPlot;
using OxyPlot.Series;
using OxyPlot.Wpf;
using System.IO.Ports;
using System.Globalization;
using System.IO;

namespace SteamSensorClient
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly string[] StatesBuf = { "Загрузка", "Норма", "Опасность", "Тревога" };

        public PlotModel plotModel { get; private set; }
        public OxyPlot.Series.LineSeries series = new OxyPlot.Series.LineSeries();
        public PlotterClass plotter;
        public ObservableCollection<IncomeData> dataRows = new ObservableCollection<IncomeData>();
        public OxyPlot.Axes.LinearAxis y_axis;
        public double max = 100;
        public SerialPort serial = new SerialPort();
        private Thread thread;
        private MediaPlayer mediaPlayer = new MediaPlayer();
        private BitmapImage img1 = new BitmapImage(new Uri(Directory.GetCurrentDirectory() + "/system.jpg"));
        private BitmapImage img2 = new BitmapImage(new Uri(Directory.GetCurrentDirectory() + "/system2.jpg"));
        private BitmapImage img3 = new BitmapImage(new Uri(Directory.GetCurrentDirectory() + "/system3.jpg"));
        private BitmapImage img_alert;
        public int alert;
        bool alert_jpg = false;
        DispatcherTimer timer = new DispatcherTimer();

        public delegate void AddDataGridRowDelegate(IncomeData d);
        public AddDataGridRowDelegate addDataGridRow;
        public delegate void HideRebootDelegate();
        public HideRebootDelegate hideReboot;
        public MainWindow()
        {
            InitializeComponent();

            //Привязка методов к делегатам
            addDataGridRow = AddRowMethod;
            hideReboot = HideRebootButton;

            img_alert = img2;
            alert = (DateTime.Now.Second % 2) + 1;
            mediaPlayer.MediaEnded += MediaPlayer_MediaEnded;
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += timer_Tick;
            plotModel = new PlotModel();
            plotModel.Title = "График показаний аэрозольного датчика";
            plotModel.TitleFontSize = 16;
            //x axis
            plotModel.Axes.Add(new OxyPlot.Axes.LinearAxis {
                Title = "Время, t (c)",
                Position = OxyPlot.Axes.AxisPosition.Bottom,
                MajorGridlineStyle = LineStyle.Dot,
                MajorGridlineColor = OxyColor.FromRgb(20, 20, 20),
                AbsoluteMinimum = 0,
                Minimum = 0,
                Maximum = 60
            });
            //y axis
            y_axis = new OxyPlot.Axes.LinearAxis
            {
                Title = "Концентрация пара",
                Position = OxyPlot.Axes.AxisPosition.Left,
                MajorGridlineStyle = LineStyle.Dot,
                MajorGridlineColor = OxyColor.FromRgb(20, 20, 20),
                Minimum = 0
            };
            plotModel.Axes.Add(y_axis);
            series.Color = OxyColor.FromRgb(50, 50, 255);
            plotModel.Series.Add(series);

            //Привязка данных к view
            plot_view.Model = plotModel;
            dataGrid.ItemsSource = dataRows;

            plotter = new PlotterClass(this);
            thread = new Thread(plotter.ThreadStartMethod);
            thread.Start();
        }

        //Метод таймера для моргания на изображении
        void timer_Tick(object sender, EventArgs e)
        {
            if (alert_jpg)
            {
                alert_jpg = false;
                iSystem.Source = img_alert;
            }
            else
            {
                alert_jpg = true;
                iSystem.Source = img1;
            }
        }

        private void MediaPlayer_MediaEnded(object sender, EventArgs e)
        {
            mediaPlayer.Position = TimeSpan.Zero;
            mediaPlayer.Play();
        }

        private void HideRebootButton()
        {
            miRebootSensor.Visibility = Visibility.Collapsed;
            series.Points.Clear();

            mediaPlayer.Stop();
            mediaPlayer.Close();
            lAlarm.Visibility = Visibility.Hidden;
            iSystem.Source = img1;
            if (timer.IsEnabled)
                timer.Stop();
        }

        //Метод делегата добавления строки datagrid'a
        private void AddRowMethod(IncomeData data)
        {
            //Производим масштабирование по приходу новых данных
            double d = Convert.ToInt32(data.data, CultureInfo.InvariantCulture);
            if (d > max) { max = d; y_axis.Maximum = max; }

            if (dataRows.Count > 65530) dataRows.Clear();
            dataRows.Add(data);
            if(dataGrid.Items.Count > 0)
            {
                var border = VisualTreeHelper.GetChild(dataGrid, 0) as Decorator;
                if (border != null)
                {
                    var scroll = border.Child as ScrollViewer;
                    if (scroll != null) scroll.ScrollToEnd();
                }
            }
            //Переключение между режимами
            if(lState.Text != StatesBuf[data.SystemState])
            {
                lState.Text = StatesBuf[data.SystemState];
                if(data.SystemState > 1)
                {
                    lState.Foreground = new SolidColorBrush(Colors.Red);
                }
                else
                {
                    lState.Foreground = new SolidColorBrush(Colors.Black);
                }
            }
        }

        private void ClosingForm(object sender, System.ComponentModel.CancelEventArgs e)
        {
            plotter.run = false;
            thread.Join();
        }
        //Выход
        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                serial.Close();
            }
            catch (Exception) { }
            Close();
        }
        //Перезагрузить модуль
        private void MenuItem_Click_1(object sender, RoutedEventArgs e)
        {
            byte[] buf = { 0x33, 0xCC, 0x55, 0xAA };
            try
            {
                serial.Write(buf, 0, 4);
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        //Соединение
        private void MenuItem_Click_2(object sender, RoutedEventArgs e)
        {
            ConnectionWindow cw = new ConnectionWindow(this);
            plotter.run = false;
            cw.ShowDialog();
            plotter.run = true;
            dataRows.Clear();
            series.Points.Clear();
            thread = new Thread(plotter.ThreadStartMethod);
            thread.Start();
        }

        private void lStateTextChanged(object sender, TextChangedEventArgs e)
        {
            if (lAlarm != null)
            {
                if (alert == 1)
                {
                    lAlarm.Content = "Обнаружена утечка пара в коробке парораспределения";
                    img_alert = img2;
                }
                else
                {
                    lAlarm.Content = "Обнаружена утечка пара в турбоциркуляционном насосе ПБ";
                    img_alert = img3;
                }

                if (lState.Text == StatesBuf[2])
                {
                    mediaPlayer.Stop();
                    mediaPlayer.Close();
                    mediaPlayer.Open(new Uri(Directory.GetCurrentDirectory() + @"/warning.mp3"));
                    mediaPlayer.Play();
                    lAlarm.Visibility = Visibility.Visible;
                    if (!timer.IsEnabled)
                        timer.Start();
                }
                else if (lState.Text == StatesBuf[3])
                {
                    mediaPlayer.Stop();
                    mediaPlayer.Close();
                    mediaPlayer.Open(new Uri(Directory.GetCurrentDirectory() + @"/alarm.mp3"));
                    mediaPlayer.Play();
                    lAlarm.Visibility = Visibility.Visible;
                    if (!timer.IsEnabled)
                        timer.Start();
                }
                else
                {
                    mediaPlayer.Stop();
                    mediaPlayer.Close();
                    lAlarm.Visibility = Visibility.Hidden;
                    iSystem.Source = img1;
                    if (timer.IsEnabled)
                        timer.Stop();
                }
            }
        }
    }

    //Класс для отрисовки графика
    public class PlotterClass
    {
        MainWindow main_wnd;
        bool restarted = false;
        public bool run = true;
        private int state = 0; //0-старт; 1-норма; 2-аларм; 3-fire.
        public PlotterClass(MainWindow mw) {
            main_wnd = mw;
        }

        public void ThreadStartMethod()
        {
            double t = 0;
            while (run)
            {
                if (main_wnd.serial.IsOpen)
                {
                    double val = ReadSerial();
                    main_wnd.series.Points.Add(new DataPoint(t, val));
                    main_wnd.Dispatcher.BeginInvoke(main_wnd.addDataGridRow, new IncomeData { time = DateTime.Now.ToString("hh:mm:ss"), data = val.ToString(), SystemState = state });
                    t += 1;
                    //Очищаем график, если дошли до конца
                    if (t > 60)
                    {
                        t = 0;
                        main_wnd.series.Points.Clear();
                        main_wnd.max = 100;
                        main_wnd.y_axis.Maximum = 100;
                    }
                    //else if(t == 20)
                    //{

                    //}
                    //else if (t == 40)
                    //{

                    //}
                    main_wnd.plotModel.InvalidatePlot(false);
                }
                else
                {
                    main_wnd.Dispatcher.BeginInvoke(main_wnd.hideReboot);
                    Thread.Sleep(100);
                }
            }
        }

        private double ReadSerial()
        {
            string buf = "";
            try
            {
                buf = main_wnd.serial.ReadLine();
            }
            catch (Exception) { }

            string [] splited = buf.Split('|');
            //Читаем статус системы
            if (splited.Length > 10)
            {
                //Получаем показания датчика
                //Если обнаружили перезагрузку, то определяем место утечки случайным образом
                if (splited[8] == "Starting" || splited[10] == "Starting") 
                {
                    if (restarted)
                    {
                        restarted = false;
                        if (main_wnd.alert == 1) main_wnd.alert = 2;
                        else main_wnd.alert = 1;
                        state = 0;
                    }
                }
                else if (splited[10] == "Alarm") {
                    state = 2;
                    restarted = true;
                }
                else if (splited[10] == "Fire") {
                    state = 3;
                    restarted = true;
                }
                else { 
                    state = 1;
                    restarted = true;
                }
                try
                {
                    double data = double.Parse(splited[4], CultureInfo.InvariantCulture);
                    //if (data < 15) { state = 1; } //меняем состояние в зависимости от данных
                    //else if (data > 100 && data < 1000) { state = 2; }
                    //else if (data > 1000) { state = 3; }
                    return data;
                }
                catch (Exception) { return 0; }
            }
            else return 0;
        }
    }

    //Класс данных для dataGrid'a
    public class IncomeData {
        public string time { get; set; }
        public string data { get; set; }
        public int SystemState;
    }
}
