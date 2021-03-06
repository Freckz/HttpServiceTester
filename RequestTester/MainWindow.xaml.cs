﻿using System;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Diagnostics;
using System.ComponentModel;
using System.Windows.Controls.Primitives;
using System.Net;
using System.Threading;
using OxyPlot;
using System.IO;
using System.ServiceModel;
using System.Configuration;
using System.Net.Configuration;
using System.Net.Sockets;
using Lib;

namespace RequestTester
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, ILogger
    {
        enum RequestDestination
        {
            Home,
            Search
        }
        private BackgroundWorker bw = new BackgroundWorker();
        private int requestTotal;
        private int requestDone;
        private int requestFailed;
        private int requestTimeout;
        private long totalTimeHome;
        private long totalTimeSearch;
        private long requestsSentToHome;
        private long requestsSentToSearch;
        private long requestsReceivedFromHome;
        private long requestsReceivedFromSearch;

        double _Frequency = 0d;
        double Frequency
        {
            get
            {
                return _Frequency;
            }
        }

        void SetFrequency()
        {
            Interlocked.Exchange(ref _Frequency, slRequestSelect.Value);
        }

        private string _requestUrl = string.Empty;
        private string _homeRequestUrl = string.Empty;

        public string RequestUrl
        {
            get
            {
                return _requestUrl;
            }
        }

        void SetRequestUrl()
        {
            _homeRequestUrl = tbRequestUrl.Text;
            string async = rbAsync.IsChecked.Value ? "testasync" : "testsync";
            string requestTime = slFrontalWorkingTime.Value.ToString();
            string requestSize = slRequestSize.Value.ToString();
            string _url = string.Format("{0}/{1}?time={2}&size={3}", tbRequestUrl.Text, async, int.Parse(requestTime).ToString(), int.Parse(requestSize));
            lock (_requestUrl)
                _requestUrl = _url;
        }

        ClientSocket clientSocket;

        #region Display

        public MainWindow()
        {
            InitializeComponent();

            SetRequestUrl();

            bw.WorkerReportsProgress = true;
            bw.WorkerSupportsCancellation = true;
            bw.DoWork += new DoWorkEventHandler(bw_DoWork);
            bw.ProgressChanged += new ProgressChangedEventHandler(bw_ProgressChanged);
            bw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bw_RunWorkerCompleted);

            clientSocket = new ClientSocket(this);
            clientSocket.SocketResponseReceived += (sock, args) => Dispatcher.Invoke(new Action(() => ReceiveConfigMessage(args.Message)));
            clientSocket.SocketDisconnected += (sock, args) => Dispatcher.Invoke(new Action(() 
                => 
                { 
                    gbRemoteConf.Header = "Remote config"; 
                    gbRemoteConf.IsEnabled = false; 
                }));
            clientSocket.SocketConnected += (sock, args) => Dispatcher.Invoke(new Action(() 
                => 
                { 
                    gbRemoteConf.Header = string.Format("Remote config {0}:{1}", clientSocket.Ip, clientSocket.Port); 
                    gbRemoteConf.IsEnabled = true;
                }));

            
            savedConfigValues.Add(ConfigType.MaxWorkerThreads, -1);
            savedConfigValues.Add(ConfigType.MaxIOThreads, -1);
            savedConfigValues.Add(ConfigType.MaxConnections, -1);
            savedConfigValues.Add(ConfigType.AvailableIOThreads, -1);
            savedConfigValues.Add(ConfigType.AvailableWorkerThreads, -1);
            savedConfigValues.Add(ConfigType.RequestQueueLimit, -1);
            savedConfigValues.Add(ConfigType.ParallelDistantRequestValue, -1);
            savedConfigValues.Add(ConfigType.MinWorkerThreads, -1);
            savedConfigValues.Add(ConfigType.MinIOThreads, -1);
            savedConfigValues.Add(ConfigType.Timeout, -1);
            savedConfigValues.Add(ConfigType.ReadWriteTimeout, -1);

        }

        void ReceiveConfigMessage(string message)
        {
            string[] configs = message.Split('|');
            foreach (string conf in configs)
            {
                string[] arrConf = conf.Split(':');
                switch (arrConf[0])
                {
                    case "MaxWorkerThreads":
                        tbMaxWTServer.Text = arrConf[1];
                        break;
                    case "MaxIOThreads":
                        tbMaxIOTServer.Text = arrConf[1];
                        break;
                    case "AvailableWorkerThreads":
                        tbAvailableWT.Text = arrConf[1];
                        break;
                    case "AvailableIOThreads":
                        tbAvailableIOT.Text = arrConf[1];
                        break;
                    case "maxconnections":
                        tbMaxConnectionsServer.Text = arrConf[1];
                        break;
                    case "runtimeQueueCount":
                        tbRuntimeRequestsInQueue.Text = arrConf[1];
                        break;
                    case "RequestQueueLimit":
                        tbRequestQueueLimit.Text = arrConf[1];
                        break;
                    case "ParallelDistantRequestValue":
                        tbParallelDistantRequest.Text = arrConf[1];
                        break;
                    case "MinWorkerThreads":
                        tbMinWT.Text = arrConf[1];
                        break;
                    case "MinIOThreads":
                        tbMinIOT.Text = arrConf[1];
                        break;
                    case "Timeout":
                        tbRequestTimeout.Text = arrConf[1];
                        break;
                    case "ReadWriteTimeout":
                        tbRequestRWTimeout.Text = arrConf[1];
                        break;
                }
            }
        }

        #endregion

        #region Menu

        private void LaunchCreate(object sender, RoutedEventArgs e)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = "CreateASPNETThreadCounters.exe";       
            startInfo.WorkingDirectory = Environment.CurrentDirectory+"\\Commons\\";
            Process.Start(startInfo);
        }

        private void LaunchPerfmon(object sender, RoutedEventArgs e)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = "Perfmon";
            Process.Start(startInfo);
        }

        private void Close(object sender, RoutedEventArgs e)
        {
            clientSocket.Dispose();
            Application curApp = Application.Current;
            curApp.Shutdown();
        }
        
        private void OpenConnectWindow(object sender, RoutedEventArgs e)
        {
            this.IsEnabled = false;
            ConnectWindow cWindow = new ConnectWindow(clientSocket, this);
            cWindow.Show();
            cWindow.Closed += new EventHandler((a, ev) => Dispatcher.Invoke(new Action(() =>
                {
                    gbRemoteConf.IsEnabled = clientSocket.IsConnected;
                    this.IsEnabled = true;
                })));

        }

        #endregion

        #region buttons

        Dictionary<ConfigType, int> savedConfigValues = new Dictionary<ConfigType, int>();
        private void ChangeConfig(object sender, RoutedEventArgs e)
        {
            Dictionary<ConfigType, int> newConfigs = new Dictionary<ConfigType, int>();
            int maxConn;
            if (int.TryParse(tbMaxConnections.Text, out maxConn) && savedConfigValues[ConfigType.MaxConnections] != maxConn)
                newConfigs.Add(ConfigType.MaxConnections, maxConn);

            int maxIo;
            if (int.TryParse(tbMaxIOThreads.Text, out maxIo) && savedConfigValues[ConfigType.MaxIOThreads] != maxIo)
                newConfigs.Add(ConfigType.MaxIOThreads, maxIo);

            int maxW;
            if (int.TryParse(tbMaxWorkerThreads.Text, out maxW) && savedConfigValues[ConfigType.MaxWorkerThreads] != maxW)
                newConfigs.Add(ConfigType.MaxWorkerThreads, maxW);

            int requestQueueLimit;
            if (int.TryParse(tbRequestQueueLimitValue.Text, out requestQueueLimit) && savedConfigValues[ConfigType.RequestQueueLimit] != requestQueueLimit)
                newConfigs.Add(ConfigType.RequestQueueLimit, requestQueueLimit);

            int parallelDistantRequestValue;
            if (int.TryParse(tbParallelDistantRequestValue.Text, out parallelDistantRequestValue) && savedConfigValues[ConfigType.ParallelDistantRequestValue] != parallelDistantRequestValue)
                newConfigs.Add(ConfigType.ParallelDistantRequestValue, parallelDistantRequestValue);

            int timeout;
            if (int.TryParse(tbRequestTimeoutValue.Text, out timeout) && savedConfigValues[ConfigType.Timeout] != timeout)
                newConfigs.Add(ConfigType.Timeout, timeout);

            int rWTimeout;
            if (int.TryParse(tbRequestRWTimeoutValue.Text, out rWTimeout) && savedConfigValues[ConfigType.ReadWriteTimeout] != rWTimeout)
                newConfigs.Add(ConfigType.ReadWriteTimeout, rWTimeout);

            string message = string.Empty;
            foreach(KeyValuePair<ConfigType, int> kvp in newConfigs)
            {
                message = string.Concat(message, "SET:", kvp.Key.ToString(), ":", kvp.Value.ToString(), "|");
            }
            message.TrimEnd('|');

            clientSocket.EnqueueMessage(message);
        }

        Action<ClientSocket, SocketResponseEventArgs> LaunchWorkAfterConnect = new Action<ClientSocket, SocketResponseEventArgs>((a, x) => { });
        private void StartRequest(object sender, RoutedEventArgs e)
        {
            if (!clientSocket.IsConnected)
            {
                LaunchWorkAfterConnect = new Action<ClientSocket, SocketResponseEventArgs>(
                    (s, a) =>
                    {
                        StartRequest(sender, e);
                        clientSocket.SocketConnected -= LaunchWorkAfterConnect;
                    });
                clientSocket.SocketConnected += LaunchWorkAfterConnect;
                OpenConnectWindow(sender, e);
                Log("Please open the socket connection before ", false);
                return;
            }

            Start.Source=new BitmapImage(new Uri(@"Resources\Play_Black.png", UriKind.Relative));
            End.Source = new BitmapImage(new Uri(@"Resources\Stop_Red.png", UriKind.Relative));

            if (bw.IsBusy != true)
            {
                bw.RunWorkerAsync(slRequestSelect.Value + "#" + tbRequestUrl.Text);
            }
        }

        private void EndRequest(object sender, RoutedEventArgs e)
        {
            if (bw.WorkerSupportsCancellation == true)
            {
                bw.CancelAsync();
            }
        }

        #endregion

        #region Async

        private void bw_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;
            string[] args = e.Argument.ToString().Split(new Char[] { '#' });
            
            if ((worker.CancellationPending == true))
            {
                e.Cancel = true;
            }
            else
            {
                Stopwatch sw = Stopwatch.StartNew();
                int countRequest=0;
                while ((sw.ElapsedMilliseconds < 1000) && countRequest < Frequency)
                {
                    ThreadPool.QueueUserWorkItem(new WaitCallback(GenerateRequest), RequestDestination.Search);
                    ThreadPool.QueueUserWorkItem(new WaitCallback(GenerateRequest), RequestDestination.Home);
                    countRequest+=2;
	            }
                if (countRequest >= Frequency)
                {
                    int waiting = (int)(1000 - sw.ElapsedMilliseconds);
                    if(waiting > 0)
                        Thread.Sleep(waiting);
                }
                worker.ReportProgress(requestDone);
                requestDone = 0;
                bw_DoWork(sender, e);
            }
            
        }

        private void bw_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {

            if (!(e.Error == null))
            {
                Log(("Error: " + e.Error.Message), true);
            }

            Start.Source = new BitmapImage(new Uri(@"Resources\Play_Green.png", UriKind.Relative));
            End.Source = new BitmapImage(new Uri(@"Resources\Stop_Black.png", UriKind.Relative));
            slRequestSelect.IsEnabled = true;
            tbRequestUrl.IsEnabled = true;

        }

        private void bw_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {         
            #region avg requests sent
            var ls = new LineSeries("tmp");

            if (lineoxysum.ItemsSource == null)
                ls.Points = new List<IDataPoint>();
            else
            {
                foreach (DataPoint point in lineoxysum.ItemsSource)
                {
                    ls.Points.Add(new DataPoint(point.X, point.Y));
                }
            }

            int pointcount = ls.Points.Count;
            double nextpoint = pointcount > 0 ? ls.Points.Max(p => p.X) + 1 : 1;

            if (pointcount == 100)
                ls.Points.RemoveAt(0);

            ls.Points.Add(new DataPoint(nextpoint, e.ProgressPercentage));
            lineoxysum.ItemsSource = ls.Points;

            labelMax.Content = ls.Points.Count > 0 ? ls.Points.Max(p => Math.Abs(p.Y)).ToString("F2") : "0";
            lbCurrReqValue.Content = ls.Points.Last().Y.ToString("F2");
            #endregion

            #region Avg home response time

            double avg = 0;
            if (requestTotal > 0)
                avg = (((double)totalTimeHome / (((double)requestTotal) / 2d)) / 1000d);

            ls = new LineSeries("tmp");

            if (lineoxyavgH.ItemsSource == null)
                ls.Points = new List<IDataPoint>();
            else
            {
                foreach (DataPoint point in lineoxyavgH.ItemsSource)
                {
                    ls.Points.Add(new DataPoint(point.X, point.Y));
                }
            }

            pointcount = ls.Points.Count;
            nextpoint = pointcount > 0 ? ls.Points.Max(p => p.X) + 1 : 1;

            if (pointcount == 100)
                ls.Points.RemoveAt(0);

            ls.Points.Add(new DataPoint(nextpoint, avg));
            lineoxyavgH.ItemsSource = ls.Points;
            lbAvgHomeTimeValue.Content = avg.ToString("F2");
            #endregion

            #region Avg search response time
            lbCurrReqValue.Content = ls.Points.Last().Y.ToString("F2");

             avg = 0;
            if (requestTotal > 0)
                avg = (((double)totalTimeSearch / (((double)requestTotal)/2d)) / 1000d);

            ls = new LineSeries("tmp");

            if (lineoxyavgS.ItemsSource == null)
                ls.Points = new List<IDataPoint>();
            else
            {
                foreach (DataPoint point in lineoxyavgS.ItemsSource)
                {
                    ls.Points.Add(new DataPoint(point.X, point.Y));
                }
            }

            pointcount = ls.Points.Count;
            nextpoint = pointcount > 0 ? ls.Points.Max(p => p.X) + 1 : 1;

            if (pointcount == 100)
                ls.Points.RemoveAt(0);

            ls.Points.Add(new DataPoint(nextpoint, avg));
            lineoxyavgS.ItemsSource = ls.Points;
            #endregion

            #region Requests sent to home
            /*
             *  Line series sent to home
             * */

            ls = new LineSeries("tmp");
            ls.Title = "Requests sent";
            ls.Color = OxyColors.DarkMagenta;

            if (lineoxyhome.ItemsSource == null)
                ls.Points = new List<IDataPoint>();
            else
            {
                foreach (DataPoint point in lineoxyhome.ItemsSource)
                {
                    ls.Points.Add(new DataPoint(point.X, point.Y));
                }
            }

            pointcount = ls.Points.Count;
            nextpoint = pointcount > 0 ? ls.Points.Max(p => p.X) + 1 : 1;

            if (pointcount == 100)
                ls.Points.RemoveAt(0);

            ls.Points.Add(new DataPoint(nextpoint, requestsSentToSearch));
            lineoxyhome.ItemsSource = ls.Points;
            lbNbRequestsSentValue.Content = requestsSentToSearch;
            #endregion

            #region Requests received from home
            /*
             *  Line series received from home
             * */

            ls = new LineSeries("tmp");
            ls.Title = "Requests received from home";
            ls.Color = OxyColors.AliceBlue;
                        
            if (lineoxyreceivehome.ItemsSource == null)
                ls.Points = new List<IDataPoint>();
            else
            {
                foreach (DataPoint point in lineoxyreceivehome.ItemsSource)
                {
                    ls.Points.Add(new DataPoint(point.X, point.Y));
                }
            }

            pointcount = ls.Points.Count;
            nextpoint = pointcount > 0 ? ls.Points.Max(p => p.X) + 1 : 1;

            if (pointcount == 100)
                ls.Points.RemoveAt(0);

            ls.Points.Add(new DataPoint(nextpoint, requestsReceivedFromHome));
            lineoxyreceivehome.ItemsSource = ls.Points;
            lbNbResponseFromHomeValue.Content = requestsReceivedFromHome;
            #endregion

            #region Requests received from home
            /*
             *  Line series received from search
             * */

            ls = new LineSeries("tmp");

            if (lineoxyreceivesearch.ItemsSource == null)
                ls.Points = new List<IDataPoint>();
            else
            {
                foreach (DataPoint point in lineoxyreceivesearch.ItemsSource)
                {
                    ls.Points.Add(new DataPoint(point.X, point.Y));
                }
            }

            
            pointcount = ls.Points.Count;
            nextpoint = pointcount > 0 ? ls.Points.Max(p => p.X) + 1 : 1;

            if (pointcount == 100)
                ls.Points.RemoveAt(0);

            ls.Points.Add(new DataPoint(nextpoint, requestsReceivedFromSearch));
            lineoxyreceivesearch.ItemsSource = ls.Points;
            lbNbResponseFromSearchValue.Content = requestsReceivedFromSearch;
            #endregion

            MasterPlot.RefreshPlot(true);
            PlotRequestsSent.RefreshPlot(true);

            labelFailed.Content = requestFailed;
            lbTiemoutRequestValue.Content = requestTimeout;
        }
        #endregion

        #region Request

        private void GenerateRequest(object state)
        {
            RequestDestination dest = (RequestDestination)state;
            string __requestUrl;
            switch (dest)
            {
                case RequestDestination.Home:
                    __requestUrl = _homeRequestUrl;
                    Interlocked.Increment(ref requestsSentToHome);
                    break;
                case RequestDestination.Search:
                default:
                    __requestUrl = RequestUrl;
                    Interlocked.Increment(ref requestsSentToSearch);
                    break;
            }
            try
            {
                WebRequest request = WebRequest.Create(__requestUrl);
                Interlocked.Increment(ref requestDone);
                //request.Timeout = 7000;
                DateTime now = DateTime.Now;
                request.BeginGetResponse(new AsyncCallback((result) =>
                {
                    WebResponse response = ((WebRequest)result.AsyncState).EndGetResponse(result);
                    Interlocked.Increment(ref requestTotal);
                    switch (dest)
                    {
                        case RequestDestination.Home:
                            Interlocked.Add(ref totalTimeHome, (long)(DateTime.Now - now).TotalMilliseconds);
                            Interlocked.Increment(ref requestsReceivedFromHome);
                            break;
                        case RequestDestination.Search:
                            Interlocked.Add(ref totalTimeSearch, (long)(DateTime.Now - now).TotalMilliseconds);
                            Interlocked.Increment(ref requestsReceivedFromSearch);
                            break;

                    }
                    response.Close();
                }), request);
            }
            catch(WebException we)
            {
                if (we.Status == WebExceptionStatus.Timeout)
                    Interlocked.Increment(ref requestTimeout);
                Interlocked.Increment(ref requestFailed);
                Log( string.Format("Request failed with status {0}", we.Status.ToString()), true);
            }

        }

        #endregion               

        public void Log(string Message, bool isError)
        {
            Dispatcher.Invoke(new Action(() =>
            {
                if (!spLogs.IsEnabled)
                    spLogs.IsEnabled = true;

                Brush b = isError ? new SolidColorBrush(Color.FromRgb(255, 30, 30)) : new SolidColorBrush(Color.FromRgb(120, 120, 120));
                spLogs.Children.Insert(0, new TextBlock()
                {
                    Text = string.Format( "{0:T} - {1}", DateTime.Now, Message),
                    Foreground = b
                });
            }));
        }

        private void slFrontalWorkingTimeValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            SetRequestUrl();
        }

        private void SyncGroupClick(object sender, RoutedEventArgs e)
        {
            SetRequestUrl();
        }

        private void tbRequestUrlFocusLost(object sender, RoutedEventArgs e)
        {
            SetRequestUrl();
        }

        private void CheckEnter(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                SetRequestUrl();
        }

        private void ChangeFrequency(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            SetFrequency();
        }
    }
}
