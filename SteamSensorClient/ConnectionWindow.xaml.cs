using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.IO.Ports;

namespace SteamSensorClient
{
    /// <summary>
    /// Логика взаимодействия для ConnectionWindow.xaml
    /// </summary>
    public partial class ConnectionWindow : Window
    {
        MainWindow main_wnd;
        public ConnectionWindow(MainWindow mw)
        {
            InitializeComponent();
            main_wnd = mw;
            string[] ports = SerialPort.GetPortNames();
            SetPorts(ports);
            if (main_wnd.serial.IsOpen)
            {
                rbConnection.IsChecked = true;
                cbCom.Text = main_wnd.serial.PortName;
                bChoose.Content = "Отключить";
            }
        }

        //Установить набор портов в комбобокс
        private void SetPorts(string [] ports)
        {
            cbCom.Items.Clear();
            foreach(string port in ports)
                cbCom.Items.Add(port);
            if (cbCom.Items.Count > 0)
            {
                cbCom.SelectedIndex = 0;
            }
        }

        //Обновить
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            string[] ports = SerialPort.GetPortNames();
            SetPorts(ports);
        }

        //Закрыть
        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            Close();
        }

        //Выбрать
        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            if (bChoose.Content.ToString() == "Выбрать")
            {
                if(cbCom.SelectedItem == null)
                {
                    MessageBox.Show("Выберите порт!");
                    return;
                }
                try
                {
                    if (!main_wnd.serial.IsOpen)
                    {
                        main_wnd.serial = new SerialPort();
                        main_wnd.serial.PortName = cbCom.Text;
                        main_wnd.serial.BaudRate = 9600;
                        main_wnd.serial.DataBits = 8;
                        main_wnd.serial.Parity = System.IO.Ports.Parity.None;
                        main_wnd.serial.StopBits = System.IO.Ports.StopBits.One;
                        main_wnd.serial.ReadTimeout = 3000;
                        main_wnd.serial.WriteTimeout = 3000;
                        main_wnd.serial.Open();
                        main_wnd.miRebootSensor.Visibility = Visibility.Visible;
                        Close();
                    }
                    else
                    {
                        MessageBox.Show("Соединение уже установлено!");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("ERROR: невозможно открыть порт:" + ex.ToString());
                }
            }
            else
            {
                try
                {
                    main_wnd.serial.Close();
                    rbConnection.IsChecked = false;
                    bChoose.Content = "Выбрать";
                    main_wnd.miRebootSensor.Visibility = Visibility.Collapsed;
                }
                catch (Exception) { }
            }
        }
    }
}
