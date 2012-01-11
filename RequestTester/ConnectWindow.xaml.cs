using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Lib;
using System.Threading;

namespace RequestTester
{
    /// <summary>
    /// Interaction logic for ConnectWindow.xaml
    /// </summary>
    public partial class ConnectWindow : Window
    {
        ClientSocket clientSocket;
        ILogger logger;

        public ConnectWindow(ClientSocket _clientSocket, ILogger _logger)
        {
            clientSocket = _clientSocket;
            logger = _logger;
            InitializeComponent();
        }

        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);

            bool connected = clientSocket.IsConnected;
            EnableControlsForConnect(connected);
            //ThreadPool.QueueUserWorkItem(new WaitCallback(SocketThread));
        }

        void EnableControlsForConnect(bool isConnected)
        {
            if (isConnected)
            {
                lbConnectionStatus.Content = "Currently connected to server";
                tbIp.Text = clientSocket.Ip.ToString();
                tbPort.Text = clientSocket.Port.ToString();
            }
            else
            {
                lbConnectionStatus.Content = "Not connected";
                tbIp.Focus();
            }

            btConnect.IsEnabled = !isConnected;
            btnDisconnect.IsEnabled = isConnected;
            tbIp.IsEnabled = !isConnected;
            tbPort.IsEnabled = !isConnected;
        }

        void Connect(object sender, RoutedEventArgs e)
        {
            btConnect.IsEnabled = false;
            string ip = tbIp.Text;
            string port = tbPort.Text;
            //new Thread(new ThreadStart( new Action(
            //    () => 
            //    {
                    if (clientSocket.Connect(ip, port))
                    {
                        clientSocket.Start();
                        Dispatcher.Invoke(new Action(() => this.Close()));
                        logger.Log(string.Format("Connected to {0}:{1}", ip, port), false);
                        Dispatcher.Invoke(new Action(() => EnableControlsForConnect(clientSocket.IsConnected)));
                    }
                    else
                    {
                        logger.Log(string.Format("Connection to {0}:{1} failed", ip, port), true);
                        Dispatcher.Invoke(new Action(() => EnableControlsForConnect(false)));
                    }
            //    }
            //))).Start();
        }

        void Disconnect(object sender, RoutedEventArgs e)
        {
            EnableControlsForConnect(!clientSocket.Disconnect());
        }
    }
}
